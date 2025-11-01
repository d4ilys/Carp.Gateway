using Daily.Carp.Feature;
using Daily.Carp.Yarp;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Daily.Carp.Configuration
{
    /// <summary>
    /// Carp配置提供者
    /// </summary>
    public abstract class CarpConfigurationActivator
    {
        /// <summary>
        /// Yarp核心配置提供者
        /// </summary>
        private readonly CarpProxyConfigProvider _yarpConfigProvider =
            CarpApp.GetRootService<CarpProxyConfigProvider>()!;

        /// <summary>
        /// 初始化
        /// </summary>
        public abstract Task Initialize();

        /// <summary>
        /// 局部刷新
        /// </summary>
        /// <param name="serviceName"></param>
        public abstract Task Refresh(string serviceName);

        /// <summary>
        /// 注入全部配置
        /// </summary>
        public virtual async Task FullLoad(Func<string, Task<IList<Service>>> addressFunc)
        {

            if (CarpApp.GetCarpConfig().Routes.Any())
            {
                var result = await YarpAdapter(addressFunc);
                _yarpConfigProvider.Refresh(result.Item2, result.Item1);
            }
        }

        /// <summary>
        /// 按服务名称注入配置
        /// </summary>
        public virtual async Task LocalLoad(Func<string, Task<IList<Service>>> addressFunc, string serviceName)
        {
            if (CarpApp.GetCarpConfig().Routes.Any())
            {
                var result = await RefreshYarpAdapter(addressFunc, serviceName);
                _yarpConfigProvider.Refresh(result.Item2, result.Item1);
            }
        }

        /// <summary>
        /// yarp配置适配
        /// </summary>
        /// <returns></returns>
        private async Task<Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>> YarpAdapter(
            Func<string, Task<IList<Service>>> addressFunc)
        {
            var clusterConfigs = new List<ClusterConfig>();

            var routeConfigs = new List<RouteConfig>();

            //获取配置
            var carpConfig = CarpApp.GetCarpConfig();

            foreach (var service in carpConfig.Routes)
            {
                try
                {
                    var destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);
                    if (!service.DownstreamHostAndPorts.Any())
                    {
                        var address = await addressFunc.Invoke(service.ServiceName);
                        foreach (var item in address)
                        {
                            var serviceString = item.ToString();
                            DestinationConfig destinationConfig = new DestinationConfig
                            {
                                Address = serviceString
                            };

                            destinations.Add($"{item}", destinationConfig);
                        }
                    }
                    else //兼容普通模式
                    {
                        foreach (var item in service.DownstreamHostAndPorts)
                        {
                            DestinationConfig destinationConfig = new DestinationConfig
                            {
                                Address = item
                            };
                            destinations.Add($"{item}", destinationConfig);
                        }
                    }


                    var clusterId = $"ClusterId-{service.ServiceName}";
                    if (clusterConfigs.All(x => x.ClusterId != clusterId))
                    {
                        ClusterConfig clusterConfig = new ClusterConfig
                        {
                            ClusterId = clusterId,
                            LoadBalancingPolicy = service.LoadBalancerOptions,
                            Destinations = destinations,
                            HttpClient = new HttpClientConfig
                            {
                                DangerousAcceptAnyServerCertificate = true
                            },
                            HttpRequest = service.HttpVersion == "2"
                                ? new ForwarderRequestConfig()
                                {
                                    ActivityTimeout = TimeSpan.FromMinutes(service.ActivityTimeout)
                                }
                                : new ForwarderRequestConfig()
                                {
                                    Version = new Version(service.HttpVersion),
                                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                                    ActivityTimeout = TimeSpan.FromMinutes(service.ActivityTimeout)
                                }
                        };
                        clusterConfigs.Add(clusterConfig);
                    }


                    var routeId = $"RouteId-{service.ServiceName}";

                    if (routeConfigs.All(r => r.RouteId != routeId))
                    {
                        var transforms = new List<IReadOnlyDictionary<string, string>>();
                        if (!string.IsNullOrWhiteSpace(service.TransmitPathTemplate))
                        {
                            transforms.Add(new Dictionary<string, string>()
                            {
                                { "PathPattern", service.TransmitPathTemplate }
                            });
                        }

                        RouteConfig routeConfig = new RouteConfig
                        {
                            RouteId = routeId,
                            ClusterId = clusterId,
                            Match = new RouteMatch
                            {
                                Path = service.PathTemplate,
                                Hosts = service.Hosts
                            },
                            Transforms = transforms.Any() ? transforms : null
                        };
                        routeConfigs.Add(routeConfig);
                    }
                }
                catch
                {
                    continue;
                }

                CarpApp.LogInfo(
                    $"{DateTime.Now} {service.ServiceName} - Endpoint refresh to complete.");
            }

            return new Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>(clusterConfigs, routeConfigs);
        }

        /// <summary>
        /// 部分yarp配置适配
        /// </summary>
        /// <returns></returns>
        private async Task<Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>> RefreshYarpAdapter(
            Func<string, Task<IList<Service>>> addressFunc, string serviceName)
        {
            var proxyConfig = _yarpConfigProvider.GetConfig();

            var clusterConfigs = new List<ClusterConfig>(proxyConfig.Clusters);

            var routeConfigs = new List<RouteConfig>(proxyConfig.Routes);

            //获取配置
            var carpConfigRoutes = CarpApp.GetCarpConfig().Routes.Where(c => c.ServiceName == serviceName);

            var serverClusterConfigs = clusterConfigs.Where(c => c.ClusterId == $"ClusterId-{serviceName}")
                .Select(c => c.ClusterId).ToList();

            foreach (var clusterId in serverClusterConfigs)
            {
                clusterConfigs.RemoveAll(c => c.ClusterId == clusterId);
                routeConfigs.RemoveAll(c => c.ClusterId == clusterId);
            }

            foreach (var service in carpConfigRoutes)
            {
                try
                {
                    var destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);
                    if (!service.DownstreamHostAndPorts.Any())
                    {
                        var address = await addressFunc.Invoke(service.ServiceName);

                        foreach (var item in address)
                        {
                            DestinationConfig destinationConfig = new DestinationConfig
                            {
                                Address = item.ToString()
                            };

                            destinations.Add($"{item}", destinationConfig);
                        }
                    }
                    else //兼容普通模式
                    {
                        foreach (var item in service.DownstreamHostAndPorts)
                        {
                            DestinationConfig destinationConfig = new DestinationConfig
                            {
                                Address = item
                            };
                            destinations.Add($"{item}", destinationConfig);
                        }
                    }


                    var clusterId = CarpApp.GenerateYarpClusterId(service.ServiceName);
                    ClusterConfig clusterConfig = new ClusterConfig
                    {
                        ClusterId = clusterId,
                        LoadBalancingPolicy = service.LoadBalancerOptions,
                        Destinations = destinations,
                        HttpClient = new HttpClientConfig
                        {
                            DangerousAcceptAnyServerCertificate = true
                        },
                        HttpRequest = service.HttpVersion == "2"
                            ? new ForwarderRequestConfig()
                            {
                                ActivityTimeout = TimeSpan.FromMinutes(service.ActivityTimeout)
                            }
                            : new ForwarderRequestConfig()
                            {
                                Version = new Version(service.HttpVersion),
                                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                                ActivityTimeout = TimeSpan.FromMinutes(service.ActivityTimeout)
                            }
                    };
                    clusterConfigs.Add(clusterConfig);

                    var routeId = CarpApp.GenerateYarpRouteId(service.ServiceName);

                    var transforms = new List<IReadOnlyDictionary<string, string>>();
                    if (!string.IsNullOrWhiteSpace(service.TransmitPathTemplate))
                    {
                        transforms.Add(new Dictionary<string, string>()
                        {
                            { "PathPattern", service.TransmitPathTemplate }
                        });
                    }

                    RouteConfig routeConfig = new RouteConfig
                    {
                        RouteId = routeId,
                        ClusterId = clusterId,
                        Match = new RouteMatch
                        {
                            Path = service.PathTemplate,
                        },
                        Transforms = transforms.Any() ? transforms : null
                    };
                    routeConfigs.Add(routeConfig);
                }
                catch
                {
                    continue;
                }

                CarpApp.LogInfo(
                    $"{DateTime.Now} {service.ServiceName} - Endpoint refresh to complete.");
            }

            return new Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>(clusterConfigs, routeConfigs);
        }
    }
}