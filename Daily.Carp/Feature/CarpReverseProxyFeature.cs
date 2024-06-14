using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Model;

namespace Daily.Carp.Feature
{
    public class CarpReverseProxyFeature
    {
        /// <summary>
        /// 当前请求匹配到的CarpRouteConfig
        /// </summary>
        public CarpRouteConfig? CarpRouteConfig { get; set; }

        /// <summary>
        /// Yarp ReverseProxyFeature
        /// </summary>
        public IReverseProxyFeature? YarpReverseProxyFeature { get; set; }
    }
}