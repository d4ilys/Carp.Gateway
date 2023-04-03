using Com.Ctrip.Framework.Apollo;
using Com.Ctrip.Framework.Apollo.Core;
using Daily.Carp.Extension;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.


builder.Configuration.AddApollo(builder.Configuration.GetSection("Apollo"))
    .AddDefault()
    .AddNamespace(ConfigConsts.NamespaceApplication);


builder.Services.AddCarp().AddKubernetes();

builder.Services.AddControllers();

#region 支持跨域  所有的Api都支持跨域

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.SetIsOriginAllowed((x) => true)
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

#endregion 支持跨域  所有的Api都支持跨域

builder.WebHost.UseKestrel(options =>
{
    var x509ca = new X509Certificate2(File.ReadAllBytes(@"jtys.cqyt.petrochina.pfx"));
    options.ListenAnyIP(6005, listenOptions => listenOptions.UseHttps(x509ca));
});

var app = builder.Build();

app.UseStaticFiles();

app.UseCors("CorsPolicy");

app.UseCarp(options =>
{
    options.AuthenticationCenter = builder.Configuration["AuthenticationCenter_Url"];  //认证中心的地址
    options.Enable = true; //启用权限验证
});

app.MapControllers();

app.Run();