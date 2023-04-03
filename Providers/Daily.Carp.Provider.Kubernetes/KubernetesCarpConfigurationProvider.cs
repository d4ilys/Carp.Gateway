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
using Daily.Carp.Feature;
using Microsoft.Extensions.Configuration;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesCarpConfigurationProvider : BaseCarpConfigurationProvider
    {
        private static readonly object lock_obj = new object();

        public KubernetesCarpConfigurationProvider()
        {
            AddService(service => service.AddKubeClient(true));
        }

        public override void Initialize()
        {
            RefreshWarp();
            Watch();
            TimingUpdate();
        }

        private void RefreshWarp()
        {
            var carpConfig = CarpApp.GetCarpConfig();
            Refresh((serviceName, provider) => GetPods(serviceName, carpConfig.Kubernetes.Namespace, provider));
        }

        //监控Pod更新Yarp配置
        private void Watch()
        {
            var carpConfig = CarpApp.GetCarpConfig();
            var eventStream = CarpApp.GetService<IKubeApiClient>().PodsV1().WatchAll(kubeNamespace: carpConfig.Kubernetes.Namespace);
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
                            RefreshWarp();
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
                timer.Elapsed += (sender, eventArgs) => { RefreshWarp(); };
                timer.Start();
            });
        }

        //api server 获取pod 
        private List<Service> GetPods(string serviceName, string namespaces, IServiceProvider provider)
        {
            lock (lock_obj)
            {
                var services = new List<Service>();
                var client = provider.GetService<IKubeApiClient>();
                var clientV1 = new EndPointClientV1(client: client);
                var endpointsV1 = clientV1.Get(serviceName, namespaces).ConfigureAwait(true).GetAwaiter().GetResult();
                var carpRouteConfig = CarpApp.GetCarpConfig().Routes.First(c => c.ServiceName == serviceName);
                foreach (var item in endpointsV1.Subsets)
                {
                    try
                    {
                        var port = item.Ports.First().Port;
                        foreach (var endpointAddressV1 in item.Addresses)
                        {
                            if (carpRouteConfig.Port != 0)
                            {
                                port = carpRouteConfig.Port;
                            }
                            
                            var host = $"{endpointAddressV1.Ip}:{port}";
                            services.Add(new Service()
                            {
                                Host = endpointAddressV1.Ip,
                                Port = port,
                                Protocol = carpRouteConfig.DownstreamScheme
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                  
                }

                return services;
            }
        }
    }
}