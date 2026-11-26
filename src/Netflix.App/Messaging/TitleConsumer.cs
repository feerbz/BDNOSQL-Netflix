using System.Diagnostics;
using MassTransit;
using Netflix.App.Data;
using Netflix.App.Live;

namespace Netflix.App.Messaging;

public sealed class TitleConsumer(TitleWriter writer, LiveEvents live) : IConsumer<TitleMessage>
{
    public async Task Consume(ConsumeContext<TitleMessage> context)
    {
        var watch = Stopwatch.StartNew();
        var title = context.Message;
        TitleWriter.Validate(title); // si falla: reintentos y después catalog-titles_error

        var existed = await writer.ExistsAsync(title.Id!);
        await writer.SaveAsync(title);

        live.TitleSaved(title, new TitleEvent(DateTime.Now, title.Id, title.Title,
            existed ? TitleStatus.YaExistia : TitleStatus.Nuevo, watch.Elapsed.TotalMilliseconds));
    }
}
