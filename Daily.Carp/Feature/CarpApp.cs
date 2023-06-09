﻿using Daily.Carp.Feature;
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
    }
}