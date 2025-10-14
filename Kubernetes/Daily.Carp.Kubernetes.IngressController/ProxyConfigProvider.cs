using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Daily.Carp.Kubernetes.IngressController;

/// <summary>
/// 代理配置
/// </summary>
public class ProxyConfigProvider : IProxyConfigProvider
{
    private readonly IProxyConfig _config;
    private CancellationTokenSource _changeTokenSource = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProxyConfigProvider()
    {
        _config = new ProxyConfig(
            new List<RouteConfig>(),
            new List<ClusterConfig>(),
            _changeTokenSource.Token);
    }

    /// <summary>
    /// GetConfig
    /// </summary>
    /// <returns></returns>
    public IProxyConfig GetConfig() => _config;

    /// <summary>
    /// 更新代理配置
    /// </summary>
    /// <param name="routes"></param>
    /// <param name="clusters"></param>
    public void UpdateConfig(List<RouteConfig> routes, List<ClusterConfig> clusters)
    {
        // 创建新的配置
        var newConfig = new ProxyConfig(
            routes,
            clusters,
            _changeTokenSource.Token);

        // 更新配置并触发变更通知
        (_config as ProxyConfig)?.Update(routes, clusters);
        _changeTokenSource.Cancel();

        // 创建变更令牌源，以便下次更新
        _changeTokenSource.Dispose();
        _changeTokenSource = new CancellationTokenSource();
    }

    private class ProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters,
        CancellationToken changeToken) : IProxyConfig
    {
        private volatile IReadOnlyList<RouteConfig> _routes = routes;
        private volatile IReadOnlyList<ClusterConfig> _clusters = clusters;

        public IReadOnlyList<RouteConfig> Routes => _routes;

        public IReadOnlyList<ClusterConfig> Clusters => _clusters;

        public IChangeToken ChangeToken { get; } = new CancellationChangeToken(changeToken);

        public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            _routes = routes;
            _clusters = clusters;
        }
    }
}