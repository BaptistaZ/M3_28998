using Microsoft.Extensions.Options;
using MongoDB.Driver;
using QueueConsumer.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // Liga secção "Messaging" do appsettings às configurações de RabbitMQ
        services.Configure<MessagingSettings>(ctx.Configuration.GetSection("Messaging"));

        // Liga secção "Mongo" do appsettings às configurações de MongoDB
        services.Configure<MongoSettings>(ctx.Configuration.GetSection("Mongo"));

        // Registo do MongoClient como singleton (thread-safe e reutilizável)
        services.AddSingleton(sp =>
        {
            var mongoSettings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return new MongoClient(mongoSettings.ConnectionString);
        });

        // Registo do serviço de background que consome da fila e persiste no MongoDB
        services.AddHostedService<QueueConsumer.Worker>();
    })
    .Build();

await host.RunAsync();