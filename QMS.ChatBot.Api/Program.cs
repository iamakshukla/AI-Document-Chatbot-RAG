
using QMS.Chatbot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Register PdfService using Dependency Injection
builder.Services.AddScoped<TextChunkService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddSingleton<VectorStoreService>();
builder.Services.AddHttpClient<EmbeddingService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:11434/");
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHttpClient<ChatGenerationService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:11434/");
    client.Timeout = TimeSpan.FromMinutes(5);
});
var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Return 503 instead of an unhandled exception when Ollama is unreachable or times out
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when (
        ex is HttpRequestException ||
        (ex is TaskCanceledException && !context.RequestAborted.IsCancellationRequested))
    {
        app.Logger.LogError(ex, "Ollama request failed");

        if (context.Response.HasStarted) throw;

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "The AI service (Ollama) is unavailable or timed out. " +
                      "Make sure Ollama is running on http://localhost:11434 and the required models are pulled."
        });
    }
});

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
