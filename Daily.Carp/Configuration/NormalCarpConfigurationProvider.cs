using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daily.Carp.Configuration
{
    internal class NormalCarpConfigurationProvider:BaseCarpConfigurationProvider
    {
        public override void Initialize()
        {
            var carpConfig = GetCarpConfig();
            Refresh((name, provider) =>
            {
                var downstreamHostAndPorts = carpConfig.Routes.Where(c => c.ServiceName  == name).First().DownstreamHostAndPorts;
                return downstreamHostAndPorts;
            });

        }
    }
}
