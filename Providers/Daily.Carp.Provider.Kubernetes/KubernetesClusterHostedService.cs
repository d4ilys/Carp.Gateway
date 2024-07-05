using Daily.Carp.Extension;
using Microsoft.Extensions.Hosting;

namespace Daily.Carp.Provider.Kubernetes
{
    /// <summary>
    /// 主机启动时构建KubernetesGenericHostedService
    /// </summary>
    public class KubernetesClusterHostedService : IHostedService
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host"></param>
        /// <param name="carpBuild"></param>
        /// <param name="type"></param>
        public KubernetesClusterHostedService(IHost? host, IServiceProvider serviceProvider, ICarpBuilder carpBuilder,KubeDiscoveryType type)
        {
            CarpApp.ServiceProvider = serviceProvider;
            if (type == KubeDiscoveryType.ClusterIP)
                _ = new KubernetesClusterIPCarpConfigurationActivator(carpBuilder.ProxyConfigProvider);
            else
                _ = new KubernetesWatchPodCarpConfigurationActivator(carpBuilder.ProxyConfigProvider);
        }

        /// <summary>
        /// 监听主机启动
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 监听主机停止
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}