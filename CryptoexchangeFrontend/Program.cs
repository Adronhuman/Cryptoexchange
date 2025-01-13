using CryptoExchangeFrontend;
using CryptoExchangeFrontend.Components;
using CryptoExchangeFrontend.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR(e =>
{
    e.EnableDetailedErrors = true;
    e.MaximumReceiveMessageSize = 102400000;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<BackendSettings>(builder.Configuration.GetSection("BackendSettings"));
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("OrderBookHttpClient", (sp, httpClient)=>
{
    var backendSettings = sp.GetRequiredService<IOptions<BackendSettings>>().Value;
    httpClient.BaseAddress = new Uri($"{backendSettings.BackendUrl}/");
});

//builder.Services.AddScoped<OrderBookService>();
//builder.Services.AddSingleton<SignalRService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
