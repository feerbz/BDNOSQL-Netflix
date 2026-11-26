using Netflix.App.Messaging;

namespace Netflix.App.Live;

public enum TitleStatus { Nuevo, YaExistia, Error }

public sealed record TitleEvent(DateTime At, string? Id, string Title, TitleStatus Status, double Milliseconds, string? Reason = null);

public sealed record QueueStats(string Name, long Ready, long Unacked, int Consumers, double PublishRate, double DeliverRate);

public sealed record RateSample(DateTime At, double Published, double Delivered);

/// <summary>
/// Estado en vivo de la app. Los consumidores y el poller lo actualizan; los componentes Blazor
/// se suscriben a <see cref="Changed"/> y se redibujan (design: sin hub SignalR propio).
/// </summary>
public sealed class LiveEvents
{
    public const int FeedSize = 100;
    public const int HistorySeconds = 60;
    /// <summary>Retroceso de posición a partir del cual se asume otra corrida (mayor que el desorden del consumo).</summary>
    private const int NewRunThreshold = 100;

    private readonly object _lock = new();
    private readonly LinkedList<TitleEvent> _feed = new();
    private readonly List<RateSample> _rates = [];
    private IReadOnlyList<QueueStats> _queues = [];

    public event Action? Changed;
    /// <summary>Llegó el agregado KPIs, el último de cada corrida.</summary>
    public event Action? CatalogLoaded;

    public TitleMessage? LastTitle { get; private set; }
    public int Position { get; private set; }
    public int TotalTitles { get; private set; }

    public IReadOnlyList<TitleEvent> Feed { get { lock (_lock) return [.. _feed]; } }
    public IReadOnlyList<RateSample> Rates { get { lock (_lock) return [.. _rates]; } }
    public IReadOnlyList<QueueStats> Queues { get { lock (_lock) return _queues; } }

    public void TitleSaved(TitleMessage title, TitleEvent saved)
    {
        lock (_lock)
        {
            LastTitle = title;
            // Los consumidores terminan en desorden: se queda la posición mayor. Un salto grande
            // hacia atrás significa que empezó otra corrida del ETL.
            if (title.Position > Position || Position - title.Position > NewRunThreshold) Position = title.Position;
            TotalTitles = title.TotalTitles;
            Add(saved);
        }
        Changed?.Invoke();
    }

    public void TitleFailed(TitleEvent failed)
    {
        lock (_lock) Add(failed);
        Changed?.Invoke();
    }

    public void QueuesUpdated(IReadOnlyList<QueueStats> queues)
    {
        lock (_lock)
        {
            _queues = queues;
            var main = queues.Where(q => !q.Name.EndsWith("_error")).ToList();
            var now = DateTime.Now;
            _rates.Add(new RateSample(now, main.Sum(q => q.PublishRate), main.Sum(q => q.DeliverRate)));
            _rates.RemoveAll(r => r.At < now.AddSeconds(-HistorySeconds));
        }
        Changed?.Invoke();
    }

    public void NotifyCatalogLoaded()
    {
        CatalogLoaded?.Invoke();
        Changed?.Invoke();
    }

    private void Add(TitleEvent e)
    {
        _feed.AddFirst(e);
        if (_feed.Count > FeedSize) _feed.RemoveLast();
    }
}
