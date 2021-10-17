namespace BrandShield.ClientApplication
{
    internal interface IClientConfig
    {
        string BaseApiUrl { get; }
    }

    // TODO FSY: From hardcode to config
    internal class ClientConfig : IClientConfig
    {
        public string BaseApiUrl => Consts.BASE_API_URL;

        private ClientConfig() { }

        public static IClientConfig Default => new ClientConfig();
    }
}
