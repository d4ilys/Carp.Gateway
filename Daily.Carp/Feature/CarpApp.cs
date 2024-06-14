using System.Collections.Concurrent;
using Daily.Carp.Configuration;
using Daily.Carp.Feature;
using Daily.Carp.Yarp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Daily.Carp
{
    public partial class CarpApp
    {
        /// <summary>
        /// 配置对象
        /// </summary>
        public static IConfiguration Configuration { get; internal set; }

        /// <summary>
        /// ASP.NET Core中的ServiceProvider
        /// </summary>
        public static IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// ASP.NET Core中容器实例获取
        /// </summary>
        public static T GetRootService<T>()
        {
            return ServiceProvider.GetService<T>();
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
            if (CarpApp.CarpConfig != null && CarpApp.CarpConfig.ShowLogInformation)
            {
                var log = CarpApp.GetRootService<ILogger<CarpApp>>();
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
            var log = CarpApp.GetRootService<ILogger<CarpApp>>();
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