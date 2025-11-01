using System.Collections.Concurrent;
using Daily.Carp.Configuration;
using KubeClient;
using System.Reactive.Linq;
using Microsoft.Extensions.Caching.Memory;
using static Daily.Carp.CarpApp;
using Timer = System.Timers.Timer;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesWatchPodCarpConfigurationActivator : CarpConfigurationActivator
    {
        private static readonly IMemoryCache Cache = GetRootService<IMemoryCache>()!;

        private const string RetryWatchLimit = "RetryWatchLimit";

        private static readonly Limiter Limiter = new Limiter();

        public override async Task Initialize()
        {
            TimingUpdate();
            await Refresh(string.Empty);
            _ = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(task => Watch());
        }

        public override async Task Refresh(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                await FullLoad(async s => await KubernetesGainer.GetPodEndPointAddress(s));
            else
                await LocalLoad(async s => await KubernetesGainer.GetPodEndPointAddress(serviceName), serviceName);
        }

        private void Watch()
        {
            try
            {
                var kubeApiClient = GetRootService<KubeApiClient>()!;

                var carpConfig = GetCarpConfig();

                var kubeNamespace = carpConfig.Kubernetes.Namespace;

                //监听Service变化，实时更新Yarp配置
                LogInfo($"Prepare to listen to namespace {kubeNamespace}.");

                void InternalWatch()
                {
                    var eventStream = kubeApiClient.PodsV1()
                        .WatchAll(kubeNamespace: kubeNamespace);
                    eventStream.Select(resourceEvent => resourceEvent.Resource).Subscribe(subsequentEvent =>
                        {
                            var serviceName = subsequentEvent.Metadata.Labels["app"];
                            if (CarpApp.GetCarpConfig()!.Routes.All(c => c.ServiceName != serviceName))
                            {
                                return;
                            }

                            var statuses = subsequentEvent.Status;
                            if (statuses.ContainerStatuses.Any(c => c.Ready))
                            {
                                if (statuses.Conditions.All(c => c.Status == "True"))
                                {
                                    try
                                    {
                                        //No Terminating
                                        if (!subsequentEvent.Metadata.DeletionTimestamp.HasValue)
                                        {
                                            _ = Refresh(serviceName);

                                            LogInfo(
                                                $"{serviceName} - Pod {subsequentEvent.Metadata.Name} is ready, refresh configuration.");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        LogError($"Listening to pod fail.{Environment.NewLine}Message: {e.Message}");
                                    }
                                }
                            }
                        },
                        error => { LogError($"Listening to pod fail.{Environment.NewLine}Message: {error.Message}"); },
                        () =>
                        {
                            var limiter = Cache.GetOrCreate(RetryWatchLimit, entry =>
                            {
                                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                                Limiter.Count = 0;
                                return Limiter;
                            });

                            limiter!.Count++;

                            if (limiter.Count < 10)
                            {
                                LogInfo($"{DateTime.Now} Listening to pod completed.");
                                InternalWatch();
                            }
                            else
                            {
                                LogError($"Listening to pod fail.{Environment.NewLine}Message: Retry limit exceeded.");
                            }
                        });
                }

                InternalWatch();
            }
            catch (Exception e)
            {
                LogError($"Watch error {Environment.NewLine}Message:{e}");
            }
        }


        //为了防止其他状况 10分钟同步一次配置
        private void TimingUpdate()
        {
            var period = TimeSpan.FromMinutes(10);
            Timer timer = new Timer();
            timer.Interval = period.TotalMilliseconds;
            timer.Elapsed += (sender, args) =>
            {
                LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} TimingUpdateCallback refresh.");
                _ = Refresh(string.Empty);
            };
            timer.Start();
        }
    }

    internal class Limiter
    {
        internal int Count { get; set; }
    }
}