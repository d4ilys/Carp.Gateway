using Daily.Carp.Extension;
using Daily.Carp.Provider.Kubernetes;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

builder.Services.AddCarp().AddKubernetes(KubeDiscoveryType.EndPoint);

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


var app = builder.Build();

app.UseStaticFiles();

app.UseCors("CorsPolicy");

app.UseCarp(options =>
{
    options.EnableAuthentication = true; //启用权限验证
    options.CustomAuthenticationAsync.Add("Jwt", async () => //这里的 “Jwt” 对应的是配置文件中的PermissionsValidation数组中的值
    {
        var flag = true;
        //验证逻辑
        flag = false;
        //.....
        return await Task.FromResult(flag);
    });
});

app.MapControllers();

app.Run("http://*:6005");