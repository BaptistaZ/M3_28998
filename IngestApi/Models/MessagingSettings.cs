namespace IngestApi.Models
{
  public class MessagingSettings
  {
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "tiago";
    public string Password { get; set; } = "12345";
    public string QueueName { get; set; } = "json_ingest_28998";
    public string? DlxName { get; set; } = "dlx";
  }
}
