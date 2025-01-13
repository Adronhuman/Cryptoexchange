using Frontend.Client.Services;
using Frontend.Client.Settings;
using Microsoft.AspNetCore.SignalR.Client;

namespace Frontend.Client.DI
{
    public static class RegisterClientServices
    {
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
