#### **前言**

说到 .NET Core API Gateway 首先想到的应该是 Ocelot，生态十分成熟，支持 Kubernetes、Consul、Eureka等服务注册发现的中间件 支持Polly 进行 熔断、降级、重试等，功能十分的强大，但是在.NET 5问世后，作者貌似已经逐渐停止维护此项目.

由于我们项目一直在使用Ocelot作为网关 而且已经升级到 .Net 7 基于现状 我们计划重新设计开发一个网关，经过调研发现微软官方已经提供了一个反向代理的组件**YARP**

YARP 是微软团队开发的一个反向代理**组件**， 除了常规的 http 和 https 转换通讯，它最大的特点是**可定制化**，很容易根据特定场景开发出需要的定制代理通道。

源码仓库：https://github.com/microsoft/reverse-proxy

文档地址 ：https://microsoft.github.io/reverse-proxy/

经过几天的设计与编写，项目初版已经完成 其名为 **Carp** ，一个方面和Yarp有关系，另一个方面Carp在英文中是`鲤鱼`的意思，恰好本人比较热垂钓 哈哈 冥冥中自有天意，**需要注意的是 本项目还没用于生产环境进行测试，请谨慎使用，如果有兴趣可以添加我的QQ 963922242 进一步交流，我也会持续维护此项目**。

Carp是.NET Core 下生态的网关 统一对外提供API管理、鉴权、身份认证、Swagger集成 等

支持Kubernetes、Consul  支持负载均衡、反向代理

#### Quick Start 

* 创建 .NET 6.0 WebAPI

* NuGet 安装Carp.Gateway

~~~c#
Install-Package Carp.Gateway
~~~

* Program.cs

~~~C#
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();  //注入配置

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddNormal();  

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

app.Run();
~~~

* appsettings.json

~~~json
  "Carp": {
    "Routes": [
      {
        "Descriptions": "简单的例子",
        "ServiceName": "Demo",
        "PathTemplate": "/api/{**catch-all}",   //客户端请求路由
        "TransmitPathTemplate": "{**catch-all}",  //下游转发路由
        "DownstreamHostAndPorts": [ "www.baidu.com", "www.jd.com" ]
      }
    ]
  }
~~~

* 运行项目观看效果把~

#### Kubernetes

Ocelot 每次负载均衡请求 Kubernertes Pod时，需要先调用一遍API Server，在我看来会对Kubernetes集群造成影响。

和Ocelot不同的是，Carp 会在项目启动的时候就把Service-Pod信息初始化完毕，采取观察者模式监控Pod的创建与删除 动态更新Pods信息 这样就避免了每次转发都需要请求API Server的问题

需要注意的是，在Kubernetes 中需要再ServiceAccount 中增加 pods 的权限，Carp才能实时监控Pod的事件信息

![1d7b5ed2623bf5349b8e148947bec5d](https://user-images.githubusercontent.com/54463101/228444662-a3b03a25-2a62-40e2-a068-a711de124535.png)

> 适配Kubernetes

~~~shell
Install-Package Carp.Gateway.Provider.Kubernetes
~~~

~~~C#
using Com.Ctrip.Framework.Apollo;
using Com.Ctrip.Framework.Apollo.Core;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

builder.Services.AddCarp().AddKubernetes();  //添加Kubernetes支持

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();

app.UseCarp(options =>
{
    options.AuthenticationCenter = "http://localhost:5000";  //认证中心的地址
    options.EnableAuthentication = true; //启用权限验证
});

app.MapControllers();

app.Run("http://*:6005");
~~~

~~~json
 "Carp": {
    "Kubernetes": {
      "Namespace": "dev"
    },
    "Routes": [
      {
        "Descriptions": "基础服务集群",
        "ServiceName": "basics",
        "PermissionsValidation": true,
        "PathTemplate": "/Basics/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
        "PermissionsValidation": true,
        "PathTemplate": "/Business/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "登录服务集群",
        "ServiceName": "lgcenter",
        "PermissionsValidation": false, //登录服务不用开启鉴权
        "PathTemplate": "/Login/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "日志服务的集群",
        "ServiceName": "logs",
        "PermissionsValidation": false,
        "PathTemplate": "/Log/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "App服务的集群",
        "ServiceName": "appservice",
        "PermissionsValidation": true,
        "PathTemplate": "/AppService/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      }
    ]
  }
~~~

#### Consul

~~~c#
using Com.Ctrip.Framework.Apollo;
using Com.Ctrip.Framework.Apollo.Core;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

builder.Services.AddCarp().AddConsul();  //添加Consul支持

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();

app.UseCarp(options =>
{
    options.AuthenticationCenter = "http://localhost:5000";  //认证中心的地址
    options.EnableAuthentication = true; //启用权限验证
});

app.MapControllers();

app.Run("http://*:6005");
~~~

~~~json
  "Carp": {
    "Consul": {
      "Host": "localhost",
      "Port": 8500,
      "Protocol": "http",
      "Token": "",
      "Interval": 2000   //轮询查询更新Consul Service信息 ，默认3秒 单位毫秒
    },
    "Routes": [
      {
        "Descriptions": "简单的例子",
        "ServiceName": "DemoService",
        "LoadBalancerOptions": "RoundRobin",
        "PathTemplate": "basics/{**catch-all}"
      }
    ]
  }
~~~

#### 普通代理模式

~~~shell
Install-Package Carp.Gateway
~~~

~~~c#
using Com.Ctrip.Framework.Apollo;
using Com.Ctrip.Framework.Apollo.Core;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

builder.Services.AddCarp().AddNormal();  //普通代理

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();

app.UseCarp(options =>
{
    options.AuthenticationCenter = "http://localhost:5000";  //认证中心的地址
    options.EnableAuthentication = true; //启用权限验证
});

app.MapControllers();

app.Run("http://*:6005");
~~~

~~~json
"Carp": {
    "Namespace": "dev",
    "Routes": [
      {
        "Descriptions": "基础服务集群",
        "ServiceName": "basics",
        "PermissionsValidation": true,
        "PathTemplate": "/Basics/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts" : [ "192.168.0.112:8001","192.168.0.113:8001"]

      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
        "PermissionsValidation": true,
        "PathTemplate": "/Business/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts" : [ "192.168.0.114:8001","192.168.0.115:8001"]
      }
    ] 
  }
~~~

#### 集成Swagger

推荐使用SwaggerUI库 - Knife4jUI ，体验会更好

[IGeekFan.AspNetCore.Knife4jUI](https://github.com/luoyunchong/IGeekFan.AspNetCore.Knife4jUI)

~~~powershell

# 安装Swagger
Install-Package Swashbuckle.AspNetCore

#这是IGeekFan版本
Install-Package IGeekFan.AspNetCore.Knife4jUI

#这是我自己修改版，支持了鉴权、身份认证、各种优化，以下代码基于我的修改版
Install-Package AspNetCore.Knife4jUI

~~~

~~~JSON

//以下DEMO基于Kubernetes
{
  "Carp": {
        "Kubernetes": {
          "Namespace": "test"
        },
        "Routes": [
          {
            "Descriptions": "基础服务集群",
            "ServiceName": "basics",
            "PermissionsValidation": true,
            "PathTemplate": "/Basics/{**catch-all}",
            "LoadBalancerOptions": "PowerOfTwoChoices",
            "DownstreamScheme": "http"
          },
          {
            "Descriptions": "主业务服务集群",
            "ServiceName": "business",
            "PermissionsValidation": true,
            "PathTemplate": "/Business/{**catch-all}",
            "LoadBalancerOptions": "PowerOfTwoChoices",
            "DownstreamScheme": "http"
          },
          // 如果每个服务的Swagger路由都是默认，需要在网关中配置Swagger
          // 例如你的 Basics 服务中的 Swagger地址为swagger/v1/swagger.json
          // Business地址也是swagger/v1/swagger.json
          // 这样就需要以下配置
          // 如果Swagger.json地址按服务路由配置则不用。
          // business/swagger/v1/swagger.json
          // basics/swagger/v1/swagger.json
          {
            "Descriptions": "基础服务Swagger",
            "ServiceName": "basics",
            "PermissionsValidation": false,
            "PathTemplate": "/Basics-Swagger/{**remainder}",
            "TransmitPathTemplate": "{**remainder}", 
            "LoadBalancerOptions": "PowerOfTwoChoices",
            "DownstreamScheme": "http"
          },
          {
            "Descriptions": "主业务服务Swagger",
            "ServiceName": "business",
            "PermissionsValidation": false,
            "PathTemplate": "/Business-Swagger/{**remainder}",
            "TransmitPathTemplate": "{**remainder}", 
            "LoadBalancerOptions": "PowerOfTwoChoices",
            "DownstreamScheme": "http"
          }
        ]
      }
}

~~~

~~~c#

using Daily.Carp.Extension;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Data;
using Daily.Carp;
using IGeekFan.AspNetCore.Knife4jUI;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

//添加Carp配置
builder.Services.AddCarp().AddKubernetes();

builder.Services.AddControllers();

//支持跨域

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

//添加Swagger配置
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CARP Gateway API", Version = "v1" });
    c.CustomOperationIds(apiDesc =>
    {
        var controllerAction = apiDesc.ActionDescriptor as ControllerActionDescriptor;
        return controllerAction.ControllerName + "-" + controllerAction.ActionName;
    });
    c.CustomSchemaIds(type => type.FullName);       //swagger支持动态生成的api接口

});

var app = builder.Build();

app.UseStaticFiles();

app.UseCors("CorsPolicy");

//根据Carp配置路由信息形成集合
//[{
//    "name": "Basics API",
//    "url": "Basics-Swagger/swagger/v1/swagger.json"
//},
//{
//    "name": "Business API",
//    "url": "Business-Swagger/swagger/v1/swagger.json"
//}]

var swaggers = JsonConvert.DeserializeObject<List<SwaggerJson>>("上面的JSON数组");

app.UseKnife4UI(c =>
{
    
    c.Authentication = true; //开启鉴权
    c.Password = "daily";   //设置密码
    swaggers.ForEach(sj =>
    {
	c.SwaggerEndpoint(sj,url, sj.name);
    });
 
});

app.MapControllers();

app.Run();

~~~

![image](https://github.com/luoyunchong/IGeekFan.AspNetCore.Knife4jUI/assets/54463101/d011c6c1-e782-49e3-95d0-9de35a2f9fe4)

* 如果你的Swagger需要暴漏在外网 可以开启密码认证 - 以下是开启Swagger鉴权效果

![image](https://github.com/d4ilys/Daily.ASPNETCore.Mini/assets/54463101/93f21178-ff24-4279-8473-08711091087b)

* 如果直接访问JSON文件 不输入密码直接401

![image](https://github.com/d4ilys/Daily.ASPNETCore.Mini/assets/54463101/38755d91-db29-44eb-ad6b-dba2a1837940)

#### 认证中心

Demos-AUC文件夹中已经提供鉴权中心的Demo

