namespace Daily.Carp.Internel
{
    public class CarpConfig
    {
        /// <summary>
        /// Kubernetes 命名空间
        /// </summary>
        public string Namespace { get; set; } = "default";

        /// <summary>
        /// 集群配置
        /// </summary>
        public List<CarpRouteConfig> Routes { get; set; }
    }


    public class CarpRouteConfig
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
        /// Http协议版本
        /// </summary>
        public string HttpVersion { get; set; } = "2";

        /// <summary>
        /// 手动指定端口
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// 普通模式下的下端服务集群地址
        /// </summary>
        public List<string> DownstreamHostAndPorts { get; set; } = new List<string>();

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