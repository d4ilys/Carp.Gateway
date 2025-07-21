using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daily.Carp.Feature;
using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using static Daily.Carp.CarpApp;

namespace Daily.Carp.Provider.Kubernetes
{
    /// <summary>
    /// Kubernetes 信息获得者
    /// </summary>
    public class KubernetesGainer
    {
        /// <summary>
        /// 通过服务名称获取Pods运行服务的 IP Port
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static async Task<IList<Service>> GetPodEndPointAddress(string serviceName)
        {
            var services = new List<Service>();
            try
            {
                var client = GetClient();
                var carpConfig = GetCarpConfig();
                var namespaces = carpConfig.Kubernetes.Namespace;
                var pods = await client.PodsV1().List(kubeNamespace: namespaces, labelSelector: $"app={serviceName}");
                var carpRouteConfig = GetCarpConfig().Routes.First(c => c.ServiceName == serviceName);
                foreach (var podV1 in pods)
                {
                    try
                    {
                        if (podV1.Status.ContainerStatuses.Any(c => c.Ready))
                        {
                            if (podV1.Metadata.DeletionTimestamp.HasValue)
                            {
                                // Terminating
                            }
                            else
                            {
                                var host = podV1.Status.PodIP;
                                var port = podV1.Spec.Containers.FirstOrDefault()?.Ports.FirstOrDefault()
                                    ?.ContainerPort;
                                services.Add(new Service()
                                {
                                    Host = host,
                                    Port = Convert.ToInt32(port),
                                    Protocol = carpRouteConfig.DownstreamScheme
                                });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError($"{serviceName} - Endpoint initialize error {Environment.NewLine}Message:{e}.");
                    }

                }

            }
            catch (Exception e)
            {
                LogError($"Endpoints error {Environment.NewLine}Message:{e}");
            }

            return services;
        }

        private static KubeApiClient GetClient()
        {
            return GetRootService<KubeApiClient>()!;
        }

        /// <summary>
        /// 通过服务名称获取Pods运行服务的 IP Port
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static async Task<IList<Service>> GetServiceClusterIp(string serviceName)
        {
            var services = new List<Service>();

            var client = GetClient();
            try
            {
                var carpConfig = GetCarpConfig();
                var kubeNamespace = carpConfig.Kubernetes.Namespace;
                var carpRouteConfig = GetCarpConfig().Routes.First(c => c.ServiceName == serviceName);
                var kubeService = await client.ServicesV1().Get(serviceName, kubeNamespace);
                var host = kubeService.Spec.ClusterIP;
                var port = kubeService.Spec.Ports[0].Port;
                services.Add(new Service()
                {
                    Host = host,
                    Port = Convert.ToInt32(port),
                    Protocol = carpRouteConfig.DownstreamScheme
                });

                carpRouteConfig.DownstreamHostAndPorts.AddRange(services.Select(s => s.ToString()));

            }
            catch (Exception e)
            {
                LogError($"{serviceName} - ClusterIP initialize error {Environment.NewLine}Message:{e}.");
            }

            return services;
        }
    }
}