using System.Collections.Concurrent;
using Cassandra;
using Netflix.App.Messaging;
using ISession = Cassandra.ISession;

namespace Netflix.App.Data;

/// <summary>Conexión única a Cassandra, con sentencias preparadas y el UDT <c>credit</c>.</summary>
public sealed class CassandraSession(IConfiguration config, ILogger<CassandraSession> logger) : IDisposable
{
    private const int Attempts = 30;
    private readonly ConcurrentDictionary<string, Task<PreparedStatement>> _prepared = new();
    private Cluster? _cluster;
    private ISession? _session;

    private ISession Session => _session ?? throw new InvalidOperationException("Cassandra sin conectar");

    public async Task ConnectAsync()
    {
        var host = config["Cassandra:Host"] ?? "localhost";
        var keyspace = config["Cassandra:Keyspace"] ?? "netflix_mart";
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                _cluster = Cluster.Builder().AddContactPoint(host).Build();
                _session = await _cluster.ConnectAsync(keyspace);
                break;
            }
            catch (Exception ex) when (attempt < Attempts)
            {
                logger.LogWarning("Cassandra no responde ({Message}); reintento {Attempt}", ex.Message, attempt);
                _cluster?.Dispose();
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        await _session.UserDefinedTypes.DefineAsync(UdtMap.For<Credit>("credit")
            .Map(c => c.PersonId, "person_id")
            .Map(c => c.Name, "name")
            .Map(c => c.Character, "character")
            .Map(c => c.Role, "role"));
        logger.LogInformation("Conectado a Cassandra en {Host}, keyspace {Keyspace}", host, keyspace);
    }

    public async Task<RowSet> ExecuteAsync(string cql, params object?[] values)
    {
        var statement = await _prepared.GetOrAdd(cql, c => Session.PrepareAsync(c));
        return await Session.ExecuteAsync(statement.Bind(values!));
    }

    /// <summary>Todas las filas de una consulta, página a página y sin bloquear hilos.</summary>
    public async Task<List<Row>> QueryAsync(string cql, params object?[] values)
    {
        var result = await ExecuteAsync(cql, values);
        var rows = new List<Row>();
        while (true)
        {
            rows.AddRange(result.Take(result.GetAvailableWithoutFetching()));
            if (result.IsFullyFetched) return rows;
            await result.FetchMoreResultsAsync();
        }
    }

    public void Dispose() => _cluster?.Dispose();
}
