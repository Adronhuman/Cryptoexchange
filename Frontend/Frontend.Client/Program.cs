using Frontend.Client.DI;
using Frontend.Client.Settings;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var orderBookApiInfo = new OrderBookApiInfo();
builder.Configuration.GetSection("OrderBookApiInfo").Bind(orderBookApiInfo);
builder.Services.AddSingleton(orderBookApiInfo);

builder.Services.AddClientServices();

await builder.Build().RunAsync();
