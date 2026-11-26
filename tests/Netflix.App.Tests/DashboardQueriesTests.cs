using System.Text.Json;
using Netflix.App.Data;
using Netflix.App.Live;
using Netflix.App.Messaging;

namespace Netflix.App.Tests;

[Collection("cassandra")]
public sealed class DashboardQueriesTests(CassandraFixture cassandra)
{
    private static TitleMessage Title(string id, string name, string type, double? score) => new()
    {
        Id = id, Title = name, Type = type, ReleaseYear = 1976, Genres = ["drama"], ProductionCountries = ["US"],
        ImdbScore = score, Description = "", AgeCertification = "PG",
        Credits = [new Credit { PersonId = 3180, Name = "Sylvester Stallone", Role = "ACTOR", Character = "Rocky" }],
    };

    private async Task<(DashboardQueries Queries, LiveEvents Live)> Seeded()
    {
        var titles = new TitleWriter(cassandra.Session);
        await titles.SaveAsync(Title("tm82169", "Rocky", "MOVIE", 8.1));
        await titles.SaveAsync(Title("tm999", "Sin puntuación", "MOVIE", null));

        var aggregates = new AggregateWriter(cassandra.Session);
        foreach (var json in new[]
        {
            """{"kind":"by_year","rows":[{"type":"MOVIE","release_year":1976,"total":2}]}""",
            """{"kind":"by_genre","rows":[{"genre":"drama","total":2,"movies":2,"shows":0,"avg_imdb":8.1}]}""",
            """{"kind":"by_country","rows":[{"country":"US","total":2,"movies":2,"shows":0}]}""",
            """{"kind":"ranking","rows":[{"list_name":"imdb_movie","position":1,"id":"tm82169","title":"Rocky","type":"MOVIE","release_year":1976,"imdb_score":8.1,"imdb_votes":588100,"tmdb_popularity":106.3}]}""",
            """{"kind":"top_people","rows":[{"role":"ACTOR","position":1,"person_id":3180,"name":"Sylvester Stallone","total":2}]}""",
            """{"kind":"kpis","rows":[{"name":"titles","value":2},{"name":"movies","value":2},{"name":"shows","value":0},{"name":"avg_imdb","value":8.1},{"name":"people","value":1}]}""",
        })
            await aggregates.SaveAsync(JsonSerializer.Deserialize<AggregateMessage>(json, JsonSerializerOptions.Web)!);

        var live = new LiveEvents();
        return (new DashboardQueries(cassandra.Session, live), live);
    }

    [Fact]
    public async Task Cada_grafica_devuelve_datos()
    {
        var (queries, _) = await Seeded();

        Assert.NotNull(await queries.KpisAsync());
        Assert.NotEmpty(await queries.ByYearAsync("MOVIE"));
        Assert.NotEmpty(await queries.ByGenreAsync());
        Assert.NotEmpty(await queries.ByCountryAsync());
        Assert.NotEmpty(await queries.RankingAsync("imdb_movie"));
        Assert.NotEmpty(await queries.TopPeopleAsync("ACTOR"));
        Assert.NotEmpty(await queries.FilmographyAsync(3180));
    }

    [Fact]
    public async Task Genero_ordenado_con_sin_puntuacion_al_final()
    {
        var (queries, _) = await Seeded();
        var drama = await queries.TitlesByGenreAsync("drama", 100);

        Assert.Equal("tm82169", drama[0].Id);
        Assert.Null(drama[^1].ImdbScore);
    }

    [Fact]
    public async Task Buscar_rocky_y_ver_su_detalle()
    {
        var (queries, _) = await Seeded();
        var found = DashboardQueries.Filter(await queries.CatalogAsync(), "ROCKY", "MOVIE", "drama");
        var detail = await queries.TitleAsync(found.Single().Id);

        Assert.Equal(8.1, detail!.ImdbScore);
        Assert.Equal("Sylvester Stallone", detail.Credits.Single().Name);
        Assert.Empty(DashboardQueries.Filter(await queries.CatalogAsync(), "rocky", "SHOW", null));
    }

    [Fact]
    public async Task La_cache_se_refresca_al_llegar_kpis()
    {
        var (queries, live) = await Seeded();
        var first = await queries.CatalogAsync();
        Assert.Same(first, await queries.CatalogAsync());

        live.NotifyCatalogLoaded();

        Assert.NotSame(first, await queries.CatalogAsync());
    }
}
