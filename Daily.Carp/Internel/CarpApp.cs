using Daily.Carp.Yarp;
using Microsoft.Extensions.Configuration;

namespace Daily.Carp.Internel
{
    public class CarpApp
    {
        /// <summary>
        /// 根服务
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get; internal set;
        }

        /// <summary>
        /// 配置对象
        /// </summary>
        public static IConfiguration Configuration
        {
            get; internal set;
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