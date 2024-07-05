using Daily.Carp.Configuration;
using Daily.Carp.Yarp;
using static Daily.Carp.CarpApp;

namespace Daily.Carp.Provider.Kubernetes
{
    internal class KubernetesClusterIPCarpConfigurationActivator : CarpConfigurationActivator
    {
        private static readonly object lock_obj = new object();

        public KubernetesClusterIPCarpConfigurationActivator(CarpProxyConfigProvider provider) : base(provider)
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


        //为了防止其他状况 10分钟同步一次配置
        private void TimingUpdate()
        {
            var period = TimeSpan.FromMinutes(10);
            _ = new Timer(state => RefreshAll(), null, period, period);
        }
    }
}