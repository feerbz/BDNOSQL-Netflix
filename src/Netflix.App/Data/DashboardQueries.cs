using Cassandra;
using Netflix.App.Live;
using Netflix.App.Messaging;

namespace Netflix.App.Data;

public sealed record Kpis(int Titles, int Movies, int Shows, double AvgImdb, int People);
public sealed record YearTotal(string Type, int Year, int Total);
public sealed record GenreTotal(string Genre, int Total, int Movies, int Shows, double? AvgImdb);
public sealed record CountryTotal(string Country, int Total, int Movies, int Shows);
public sealed record GenreTitle(string Id, string Title, string Type, int Year, double? ImdbScore);
public sealed record RankingEntry(int Position, string Id, string Title, string Type, int Year, double? ImdbScore, int? ImdbVotes, double? Popularity);
public sealed record PersonEntry(int Position, int PersonId, string Name, int Total);
public sealed record Film(string TitleId, string Title, string Type, int Year, string Role, string? Character, double? ImdbScore);
public sealed record CatalogItem(string Id, string Title, string Type, int Year, IReadOnlyList<string> Genres, double? ImdbScore);

/// <summary>Lecturas del dashboard: un método por gráfica y cada uno lee una partición. Nunca escribe.</summary>
public sealed class DashboardQueries
{
    private readonly CassandraSession _cassandra;
    private volatile IReadOnlyList<CatalogItem>? _catalog;

    public DashboardQueries(CassandraSession cassandra, LiveEvents live)
    {
        _cassandra = cassandra;
        live.CatalogLoaded += () => _catalog = null;
    }

    /// <summary>null si todavía no se ha cargado el catálogo.</summary>
    public async Task<Kpis?> KpisAsync()
    {
        var values = (await _cassandra.QueryAsync("SELECT name, value FROM kpis"))
            .ToDictionary(r => r.GetValue<string>("name"), r => r.GetValue<double>("value"));
        return values.ContainsKey("titles")
            ? new Kpis((int)values["titles"], (int)values["movies"], (int)values["shows"], values["avg_imdb"], (int)values["people"])
            : null;
    }

    public async Task<IReadOnlyList<YearTotal>> ByYearAsync(string type) =>
        (await _cassandra.QueryAsync("SELECT type, release_year, total FROM agg_by_year WHERE type = ?", type))
        .Select(r => new YearTotal(r.GetValue<string>("type"), r.GetValue<int>("release_year"), r.GetValue<int>("total")))
        .ToList();

    public async Task<IReadOnlyList<GenreTotal>> ByGenreAsync() =>
        (await _cassandra.QueryAsync("SELECT genre, total, movies, shows, avg_imdb FROM agg_by_genre WHERE scope = 'all'"))
        .Select(r => new GenreTotal(r.GetValue<string>("genre"), r.GetValue<int>("total"), r.GetValue<int>("movies"),
            r.GetValue<int>("shows"), r.GetValue<double?>("avg_imdb")))
        .OrderByDescending(g => g.Total).ToList();

    public async Task<IReadOnlyList<CountryTotal>> ByCountryAsync() =>
        (await _cassandra.QueryAsync("SELECT country, total, movies, shows FROM agg_by_country WHERE scope = 'all'"))
        .Select(r => new CountryTotal(r.GetValue<string>("country"), r.GetValue<int>("total"), r.GetValue<int>("movies"), r.GetValue<int>("shows")))
        .OrderByDescending(c => c.Total).ToList();

    public async Task<IReadOnlyList<GenreTitle>> TitlesByGenreAsync(string genre, int limit = 20) =>
        (await _cassandra.QueryAsync("SELECT id, title, type, release_year, imdb_score FROM titles_by_genre WHERE genre = ? LIMIT ?", genre, limit))
        .Select(r => new GenreTitle(r.GetValue<string>("id"), r.GetValue<string>("title"), r.GetValue<string>("type"),
            r.GetValue<int>("release_year"), r.GetValue<double>("imdb_score") is var s && s >= 0 ? s : null))
        .ToList();

    /// <param name="list">{imdb|popularity}_{all|movie|show}</param>
    public async Task<IReadOnlyList<RankingEntry>> RankingAsync(string list) =>
        (await _cassandra.QueryAsync("SELECT * FROM ranking WHERE list_name = ?", list))
        .Select(r => new RankingEntry(r.GetValue<int>("position"), r.GetValue<string>("id"), r.GetValue<string>("title"),
            r.GetValue<string>("type"), r.GetValue<int>("release_year"), r.GetValue<double?>("imdb_score"),
            r.GetValue<int?>("imdb_votes"), r.GetValue<double?>("tmdb_popularity")))
        .ToList();

    public async Task<IReadOnlyList<PersonEntry>> TopPeopleAsync(string role) =>
        (await _cassandra.QueryAsync("SELECT position, person_id, name, total FROM top_people WHERE role = ?", role))
        .Select(r => new PersonEntry(r.GetValue<int>("position"), r.GetValue<int>("person_id"), r.GetValue<string>("name"), r.GetValue<int>("total")))
        .ToList();

    public async Task<IReadOnlyList<Film>> FilmographyAsync(int personId) =>
        (await _cassandra.QueryAsync("SELECT * FROM credits_by_person WHERE person_id = ?", personId))
        .Select(r => new Film(r.GetValue<string>("title_id"), r.GetValue<string>("title"), r.GetValue<string>("type"),
            r.GetValue<int>("release_year"), r.GetValue<string>("role"), r.GetValue<string?>("character"), r.GetValue<double?>("imdb_score")))
        .ToList();

    /// <summary>Detalle y reparto en una sola lectura.</summary>
    public async Task<TitleMessage?> TitleAsync(string id)
    {
        var row = (await _cassandra.QueryAsync("SELECT * FROM titles_by_id WHERE id = ?", id)).SingleOrDefault();
        return row is null ? null : new TitleMessage
        {
            Id = row.GetValue<string>("id"),
            Title = row.GetValue<string>("title"),
            Type = row.GetValue<string>("type"),
            Description = row.GetValue<string>("description"),
            ReleaseYear = row.GetValue<int?>("release_year"),
            AgeCertification = row.GetValue<string?>("age_certification"),
            Runtime = row.GetValue<int?>("runtime"),
            Seasons = row.GetValue<int?>("seasons"),
            Genres = List(row, "genres"),
            ProductionCountries = List(row, "production_countries"),
            ImdbScore = row.GetValue<double?>("imdb_score"),
            ImdbVotes = row.GetValue<int?>("imdb_votes"),
            TmdbPopularity = row.GetValue<double?>("tmdb_popularity"),
            TmdbScore = row.GetValue<double?>("tmdb_score"),
            Credits = row.GetValue<IEnumerable<Credit>>("credits")?.ToList() ?? [],
        };
    }

    /// <summary>
    /// Todo el catálogo, en memoria (~6 mil filas). Solo se guarda en caché cuando ya están todos
    /// los títulos que indica el KPI: "kpis" puede procesarse antes que los títulos pendientes.
    /// </summary>
    public async Task<IReadOnlyList<CatalogItem>> CatalogAsync()
    {
        if (_catalog is { } cached) return cached;

        var items = (await _cassandra.QueryAsync("SELECT id, title, type, release_year, genres, imdb_score FROM titles_by_id"))
            .Select(r => new CatalogItem(r.GetValue<string>("id"), r.GetValue<string>("title"), r.GetValue<string>("type"),
                r.GetValue<int>("release_year"), List(r, "genres"), r.GetValue<double?>("imdb_score")))
            .OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var expected = (await KpisAsync())?.Titles;
        if (items.Count >= expected) _catalog = items;
        return items;
    }

    public static IReadOnlyList<CatalogItem> Filter(IEnumerable<CatalogItem> items, string? text, string? type, string? genre) =>
        items.Where(i => (string.IsNullOrWhiteSpace(text) || i.Title.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase))
                         && (string.IsNullOrEmpty(type) || i.Type == type)
                         && (string.IsNullOrEmpty(genre) || i.Genres.Contains(genre)))
            .ToList();

    private static List<string> List(Row row, string column) =>
        row.GetValue<IEnumerable<string>>(column)?.ToList() ?? [];
}
