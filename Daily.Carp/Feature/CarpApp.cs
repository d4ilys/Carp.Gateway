﻿using System.Collections.Concurrent;
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
            if (ServiceProvider == null)
            {
                return default;
            }

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
    }
}