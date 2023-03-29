using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class CarpConfig
    {
        public string Namespace { get; set; } = "default";

        public List<CarpRouteConfig> Routes { get; set; }
    }

    internal class CarpRouteConfig
    {
        /// <summary>
        /// 说明
        /// </summary>
        public string Descriptions { get; set; }

        /// <summary>
        /// Kubernetes-Service名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 是否权限验证
        /// </summary>
        public bool PermissionsValidation { get; set; }

        /// <summary>
        /// 下端协议
        /// </summary>
        public string DownstreamScheme { get; set; } = "http";

        /// <summary>
        /// 路由模板
        /// </summary>
        public string PathTemplate { get; set; }

        /// <summary>
        /// 负载均衡策略
        /// </summary>
        public string LoadBalancerOptions { get; set; } = "PowerOfTwoChoices";
    }
}