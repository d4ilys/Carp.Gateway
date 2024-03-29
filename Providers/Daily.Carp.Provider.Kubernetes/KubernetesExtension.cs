﻿using Daily.Carp.Configuration;
using Daily.Carp.Extension;
using Daily.Carp.Internel;
using Daily.Carp.Provider.Kubernetes;
using KubeClient;
using KubeClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Daily.Carp.Extension
{
    public static class KubernetesExtension
    {
        /// <summary>
        /// Kubernetes服务集群
        /// </summary>
        /// <param name="builder"></param>
        public static void AddKubernetes(this ICarpBuilder builder)
        {
            builder.Service.AddKubeClient(true);
            builder.Service.AddHostedService(serviceProvider =>
                new KubernetesGenericHostedService(serviceProvider.GetService<IHost>(), serviceProvider, builder));
        }
    }
}