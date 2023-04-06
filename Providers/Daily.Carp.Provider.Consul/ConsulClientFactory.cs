using Consul;

namespace Daily.Carp.Provider.Consul
{
    internal class ConsulClientFactory : IConsulClientFactory
    {
        public ConsulClientFactory(ConsulRegistryConfiguration config)
        {
            Config = config;
        }

        public ConsulRegistryConfiguration Config { get; set; }
 
        public IConsulClient Get()
        {
            return new ConsulClient(c =>
            {
                c.Address = new Uri($"{Config.Scheme}://{Config.Host}:{Config.Port}");

                if (!string.IsNullOrEmpty(Config?.Token))
                {
                    c.Token = Config.Token;
                }
            });
        }
    }
}