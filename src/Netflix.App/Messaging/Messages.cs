using System.Text.Json;
using System.Text.Json.Serialization;

namespace Netflix.App.Messaging;

// Contrato con el ETL: JSON plano en snake_case, sin el sobre de MassTransit (design D3).

public sealed class Credit
{
    [JsonPropertyName("person_id")] public int PersonId { get; set; }
    public string Name { get; set; } = "";
    public string? Character { get; set; }
    public string Role { get; set; } = "";
}

public sealed record TitleMessage
{
    public string? Id { get; init; }
    public string Title { get; init; } = "";
    public string? Type { get; init; }
    public string Description { get; init; } = "";
    [JsonPropertyName("release_year")] public int? ReleaseYear { get; init; }
    [JsonPropertyName("age_certification")] public string? AgeCertification { get; init; }
    public int? Runtime { get; init; }
    public int? Seasons { get; init; }
    public List<string> Genres { get; init; } = [];
    [JsonPropertyName("production_countries")] public List<string> ProductionCountries { get; init; } = [];
    [JsonPropertyName("imdb_score")] public double? ImdbScore { get; init; }
    [JsonPropertyName("imdb_votes")] public int? ImdbVotes { get; init; }
    [JsonPropertyName("tmdb_popularity")] public double? TmdbPopularity { get; init; }
    [JsonPropertyName("tmdb_score")] public double? TmdbScore { get; init; }
    public List<Credit> Credits { get; init; } = [];
    public int Position { get; init; }
    [JsonPropertyName("total_titles")] public int TotalTitles { get; init; }
}

public sealed record AggregateMessage
{
    public string Kind { get; init; } = "";
    public List<JsonElement> Rows { get; init; } = [];
}
