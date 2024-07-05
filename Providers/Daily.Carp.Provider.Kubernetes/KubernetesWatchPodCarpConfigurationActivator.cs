using System.Reactive.Linq;
using Daily.Carp.Configuration;
using Daily.Carp.Yarp;
using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using static Daily.Carp.CarpApp;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesWatchPodCarpConfigurationActivator : CarpConfigurationActivator
    {
        public sealed override async Task Initialize()
        {
            await Refresh(string.Empty);
            await Watch();
            TimingUpdate();
        }

        public override async Task Refresh(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                await FullLoad(async s => await KubernetesGainer.GetPodEndPointAddress(s));
            else
                await LocalLoad(async s => await KubernetesGainer.GetPodEndPointAddress(serviceName), serviceName);

            LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Configuration refresh.");
        }

        private Task Watch()
        {
            var kubeApiClient = KubeApiClient.Create(KubeClientOptions.FromPodServiceAccount());
            try
            {
                var carpConfig = GetCarpConfig();
                var kubeNamespace = carpConfig.Kubernetes.Namespace;
                //监听Service变化，实时更新Yarp配置
                LogInfo($"Prepare to listen to namespace {kubeNamespace}.");

                var eventStream = kubeApiClient.PodsV1()
                    .WatchAll(kubeNamespace: kubeNamespace);
                eventStream.Select(resourceEvent => resourceEvent.Resource).Subscribe(async subsequentEvent =>
                    {
                        try
                        {
                            var serviceName = subsequentEvent.Metadata.Labels["app"];
                            await Refresh(serviceName);
                        }
                        catch (Exception e)
                        {
                            LogError($"Listening to pod fail.{Environment.NewLine}Message: {e.Message}");
                        }
                    },
                    error => LogError($"Listening to pod fail.{Environment.NewLine}Message: {error.Message}"),
                    () => { LogInfo("Listening to pod completed."); });
            }
            catch (Exception e)
            {
                LogError($"Watch error {Environment.NewLine}Message:{e}");
            }

            return Task.CompletedTask;
        }


        //为了防止其他状况 10分钟同步一次配置
        private void TimingUpdate()
        {
            var period = TimeSpan.FromMinutes(10);
            _ = new Timer(async state => await Refresh(string.Empty), null, period, period);
        }
    }
}