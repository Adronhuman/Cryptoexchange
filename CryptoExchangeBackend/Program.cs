using CryptoExchangeBackend.Hubs;
using CryptoExchangeBackend.Interfaces;
using CryptoExchangeBackend.Providers.Binance;
using CryptoExchangeBackend.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IOrderBookProvider, OrderBookProvider>();
builder.Services.AddHostedService<StreamingDataWorker>();

builder.Services.AddControllers();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowFrontend");
app.MapControllers();
app.MapHub<OrderBookHub>("/orderBookHub");

app.Run();
