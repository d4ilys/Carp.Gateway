using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daily.Carp.Feature;
using KubeClient;
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
        public static List<Service> GetPodEndPointAddress(string serviceName)
        {
            var services = new List<Service>();
            try
            {
                var carpConfig = GetCarpConfig();
                var namespaces = carpConfig.Kubernetes.Namespace;
                var client = GetRootService<IKubeApiClient>();

                var pods = client.PodsV1().List(kubeNamespace: namespaces, labelSelector: $"app={serviceName}")
                    .ConfigureAwait(false).GetAwaiter().GetResult();
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
                        LogError($"Endpoints foreach error {Environment.NewLine}Message:{e}");
                    }
                }

                LogInfo($"EndPoint {JsonConvert.SerializeObject(services)}.");
            }
            catch (Exception e)
            {
                LogError($"Endpoints error {Environment.NewLine}Message:{e}");
            }


            return services;
        }

        /// <summary>
        /// 通过服务名称获取Pods运行服务的 IP Port
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static List<Service> GetServiceInternalPointAddress(string serviceName)
        {
            var services = new List<Service>();
            try
            {
                var carpConfig = GetCarpConfig();
                var kubeNamespace = carpConfig.Kubernetes.Namespace;
                var client = GetRootService<IKubeApiClient>();
                var carpRouteConfig = GetCarpConfig().Routes.First(c => c.ServiceName == serviceName);
                var kubeService =  client.ServicesV1().Get(serviceName, kubeNamespace).ConfigureAwait(false).GetAwaiter().GetResult();
                var host = kubeService.Spec.ClusterIP;
                var port = kubeService.Spec.Ports[0].Port;
                services.Add(new Service()
                {
                    Host = host,
                    Port = Convert.ToInt32(port),
                    Protocol = carpRouteConfig.DownstreamScheme
                });
                LogInfo($"Service - EndPoint Init ：{JsonConvert.SerializeObject(services)}.");
            }
            catch (Exception e)
            {
                LogError($"Endpoints error {Environment.NewLine}Message:{e}");
            }


            return services;
        }
    }
}