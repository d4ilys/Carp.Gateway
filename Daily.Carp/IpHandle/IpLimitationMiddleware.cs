using System.Text;
using Daily.Carp.Extension;
using Microsoft.AspNetCore.Http;

namespace Daily.Carp.IpHandle;

/// <summary>
/// IP限制中间件
/// </summary>
public class IpLimitationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next"></param>
    public IpLimitationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Handle request
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    {
        var carpReverseProxyFeature = context.GetCarpReverseProxyFeature();
        var config = carpReverseProxyFeature.CarpRouteConfig;
        var ip = GetIpAddress(context);

        switch (config)
        {
            case { IpWhiteList: not null }:
            {
                //验证IP是否在白名单中
                if (config.IpWhiteList.Any(s => s == ip))
                {
                    await _next(context);
                }
                else
                {
                    await ResultMessageAsync(context, "no permission.");
                }

                break;
            }
            case { IpBlackList: not null }:
            {
                //验证IP是否在黑名单中
                if (config.IpBlackList.Any(s => s == ip))
                {
                    await ResultMessageAsync(context, "no permission.");
                }
                else
                {
                    await _next(context);
                }

                break;
            }
            default:
                await _next(context);
                break;
        }
    }
    private string? GetIpAddress(HttpContext context)
    {
        // 兼容反向代理 
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor;
        }

        return context.Connection?.RemoteIpAddress?.MapToIPv4()?.ToString();
    }

    private async Task ResultMessageAsync(HttpContext httpContext, string message)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.ContentType = "text/plain; charset=utf-8";
        //设置stream存放ResponseBody
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(message));
        await memoryStream.CopyToAsync(httpContext.Response.Body);
    }
}