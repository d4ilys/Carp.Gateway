using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Yarp.ReverseProxy.Configuration;

namespace Daily.Carp.Yarp
{
    public class CarpProxyConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public CarpProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            ChangeToken = new CancellationChangeToken(_cts.Token);
            Routes = routes;
            Clusters = clusters;
        }

        /// <summary>
        /// 路由规则
        /// </summary>
        public IReadOnlyList<RouteConfig> Routes { get; }

        /// <summary>
        /// 路由映射
        /// </summary>
        public IReadOnlyList<ClusterConfig> Clusters { get; }


        public IChangeToken ChangeToken { get; }

        internal void SignalChange()
        {
            _cts.Cancel();
        }

    }
}