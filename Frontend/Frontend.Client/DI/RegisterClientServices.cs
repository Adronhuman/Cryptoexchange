using Frontend.Client.Services;
using Frontend.Client.Settings;
using Microsoft.AspNetCore.SignalR.Client;

namespace Frontend.Client.DI
{
    public static class RegisterClientServices
    {
        // Initially required due to server-side prerendering being enabled by default.
        // However, I abandoned it because of issues with JsInterop.
        public static IServiceCollection AddClientServices(this IServiceCollection services)
        {
            services.AddTransient(sp =>
            {
                var orderBookApi = EnsureSettingsArePresent(sp);
                var connection = new HubConnectionBuilder()
                    .WithUrl($"{orderBookApi.BaseUrl}/{orderBookApi.HubEndpoint}")
                    .Build();
                return connection;
            });
            services.AddHttpClient("OrderBookHttpClient", (sp, httpClient) =>
            {
                var orderBookApi = EnsureSettingsArePresent(sp);
                httpClient.BaseAddress = new Uri($"{orderBookApi.BaseUrl}/");
            });
            services.AddTransient<OrderBookService>();

            return services;
        }

        private static OrderBookApiInfo EnsureSettingsArePresent(IServiceProvider sp)
        {
            var orderBookApi = sp.GetService<OrderBookApiInfo>();

            if (orderBookApi == null
                || string.IsNullOrEmpty(orderBookApi.BaseUrl)
                || string.IsNullOrEmpty(orderBookApi.WholeBookEndpoint)
                || string.IsNullOrEmpty(orderBookApi.HubEndpoint))
            {
                throw new InvalidOperationException("OrderBookApiInfo appSettings are not configured");
            }

            return orderBookApi;
        }
    }
}
