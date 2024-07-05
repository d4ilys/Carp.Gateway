using Daily.Carp.Configuration;
using Daily.Carp.Extension;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Daily.Carp.Internal
{
    /// <summary>
    /// CarpHostedService
    /// </summary>
    public class CarpHostedService : IHostedService
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="carpBuilder"></param>
        public CarpHostedService(IServiceProvider serviceProvider, ICarpBuilder carpBuilder)
        {
            // 存储根服务
            CarpApp.ServiceProvider = serviceProvider;
            CarpApp.Configuration = serviceProvider.GetService<IConfiguration>();
            carpBuilder.HostedServiceDelegate?.Invoke(serviceProvider);
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