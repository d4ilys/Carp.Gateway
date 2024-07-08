using Daily.Carp.Extension;
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
        /// <param name="serviceScopeProvider"></param>
        /// <param name="carpBuilder"></param>
        public CarpHostedService(IServiceProvider serviceScopeProvider, ICarpBuilder carpBuilder)
        {
            carpBuilder.HostedServiceDelegate?.Invoke(serviceScopeProvider);
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