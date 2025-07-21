using Daily.Carp.Configuration;
using Daily.Carp.Yarp;
using static Daily.Carp.CarpApp;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesClusterIpCarpConfigurationActivator : CarpConfigurationActivator
    {
        public sealed override async Task Initialize()
        {
            TimingUpdate();
            await Refresh(string.Empty);
        }

        public override async Task Refresh(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                await FullLoad(async s => await KubernetesGainer.GetServiceClusterIp(s));
            else
                await LocalLoad(async s => await KubernetesGainer.GetServiceClusterIp(serviceName),
                    serviceName);

            LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Configuration refresh.");
        }

        //为了防止其他状况 10分钟同步一次配置
        private void TimingUpdate()
        {
            var period = TimeSpan.FromMinutes(1);
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = period.TotalMilliseconds;
            timer.Elapsed += (sender, args) =>
            {
                LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} TimingUpdateCallback refresh.");
                _ = Refresh(string.Empty);
            };
            timer.Start();
        }

        private async void TimingUpdateCallback(object? state)
        {
            await Refresh(string.Empty);
        }
    }
}