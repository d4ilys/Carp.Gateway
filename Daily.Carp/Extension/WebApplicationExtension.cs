using Daily.Carp.Internel;
using Daily.LinkTracking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Daily.Carp.Retry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Daily.Carp.Extension
{
    public static class WebApplicationExtension
    {
        public static WebApplicationBuilder InjectCarp(this WebApplicationBuilder builder)
        {
            CarpApp.Configuration = builder.Configuration;

            builder.Services.AddHttpContextAccessor();

            return builder;
        }

        /// <summary>
        /// 代理中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static WebApplication UseCarp(this WebApplication app, Action<CarpAppOptions>? options = null)
        {
            //
            var optionsInternal = new CarpAppOptions
            {
                App = app
            };
            options?.Invoke(optionsInternal);
            if (optionsInternal.EnableAuthentication)
            {
                app.UseCarpAuthenticationMiddleware(optionsInternal);
            }

            app.MapReverseProxy(builder =>
            {
                builder.UseMiddleware<RetryMiddleware>();
                optionsInternal.ReverseConfigApp?.Invoke(builder);
            });

            return app;
        }
    }
    
    public class CarpAppOptions
    {
        ///// <summary>
        ///// 自定义鉴权过程
        ///// </summary>
        //public Func<bool>? CustomAuthentication { get; set; } = null;

        /// <summary>
        /// 自定义鉴权
        /// </summary>
        public Dictionary<string, Func<Task<bool>>> CustomAuthenticationAsync { get; set; } =
            new Dictionary<string, Func<Task<bool>>>();

        /// <summary>
        /// 是否开启权限验证
        /// </summary>
        public bool EnableAuthentication { get; set; } = false;

        /// <summary>
        /// 鉴权中心的地址
        /// </summary>
        public string AuthenticationCenter { get; set; } = string.Empty;

        public WebApplication App { get; set; }

        /// <summary>
        /// MapReverseProxy 参数
        /// </summary>
        public Action<IReverseProxyApplicationBuilder>? ReverseConfigApp { get; set; } = null;
    }
}