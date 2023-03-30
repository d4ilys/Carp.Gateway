using Com.Ctrip.Framework.Apollo;
using Com.Ctrip.Framework.Apollo.Core;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.


builder.Configuration.AddApollo(builder.Configuration.GetSection("Apollo"))
    .AddDefault()
    .AddNamespace(ConfigConsts.NamespaceApplication);

builder.Services.AddCarp().AddKubernetes();

builder.Services.AddControllers();


var app = builder.Build();

app.UseStaticFiles();

app.UseCarp(options =>
{
    options.AuthenticationCenter = builder.Configuration["AuthenticationCenter_Url"];  //认证中心的地址
    options.Enable = true; //启用权限验证
});

app.MapControllers();

app.Run("http://*:6005");