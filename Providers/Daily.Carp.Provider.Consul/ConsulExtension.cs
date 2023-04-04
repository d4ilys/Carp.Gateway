using Daily.Carp.Configuration;
using Daily.Carp.Provider.Consul;
using Microsoft.Extensions.DependencyInjection;

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
            var provider = new ConsulCarpConfigurationActiver(builder.ProxyConfigProvider);
            builder.Service.AddSingleton<CarpConfigurationActiver>(provider);
        }
    }
}