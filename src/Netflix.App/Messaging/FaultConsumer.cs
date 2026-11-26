using MassTransit;
using Netflix.App.Live;

namespace Netflix.App.Messaging;

/// <summary>Un título agotó sus reintentos: se avisa a la UI (el mensaje ya está en la cola de errores).</summary>
public sealed class FaultConsumer(LiveEvents live) : IConsumer<Fault<TitleMessage>>
{
    public Task Consume(ConsumeContext<Fault<TitleMessage>> context)
    {
        var title = context.Message.Message;
        live.TitleFailed(new TitleEvent(DateTime.Now, title.Id, title.Title, TitleStatus.Error, 0,
            context.Message.Exceptions.FirstOrDefault()?.Message));
        return Task.CompletedTask;
    }
}
