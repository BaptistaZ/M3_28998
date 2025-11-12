using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using QueueConsumer.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<MessagingSettings>(ctx.Configuration.GetSection("Messaging"));
        services.Configure<MongoSettings>(ctx.Configuration.GetSection("Mongo"));

        services.AddSingleton(sp =>
        {
            var m = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return new MongoClient(m.ConnectionString);
        });

        services.AddHostedService<QueueConsumer.Worker>();
    })
    .Build();

await host.RunAsync();
