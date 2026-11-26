using System.Text.Json;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Netflix.App.Data;
using Netflix.App.Live;
using Netflix.App.Messaging;

namespace Netflix.App.Tests;

[Collection("cassandra")]
public sealed class ConsumerTests(CassandraFixture cassandra)
{
    // Tal como lo publica el ETL (snake_case, sin sobre).
    private const string RockyJson = """
        { "id": "tm82169", "title": "Rocky", "type": "MOVIE", "description": "…", "release_year": 1976,
          "age_certification": "PG", "runtime": 119, "seasons": null, "genres": ["drama","sport"],
          "production_countries": ["US"], "imdb_score": 8.1, "imdb_votes": 588100,
          "tmdb_popularity": 106.361, "tmdb_score": 7.782, "position": 2, "total_titles": 6137,
          "credits": [ { "person_id": 3180, "name": "Sylvester Stallone", "character": "Rocky", "role": "ACTOR" } ] }
        """;

    private static TitleMessage Rocky() =>
        JsonSerializer.Deserialize<TitleMessage>(RockyJson, JsonSerializerOptions.Web)!;

    private ServiceProvider Provider() => new ServiceCollection()
        .AddSingleton(cassandra.Session)
        .AddSingleton<TitleWriter>()
        .AddSingleton<AggregateWriter>()
        .AddSingleton<LiveEvents>()
        .AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<TitleConsumer>();
            x.AddConsumer<AggregateConsumer>();
            x.AddConsumer<FaultConsumer>();
        })
        .BuildServiceProvider(true);

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(100);
        Assert.True(condition(), "No ocurrió a tiempo");
    }

    [Fact]
    public void El_contrato_snake_case_se_deserializa()
    {
        var rocky = Rocky();
        Assert.Equal((1976, "PG", 8.1, 588100, 6137), (rocky.ReleaseYear, rocky.AgeCertification, rocky.ImdbScore, rocky.ImdbVotes, rocky.TotalTitles));
        Assert.Equal(3180, rocky.Credits.Single().PersonId);
    }

    [Fact]
    public void El_duplicado_de_demo_respeta_el_contrato()
    {
        var json = DemoPublisher.Serialize(Rocky());

        Assert.Contains("\"release_year\":1976", json);
        Assert.Contains("\"person_id\":3180", json);
        Assert.Contains("\"id\":\"tm82169\"", json);
    }

    [Fact]
    public async Task Titulo_nuevo_y_repetido()
    {
        await using var provider = Provider();
        var harness = provider.GetRequiredService<ITestHarness>();
        var live = provider.GetRequiredService<LiveEvents>();
        // Las clases de prueba comparten keyspace: este caso necesita partir de cero.
        foreach (var table in new[] { "titles_by_id", "titles_by_genre", "credits_by_person" })
            await cassandra.Session.ExecuteAsync($"TRUNCATE {table}");
        await harness.Start();

        await harness.Bus.Publish(Rocky());
        await WaitFor(() => live.Feed.Count == 1);
        await harness.Bus.Publish(Rocky());
        await WaitFor(() => live.Feed.Count == 2);

        Assert.Equal([TitleStatus.YaExistia, TitleStatus.Nuevo], live.Feed.Select(e => e.Status));
        var db = cassandra.Session;
        Assert.Single(await db.QueryAsync("SELECT id FROM titles_by_id WHERE id = ?", "tm82169"));
        Assert.Single(await db.QueryAsync("SELECT id FROM titles_by_genre WHERE genre = ?", "drama"));
        var films = await db.QueryAsync("SELECT title_id FROM credits_by_person WHERE person_id = ?", 3180);
        Assert.Equal("tm82169", films.Single().GetValue<string>("title_id"));
    }

    [Fact]
    public async Task Titulo_sin_id_falla_y_avisa()
    {
        await using var provider = Provider();
        var harness = provider.GetRequiredService<ITestHarness>();
        var live = provider.GetRequiredService<LiveEvents>();
        await harness.Start();

        await harness.Bus.Publish(Rocky() with { Id = null, Title = "Corrupto" });
        await WaitFor(() => live.Feed.Count == 1);

        var failed = live.Feed.Single();
        Assert.Equal(TitleStatus.Error, failed.Status);
        Assert.Contains("id ausente", failed.Reason);
    }

    [Fact]
    public async Task Agregado_kpis_se_guarda_y_notifica()
    {
        await using var provider = Provider();
        var harness = provider.GetRequiredService<ITestHarness>();
        var loaded = false;
        provider.GetRequiredService<LiveEvents>().CatalogLoaded += () => loaded = true;
        await harness.Start();

        var message = JsonSerializer.Deserialize<AggregateMessage>(
            """{ "kind": "kpis", "rows": [ { "name": "titles", "value": 6137 } ] }""", JsonSerializerOptions.Web)!;
        await harness.Bus.Publish(message);
        await WaitFor(() => loaded);

        var rows = await cassandra.Session.QueryAsync("SELECT value FROM kpis WHERE name = ?", "titles");
        Assert.Equal(6137d, rows.Single().GetValue<double>("value"));
    }
}
