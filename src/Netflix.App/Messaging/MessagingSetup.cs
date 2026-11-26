using MassTransit;

namespace Netflix.App.Messaging;

public static class MessagingSetup
{
    public const string Exchange = "netflix.catalog";
    public const string TitlesQueue = "catalog-titles";
    public const string AggregatesQueue = "catalog-aggregates";

    public static IServiceCollection AddCatalogMessaging(this IServiceCollection services, IConfiguration config) =>
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TitleConsumer>();
            x.AddConsumer<AggregateConsumer>();
            x.AddConsumer<FaultConsumer>();

            x.UsingRabbitMq((context, bus) =>
            {
                bus.Host(config["RabbitMq:Host"] ?? "localhost", "/", host =>
                {
                    host.Username(config["RabbitMq:User"] ?? "guest");
                    host.Password(config["RabbitMq:Password"] ?? "guest");
                });

                bus.ReceiveEndpoint(TitlesQueue, e =>
                {
                    RawJsonFrom(e, "title");
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(1)));
                    e.ConfigureConsumer<TitleConsumer>(context);
                });

                bus.ReceiveEndpoint(AggregatesQueue, e =>
                {
                    RawJsonFrom(e, "aggregate");
                    e.ConcurrentMessageLimit = 1; // en orden: "kpis" es el último
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(1)));
                    e.ConfigureConsumer<AggregateConsumer>(context);
                });

                // Cola temporal: los eventos de error solo interesan mientras la app está viva.
                bus.ReceiveEndpoint(new TemporaryEndpointDefinition(), e => e.ConfigureConsumer<FaultConsumer>(context));
            });
        });

    /// <summary>JSON plano publicado por Python: sin sobre y con binding explícito por routing key.</summary>
    private static void RawJsonFrom(IRabbitMqReceiveEndpointConfigurator endpoint, string routingKey)
    {
        endpoint.ConfigureConsumeTopology = false;
        endpoint.UseRawJsonDeserializer(isDefault: true);
        endpoint.Bind(Exchange, binding =>
        {
            binding.ExchangeType = "topic";
            binding.RoutingKey = routingKey;
        });
    }
}
