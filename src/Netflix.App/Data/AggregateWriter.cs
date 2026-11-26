using System.Text.Json;
using Netflix.App.Messaging;

namespace Netflix.App.Data;

/// <summary>Upsert de las filas de un agregado en su tabla (design D7): un INSERT por tipo.</summary>
public sealed class AggregateWriter(CassandraSession cassandra)
{
    public async Task SaveAsync(AggregateMessage message)
    {
        Func<JsonElement, Task> insert = message.Kind switch
        {
            "kpis" => row => cassandra.ExecuteAsync(
                "INSERT INTO kpis (name, value) VALUES (?, ?)",
                Text(row, "name"), Number(row, "value")),

            "by_year" => row => cassandra.ExecuteAsync(
                "INSERT INTO agg_by_year (type, release_year, total) VALUES (?, ?, ?)",
                Text(row, "type"), Int(row, "release_year"), Int(row, "total")),

            "by_genre" => row => cassandra.ExecuteAsync(
                "INSERT INTO agg_by_genre (scope, genre, total, movies, shows, avg_imdb) VALUES ('all', ?, ?, ?, ?, ?)",
                Text(row, "genre"), Int(row, "total"), Int(row, "movies"), Int(row, "shows"), Number(row, "avg_imdb")),

            "by_country" => row => cassandra.ExecuteAsync(
                "INSERT INTO agg_by_country (scope, country, total, movies, shows) VALUES ('all', ?, ?, ?, ?)",
                Text(row, "country"), Int(row, "total"), Int(row, "movies"), Int(row, "shows")),

            "ranking" => row => cassandra.ExecuteAsync(
                "INSERT INTO ranking (list_name, position, id, title, type, release_year, imdb_score, imdb_votes, " +
                "tmdb_popularity) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                Text(row, "list_name"), Int(row, "position"), Text(row, "id"), Text(row, "title"), Text(row, "type"),
                Int(row, "release_year"), Number(row, "imdb_score"), Int(row, "imdb_votes"), Number(row, "tmdb_popularity")),

            "top_people" => row => cassandra.ExecuteAsync(
                "INSERT INTO top_people (role, position, person_id, name, total) VALUES (?, ?, ?, ?, ?)",
                Text(row, "role"), Int(row, "position"), Int(row, "person_id"), Text(row, "name"), Int(row, "total")),

            _ => throw new ArgumentException($"Agregado desconocido: '{message.Kind}'"),
        };

        foreach (var chunk in message.Rows.Chunk(32))
            await Task.WhenAll(chunk.Select(insert));
    }

    private static string? Text(JsonElement row, string name) => row.GetProperty(name).GetString();

    private static double? Number(JsonElement row, string name) =>
        row.GetProperty(name) is { ValueKind: not JsonValueKind.Null } value ? value.GetDouble() : null;

    private static int? Int(JsonElement row, string name) => (int?)Number(row, name);
}
