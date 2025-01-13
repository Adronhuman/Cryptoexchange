using BlazorAppWebAssembly.Client.Services;
using BlazorAppWebAssembly.Client.Settings;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorAppWebAssembly.Client.DI
{
    public static class RegisterClientServices
    {
        public static IServiceCollection AddClientServices(this IServiceCollection services)
        {
            services.AddTransient<HubConnection>(serviceProvider =>
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
