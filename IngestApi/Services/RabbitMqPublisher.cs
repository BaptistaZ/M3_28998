using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using IngestApi.Models;

namespace IngestApi.Services
{
    public class RabbitMqPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly MessagingSettings _settings;

        public RabbitMqPublisher(IOptions<MessagingSettings> options)
        {
            _settings = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection("ingest-api");
            _channel = _connection.CreateModel();

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _settings.DlxName ?? "dlx" }
            };

            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );
        }

        public Task<string> PublishAsync(string json)
        {
            var props = _channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // persistente
            var id = Guid.NewGuid().ToString("N");
            props.MessageId = id;
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            var body = Encoding.UTF8.GetBytes(json);
            _channel.BasicPublish(exchange: "",
                                  routingKey: _settings.QueueName,
                                  basicProperties: props,
                                  body: body);
            return Task.FromResult(id);
        }

        public void Dispose()
        {
            try { _channel?.Close(); _connection?.Close(); } catch {}
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
