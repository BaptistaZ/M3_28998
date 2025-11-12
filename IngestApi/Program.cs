using System.Text.Json;
using IngestApi.Models;
using IngestApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configs
builder.Services.Configure<MessagingSettings>(builder.Configuration.GetSection("Messaging"));
// Serviços
builder.Services.AddSingleton<RabbitMqPublisher>();
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapPost("/ingest", async (HttpRequest req, RabbitMqPublisher publisher) =>
{
    using var reader = new StreamReader(req.Body);
    var payload = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(payload) || !IsValidJson(payload))
        return Results.BadRequest(new { error = "JSON inválido ou vazio." });

    var id = await publisher.PublishAsync(payload);
    return Results.Accepted($"/messages/{id}", new { messageId = id, status = "queued" });
})
.WithOpenApi();

app.Run();

static bool IsValidJson(string text)
{
  try { using var _ = JsonDocument.Parse(text); return true; }
  catch { return false; }
}

