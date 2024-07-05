using Daily.Carp.Configuration;
using Daily.Carp.Provider.Kubernetes;
using KubeClient;

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
        public static void AddKubernetes(this ICarpBuilder builder,
            KubeDiscoveryType type = KubeDiscoveryType.ClusterIP)
        {
            builder.Service.AddKubeClient();


            builder.HostedServiceDelegate = async provider =>
            {
                CarpConfigurationActivator activator;

                if (type == KubeDiscoveryType.ClusterIP)
                    activator = new KubernetesClusterIpCarpConfigurationActivator();
                else
                    activator = new KubernetesWatchPodCarpConfigurationActivator();

                await activator.Initialize();
            };
        }
    }
}