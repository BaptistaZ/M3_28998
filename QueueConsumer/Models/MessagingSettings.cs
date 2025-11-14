namespace QueueConsumer.Models
{
    /// Configurações de ligação ao RabbitMQ para o serviço consumidor.
    /// Tipicamente preenchidas a partir da secção "Messaging" do appsettings.json.
    public sealed class MessagingSettings
    {
        /// Hostname do broker RabbitMQ (ex.: "localhost" ou nome do serviço no Docker Compose).
        public string HostName { get; init; } = "localhost";

        /// Porta AMQP utilizada pelo RabbitMQ (por omissão, 5672).
        public int Port { get; init; } = 5672;

        /// Nome de utilizador utilizado para autenticação no RabbitMQ.
        public string UserName { get; init; } = "tiago";

        /// Palavra-passe utilizada para autenticação no RabbitMQ.
        public string Password { get; init; } = "12345";

        /// Nome da fila principal de onde o consumidor lê as mensagens JSON.
        public string QueueName { get; init; } = "json_ingest_28998";

        /// Nome do exchange associado à Dead Letter Queue (DLX).
        public string DeadLetterExchangeName { get; init; } = "dlx";
    }
}