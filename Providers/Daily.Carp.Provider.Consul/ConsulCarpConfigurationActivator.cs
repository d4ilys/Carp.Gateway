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
        private static readonly object lock_obj = new object();

        public sealed override async Task Initialize()
        {
            await RefreshAll();
            TimingUpdate();
        }

        public async Task RefreshAll()
        {
            await FullLoad(GetServices);
        }

        public override Task Refresh(string serviceName)
        {
            throw new NotImplementedException();
        }

        //为了防止其他状况 1分钟同步一次配置
        private void TimingUpdate()
        {
            Task.Run(() =>
            {
                var timer = new Timer();
                timer.Interval = CarpApp.GetCarpConfig().Consul.Interval;
                timer.Elapsed += (sender, eventArgs) => { RefreshAll(); };
                timer.Start();
            });
        }

        private Task<IList<Service>> GetServices(string serviceName)
        {
            lock (lock_obj)
            {
                IList<Service> services = new List<Service>();
                var client = GetService<IConsulClientFactory>()?.Get();
                var queryResult = client?.Health.Service(serviceName, string.Empty, true).ConfigureAwait(true)
                    .GetAwaiter().GetResult();
                foreach (var serviceEntry in queryResult.Response)
                {
                    try
                    {
                        if (IsValid(serviceEntry))
                        {
                            var nodes = client?.Catalog.Nodes().ConfigureAwait(true).GetAwaiter().GetResult();
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

                return Task.FromResult(services);
            }
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