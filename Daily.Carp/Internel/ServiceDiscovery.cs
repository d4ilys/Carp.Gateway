using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Daily.Carp.Internel
{
    public class ServiceDiscovery
    {
        public static IServiceCollection Services { get; set; } = new ServiceCollection();

        public static IServiceProvider ServiceProvider { get; set; }


        public static void BuildServiceProvider()
        {
            ServiceProvider = Services.BuildServiceProvider();
        }

        public static T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }
    }
}