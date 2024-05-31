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
    internal class KubernetesCarpConfigurationWatchPodActivator : CarpConfigurationActivator
    {
        private static readonly object lock_obj = new object();

        public KubernetesCarpConfigurationWatchPodActivator(CarpProxyConfigProvider provider) : base(provider)
        {
            Initialize();
        }

        public sealed override void Initialize()
        {
            RefreshAll();
            Watch();
            TimingUpdate();
        }

        public override void RefreshAll()
        {
            Inject(KubernetesGainer.GetPodEndPointAddress);
            LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Configuration refresh.");
        }

        public override void Refresh(string serviceName)
        {
            RefreshInject(s => KubernetesGainer.GetPodEndPointAddress(serviceName), serviceName);
        }


        private void Watch()
        {
            var carpConfig = GetCarpConfig();
            var kubeNamespace = carpConfig.Kubernetes.Namespace;
            //监听Service变化，实时更新Yarp配置
            LogInfo($"Prepare to listen to namespace {kubeNamespace}.");
            var eventStream = GetService<IKubeApiClient>().PodsV1()
                .WatchAll(kubeNamespace: kubeNamespace);
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


    }
}