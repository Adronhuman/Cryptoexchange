using BlazorAppWebAssembly.Client.DI;
using BlazorAppWebAssembly.Client.Services;
using BlazorAppWebAssembly.Client.Settings;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var orderBookApiInfo = new OrderBookApiInfo();
builder.Configuration.GetSection("OrderBookApiInfo").Bind(orderBookApiInfo);
builder.Services.AddSingleton(orderBookApiInfo);

builder.Services.AddClientServices();

await builder.Build().RunAsync();
