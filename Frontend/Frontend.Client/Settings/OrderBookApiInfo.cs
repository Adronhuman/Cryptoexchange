namespace Frontend.Client.Settings
{
    public class OrderBookApiInfo
    {
        public string BaseUrl { get; set; }

        public string WholeBookEndpoint { get; set; }
        public string HubEndpoint { get; set; }
        public List<int> DepthLevels { get; set; }
    }
}
