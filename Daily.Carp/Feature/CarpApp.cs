using Daily.Carp.Configuration;
using Daily.Carp.Feature;
using Daily.Carp.Yarp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Daily.Carp
{
    public partial class CarpApp
    {
        /// <summary>
        /// 配置对象
        /// </summary>
        public static IConfiguration Configuration
        {
            get; internal set;
        }

        /// <summary>
        /// ASP.NET Core中的ServiceProvider
        /// </summary>
        internal static IServiceProvider ServiceProvider
        {
            get; set;
        }

        /// <summary>
        /// ASP.NET Core中容器实例获取
        /// </summary>
        public static T GetRootService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public static CarpConfig? CarpConfig { get; set; } =  null;

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

        /// <summary>
        /// 通过ServiceName随机取得一个该服务的地址
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static string GetAddressByServiceName(string serviceName)
        {
            var carpConfigurationActivator = ServiceProvider.GetService<CarpConfigurationActivator>();
            var proxyConfig = carpConfigurationActivator.YarpConfigProvider.GetConfig();
            var hosts = proxyConfig.Clusters.Where(c => c.ClusterId == $"ClusterId-{serviceName}").Select(c => c.Destinations.Keys);
            string result = "";
            foreach (var host in hosts)
            {
                var random = new Random();
                var hostList = host.ToList();
                var next = random.Next(0, host.Count());
                result = hostList[next];
            }
            return result;
        }
    }
}