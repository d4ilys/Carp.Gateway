using Daily.Carp.Configuration;
using Daily.Carp.Yarp;
using static Daily.Carp.CarpApp;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesClusterIpCarpConfigurationActivator : CarpConfigurationActivator
    {
        public sealed override async Task Initialize()
        {
            await Refresh(string.Empty);
            TimingUpdate();
        }

        public override async Task Refresh(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                await FullLoad(async s => await KubernetesGainer.GetServiceInternalPointAddress(s));
            else
                await LocalLoad(async s => await KubernetesGainer.GetServiceInternalPointAddress(serviceName),
                    serviceName);

            LogInfo($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Configuration refresh.");
        }

        //为了防止其他状况 10分钟同步一次配置
        private void TimingUpdate()
        {
            var period = TimeSpan.FromMinutes(10);
            _ = new Timer(async state => { await Refresh(string.Empty); }, null, period, period);
        }
    }
}