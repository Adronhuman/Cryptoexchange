using BlazorAppWebAssembly.Client.Pages;
using BlazorAppWebAssembly.Client.Services;
using BlazorAppWebAssembly.Components;
using BlazorAppWebAssembly.Hubs;
using BlazorAppWebAssembly.Client.DI;
using Microsoft.AspNetCore.SignalR.Client;
using BlazorAppWebAssembly.Client.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSignalR();

var orderBookApi = new OrderBookApiInfo();
builder.Configuration.GetSection("OrderBookApiInfo").Bind(orderBookApi);
builder.Services.AddSingleton(orderBookApi);
builder.Services.AddClientServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorAppWebAssembly.Client._Imports).Assembly);

app.MapHub<OrderBookHub>("/orderBookHub");
app.Run();
