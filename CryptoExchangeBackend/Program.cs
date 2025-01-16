using CryptoExchangeBackend.Hubs;
using CryptoExchangeBackend.Impl.Loggers;
using CryptoExchangeBackend.Impl.Providers;
using CryptoExchangeBackend.Impl.Providers.Binance;
using CryptoExchangeBackend.Interfaces;
using CryptoExchangeBackend.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ApiClient>();
builder.Services.AddSingleton<MultiplePriceLevelsOrderBookProvider>();
builder.Services.AddSingleton<IOrderBookLogger>(sp =>
{
    try
    {
        var connectionString = builder.Configuration.GetConnectionString("MongoDb");
        return new MongoOrderBookLogger(connectionString);
    }
    catch
    {
        return new FallbackLogger();
    }
});

builder.Services.AddControllers();
builder.Services.AddHostedService<BinanceWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
        //.SetIsOriginAllowed((host) => true)
        .WithOrigins("https://localhost:7103", "http://localhost:5002")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
        //policy.WithOrigins("https://localhost:7103", "http://localhost:5002")
        //      .AllowAnyHeader()
        //      .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//app.RegisterBinanceWorkers();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowFrontend");
app.MapControllers();
app.MapHub<OrderBookHub>("/orderBookHub");

app.Run();
