using Daily.Carp.Extension;
using Yarp.ReverseProxy.Transforms;

namespace GrpcGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddCarp(options =>
            {
                //��չYARP ����Grpc
                options.ReverseProxyBuilderInject = proxyBuilder =>
                {
                    proxyBuilder.AddTransforms(context =>
                    {
                        context.AddRequestTransform(transformContext =>
                        {
                            if (transformContext.Path.HasValue)
                            {
                                var routers = transformContext.Path.Value.Split("/");
                                if (routers[2].Contains("grpc", StringComparison.OrdinalIgnoreCase))
                                {
                                    var newUrl = $"/{string.Join("/", routers.Skip(2))}";
                                    transformContext.Path = newUrl;
                                }
                            }

                            return ValueTask.CompletedTask;
                        });
                    });
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseCarp();

            app.Run("https://*:7212");
        }
    }
}