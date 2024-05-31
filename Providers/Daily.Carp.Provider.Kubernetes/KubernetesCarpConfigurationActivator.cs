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
            Initialize();
        }

        public sealed override void Initialize()
        {
            RefreshAll();
            TimingUpdate();
        }

        public override void RefreshAll()
        {
            Inject(KubernetesGainer.GetServiceInternalPointAddress);
            LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Configuration refresh.");
        }

        public override void Refresh(string serviceName)
        {
            RefreshInject(s => KubernetesGainer.GetServiceInternalPointAddress(serviceName), serviceName);
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