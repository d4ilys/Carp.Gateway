using Daily.Carp.Internel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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

        public static WebApplication UseCarp(this WebApplication app)
        {
            app.MapReverseProxy();

            return app;
        }
    }
}