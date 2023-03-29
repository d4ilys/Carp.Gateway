using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace Daily.Carp.Yarp
{
    /// <summary>
    /// Carp 路由映射提供器
    /// </summary>
    public class CarpProxyConfigProvider : IProxyConfigProvider
    {
        private volatile CarpProxyConfig _config;

        private List<RouteConfig> _routes = new List<RouteConfig>();

        private List<ClusterConfig> _clusters = new List<ClusterConfig>();

        public CarpProxyConfigProvider()
        {
            _config = new CarpProxyConfig(_routes, _clusters);
        }

        public IProxyConfig GetConfig()
        {
            return _config;
        }

        public void Refresh(IReadOnlyList<RouteConfig> routeConfigs, IReadOnlyList<ClusterConfig> clusterConfigs)
        {
            var oldConfig = _config;
            _config = new CarpProxyConfig(routeConfigs, clusterConfigs);
            oldConfig.SignalChange();
        }
    }
}