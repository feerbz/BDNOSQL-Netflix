using Netflix.App.Live;
using Netflix.App.Messaging;

namespace Netflix.App.Tests;

public sealed class LiveEventsTests
{
    private static void Save(LiveEvents live, int position) => live.TitleSaved(
        new TitleMessage { Position = position, TotalTitles = 6137 },
        new TitleEvent(DateTime.Now, "tm1", "x", TitleStatus.Nuevo, 1));

    [Fact]
    public void El_progreso_ignora_el_desorden_y_reinicia_en_otra_corrida()
    {
        var live = new LiveEvents();

        Save(live, 6137);
        Save(live, 6136);
        Assert.Equal(6137, live.Position);

        Save(live, 1);
        Assert.Equal(1, live.Position);
    }

    [Fact]
    public void El_feed_guarda_los_ultimos_100()
    {
        var live = new LiveEvents();
        for (var i = 1; i <= 150; i++) Save(live, i);

        Assert.Equal(LiveEvents.FeedSize, live.Feed.Count);
    }
}
