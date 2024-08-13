using Consul;
using Daily.Carp.Configuration;
using Daily.Carp.Feature;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;
using Daily.Carp.Yarp;

namespace Daily.Carp.Provider.Consul
{
    internal class ConsulCarpConfigurationActivator : CarpConfigurationActivator
    {
        public sealed override async Task Initialize()
        {
            await Refresh(string.Empty);
            TimingUpdate();
        }


        public override async Task Refresh(string serviceName)
        {
            await FullLoad(GetServices);
        }

        private void TimingUpdate()
        {
            var timer = new Timer();
            timer.Interval = CarpApp.GetCarpConfig().Consul.Interval;
            timer.Elapsed += (sender, eventArgs) => { _ = Refresh(string.Empty); };
            timer.Start();
        }

        private async Task<IList<Service>> GetServices(string serviceName)
        {
            IList<Service> services = new List<Service>();
            var client = CarpApp.GetRootService<IConsulClientFactory>()?.Get();
            var queryResult = await client?.Health.Service(serviceName, string.Empty, true)!;
            foreach (var serviceEntry in queryResult.Response)
            {
                try
                {
                    if (IsValid(serviceEntry))
                    {
                        var nodes = await client?.Catalog.Nodes();
                        if (nodes?.Response == null)
                        {
                            services.Add(BuildService(serviceEntry, null));
                        }
                        else
                        {
                            var serviceNode =
                                nodes.Response.FirstOrDefault(n => n.Address == serviceEntry.Service.Address);
                            services.Add(BuildService(serviceEntry, serviceNode));
                        }
                    }
                }
                catch (Exception e)
                {
                    continue;
                }
            }

            return services;
        }

        private Service BuildService(ServiceEntry serviceEntry, Node? serviceNode)
        {
            var services = new Service
            {
                Host = serviceNode == null ? serviceEntry.Service.Address : serviceNode.Name,
                Port = serviceEntry.Service.Port
            };

            return services;
        }

        private bool IsValid(ServiceEntry serviceEntry)
        {
            if (string.IsNullOrEmpty(serviceEntry.Service.Address) ||
                serviceEntry.Service.Address.Contains("http://") || serviceEntry.Service.Address.Contains("https://") ||
                serviceEntry.Service.Port <= 0)
            {
                return false;
            }

            return true;
        }
    }
}