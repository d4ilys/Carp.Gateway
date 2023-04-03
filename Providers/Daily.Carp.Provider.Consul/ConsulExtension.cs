using Daily.Carp.Provider.Consul;

namespace Daily.Carp.Extension
{
    public static class ConsulExtension
    {
        /// <summary>
        /// Consul 注册
        /// </summary>
        /// <param name="builder"></param>
        public static void AddConsul(this ICarpBuilder builder)
        {
            var provider = new ConsulCarpConfigurationProvider();
            provider.Initialize();
        }
    }
}