namespace Daily.Carp.Provider.Consul
{
    public class ConsulRegistryConfiguration
    {
        public ConsulRegistryConfiguration(string scheme, string host, int port, string keyOfServiceInConsul,
            string token)
        {
            Host = string.IsNullOrEmpty(host) ? "localhost" : host;
            Port = port > 0 ? port : 8500;
            Scheme = string.IsNullOrEmpty(scheme) ? "http" : scheme;
            Token = token;
        }

        public string Scheme { get; }
        public string Host { get; }
        public int Port { get; }
        public string Token { get; }
    }
}