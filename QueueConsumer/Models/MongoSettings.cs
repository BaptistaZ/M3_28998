namespace QueueConsumer.Models
{
    /// Configurações de acesso ao MongoDB para o serviço consumidor.
    public sealed class MongoSettings
    {
        /// Connection string para o MongoDB.
        /// Nota: Para efeitos académicos, a connection string é inicializada com uma configuração local.
        public string ConnectionString { get; init; } =
            "mongodb://tiago_28998:28998@localhost:27017/integracao_28998?authSource=integracao_28998";

        /// Nome da base de dados utilizada para persistir os documentos ingeridos.
        public string DatabaseName { get; init; } = "integracao_28998";

        /// Nome da coleção onde são persistidos os documentos JSON processados.
        public string CollectionName { get; init; } = "ingest";
    }
}