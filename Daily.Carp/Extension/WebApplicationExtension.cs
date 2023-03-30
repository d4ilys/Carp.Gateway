using Daily.Carp.Internel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

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
            optionsInternal.app = app;
            options?.Invoke(optionsInternal);
            //自定义访问
            if (optionsInternal.CustomAuthentication != null)
            {
                app.Use(async (context, next) =>
                {
                    var pass = optionsInternal.CustomAuthentication?.Invoke();

                    if (pass != null && pass == true)
                    {
                        await next();
                    }
                    else
                    {
                        //直接无权限
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "text/plain; charset=utf-8";
                        //设置stream存放ResponseBody
                        using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Unrequited..")))
                        {
                            await memoryStream.CopyToAsync(context.Response.Body);
                        }
                    }
                });
            }
            else
            {
                app.UseCarpAuthorization(optionsInternal);
            }

            app.MapReverseProxy();

            return app;
        }


        private static WebApplication UseCarpAuthorization(this WebApplication app, CarpAppOptions options)
        {
            app.Use(async (context, next) =>
            {
                var flag = true;

                try
                {
                    // 获取所有未成功验证的需求
                    var serviceName = "";
                    try
                    {
                        serviceName = context.Request.Path.Value.Split("/")[1];
                    }
                    catch
                    {
                    }

                    string GetTempServiceName(string temp)
                    {
                        var strings = temp.Split("/");
                        return temp.StartsWith("/") ? strings[1] : strings[0];
                    }

                    var needVerification =
                        CarpApp.CarpConfig.Routes.Any(c =>
                            c.PermissionsValidation && GetTempServiceName(c.ServiceName.ToLower()) == serviceName.ToLower());
                    //需要验证
                    if (needVerification)
                    {
                        var httpClientFactory = context.RequestServices.GetService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient("nossl");
                        var token = context.Request.Headers["Authorization"];
                        var asstoken = "";
                        if (!(token.Count == 0))
                        {
                            asstoken = token.ToString();
                            httpClient.DefaultRequestHeaders.Add("Authorization", asstoken);
                            //去鉴权中心 校验token是否合法
                            var httpResponseMessage =
                                await httpClient.GetAsync(
                                    $"{options.AuthenticationCenter}/connect/userinfo");
                            flag = httpResponseMessage.StatusCode == HttpStatusCode.OK;
                        }
                        else
                        {
                            flag = false;
                        }


                    }

                }
                catch
                {
                    flag = false;
                }
            
                if (flag)
                {
                    await next();
                }
                else
                {
                    //直接无权限
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    //设置stream存放ResponseBody
                    using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Unrequited..")))
                    {
                        await memoryStream.CopyToAsync(context.Response.Body);
                    }
                }
            });


            return app;
        }
    }

    public class CarpAppOptions
    {
        /// <summary>
        /// 自定义鉴权过程
        /// </summary>
        public Func<bool>? CustomAuthentication { get; set; } = null;

        /// <summary>
        /// 是否开启权限验证
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// 鉴权中心的地址
        /// </summary>
        public string AuthenticationCenter { get; set; } = string.Empty;


        public WebApplication app { get; set; }
    }
}