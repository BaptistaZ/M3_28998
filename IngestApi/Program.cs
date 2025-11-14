using System.Text.Json;
using IngestApi.Models;
using IngestApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ===================== Configuração de Serviços ===================== //

// Liga a secção "Messaging" do appsettings.json ao modelo MessagingSettings
builder.Services.Configure<MessagingSettings>(
    builder.Configuration.GetSection("Messaging"));

// Serviço responsável por publicar mensagens no RabbitMQ
builder.Services.AddSingleton<RabbitMqPublisher>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===================== Middleware & Swagger ===================== //

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ===================== Endpoints ===================== //

/// Endpoint de health check simples para verificar se a API está viva.
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
   .WithName("HealthCheck")
   .WithOpenApi();

/// Endpoint de ingestão genérica de JSON.
/// Recebe o corpo do pedido como texto, valida se é JSON e envia para a fila do RabbitMQ.
app.MapPost("/ingest", async (HttpRequest req, RabbitMqPublisher publisher) =>
{
    using var reader = new StreamReader(req.Body);
    var payload = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(payload) || !IsValidJson(payload))
    {
        return Results.BadRequest(new { error = "JSON inválido ou vazio." });
    }

    var messageId = await publisher.PublishAsync(payload);

    return Results.Accepted(
        $"/messages/{messageId}",
        new { messageId, status = "queued" }
    );
})
.WithName("IngestJson")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest)
.WithOpenApi();

app.Run();

/// Valida se uma string é um JSON bem formado.
static bool IsValidJson(string text)
{
    try
    {
        using var _ = JsonDocument.Parse(text);
        return true;
    }
    catch (JsonException)
    {
        return false;
    }
}