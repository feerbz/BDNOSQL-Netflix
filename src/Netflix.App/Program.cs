using System.Globalization;
using ApexCharts;
using MudBlazor.Services;
using Netflix.App.Components;
using Netflix.App.Data;
using Netflix.App.Live;
using Netflix.App.Messaging;

// Miles con coma y decimales con punto (6,137 · 8.1).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("es-MX");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<CassandraSession>();
builder.Services.AddSingleton<TitleWriter>();
builder.Services.AddSingleton<AggregateWriter>();
builder.Services.AddSingleton<LiveEvents>();
builder.Services.AddSingleton<DashboardQueries>();
builder.Services.AddCatalogMessaging(builder.Configuration);
builder.Services.AddRabbitManagement(builder.Configuration);
builder.Services.AddHostedService<QueueStatsPoller>();
builder.Services.AddSingleton<DemoPublisher>();
builder.Services.AddMudServices();
builder.Services.AddApexCharts();

var app = builder.Build();

// Antes de app.Run(): los consumidores no arrancan sin base de datos.
await app.Services.GetRequiredService<CassandraSession>().ConnectAsync();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
