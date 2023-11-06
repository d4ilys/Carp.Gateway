using Daily.Carp.Extension;
using Microsoft.Extensions.Hosting;

namespace Daily.Carp.Provider.Kubernetes
{
    /// <summary>
    /// 主机启动时构建KubernetesGenericHostedService
    /// </summary>
    public class KubernetesGenericHostedService : IHostedService
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host"></param>
        public KubernetesGenericHostedService(IHost? host, IServiceProvider serviceProvider, ICarpBuilder carpBuilder)
        {
            // 存储根服务
            CarpApp.ServiceProvider = serviceProvider;
            var provider = new KubernetesCarpConfigurationActivator(carpBuilder.ProxyConfigProvider);
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