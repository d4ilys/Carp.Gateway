using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daily.Carp.Feature;
using Daily.Carp.Internel;
using Daily.Carp.Yarp;

namespace Daily.Carp.Configuration
{
    internal class NormalCarpConfigurationActiver : CarpConfigurationActiver
    {
        public NormalCarpConfigurationActiver(CarpProxyConfigProvider provider) : base(provider)
        {
            Initialize();
        }

        public override void Initialize()
        {
            var carpConfig = CarpApp.GetCarpConfig();
            Inject(serviceName =>
            {
                var services = new List<Service>();
                var serviceRouteConfig = carpConfig.Routes.Where(c => c.ServiceName == serviceName).First();
                foreach (var downstreamHostAndPort in serviceRouteConfig.DownstreamHostAndPorts)
                {
                    var service = new Service();
                    var strings = downstreamHostAndPort.Split(":");
                    service.Host = TryGetValueByArray(strings, 0);
                    service.Port = Convert.ToInt32(TryGetValueByArray(strings,1,"0"));
                    service.Protocol = serviceRouteConfig.DownstreamScheme;
                    services.Add(service);
                }

                return services;
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

        public override void Refresh()
        {
            Initialize();
        }
    }
}