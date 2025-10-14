using k8s;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms;

namespace Daily.Carp.Kubernetes.IngressController
{
    /// <summary>
    /// 扩展
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 添加Kubernetes Ingress Controller
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKubernetesIngressController(this IServiceCollection services)
        {
            // 优先使用集群内配置，如果没有则使用 kubeconfig 文件
            KubernetesClientConfiguration config;
            try
            {
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            catch
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }

            services.AddMemoryCache();
            services.AddSingleton<IKubernetes>(new k8s.Kubernetes(config));
            services.AddSingleton<IngressToYarpConfigConverter>();

            // 添加 YARP 反向代理
            services.AddReverseProxy()
                .LoadFromMemory(new List<Yarp.ReverseProxy.Configuration.RouteConfig>(),
                    new List<Yarp.ReverseProxy.Configuration.ClusterConfig>())
                .AddTransforms(transformBuilderContext =>
                {
                    // 添加通用转换规则
                    transformBuilderContext.AddXForwarded();
                    transformBuilderContext.AddXForwardedFor();
                    transformBuilderContext.AddXForwardedHost();
                    transformBuilderContext.AddXForwardedProto();
                });

            // 添加 Ingress 控制器服务
            services.AddHostedService<IngressControllerHostedService>();

            // 添加配置同步服务
            services.AddSingleton<ProxyConfigProvider>();

            return services;
        }
    }
}