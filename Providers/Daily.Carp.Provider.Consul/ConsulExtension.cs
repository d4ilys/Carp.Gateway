using Daily.Carp.Configuration;
using Daily.Carp.Provider.Consul;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            var carpConfigConsul = CarpApp.GetCarpConfig().Consul;
            var config = new ConsulRegistryConfiguration(carpConfigConsul.Protocol, carpConfigConsul.Host,
                carpConfigConsul.Port, "", carpConfigConsul.Token);

            builder.Service.AddSingleton<IConsulClientFactory>(new ConsulClientFactory(config));

            builder.Service.AddSingleton<CarpConfigurationActivator>(provider =>
                new ConsulCarpConfigurationActivator());
        }
    }
}