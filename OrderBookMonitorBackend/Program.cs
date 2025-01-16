using OrderBookMonitorBackend.Hubs;
using OrderBookMonitorBackend.Impl.Loggers;
using OrderBookMonitorBackend.Impl.Providers;
using OrderBookMonitorBackend.Impl.Providers.Binance;
using OrderBookMonitorBackend.Interfaces;
using OrderBookMonitorBackend.Workers;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ApiClient>();
builder.Services.AddSingleton<MultiplePriceLevelsOrderBookProvider>();
builder.Services.AddSingleton<IOrderBookLogger>(sp =>
{
    var noopLogger = new FallbackLogger();
    try
    {
        var connectionString = builder.Configuration.GetConnectionString("MongoDb");
        if (connectionString != null)
        {
            return new MongoOrderBookLogger(connectionString);
        }
    }
    // logging isn't crucial for application
    catch { }

    Trace.TraceWarning("Failed to connect to MongoDB. Logs will not be stored.");
    return noopLogger;
});

builder.Services.AddControllers();
builder.Services.AddHostedService<BinanceWorker>();

builder.Services.AddCors(options =>
{
    var frontendOrigin = builder.Configuration["FrontendOrigin"];
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (frontendOrigin != null)
        {
            policy
            .WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowFrontend");
app.MapControllers();
app.MapHub<OrderBookHub>("/orderBookHub");

app.Run();
