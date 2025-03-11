using System.Text;
using Daily.Carp.Extension;
using Daily.Carp.Feature;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Daily.Carp.IpWhites;

/// <summary>
/// 重试中间件
/// </summary>
public class IpWhitesMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next"></param>
    public IpWhitesMiddleware(RequestDelegate next)
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
        var ip = context.Connection?.RemoteIpAddress?.MapToIPv4()?.ToString();
        if (config is { IpWhites: not null })
        {
            //验证IP是否在白名单中
            if (config.IpWhites.Any(s => s == ip))
            {
                await _next(context);
            }
            else
            {
                await ResultMessageAsync(context, "no permission.");
            }
        }
        else
        {
            await _next(context);
        }
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