using Daily.Carp.Configuration;
using KubeClient;
using Newtonsoft.Json;
using System.Reactive.Linq;
using System.Timers;
using Daily.Carp.Internel;
using Yarp.ReverseProxy.Configuration;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.DependencyInjection;
using Daily.Carp.Yarp;
using Daily.Carp.Extension;
using Microsoft.Extensions.Configuration;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesConfigurationProvider
    {
        private static readonly object lock_obj = new object();

        public KubernetesConfigurationProvider()
        {
            ServiceDiscovery.Services.AddKubeClient(true);
            ServiceDiscovery.BuildServiceProvider();
        }

        public void Initialize()
        {
            Refresh();
            Watch();
            TimingUpdate();
        }

        //刷新配置
        public void Refresh()
        {
            var result = PodsYarpAdapter();
            ServiceDiscovery.GetService<CarpProxyConfigProvider>().Refresh(result.Item2, result.Item1);
        }

        //监控Pod更新Yarp配置
        private void Watch()
        {
            var eventStream = ServiceDiscovery.GetService<IKubeApiClient>().PodsV1().WatchAll(kubeNamespace: "test");
            eventStream.Select(resourceEvent => resourceEvent.Resource).Subscribe(
                async subsequentEvent =>
                {
                    try
                    {
                        //监听POD创建成功后，更新Config
                        var any = subsequentEvent.Status.ContainerStatuses.Any(c => c.Ready == true);
                        if (any)
                        {
                            //延迟更新Config
                            await Task.Delay(2000);
                            Refresh();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                },
                error => Console.WriteLine(error));
        }

        //为了防止其他状况 1分钟同步一次配置
        private void TimingUpdate()
        {
            Task.Run(() =>
            {
                var timer = new Timer();
                timer.Interval = 60 * 1000;
                timer.Elapsed += (sender, eventArgs) =>
                {
                    Refresh();
                };
                timer.Start();
            });
        }

        //读取配置
        public CarpConfig GetCarpConfig()
        {
            var carpConfigs = CarpApp.Configuration.GetSection("Carp").Get<CarpConfig>();
            return carpConfigs;
        }

        //pod和yarp适配
        private Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>> PodsYarpAdapter()
        {
            var clusterConfigs = new List<ClusterConfig>();

            var routeConfigs = new List<RouteConfig>();

            //获取配置
            var carpConfig = GetCarpConfig();

            Console.WriteLine(JsonConvert.SerializeObject(carpConfig));

            foreach (var service in carpConfig.Routes)
            {
                var pods = GetPods(service.ServiceName, carpConfig.Namespace);
                var destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);
                foreach (var pod in pods)
                {
                    DestinationConfig destinationConfig = new DestinationConfig
                    {
                        Address = $"{service.DownstreamScheme}://{pod}"
                    };
                    destinations.Add($"{pod}", destinationConfig);
                }

                var clusterId = $"ClusterId-{service.ServiceName}-{Guid.NewGuid()}";
                ClusterConfig clusterConfig = new ClusterConfig
                {
                    ClusterId = clusterId,
                    LoadBalancingPolicy = service.LoadBalancerOptions,
                    Destinations = destinations
                };
                clusterConfigs.Add(clusterConfig);

                var routeId = $"RouteId-{service}-{Guid.NewGuid()}";
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
            return new Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>(clusterConfigs,routeConfigs);
        }

        //api server 获取pod 
        private List<string> GetPods(string serviceName, string namespaces)
        {
            lock (lock_obj)
            {
                var list = new List<string>();
                var client = ServiceDiscovery.GetService<IKubeApiClient>();
                var clientV1 = new EndPointClientV1(client: client);
                var endpointsV1 = clientV1.Get(serviceName, namespaces).ConfigureAwait(true).GetAwaiter().GetResult();
                foreach (var item in endpointsV1.Subsets)
                {
                    var port = item.Ports.First().Port;
                    foreach (var endpointAddressV1 in item.Addresses)
                    {
                        var host = $"{endpointAddressV1.Ip}:{port}";
                        list.Add(host);
                    }
                }

                return list;
            }
        }
    }
}