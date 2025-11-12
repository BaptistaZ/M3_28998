using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using QueueConsumer.Models;

namespace QueueConsumer
{
    public class Worker : BackgroundService
    {
        private readonly MessagingSettings _mq;
        private readonly MongoSettings _mongo;
        private readonly IMongoCollection<BsonDocument> _collection;
        private IConnection? _connection;
        private IModel? _channel;

        public Worker(IOptions<MessagingSettings> mq, IOptions<MongoSettings> mongo, MongoClient client)
        {
            _mq = mq.Value;
            _mongo = mongo.Value;

            var db = client.GetDatabase(_mongo.DatabaseName);
            _collection = db.GetCollection<BsonDocument>(_mongo.CollectionName);

            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("messageId");
            _collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeys));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _mq.HostName,
                Port = _mq.Port,
                UserName = _mq.UserName,
                Password = _mq.Password,
                DispatchConsumersAsync = true
            };
            _connection = factory.CreateConnection("queue-consumer");
            _channel = _connection.CreateModel();

            var args = new Dictionary<string, object> { { "x-dead-letter-exchange", _mq.DlxName ?? "dlx" } };
            _channel.QueueDeclare(_mq.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: args);

            var dlx = _mq.DlxName ?? "dlx";
            _channel.ExchangeDeclare(dlx, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare($"{_mq.QueueName}.dlq", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind($"{_mq.QueueName}.dlq", dlx, routingKey: _mq.QueueName);

            _channel.BasicQos(0, prefetchCount: 10, global: false);
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel is null) throw new InvalidOperationException("Channel not initialized.");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var doc = BsonDocument.Parse(json);

                    doc["messageId"] = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString("N");
                    doc["receivedAt"] = DateTime.UtcNow;
                    doc["routingKey"] = ea.RoutingKey;

                    await _collection.InsertOneAsync(doc, cancellationToken: stoppingToken);
                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Erro ao processar mensagem: {ex}");
                    _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(_mq.QueueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            try { _channel?.Close(); _connection?.Close(); } catch {}
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}