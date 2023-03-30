using Daily.Carp.Internel;
using Daily.Carp.Yarp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using static Daily.Carp.Internel.CarpApp;

namespace Daily.Carp.Configuration
{
    public abstract class BaseCarpConfigurationProvider
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public abstract void Initialize();

        public void AddService(Action<IServiceCollection>? action = null)
        {
            action?.Invoke(ServiceDiscovery.Services);
        }

        /// <summary>
        /// 刷新配置
        /// </summary>
        public virtual void Refresh(Func<string, IServiceProvider, IEnumerable<string>> addressFunc)
        {
            ServiceDiscoveryBuild();
            var result = YarpAdapter(addressFunc);
            ServiceDiscovery.GetService<CarpProxyConfigProvider>().Refresh(result.Item2, result.Item1);
        }

        private void ServiceDiscoveryBuild()
        {
            ServiceDiscovery.BuildServiceProvider();
        }

        /// <summary>
        /// yarp配置适配
        /// </summary>
        /// <returns></returns>
        private Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>> YarpAdapter(
            Func<string, IServiceProvider, IEnumerable<string>> addressFunc)
        {
            var clusterConfigs = new List<ClusterConfig>();

            var routeConfigs = new List<RouteConfig>();

            //获取配置
            var carpConfig = GetCarpConfig();

            //Console.WriteLine(JsonSerializer.Serialize(carpConfig, new JsonSerializerOptions()
            //{
            //    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            //}));
            foreach (var service in carpConfig.Routes)
            {
                try
                {
                    var destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);
                    var address = addressFunc.Invoke(service.ServiceName, ServiceDiscovery.ServiceProvider);
                    foreach (var item in address)
                    {
                        DestinationConfig destinationConfig = new DestinationConfig
                        {
                            Address = $"{service.DownstreamScheme}://{item}"
                        };
                        destinations.Add($"{item}", destinationConfig);
                    }

                    var clusterId = $"ClusterId-{Guid.NewGuid()}";
                    ClusterConfig clusterConfig = new ClusterConfig
                    {
                        ClusterId = clusterId,
                        LoadBalancingPolicy = service.LoadBalancerOptions,
                        Destinations = destinations,
                        HttpRequest = service.HttpVersion == "2" ? null : new ForwarderRequestConfig()
                        {
                            Version = new Version(service.HttpVersion),
                            VersionPolicy = HttpVersionPolicy.RequestVersionExact
                        }
                    };
                    clusterConfigs.Add(clusterConfig);

                    var routeId = $"RouteId-{Guid.NewGuid()}";
                    RouteConfig routeConfig = new RouteConfig
                    {
                        RouteId = routeId,
                        ClusterId = clusterId,
                        Match = new RouteMatch
                        {
                            Path = service.PathTemplate,
                        }
                    };
                    routeConfigs.Add(routeConfig);
                }
                catch
                {
                    continue;
                }
               
            }

            return new Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>(clusterConfigs, routeConfigs);
        }

    }
}