using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Netflix.App.Data;
using CassandraCluster = Cassandra.Cluster;

namespace Netflix.App.Tests;

/// <summary>
/// Pruebas de integración contra el Cassandra de Docker, en un keyspace aparte
/// (netflix_mart_test) que se recrea desde cassandra/schema.cql. No toca los datos reales.
/// </summary>
public sealed class CassandraFixture : IAsyncLifetime
{
    private const string Keyspace = "netflix_mart_test";

    public CassandraSession Session { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "cassandra", "schema.cql")))
            directory = directory.Parent ?? throw new FileNotFoundException("cassandra/schema.cql");

        var lines = File.ReadAllLines(Path.Combine(directory.FullName, "cassandra", "schema.cql"))
            .Where(line => !line.TrimStart().StartsWith("--"));
        var statements = string.Join('\n', lines).Replace("netflix_mart", Keyspace)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        using (var cluster = CassandraCluster.Builder().AddContactPoint("localhost").Build())
        using (var admin = cluster.Connect())
        {
            admin.Execute($"DROP KEYSPACE IF EXISTS {Keyspace}");
            foreach (var statement in statements) admin.Execute(statement);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Cassandra:Keyspace"] = Keyspace })
            .Build();
        Session = new CassandraSession(config, NullLogger<CassandraSession>.Instance);
        await Session.ConnectAsync();
    }

    public Task DisposeAsync()
    {
        Session.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("cassandra")]
public sealed class CassandraCollection : ICollectionFixture<CassandraFixture>;
