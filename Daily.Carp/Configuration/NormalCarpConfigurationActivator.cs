using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daily.Carp.Feature;
using Daily.Carp.Yarp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Daily.Carp.Configuration
{
    internal class NormalCarpConfigurationActivator : CarpConfigurationActivator
    {
        public override async Task Initialize()
        {
            var carpConfig = CarpApp.GetCarpConfig();
            await FullLoad(serviceName =>
            {
                IList<Service> services = new List<Service>();
                var serviceRouteConfig = carpConfig.Routes.First(c => c.ServiceName == serviceName);
                foreach (var downstreamHostAndPort in serviceRouteConfig.DownstreamHostAndPorts)
                {
                    var service = new Service();
                    var strings = downstreamHostAndPort.Split(":");
                    service.Host = TryGetValueByArray(strings, 0);
                    service.Port = Convert.ToInt32(TryGetValueByArray(strings, 1, "0"));
                    service.Protocol = serviceRouteConfig.DownstreamScheme;
                    services.Add(service);
                }

                return Task.FromResult(services);
            });
            Watch();
        }

        private void Watch()
        {
            ChangeToken.OnChange(CarpApp.Configuration.GetReloadToken, () =>
            {
                CarpApp.CarpConfig = CarpApp.Configuration.GetSection("Carp").Get<CarpConfig>();

                try
                {
                    Initialize();
                }
                catch
                {
                    // ignored
                }

                CarpApp.LogInfo($"{DateTime.Now}:监听到配置文件发生改变，配置已更新..");
            });
        }

        private T TryGetValueByArray<T>(T[] array, int index, T defaultValue = default)
        {
            T res;
            try
            {
                res = array[index];
            }
            catch
            {
                res = defaultValue;
            }

            return res;
        }


        public override async Task Refresh(string serviceName)
        {
            var carpConfig = CarpApp.GetCarpConfig();
            await LocalLoad(name =>
            {
                IList<Service> services = new List<Service>();
                var serviceRouteConfig = carpConfig.Routes.First(c => c.ServiceName == name);
                foreach (var downstreamHostAndPort in serviceRouteConfig.DownstreamHostAndPorts)
                {
                    var service = new Service();
                    var strings = downstreamHostAndPort.Split(":");
                    service.Host = TryGetValueByArray(strings, 0);
                    service.Port = Convert.ToInt32(TryGetValueByArray(strings, 1, "0"));
                    service.Protocol = serviceRouteConfig.DownstreamScheme;
                }

                return Task.FromResult(services);
            }, serviceName);
        }
    }
}