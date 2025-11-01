using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daily.Carp.Feature;
using Microsoft.AspNetCore.Http;

namespace Daily.Carp.Extension
{
    /// <summary>
    /// HttpContext扩展
    /// </summary>
    public static class HttpContextExtension
    {
        /// <summary>
        /// 获取当前Request代理信息
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static CarpReverseProxyFeature GetCarpReverseProxyFeature(this HttpContext context)
        {
            var reverseProxyFeature = context.GetReverseProxyFeature();

            var carpReverseProxyFeature = new CarpReverseProxyFeature
            {
                YarpReverseProxyFeature = reverseProxyFeature
            };

            var configClusterId = reverseProxyFeature.Cluster.Config.ClusterId;

            var serviceName = CarpApp.GetCarpServiceByClusterId(configClusterId);

            var carpRouteConfig = CarpApp.GetCarpConfig().Routes.FirstOrDefault(m => m.ServiceName == serviceName);

            carpReverseProxyFeature.CarpRouteConfig = carpRouteConfig;

            return carpReverseProxyFeature;
        }
    }
}