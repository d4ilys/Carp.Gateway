using Daily.Carp.Provider.Kubernetes;
using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Daily.Carp.Extension
{
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
            builder.Service.AddKubeClient(true);
            builder.Service.AddHostedService(serviceProvider =>
                new KubernetesClusterHostedService(serviceProvider.GetService<IHost>(), serviceProvider, builder,
                    type));
        }
    }
}