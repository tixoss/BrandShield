namespace BrandShield.ClientApplication
{
    internal interface IClientConfig
    {
        string BaseApiUrl { get; }
    }

    // TODO FSY: From hardcode to config
    internal class ClientConfig : IClientConfig
    {
        public string BaseApiUrl => "http://localhost:60362/api";

        public static IClientConfig Default => new ClientConfig();
    }
}
