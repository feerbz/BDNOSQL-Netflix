using System.Text.RegularExpressions;
using Netflix.App.Messaging;

namespace Netflix.App.Data;

/// <summary>Escritura de un título en sus 3 tablas. Todo son upserts por clave natural (design D4).</summary>
public sealed partial class TitleWriter(CassandraSession cassandra)
{
    private const int Parallelism = 32;

    [GeneratedRegex(@"^t[ms]\d+$")]
    private static partial Regex IdPattern();

    public static void Validate(TitleMessage title)
    {
        if (title.Id is null || !IdPattern().IsMatch(title.Id))
            throw new ArgumentException($"Mensaje inválido: id ausente o con formato incorrecto ({title.Id ?? "sin id"})");
        if (title.Type is not ("MOVIE" or "SHOW"))
            throw new ArgumentException($"Mensaje inválido: tipo '{title.Type}'");
        if (title.ReleaseYear is not (>= 1900 and <= 2030))
            throw new ArgumentException($"Mensaje inválido: año '{title.ReleaseYear}'");
    }

    public async Task<bool> ExistsAsync(string id) =>
        (await cassandra.ExecuteAsync("SELECT id FROM titles_by_id WHERE id = ?", id)).Any();

    public async Task SaveAsync(TitleMessage t)
    {
        var writes = new List<Func<Task>>
        {
            () => cassandra.ExecuteAsync(
                "INSERT INTO titles_by_id (id, title, type, description, release_year, age_certification, runtime, " +
                "seasons, genres, production_countries, imdb_score, imdb_votes, tmdb_popularity, tmdb_score, credits) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                t.Id, t.Title, t.Type, t.Description, t.ReleaseYear, t.AgeCertification, t.Runtime,
                t.Seasons, t.Genres, t.ProductionCountries, t.ImdbScore, t.ImdbVotes, t.TmdbPopularity,
                t.TmdbScore, t.Credits),
        };
        // Sin puntuación se guarda -1 para que quede al final del género.
        writes.AddRange(t.Genres.Select(genre => (Func<Task>)(() => cassandra.ExecuteAsync(
            "INSERT INTO titles_by_genre (genre, imdb_score, id, title, type, release_year) VALUES (?, ?, ?, ?, ?, ?)",
            genre, t.ImdbScore ?? -1d, t.Id, t.Title, t.Type, t.ReleaseYear))));
        writes.AddRange(t.Credits.Select(c => (Func<Task>)(() => cassandra.ExecuteAsync(
            "INSERT INTO credits_by_person (person_id, release_year, title_id, role, title, type, character, imdb_score) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            c.PersonId, t.ReleaseYear, t.Id, c.Role, t.Title, t.Type, c.Character, t.ImdbScore))));

        // Algunos títulos tienen cientos de créditos: se escriben en tandas.
        foreach (var chunk in writes.Chunk(Parallelism))
            await Task.WhenAll(chunk.Select(write => write()));
    }
}
