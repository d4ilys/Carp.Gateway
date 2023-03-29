**前言**

说到 .NET Core API Gateway 首先想到的应该是 Ocelot，生态十分成熟，支持 Kubernetes、Consul、Eureka等服务注册发现的中间件 支持Polly 进行 熔断、降级、重试等，功能十分的强大，但是在.NET 5问世后，作者貌似已经停止维护此项目.

由于我们项目一直在使用Ocelot作为网关 而且已经升级到 .Net 7 基于现状 我们计划重新设计开发一个网关，经过调研发现微软官方已经提供了一个反向代理的组件**YARP**

YARP 是微软团队开发的一个反向代理**组件**， 除了常规的 http 和 https 转换通讯，它最大的特点是**可定制化**，很容易根据特定场景开发出需要的定制代理通道。

源码仓库：https://github.com/microsoft/reverse-proxy

文档地址 ：https://microsoft.github.io/reverse-proxy/

经过几天的设计与编写，项目初版已经完成 其名为 **Carp** ，一个方面和Yarp有关系，另一个方面Carp在英文中是`鲤鱼`的意思，恰好本人比较热垂钓 哈哈 冥冥中自有天意，**需要注意的是 本项目还没用于生产环境进行测试，轻谨慎使用，作者也会持续更新**。

Ocelot 每次负载均衡请求 Kubernertes Pod时，需要先调用一遍API Server，在我看来会对Kubernetes集群造成影响。

和Ocelot不同的是，Carp 会在项目启动的时候就把Service-Pod信息初始化完毕，采取观察者模式监控Pod的创建与删除 动态更新Pods信息 这样就避免了每次转发都需要请求API Server的问题

需要注意的是，在Kubernetes 中需要再ServiceAccount 中增加 pods 的权限，Carp才能实时监控Pod的事件信息

![1d7b5ed2623bf5349b8e148947bec5d](https://user-images.githubusercontent.com/54463101/228444662-a3b03a25-2a62-40e2-a068-a711de124535.png)

> 适配Kubernetes

~~~C#
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddKubernetes();  //添加K8s支持

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();	//添加中间件

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
        "DownstreamScheme": "http"

      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
        "PermissionsValidation": true,
        "PathTemplate": "/Business/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      }
    ] 
  }
~~~

> 普通代理模式

~~~c#
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddNormal();

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

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

