using Daily.Carp.Configuration;
using Daily.Carp.Internal;
using Daily.Carp.Yarp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Configuration;

namespace Daily.Carp.Extension
{
    /// <summary>
    /// 扩展
    /// </summary>
    public static class CarpExtension
    {
        /// <summary>
        /// 添加Carp IOC注册
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ICarpBuilder AddCarp(this IServiceCollection service, Action<CarpOptions>? options = null)
        {
            //HttpClientFactory
            service.AddHttpClient("IgnoreSsl").ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true
                };
            });

            service.AddHttpContextAccessor();

            ICarpBuilder builder = new CarpBuilder();
            builder.Service = service;
            builder.ProxyConfigProvider = new CarpProxyConfigProvider();
            service.AddSingleton(builder.ProxyConfigProvider);
            var reverseProxyBuilder = service.AddReverseProxy()
                .LoadFormCustom(builder.ProxyConfigProvider);

            var carpOptions = new CarpOptions();
            //扩展注入
            if (options != null)
            {
                options.Invoke(carpOptions);

                carpOptions.ReverseProxyBuilderInject?.Invoke(reverseProxyBuilder);
            }

            builder.Service.AddSingleton(provider =>
            {
                CarpConfigurationActivator activator = new NormalCarpConfigurationActivator();
                return activator;
            });

            builder.HostedServiceDelegate = async provider =>
            {
                if (carpOptions.CarpConfigDelegate != null)
                {
                    CarpApp.SetCarpConfigFunc(() => carpOptions.CarpConfigDelegate(provider));
                }

                if (carpOptions.CarpConfig != null)
                {
                    CarpApp.SetCarpConfigFunc(() => carpOptions.CarpConfig);
                }

                var activator = provider.GetService<CarpConfigurationActivator>();
                await activator!.Initialize();
            };

            builder.Service.AddHostedService(serviceProvider => new CarpHostedService(serviceProvider, builder));

            return builder;
        }

        //通过K8S加载
        private static IReverseProxyBuilder LoadFormCustom(this IReverseProxyBuilder builder,
            IProxyConfigProvider configProvider)
        {
            builder.Services.AddSingleton<IProxyConfigProvider>(configProvider);
            return builder;
        }
    }


    public interface ICarpBuilder
    {
        public CarpProxyConfigProvider ProxyConfigProvider { get; set; }

        public IServiceCollection Service { get; set; }

        /// <summary>
        /// 服务启动后执行动作
        /// </summary>
        public Func<IServiceProvider, Task>? HostedServiceDelegate { get; set; }
    }

    public class CarpBuilder : ICarpBuilder
    {
        public CarpProxyConfigProvider ProxyConfigProvider { get; set; }

        public IServiceCollection Service { get; set; }

        public Func<IServiceProvider, Task>? HostedServiceDelegate { get; set; }
    }

    public class CarpOptions
    {
        public Action<IReverseProxyBuilder> ReverseProxyBuilderInject { get; set; } = null;

        /// <summary>
        ///  自定义配置委托，每次获取都会执行该委托
        /// </summary>
        public Func<IServiceProvider, CarpConfig>? CarpConfigDelegate { get; set; } = null;

        /// <summary>
        /// 自定义配置
        /// </summary>
        public CarpConfig? CarpConfig { get; set; } = null;
    }
}