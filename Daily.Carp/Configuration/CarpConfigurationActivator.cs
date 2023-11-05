using Daily.Carp.Yarp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using Daily.Carp.Feature;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
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
        /// 内部专用IOC
        /// </summary>
        private IServiceCollection InternelServiceCollection = new ServiceCollection();

        /// <summary>
        /// 内部专用IOC
        /// </summary>
        private IServiceProvider? InternalServiceProvider;

        /// <summary>
        /// Yarp核心配置提供者
        /// </summary>
        public CarpProxyConfigProvider YarpConfigProvider { get; set; }

        protected CarpConfigurationActivator(CarpProxyConfigProvider provider)
        {
            YarpConfigProvider = provider;
        }


        /// <summary>
        /// 初始化
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// 刷新配置
        /// </summary>
        public abstract void RefreshAll();

        public abstract void Refresh(string serviceName);

        /// <summary>
        /// 内部容器添加服务
        /// </summary>
        /// <param name="action"></param>
        public void ConfigureServices(Action<IServiceCollection>? action = null)
        {
            action?.Invoke(InternelServiceCollection);
            InternalServiceProvider = InternelServiceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// 获取内部容器服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>() => InternalServiceProvider.GetService<T>();

        /// <summary>
        /// 按服务名称注入配置
        /// </summary>
        public virtual void Inject(Func<string, IEnumerable<Service>> addressFunc)
        {
            var result = YarpAdapter(addressFunc);
            YarpConfigProvider.Refresh(result.Item2, result.Item1);
        }

        /// <summary>
        /// 按服务名称注入配置
        /// </summary>
        public virtual void RefreshInject(Func<string, IEnumerable<Service>> addressFunc, string serviceName)
        {
            var result = RefreshYarpAdapter(addressFunc, serviceName);
            YarpConfigProvider.Refresh(result.Item2, result.Item1);
        }

        /// <summary>
        /// yarp配置适配
        /// </summary>
        /// <returns></returns>
        private Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>> YarpAdapter(
            Func<string, IEnumerable<Service>> addressFunc)
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
                        var address = addressFunc.Invoke(service.ServiceName);
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


                    var clusterId = $"ClusterId-{service.ServiceName}";
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
                            ? null
                            : new ForwarderRequestConfig()
                            {
                                Version = new Version(service.HttpVersion),
                                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                                ActivityTimeout = TimeSpan.FromSeconds(10)
                            }
                    };
                    clusterConfigs.Add(clusterConfig);

                    var routeId = $"RouteId-{service.ServiceName}";

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
            }

            return new Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>(clusterConfigs, routeConfigs);
        }

        /// <summary>
        /// 部分yarp配置适配
        /// </summary>
        /// <returns></returns>
        private Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>> RefreshYarpAdapter(
            Func<string, IEnumerable<Service>> addressFunc, string serviceName)
        {
            CarpApp.LogInfo($"{DateTime.Now},监听到:{serviceName}Pod修改，正在刷新配置...");
            var proxyConfig = YarpConfigProvider.GetConfig();

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
                        var address = addressFunc.Invoke(service.ServiceName);
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


                    var clusterId = $"ClusterId-{service.ServiceName}";
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
                            ? null
                            : new ForwarderRequestConfig()
                            {
                                Version = new Version(service.HttpVersion),
                                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                                ActivityTimeout = TimeSpan.FromSeconds(10)
                            }
                    };
                    clusterConfigs.Add(clusterConfig);

                    var routeId = $"RouteId-{service.ServiceName}";

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

                CarpApp.LogInfo($"{DateTime.Now},{serviceName}，刷新成功：{JsonSerializer.Serialize(service)}.");
            }

            return new Tuple<IReadOnlyList<ClusterConfig>, IReadOnlyList<RouteConfig>>(clusterConfigs, routeConfigs);
        }
    }
}