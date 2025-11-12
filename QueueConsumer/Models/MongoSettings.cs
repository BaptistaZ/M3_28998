namespace QueueConsumer.Models
{
  public class MongoSettings
  {
    public string ConnectionString { get; set; } =
        "mongodb://tiago_28998:28998@localhost:27017/integracao_28998?authSource=integracao_28998";
    public string DatabaseName { get; set; } = "integracao_28998";
    public string CollectionName { get; set; } = "ingest";
  }
}
