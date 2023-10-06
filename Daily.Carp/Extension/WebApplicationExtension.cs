using Daily.Carp.Internel;
using Daily.LinkTracking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Daily.Carp.Extension
{
    public static class WebApplicationExtension
    {
        public static WebApplicationBuilder InjectCarp(this WebApplicationBuilder builder)
        {
            CarpApp.Configuration = builder.Configuration;

            builder.Services.AddHostedService<GenericHostedService>();

            builder.Services.AddHttpContextAccessor();

            return builder;
        }

        public static WebApplication UseCarp(this WebApplication app, Action<CarpAppOptions>? options = null)
        {
            var optionsInternal = new CarpAppOptions();
            optionsInternal.App = app;
            options?.Invoke(optionsInternal);
            if (optionsInternal.EnableAuthentication)
            {
                 app.UseCarpAuthenticationMiddleware(optionsInternal);
            }

            app.MapReverseProxy();

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
        public Dictionary<string,Func<Task<bool>>> CustomAuthenticationAsync { get; set; } = new Dictionary<string, Func<Task<bool>>>();

        /// <summary>
        /// 是否开启权限验证
        /// </summary>
        public bool EnableAuthentication { get; set; } = false;

        /// <summary>
        /// 鉴权中心的地址
        /// </summary>
        public string AuthenticationCenter { get; set; } = string.Empty;

        public WebApplication App { get; set; }
    }
}