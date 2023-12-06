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

                //var clientV1 = new EndPointClientV1(client: client);
                //var endpointsV1 = clientV1.Get(serviceName, namespaces).ConfigureAwait(true).GetAwaiter()
                //    .GetResult();
                //foreach (var item in endpointsV1.Subsets)
                //{
                //    try
                //    {
                //        var port = item.Ports.First().Port;
                //        foreach (var endpointAddressV1 in item.Addresses)
                //        {
                //            if (carpRouteConfig.Port != 0)
                //            {
                //                port = carpRouteConfig.Port;
                //            }

                //            var host = $"{endpointAddressV1.Ip}:{port}";
                //            services.Add(new Service()
                //            {
                //                Host = endpointAddressV1.Ip,
                //                Port = port,
                //                Protocol = carpRouteConfig.DownstreamScheme
                //            });
                //        }
                //    }
                //    catch (Exception e)
                //    {
                //        LogError($"Endpoints foreach error {Environment.NewLine}Message:{e}");
                //        continue;
                //    }
                //}

                LogInfo($"EndPoint {JsonConvert.SerializeObject(services)}.");
            }
            catch (Exception e)
            {
                LogError($"Endpoints error {Environment.NewLine}Message:{e}");
            }


            return services;
        }
    }
}