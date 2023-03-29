using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Daily.Carp.Internel
{
    public class GenericHostedService : IHostedService
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host"></param>
        public GenericHostedService(IHost host)
        {
            // 存储根服务
            CarpApp.ServiceProvider = host.Services;
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