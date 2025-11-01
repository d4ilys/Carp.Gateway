using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
            await FullLoad(serviceName =>
            {
                var carpConfig = CarpApp.GetCarpConfig();
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

        private static CarpConfig? _cacheCarpConfig = null;

        private void Watch()
        {
            ChangeToken.OnChange(CarpApp.Configuration.GetReloadToken, () =>
            {
                try
                {
                    if (_cacheCarpConfig == null)
                    {
                        _cacheCarpConfig = CarpApp.GetCarpConfig();
                        _ = Initialize();
                    }
                    else
                    {
                        var cacheConfig = JsonSerializer.Serialize(_cacheCarpConfig);
                        var latestConfig = JsonSerializer.Serialize(CarpApp.GetCarpConfig());
                        //判断是否更新
                        if (cacheConfig != latestConfig)
                        {
                            _cacheCarpConfig = CarpApp.GetCarpConfig();
                            _ = Initialize();
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                CarpApp.LogInfo($"{DateTime.Now}:Configuration updated..");
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
            await LocalLoad(name =>
            {
                var carpConfig = CarpApp.GetCarpConfig();
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