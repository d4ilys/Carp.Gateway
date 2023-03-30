using Daily.Carp.Configuration;
using Daily.Carp.Internel;
using Daily.Carp.Yarp;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Yarp.ReverseProxy.Configuration;

namespace Daily.Carp.Extension
{
    public static class CarpExtension
    {
        public static ICarpBuilder AddCarp(this IServiceCollection service)
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
            ServiceDiscovery.Services.AddSingleton(builder.ProxyConfigProvider);
            service.AddReverseProxy()
                .LoadFormKubernetes(builder.ProxyConfigProvider);
            return builder;
        }

        //通过K8S加载
        internal static IReverseProxyBuilder LoadFormKubernetes(this IReverseProxyBuilder builder,
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
            var provider = new NormalCarpConfigurationProvider();
            provider.Initialize();
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
}