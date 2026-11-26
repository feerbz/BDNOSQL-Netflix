using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Netflix.App.Live;

namespace Netflix.App.Messaging;

/// <summary>Cada 2 s lee el estado de las colas y lo entrega a la UI (design D9).</summary>
public sealed class QueueStatsPoller(IHttpClientFactory http, LiveEvents live, ILogger<QueueStatsPoller> logger)
    : BackgroundService
{
    private static readonly string[] Queues =
    [
        MessagingSetup.TitlesQueue, MessagingSetup.AggregatesQueue,
        MessagingSetup.TitlesQueue + "_error", MessagingSetup.AggregatesQueue + "_error",
    ];

    private string _lastLine = "";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("No se pudo leer la Management API: {Message}", ex.Message);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollAsync(CancellationToken token)
    {
        var found = await http.CreateClient(RabbitManagement.ClientName)
            // avg_rate sobre 10 s: la tasa instantánea llega a saltos (0, 60, 0…).
            .GetFromJsonAsync<List<QueueInfo>>("api/queues?msg_rates_age=10&msg_rates_incr=2", token) ?? [];

        var stats = Queues.Select(name =>
        {
            var queue = found.FirstOrDefault(q => q.Name == name); // las colas _error nacen con el primer fallo
            return new QueueStats(name, queue?.Ready ?? 0, queue?.Unacked ?? 0, queue?.Consumers ?? 0,
                queue?.MessageStats?.Publish?.Average ?? 0, queue?.MessageStats?.Deliver?.Average ?? 0);
        }).ToList();
        live.QueuesUpdated(stats);

        var line = string.Join(" · ", stats.Select(q => $"{q.Name}: {q.Ready} en espera"));
        if (line != _lastLine) logger.LogInformation("Colas: {Queues}", _lastLine = line);
    }

    private sealed record QueueInfo(
        string Name,
        [property: JsonPropertyName("messages_ready")] long Ready,
        [property: JsonPropertyName("messages_unacknowledged")] long Unacked,
        int Consumers,
        [property: JsonPropertyName("message_stats")] Stats? MessageStats);

    private sealed record Stats(
        [property: JsonPropertyName("publish_details")] Detail? Publish,
        [property: JsonPropertyName("deliver_get_details")] Detail? Deliver);

    private sealed record Detail([property: JsonPropertyName("avg_rate")] double Average);
}
