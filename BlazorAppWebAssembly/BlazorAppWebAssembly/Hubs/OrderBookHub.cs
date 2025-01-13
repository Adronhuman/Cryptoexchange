using Microsoft.AspNetCore.SignalR;

namespace BlazorAppWebAssembly.Hubs
{
    public class OrderBookHub: Hub
    {
        public async Task SendTestFisting(string message)
        => await Clients.All.SendAsync("ReceiveFisting", $"Fisting from some client: {message}");

    }
}
