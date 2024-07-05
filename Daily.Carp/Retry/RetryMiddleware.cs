using Daily.Carp.Extension;
using Daily.Carp.Feature;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Yarp.ReverseProxy.Model;

namespace Daily.Carp.Retry;

/// <summary>
/// 重试中间件
/// </summary>
public class RetryMiddleware
{
    /// <summary>
    /// 重试次数
    /// </summary>
    public static readonly AsyncLocal<int> RetryCount = new AsyncLocal<int>();

    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next"></param>
    public RetryMiddleware(RequestDelegate next)
    {
        _next = next;
        RetryCount.Value = 0;
    }

    /// <summary>
    /// Handle request
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        await _next(context);

        await RetryTrigger(context);

    }

    private async Task RetryTrigger(HttpContext context)
    {
        if (context.Response.StatusCode >= 400)
        {
            var carpReverseProxyFeature = context.GetCarpReverseProxyFeature();

            if (carpReverseProxyFeature.CarpRouteConfig?.RetryPolicy != null)
            {
                await Retry(context, carpReverseProxyFeature);
            }
        }
    }


    //重试逻辑方法
    private async Task Retry(HttpContext context, CarpReverseProxyFeature carpReverseProxyFeature)
    {
        //如果没有配置重试策略，则直接返回
        if (carpReverseProxyFeature.CarpRouteConfig is { RetryPolicy: null })
        {
            return;
        }

        var retryPolicy = carpReverseProxyFeature.CarpRouteConfig!.RetryPolicy;

        //如果重试次数大于配置的重试次数，则直接返回
        if (RetryCount.Value >= retryPolicy.RetryCount)
        {
            CarpApp.LogInfo($"Trigger retry : {carpReverseProxyFeature.YarpReverseProxyFeature?.ProxiedDestination?.DestinationId} , The number of retries reached the upper limit. Procedure.");
            return;
        }

        //重试次数+1
        RetryCount.Value++;

        //获取当前状态码
        var statusCode = context.Response.StatusCode;

        //范围
        var statueCodeRange = (statusCode / 100) + "xx";

        //需要重试的状态码
        var statueCodes = new List<string>(2)
        {
            statusCode.ToString(),
            statueCodeRange
        };

        //用户是否配置指定状态码
        var isConfigRetryOnStatusCodes = retryPolicy.RetryOnStatusCodes.Count == 0;

        var isRetry = false;

        //如果没有配置重试状态码
        if (isConfigRetryOnStatusCodes)
        {
            // 如果当前状态码大于等于500，则进行重试
            isRetry = statusCode >= 500;
        }
        else
        {
            //如果当前状态码在重试状态码列表中，则进行重试
            isRetry = retryPolicy.RetryOnStatusCodes.Any(s => statueCodes.Contains(s));
        }

        if (isRetry)
        {

            //获取到YarpReverseProxyFeature
            var reverseProxyFeature = carpReverseProxyFeature.YarpReverseProxyFeature;
            if (reverseProxyFeature != null)
            {
                //获取可用的下端地址
                var available = reverseProxyFeature.AvailableDestinations;

                //排查错误地址，获取健康的目标
                var healthyDestinations = available
                    .Where(m => m != reverseProxyFeature.ProxiedDestination)
                    .ToList();

                if (healthyDestinations.Count == 0)
                {
                    return;
                }

                //更新可用目标
                reverseProxyFeature.AvailableDestinations = healthyDestinations;

                //重置请求流
                context.Request.Body.Position = 0;
                context.Response.Clear();
                reverseProxyFeature.ProxiedDestination = null;

                CarpApp.LogInfo($"Trigger retry {RetryCount.Value} : {context.Request.GetDisplayUrl()}");

                //重新调用中间件
                await _next(context);

                //触发重试逻辑
                await RetryTrigger(context);
            }
        }
    }
}