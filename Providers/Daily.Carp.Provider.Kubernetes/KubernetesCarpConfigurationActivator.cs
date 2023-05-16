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
            Refresh();
            Watch();
            TimingUpdate();
        }

        public override void Refresh()
        {
            var carpConfig = CarpApp.GetCarpConfig();
            Inject(serviceName => GetPods(serviceName, carpConfig.Kubernetes.Namespace));
        }


        private void Watch()
        {
            var carpConfig = CarpApp.GetCarpConfig();
            //监听Service变化，实时更新Yarp配置
            var eventStream = GetService<IKubeApiClient>().PodsV1()
                .WatchAll(kubeNamespace: carpConfig.Kubernetes.Namespace);
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
                timer.Elapsed += (sender, eventArgs) => { Refresh(); };
                timer.Start();
            });
        }

        //api server 获取pod 
        private List<Service> GetPods(string serviceName, string namespaces)
        {
            lock (lock_obj)
            {
                var services = new List<Service>();
                var client = GetService<IKubeApiClient>();
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