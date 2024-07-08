using Daily.Carp.Extension;
using Daily.Carp.Provider.Kubernetes;
using KubeClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var path = Path.Combine(AppContext.BaseDirectory, "admin.conf");
// 通过配置文件
KubeClientOptions clientOptions = K8sConfig.Load(path).ToKubeClientOptions(
    defaultKubeNamespace: "default"
);

builder.Services.AddCarp().AddKubernetes(KubeDiscoveryType.EndPoint, clientOptions);

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