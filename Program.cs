using Serilog;
using AIContentGenerator.Services;
using AIContentGenerator.Integrations;
using AIContentGenerator.Middleware;

// Configure Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Application starting up");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to logging
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog();

    // Add services
    builder.Services.AddControllers();

    // Add Swagger (API testing UI)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddScoped<IContentService, ContentService>();
    builder.Services.AddHttpClient<OpenAIClient>();
    builder.Services.AddLogging();

    var app = builder.Build();

    // Middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        Log.Information("Swagger UI enabled in Development environment");
    }

    // Add rate limiting middleware (60 requests per minute)
    app.UseMiddleware<RateLimitingMiddleware>(60);
    Log.Information("Rate limiting middleware configured");

    app.UseHttpsRedirection();

    // IMPORTANT → enables controller routes
    app.MapControllers();

    Log.Information("Application started successfully. Listening on configured endpoints.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}