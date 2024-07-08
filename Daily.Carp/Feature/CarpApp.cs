using System.Collections.Concurrent;
using Daily.Carp.Configuration;
using Daily.Carp.Feature;
using Daily.Carp.Yarp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Daily.Carp
{
    /// <summary>
    /// CarpApp
    /// </summary>
    public partial class CarpApp
    {
        /// <summary>
        /// 配置对象
        /// </summary>
        public static IConfiguration Configuration { get; internal set; }

        /// <summary>
        /// ASP.NET Core中的ServiceProvider
        /// </summary>
        private static IServiceProvider? _serviceScopeProvider;

        /// <summary>
        /// ASP.NET Core中的ServiceProvider
        /// </summary>
        private static IServiceProvider? _serviceRootProvider;

        /// <summary>
        /// 设置Root IServiceProvider
        /// </summary>
        /// <param name="serviceScopeProvider"></param>
        public static void SetRootServiceProvider(IServiceProvider serviceScopeProvider)
        {
            _serviceRootProvider = serviceScopeProvider;
        }

        /// <summary>
        /// Root容器实例获取
        /// </summary>
        public static T? GetRootService<T>()
        {
            if (_serviceRootProvider == null) throw new Exception("ServiceProvider is null");
            return _serviceRootProvider.GetService<T>();
        }

        public static CarpConfig? CarpConfig { get; set; } = null;

        /// <summary>
        /// 读取Carp配置
        /// </summary>
        /// <returns></returns>
        public static CarpConfig GetCarpConfig()
        {
            if (CarpConfig == null)
            {
                var c = Configuration.GetSection("Carp").Get<CarpConfig>();
                CarpConfig = c;
                return c;
            }

            return CarpConfig;
        }

        private static readonly ConcurrentDictionary<string, int> Polling = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// 信息日志
        /// </summary>
        /// <param name="info"></param>
        public static void LogInfo(string info)
        {
            if (CarpConfig != null && CarpApp.CarpConfig.ShowLogInformation)
            {
                var log = GetRootService<ILogger<CarpApp>>();
                if (log != null)
                {
                    log?.LogInformation($"Carp: {info}");
                }
                else
                {
                    Console.WriteLine($"Carp: {info}");
                }
            }
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="info"></param>
        public static void LogError(string info)
        {
            var log = GetRootService<ILogger<CarpApp>>();
            if (log != null)
            {
                log?.LogError($"Carp: {info}");
            }
            else
            {
                Console.WriteLine($"Carp: {info}");
            }
        }

        /// <summary>
        /// 根据ServiceName 生成Yarp ClusterId
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        internal static string GenerateYarpClusterId(string serviceName)
        {
            return $"ClusterId-{serviceName}";
        }

        /// <summary>
        /// 根据 ClusterId 获取 ServiceName
        /// </summary>
        /// <param name="clusterId"></param>
        /// <returns></returns>
        internal static string GetCarpServiceByClusterId(string clusterId)
        {
            return clusterId.Replace("ClusterId-", "");
        }

        /// <summary>
        /// 根据ServiceName 生成Yarp RouteId
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        internal static string GenerateYarpRouteId(string serviceName)
        {
            return $"RouteId-{serviceName}";
        }
    }
}