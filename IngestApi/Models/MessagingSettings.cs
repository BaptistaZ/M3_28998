namespace IngestApi.Models
{
    /// Configurações de ligação ao RabbitMQ para a API de ingestão.
    public sealed class MessagingSettings
    {
        /// Hostname do broker RabbitMQ (ex.: "localhost" ou nome do serviço no Docker Compose).
        public string HostName { get; init; } = "localhost";

        /// Porta AMQP (por omissão, 5672).
        public int Port { get; init; } = 5672;

        /// Nome de utilizador para autenticação no RabbitMQ.
        public string UserName { get; init; } = "tiago";

        /// Palavra-passe para autenticação no RabbitMQ.
        public string Password { get; init; } = "12345";

        /// Nome da fila principal onde as mensagens JSON serão publicadas.
        public string QueueName { get; init; } = "json_ingest_28998";

        /// Nome do exchange associado à Dead Letter Queue (DLX).
        public string DeadLetterExchangeName { get; init; } = "dlx";
    }
}