using Frontend.Client.Services;
using Frontend.Client.Settings;
using Microsoft.AspNetCore.SignalR.Client;
using System.Drawing;
using System.Net.NetworkInformation;

namespace Frontend.Client.DI
{
    public static class RegisterClientServices
    {
        // Initially required due to server-side prerendering being enabled by default.
        // However, I abandoned it because of issues with JsInterop.
        public static IServiceCollection AddClientServices(this IServiceCollection services)
        {
            services.AddTransient(serviceProvider =>
            {
                var orderBookApi = serviceProvider.GetService<OrderBookApiInfo>();
                var connection = new HubConnectionBuilder()
                    .WithUrl($"{orderBookApi.BaseUrl}/{orderBookApi.HubEndpoint}")
                    .Build();
                return connection;
            });
            services.AddHttpClient("OrderBookHttpClient", (sp, httpClient) =>
            {
                var orderBookApi = sp.GetRequiredService<OrderBookApiInfo>();
                httpClient.BaseAddress = new Uri($"{orderBookApi.BaseUrl}/");
            });
            services.AddTransient<OrderBookService>();

            return services;
        }
    }
}
