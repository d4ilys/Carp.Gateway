using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Daily.Carp
{
    public partial class CarpApp
    {
        /// <summary>
        /// Carp内置的IOC容器
        /// </summary>
        public static IServiceCollection Services { get; set; } = new ServiceCollection();

        /// <summary>
        /// Carp内置的容器提供者
        /// </summary>
        public static IServiceProvider ServiceProvider { get; set; }


        /// <summary>
        /// 生成Carp内置的容器提供者
        /// </summary>
        public static void BuildServiceProvider()
        {
            ServiceProvider = Services.BuildServiceProvider();
        }
        /// <summary>
        /// Carp内置的容器实例获取
        /// </summary>
        public static T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }
    }
}