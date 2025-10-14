using k8s;
using k8s.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Daily.Carp.Kubernetes.IngressController;

/// <summary>
/// 启动执行 
/// </summary>
public class IngressControllerHostedService(
    ILogger<IngressControllerHostedService> logger,
    k8s.Kubernetes client,
    IngressToYarpConfigConverter yarpConverter,
    IMemoryCache memoryCache,
    ProxyConfigProvider proxyConfigProvider)
    : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private bool _disposed = false;
    private Task? _watcherTask;
    private const string IngressClassName = "ingress-carp";

    /// <summary>
    /// 启动
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Ingress Controller service");

        // 启动 Ingress 资源监听器
        _watcherTask = WatchIngressResourcesAsync(_cts.Token);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Ingress Controller service");
        await _cts.CancelAsync();
        if (_watcherTask != null)
        {
            await Task.WhenAny(_watcherTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    private bool ValidateIngressClass(V1Ingress ingress)
    {
        var v1IngressClass = memoryCache.GetOrCreate(ingress.Spec.IngressClassName,
            entry => client.ReadIngressClass(ingress.Spec.IngressClassName));
        return v1IngressClass?.Spec.Controller == IngressClassName;
    }

    private async Task WatchIngressResourcesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 首先获取当前所有的 Ingress 资源
            var ingresses =
                await client.ListIngressForAllNamespacesAsync(cancellationToken: cancellationToken);

            //过滤 出当前 IngressClassName 为 carp 的 Ingress 资源，并更新Yarp配置
            UpdateProxyConfig(ingresses.Items.Where(v => v.Spec.IngressClassName == IngressClassName));

            // 监听 Ingress 资源的变化
            client.WatchListIngressForAllNamespaces(onEvent: (type, ingress) =>
            {
                // 判断当前 Ingress 是否属于 Carp
                if (ingress.Spec.IngressClassName == IngressClassName)
                {
                    HandleIngressEvent(type, ingress);
                }
            });
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Ingress watcher was canceled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Ingress watcher");
        }
    }

    private void HandleIngressEvent(WatchEventType type, V1Ingress ingress)
    {
        logger.LogInformation(
            $"Received {type} event for Ingress {ingress.Metadata.NamespaceProperty}/{ingress.Metadata.Name}");

        // 获取当前所有的 Ingress 资源并更新代理配置
        var task = client.ListIngressForAllNamespacesAsync();
        task.Wait();
        UpdateProxyConfig(task.Result.Items);
    }

    private void UpdateProxyConfig(IEnumerable<V1Ingress> ingresses)
    {
        try
        {
            // 将 Ingress 规则转换为 YARP 配置
            var (routes, clusters) = yarpConverter.Convert(ingresses);

            // 更新代理配置
            proxyConfigProvider.UpdateConfig(routes, clusters);

            logger.LogInformation(
                $"Updated proxy configuration with {routes.Count} routes and {clusters.Count} clusters");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update proxy configuration");
        }
    }

    /// <summary>
    ///  Dispose
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _cts.Dispose();
        }

        _disposed = true;
    }
}