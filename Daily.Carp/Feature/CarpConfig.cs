namespace Daily.Carp
{
    public class CarpConfig
    {
        public Kubernetes Kubernetes { get; set; } = new Kubernetes();

        public Consul Consul { get; set; } = new Consul();

        /// <summary>
        /// 集群配置
        /// </summary>
        public List<CarpRouteConfig> Routes { get; set; }
    }

    public class Kubernetes
    {
        /// <summary>
        /// Kubernetes 命名空间
        /// </summary>
        public string Namespace { get; set; } = "default";
    }

    public class Consul
    {
        public string Protocol { get; }
        public string Host { get; }
        public int Port { get; }
        public string Token { get; }

        /// <summary>
        /// 轮询读取更新Consul Service信息，默认3秒
        /// </summary>
        public int Interval { get; set; } = 3 * 1000;
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
        public List<string> PermissionsValidation { get; set; } =
            new List<string>();

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
        /// 客户端请求路由模板
        /// </summary>
        public string PathTemplate { get; set; }

        /// <summary>
        /// 转发路由模板
        /// </summary>
        public string TransmitPathTemplate { get; set; }

        /// <summary>
        /// 负载均衡策略
        /// </summary>
        public string LoadBalancerOptions { get; set; } = "PowerOfTwoChoices";
    }
}