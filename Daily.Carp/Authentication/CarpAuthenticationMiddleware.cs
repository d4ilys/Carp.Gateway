using Daily.Carp;
using Daily.Carp.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace Daily.LinkTracking
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    internal class CarpAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private CarpAppOptions _options;

        public CarpAuthenticationMiddleware(RequestDelegate next, CarpAppOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
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
                        c.PermissionsValidation &&
                        GetTempServiceName(c.ServiceName.ToLower()) == serviceName.ToLower());
                //需要验证
                if (needVerification)
                {
                    if (_options.CustomAuthentication != null)
                    {
                        flag = _options.CustomAuthentication.Invoke();
                    }
                    else
                    {
                        var httpClientFactory = context.RequestServices.GetService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient("nossl");
                        context.Request.Headers.TryGetValue("Authorization", out var token);
                        var asstoken = "";
                        if (token.Count != 0)
                        {
                            asstoken = token.ToString();
                            httpClient.DefaultRequestHeaders.Add("Authorization", asstoken);
                            //去鉴权中心 校验token是否合法
                            var httpResponseMessage =
                                await httpClient.GetAsync(
                                    $"{_options.AuthenticationCenter}/connect/userinfo");
                            flag = httpResponseMessage.StatusCode == HttpStatusCode.OK;
                        }
                        else
                        {
                            flag = false;
                        }
                    }

                  
                }
            }
            catch
            {
                flag = false;
            }

            if (flag)
            {
                await _next(context);
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
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseCarpAuthenticationMiddleware(this IApplicationBuilder builder,
            CarpAppOptions options)
        {
            return builder.UseMiddleware<CarpAuthenticationMiddleware>(options);
        }
    }
}