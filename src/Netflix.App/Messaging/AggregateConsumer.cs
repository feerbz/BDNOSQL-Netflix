using MassTransit;
using Netflix.App.Data;
using Netflix.App.Live;

namespace Netflix.App.Messaging;

public sealed class AggregateConsumer(AggregateWriter writer, LiveEvents live) : IConsumer<AggregateMessage>
{
    public async Task Consume(ConsumeContext<AggregateMessage> context)
    {
        await writer.SaveAsync(context.Message);
        if (context.Message.Kind == "kpis") live.NotifyCatalogLoaded();
    }
}
