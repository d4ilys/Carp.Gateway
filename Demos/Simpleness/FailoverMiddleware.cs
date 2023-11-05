using Daily.Carp;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Simpleness
{
    // 统计平台的请求负载情况
    public class FailoverMiddleware
    {
        private readonly RequestDelegate _next;


        public FailoverMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            //如果是502则补偿
            if (context.Response.StatusCode == 502)
            {
                string serviceName;
                try
                {
                    serviceName = context.Request.Path.Value.Split("/")[1];
                    if (string.IsNullOrWhiteSpace(serviceName))
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    context.Response.StatusCode = 200;
                    return;
                }

                foreach (var i in Enumerable.Range(0, 3))
                {
                    var server = CarpApp.GetAddressByServiceName(serviceName);
                    var httpClientFactory = context.RequestServices.GetService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient();
                    try
                    {
                        using var httpMessage = new HttpRequestMessage();
                        var url = server.Trim().EndsWith("/") ? server.Remove(server.Length - 1) : server;
                        url = $"{url}{context.Request.Path}";

                        if (string.Equals("GET", context.Request.Method, StringComparison.InvariantCultureIgnoreCase))
                        {
                            httpMessage.Method = HttpMethod.Get;
                        }

                        if (string.Equals("POST", context.Request.Method, StringComparison.InvariantCultureIgnoreCase))
                        {
                            httpMessage.Method = HttpMethod.Post;
                            if (context.Request.ContentType == "application/json")
                            {
                                context.Request.EnableBuffering();
                                var stream = context.Request.Body;
                                var requestReader = new StreamReader(stream);
                                var bodyJson = await requestReader.ReadToEndAsync();
                                context.Request.Body.Position = 0;
                                var httpContent = new StringContent(bodyJson);
                                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                httpMessage.Content = httpContent;
                                httpContent.Headers.ContentType = new MediaTypeHeaderValue(context.Request.ContentType);
                            }

                            if (context.Request.ContentType != null &&
                                context.Request.ContentType.StartsWith("multipart/form-data"))
                            {
                                var form = await context.Request.ReadFormAsync();
                                var boundary = "---------------" + DateTime.Now.Ticks.ToString("x");
                                var httpContent = new MultipartFormDataContent(boundary);
                                foreach (var keyValuePair in form)
                                {
                                    httpContent.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                                }

                                foreach (var keyValuePair in form.Files)
                                {
                                    var stream = keyValuePair.OpenReadStream();
                                    var streamContent = new StreamContent(stream, Convert.ToInt32(stream.Length));
                                    streamContent.Headers.TryAddWithoutValidation("Content-Type", "application/octet-stream");
                                    httpContent.Add(streamContent,
                                        keyValuePair.Name, keyValuePair.FileName);
                                    stream.Position = 0;
                                }

                                httpContent.Headers.Remove("Content-Type");
                                httpContent.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
                                httpMessage.Content = httpContent;
                            }
                        }

                        var query = context.Request.QueryString.ToString();
                        if (!string.IsNullOrWhiteSpace(query))
                            url = $"{url}{query}";

                        httpMessage.RequestUri = new Uri(url);
                        //设置Header
                        foreach (var keyValuePair in context.Request.Headers)
                        {
                            if (httpMessage.Headers.Any(pair => pair.Key == keyValuePair.Key) == false)
                            {
                                if (keyValuePair.Key != "Content-Type" && keyValuePair.Key != "Content-Length")
                                {
                                    httpMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value.ToString());
                                }
                            }
                        }

                        var httpResponseMessage = await httpClient.SendAsync(httpMessage);
                        if (!httpResponseMessage.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        context.Response.StatusCode = 200;
                        var httpResponseContent = httpResponseMessage.Content;
                        await using var readAsStreamAsync = await httpResponseContent.ReadAsStreamAsync();
                        context.Response.ContentType = httpResponseContent.Headers.ContentType.MediaType;
                        //设置stream存放ResponseBody
                        await readAsStreamAsync.CopyToAsync(context.Response.Body);
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }

                    if (context.Response.StatusCode != 502)
                        break;
                    //重新请求一次
                }
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class FailoverMiddlewareExtensions
    {
        public static IApplicationBuilder UseFailover(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FailoverMiddleware>();
        }
    }
}