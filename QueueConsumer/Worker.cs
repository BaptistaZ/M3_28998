using System.Text;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using QueueConsumer.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QueueConsumer
{
    /// Serviço de background responsável por:
    ///  - ligar ao RabbitMQ,
    ///  - consumir mensagens da fila configurada,
    ///  - e persistir no MongoDB.
    public class Worker : BackgroundService
    {
        private readonly MessagingSettings _mq;
        private readonly MongoSettings _mongo;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger<Worker> _logger;

        private IConnection? _connection;
        private IModel? _channel;

        public Worker(
            IOptions<MessagingSettings> mq,
            IOptions<MongoSettings> mongo,
            MongoClient client,
            ILogger<Worker> logger)
        {
            _mq = mq.Value;
            _mongo = mongo.Value;
            _logger = logger;

            // Obtém a base de dados e a coleção onde os documentos serão persistidos
            var db = client.GetDatabase(_mongo.DatabaseName);
            _collection = db.GetCollection<BsonDocument>(_mongo.CollectionName);

            // Cria índice em messageId (idempotente: se já existir, o driver ignora)
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("messageId");
            _collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeys));

            _logger.LogInformation(
                "Worker inicializado. Base de dados: {Database}, coleção: {Collection}.",
                _mongo.DatabaseName,
                _mongo.CollectionName);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "A iniciar ligação ao RabbitMQ em {Host}:{Port}...",
                _mq.HostName,
                _mq.Port);

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

            // DLX configurado 
            var dlxName = _mq.DeadLetterExchangeName ?? "dlx";

            // Declara fila principal com DLX associado
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", dlxName }
            };

            _channel.QueueDeclare(
                queue: _mq.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args);

            // Declara exchange e fila da Dead Letter Queue (DLQ)
            _channel.ExchangeDeclare(
                exchange: dlxName,
                type: ExchangeType.Direct,
                durable: true);

            var dlqName = $"{_mq.QueueName}.dlq";

            _channel.QueueDeclare(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBind(
                queue: dlqName,
                exchange: dlxName,
                routingKey: _mq.QueueName);

            // Limita o número de mensagens em pré-fetch por consumidor
            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 10,
                global: false);

            _logger.LogInformation(
                "Ligação ao RabbitMQ estabelecida. Fila principal: {Queue}, DLQ: {Dlq}.",
                _mq.QueueName,
                dlqName);

            return base.StartAsync(cancellationToken);
        }

        /// Regista o consumidor assíncrono e inicia o ciclo de processamento.
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel is null)
            {
                throw new InvalidOperationException("O canal do RabbitMQ não foi inicializado.");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var document = BsonDocument.Parse(json);

                    // Metadados de integração
                    document["messageId"] = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString("N");
                    document["receivedAt"] = DateTime.UtcNow;
                    document["routingKey"] = ea.RoutingKey;

                    await _collection.InsertOneAsync(document, cancellationToken: stoppingToken);

                    _channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                    _logger.LogInformation(
                        "Mensagem processada e persistida com sucesso. MessageId: {MessageId}",
                        document["messageId"]);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erro ao processar mensagem. A mensagem será enviada para a DLQ.");

                    // Nack com requeue = false → vai para a DLQ associada ao DLX configurado
                    _channel!.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(
                queue: _mq.QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "Consumidor registado na fila {Queue}. A aguardar mensagens...",
                _mq.QueueName);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            try
            {
                if (_channel is { IsOpen: true })
                {
                    _channel.Close();
                }

                if (_connection is { IsOpen: true })
                {
                    _connection.Close();
                }

                _logger.LogInformation("Ligação ao RabbitMQ encerrada.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao encerrar ligação ao RabbitMQ.");
            }
            finally
            {
                _channel?.Dispose();
                _connection?.Dispose();
                base.Dispose();
            }
        }
    }
}