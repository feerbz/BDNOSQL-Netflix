using System.Net.Http.Headers;
using System.Text;

namespace Netflix.App.Messaging;

/// <summary>Cliente HTTP de la Management API de RabbitMQ (la usan el poller y los botones de demo).</summary>
public static class RabbitManagement
{
    public const string ClientName = "rabbitmq-management";

    public static IServiceCollection AddRabbitManagement(this IServiceCollection services, IConfiguration config)
    {
        var credentials = $"{config["RabbitMq:User"] ?? "guest"}:{config["RabbitMq:Password"] ?? "guest"}";
        services.AddHttpClient(ClientName, client =>
        {
            client.BaseAddress = new Uri($"http://{config["RabbitMq:Host"] ?? "localhost"}:15672/");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
            client.Timeout = TimeSpan.FromSeconds(5);
        }).RemoveAllLoggers(); // una petición cada 2 s llenaría el log
        return services;
    }
}
