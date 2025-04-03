using Daily.LinkTracking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Daily.Carp.IpHandle;
using Daily.Carp.Retry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Daily.Carp.Extension
{
    /// <summary>
    /// 
    /// </summary>
    public static class WebApplicationExtension
    {
        /// <summary>
        /// 代理中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static WebApplication UseCarp(this WebApplication app, Action<CarpAppOptions>? options = null)
        {
            var optionsInternal = new CarpAppOptions
            {
                App = app
            };

            options?.Invoke(optionsInternal);

            if (optionsInternal.CustomAuthenticationAsync.Any())
            {
                app.UseCarpAuthenticationMiddleware(optionsInternal);
            }

            app.MapReverseProxy(builder =>
            {
                builder.UseMiddleware<IpLimitationMiddleware>();
                builder.UseMiddleware<RetryMiddleware>();
                optionsInternal.ReverseConfigApp?.Invoke(builder);
            });

            CarpApp.SetRootServiceProvider(app.Services);

            CarpApp.Configuration = app.Services.GetService<IConfiguration>()!;

            return app;
        }
    }

    /// <summary>
    /// CarpAppOptions
    /// </summary>
    public class CarpAppOptions
    {
        /// <summary>
        /// 自定义鉴权
        /// </summary>
        public Dictionary<string, Func<Task<bool>>> CustomAuthenticationAsync { get; set; } =
            new Dictionary<string, Func<Task<bool>>>();

        public WebApplication App { get; set; }

        /// <summary>
        /// MapReverseProxy 参数
        /// </summary>
        public Action<IReverseProxyApplicationBuilder>? ReverseConfigApp { get; set; } = null;
    }
}