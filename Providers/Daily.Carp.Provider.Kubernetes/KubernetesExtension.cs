using Daily.Carp.Configuration;
using Daily.Carp.Provider.Kubernetes;
using KubeClient;
using Microsoft.Extensions.DependencyInjection;

namespace Daily.Carp.Extension
{
    /// <summary>
    /// 
    /// </summary>
    public static class KubernetesExtension
    {
        /// <summary>
        /// Kubernetes服务发现
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="type">服务发现类型</param>
        /// <param name="options">KubeClient自定义配置</param>
        public static void AddKubernetes(this ICarpBuilder builder,
            KubeDiscoveryType type = KubeDiscoveryType.ClusterIP, KubeClientOptions? options = null)
        {
            var client = options == null
                ? KubeApiClient.CreateFromPodServiceAccount()
                : KubeApiClient.Create(options);

            builder.Service.AddSingleton(client);

            builder.Service.AddMemoryCache();

            builder.Service.AddSingleton(provider =>
            {
                CarpConfigurationActivator activator;

                if (type == KubeDiscoveryType.ClusterIP)
                    activator = new KubernetesClusterIpCarpConfigurationActivator();
                else
                    activator = new KubernetesWatchPodCarpConfigurationActivator();

                return activator;
            });
        }
    }
}