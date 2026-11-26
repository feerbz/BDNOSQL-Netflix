using System.Net.Http.Json;
using System.Text.Json;

namespace Netflix.App.Messaging;

/// <summary>
/// Botones de demo (design D9). Publican en el exchange como lo haría el ETL, con JSON plano:
/// nunca escriben en Cassandra.
/// </summary>
public sealed class DemoPublisher(IHttpClientFactory http)
{
    private const string Corrupt =
        """{"title":"Título corrupto (demo)","type":"MOVIE","release_year":2026,"genres":[],"credits":[]}""";

    public static string Serialize(TitleMessage title) => JsonSerializer.Serialize(title, JsonSerializerOptions.Web);

    /// <summary>Un título sin id: agota los reintentos y acaba en catalog-titles_error.</summary>
    public Task InjectCorruptAsync() => PublishAsync(Corrupt);

    public Task ResendAsync(TitleMessage title) => PublishAsync(Serialize(title));

    private async Task PublishAsync(string payload)
    {
        var response = await http.CreateClient(RabbitManagement.ClientName).PostAsJsonAsync(
            $"api/exchanges/%2F/{MessagingSetup.Exchange}/publish", new
            {
                properties = new { content_type = "application/json", delivery_mode = 2 },
                routing_key = "title",
                payload,
                payload_encoding = "string",
            });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!result.GetProperty("routed").GetBoolean())
            throw new InvalidOperationException("RabbitMQ no enrutó el mensaje a ninguna cola");
    }
}
