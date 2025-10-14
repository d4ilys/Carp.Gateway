using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace Daily.Carp.Kubernetes.IngressController
{
    /// <summary>
    /// Ingress Yarp 配置转换器
    /// </summary>
    public class IngressToYarpConfigConverter(k8s.Kubernetes client, ILogger<IngressToYarpConfigConverter> logger)
    {
        /// <summary>
        /// Convert
        /// </summary>
        /// <param name="ingresses"></param>
        /// <returns></returns>
        public (List<RouteConfig> Routes, List<ClusterConfig> Clusters) Convert(IEnumerable<V1Ingress> ingresses)
        {
            var routes = new List<RouteConfig>();
            var clusters = new Dictionary<string, ClusterConfig>();

            foreach (var ingress in ingresses)
            {
                if (ingress.Spec?.Rules == null || !ingress.Spec.Rules.Any())
                    continue;

                var ingressNamespace = ingress.Metadata.NamespaceProperty;
                var ingressName = ingress.Metadata.Name;

                foreach (var rule in ingress.Spec.Rules)
                {
                    // 处理主机规则
                    var host = rule.Host;
                    if (string.IsNullOrEmpty(host))
                        continue;

                    if (rule.Http?.Paths == null || !rule.Http.Paths.Any())
                        continue;

                    foreach (var path in rule.Http.Paths)
                    {
                        // 创建路由
                        var routeId = $"ingress-{ingressNamespace}-{ingressName}-{host}-{path.Path?.Replace("/", "-")}";
                        routeId = routeId.Replace("--", "-").Trim('-');

                        var route = new RouteConfig
                        {
                            RouteId = routeId,
                            ClusterId = GetClusterId(ingressNamespace, path.Backend?.Service?.Name),
                            Match = new RouteMatch
                            {
                                Hosts = [host],
                                Path = path.PathType == "Exact" ? path.Path : $"{path.Path}*"
                            }
                        };

                        routes.Add(route);

                        // 创建集群配置
                        if (!clusters.ContainsKey(route.ClusterId) && path.Backend?.Service != null)
                        {
                            var serviceName = path.Backend.Service.Name;
                            var servicePort = path.Backend.Service.Port?.Number ?? 80;
                            var service = GetService(ingressNamespace, serviceName);
                            var annotations = service.Annotations();
                            var protocol = "http";
                            if (annotations.TryGetValue("protocol", out var annotationProtocol))
                            {
                                protocol = annotationProtocol;
                            }

                            var cluster = new ClusterConfig
                            {
                                ClusterId = route.ClusterId,
                                Destinations = new Dictionary<string, DestinationConfig>
                                {
                                    {
                                        $"destination-{serviceName}-{servicePort}",
                                        new DestinationConfig
                                        {
                                            Address =
                                                $"{protocol}://{serviceName}.{ingressNamespace}.svc.cluster.local:{servicePort}"
                                        }
                                    }
                                }
                            };

                            clusters[route.ClusterId] = cluster;
                        }
                    }
                }
            }

            return (routes, clusters.Values.ToList());
        }

        private static string GetClusterId(string @namespace, string? serviceName)
        {
            return $"cluster-{@namespace}-{serviceName}";
        }

        private V1Service? GetService(string @namespace, string serviceName)
        {
            try
            {
                return client.ReadNamespacedService(serviceName, @namespace);
            }
            catch (Exception ex)
            {
                // 处理 Service 不存在的异常（如日志记录）
                logger.LogError($"获取 Service {serviceName} 失败: {ex.Message}");
                return null;
            }
        }
    }
}