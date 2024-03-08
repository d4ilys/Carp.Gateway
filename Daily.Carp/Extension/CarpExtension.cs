using Daily.Carp.Configuration;
using Daily.Carp.Internel;
using Daily.Carp.Yarp;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using System.Linq;

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
            service.AddHttpClient("nossl").ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true
                };
            });

            ICarpBuilder builder = new CarpBuilder();
            builder.Service = service;
            builder.ProxyConfigProvider = new CarpProxyConfigProvider();

            var reverseProxyBuilder = service.AddReverseProxy()
                .LoadFormCustom(builder.ProxyConfigProvider);

            //扩展注入
            if (options != null)
            {
                CarpOptions option = new CarpOptions();

                options.Invoke(option);

                option.ReverseProxyBuilderInject?.Invoke(reverseProxyBuilder);
            }

            return builder;
        }

        //通过K8S加载
        internal static IReverseProxyBuilder LoadFormCustom(this IReverseProxyBuilder builder,
            IProxyConfigProvider configProvider)
        {
            builder.Services.AddSingleton<IProxyConfigProvider>(configProvider);
            return builder;
        }

        /// <summary>
        /// 普通集群方式
        /// </summary>
        /// <param name="builder"></param>
        public static void AddNormal(this ICarpBuilder builder)
        {
            builder.Service.AddHostedService(serviceProvider =>
                new NormalGenericHostedService(serviceProvider.GetService<IHost>(), serviceProvider, builder));
        }
    }


    public interface ICarpBuilder
    {
        public CarpProxyConfigProvider ProxyConfigProvider { get; set; }
        public IServiceCollection Service { get; set; }
    }

    public class CarpBuilder : ICarpBuilder
    {
        public CarpProxyConfigProvider ProxyConfigProvider { get; set; }

        public IServiceCollection Service { get; set; }
    }

    public class CarpOptions
    {
        public Action<IReverseProxyBuilder> ReverseProxyBuilderInject { get; set; } = null;
    }
}