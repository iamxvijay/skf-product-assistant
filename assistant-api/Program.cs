using assistant_api.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// HttpClient with Bearer token for OpenAI
builder.Services.AddHttpClient("openai", client =>
{
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
});

// App services
builder.Services.AddSingleton<JsonDataLoader>();
builder.Services.AddSingleton<LLMHelix>();
builder.Services.AddSingleton<LLMSpectro>();
builder.Services.AddSingleton<ChatHistoryService>();

// Enable CORS (allow any origin, header, method)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://azureapp:54436", "http://localhost:54436")
              .AllowAnyHeader()
              .AllowAnyMethod();
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

app.UseCors();         // <---- Enable CORS globally

app.UseAuthorization();

app.MapControllers();

app.Run();
