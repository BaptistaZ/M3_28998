using System.Text;
using IngestApi.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace IngestApi.Services
{
    /// Serviço responsável por publicar mensagens JSON na fila configurada no RabbitMQ.
    /// É registado como singleton e partilha a ligação/canal ao longo da vida da aplicação.
    public sealed class RabbitMqPublisher : IDisposable
    {
        private static readonly Encoding Utf8 = Encoding.UTF8;

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly MessagingSettings _settings;
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(
            IOptions<MessagingSettings> options,
            ILogger<RabbitMqPublisher> logger)
        {
            _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                DispatchConsumersAsync = true
            };

            // Cria ligação e canal para publicação de mensagens
            _connection = factory.CreateConnection("ingest-api");
            _channel = _connection.CreateModel();

            // Garante que a fila principal existe e está associada ao Dead Letter Exchange
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _settings.DeadLetterExchangeName }
            };

            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            _logger.LogInformation(
                "Ligação ao RabbitMQ estabelecida ({Host}:{Port}), fila {Queue} pronta.",
                _settings.HostName,
                _settings.Port,
                _settings.QueueName);
        }

        /// Publica um payload JSON na fila configurada.
        /// Devolve o MessageId gerado para correlação.
        /// <param name="json">Conteúdo JSON a publicar.</param>
        /// <returns>Identificador único da mensagem publicada.</returns>
        /// <exception cref="ArgumentException">Se o JSON for nulo, vazio ou em branco.</exception>
        public Task<string> PublishAsync(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("O payload JSON não pode ser nulo ou vazio.", nameof(json));
            }

            var messageId = Guid.NewGuid().ToString("N");

            var props = _channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // 2 = persistente
            props.MessageId = messageId;
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            var body = Utf8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "",
                routingKey: _settings.QueueName,
                basicProperties: props,
                body: body
            );

            _logger.LogInformation(
                "Mensagem publicada na fila {Queue} com MessageId {MessageId}.",
                _settings.QueueName,
                messageId);

            // O client do RabbitMQ é síncrono; aqui só devolvemos o ID já gerado
            return Task.FromResult(messageId);
        }

        public void Dispose()
        {
            try
            {
                if (_channel.IsOpen)
                {
                    _channel.Close();
                }

                if (_connection.IsOpen)
                {
                    _connection.Close();
                }

                _logger.LogInformation("Ligação ao RabbitMQ encerrada com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ocorreu um erro ao libertar recursos do RabbitMQ.");
            }
            finally
            {
                _channel.Dispose();
                _connection.Dispose();
            }
        }
    }
}