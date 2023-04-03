using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daily.Carp.Feature;
using Daily.Carp.Internel;

namespace Daily.Carp.Configuration
{
    internal class NormalCarpConfigurationProvider:BaseCarpConfigurationProvider
    {
        public override void Initialize()
        {
            var carpConfig = CarpApp.GetCarpConfig();
            Refresh((name, provider) =>
            {
                var services = new List<Service>();
                var serviceRouteConfig = carpConfig.Routes.Where(c => c.ServiceName  == name).First();
                foreach (var downstreamHostAndPort in serviceRouteConfig.DownstreamHostAndPorts)
                {
                    var service = new Service();
                    var strings = downstreamHostAndPort.Split(":");
                    service.Host = strings[0];
                    service.Port = Convert.ToInt32(strings[1]);
                    service.Protocol = serviceRouteConfig.DownstreamScheme;
                    services.Add(service);
                }
                return services;
            });

        }
    }
}
