using assistant_api.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// HttpClient for Azure OpenAI (api-key in header)
builder.Services.AddHttpClient("openai", client =>
{
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Add("api-key", apiKey); // Azure OpenAI uses api-key header!
    }
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// App services
builder.Services.AddSingleton<JsonDataLoader>();
builder.Services.AddSingleton<LLMHelix>();
builder.Services.AddSingleton<LLMSpectro>();
builder.Services.AddSingleton<ChatHistoryService>();

// Enable CORS (allow only your origins, no trailing slash)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://skf-assistant-web-a6fdardwh2dkemag.southindia-01.azurewebsites.net",
            "http://localhost:54436")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("x-session-id");
    });
});

// === Add Rate Limiting ===
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", o =>
    {
        o.PermitLimit = 10; // 10 requests
        o.Window = TimeSpan.FromMinutes(1); // per minute
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0; // No queueing
    });
});

// Add controllers, swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Uncomment if using HTTPS

app.UseCors();         // Enable CORS globally

app.UseRateLimiter();  // <-- Add this after CORS, before Auth

app.UseAuthorization();

app.MapControllers();

app.Run();
