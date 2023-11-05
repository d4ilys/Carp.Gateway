using Daily.Carp.Configuration;
using Daily.Carp.Feature;
using Daily.Carp.Yarp;
using KubeClient;
using KubeClient.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Reactive.Linq;
using static Daily.Carp.CarpApp;
using Timer = System.Timers.Timer;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesCarpConfigurationActivator : CarpConfigurationActivator
    {
        private static readonly object lock_obj = new object();

        public KubernetesCarpConfigurationActivator(CarpProxyConfigProvider provider) : base(provider)
        {
            ConfigureServices(service => { service.AddKubeClient(true); });
            Initialize();
        }

        public override void Initialize()
        {
            RefreshAll();
            Watch();
            TimingUpdate();
        }

        public override void RefreshAll()
        {
            var carpConfig = GetCarpConfig();
            Inject(serviceName => GetPods(serviceName, carpConfig.Kubernetes.Namespace));
            LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Configuration refresh.");
        }

        public override void Refresh(string serviceName)
        {
            var carpConfig = GetCarpConfig();
            RefreshInject(s => GetPods(serviceName, carpConfig.Kubernetes.Namespace), serviceName);
        }


        private void Watch()
        {
            var carpConfig = GetCarpConfig();
            var k8snamespace = carpConfig.Kubernetes.Namespace;
            //监听Service变化，实时更新Yarp配置
            LogInfo($"Prepare to listen to namespace {k8snamespace}.");
            var eventStream = GetService<IKubeApiClient>().PodsV1()
                .WatchAll(kubeNamespace: k8snamespace);
            eventStream.Select(resourceEvent => resourceEvent.Resource).Subscribe(subsequentEvent =>
                {
                    try
                    {
                        var serviceName = subsequentEvent.Metadata.Labels["app"];
                        Refresh(serviceName);
                    }
                    catch (Exception e)
                    {
                        LogError($"Listening to pod fail.{Environment.NewLine}Message: {e.Message}");
                    }
                },
                error => LogError($"Listening to pod fail.{Environment.NewLine}Message: {error.Message}"),
                () => { LogInfo("Listening to pod completed."); });
        }


        //为了防止其他状况 1分钟同步一次配置
        private void TimingUpdate()
        {
            Task.Run(() =>
            {
                var timer = new Timer();
                timer.Interval = 60 * 1000;
                timer.Elapsed += (sender, eventArgs) => { RefreshAll(); };
                timer.Start();
            });
        }

        //api server 获取pod 
        private List<Service> GetPods(string serviceName, string namespaces)
        {
            var services = new List<Service>();
            try
            {
                var client = GetService<IKubeApiClient>();

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