using Daily.Carp.Yarp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace Daily.Carp.Configuration
{
    public class CarpConfigurationProvider
    {

        private CarpProxyConfigProvider _proxyConfigProvider;

        public CarpConfigurationProvider(CarpProxyConfigProvider proxyConfigProvider)
        {
            _proxyConfigProvider = proxyConfigProvider;
        }

        //默认实现
        private Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>> InternelGet()
        {
            var cc = new List<ClusterConfig>();
            var rc = new List<RouteConfig>();
            var tuple = new Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>(cc, rc);
            return tuple;
        }

        public void Initialize()
        {
            var result = InternelGet();
            _proxyConfigProvider.Refresh(result.Item2, result.Item1);
        }
    }
}