#### 目录

🍧 [**前言**](#前言)  <br />
✨ [Quick Start](#quick-start) <br />
☁️ [集成Kubernetes](#kubernetes) <br />
🍢 [集成Consul](#consul) <br />
⚓ [普通代理模式](#普通代理模式) <br />🎉 [GRPC](#GRPC) <br />👍 [WebSocket](#WebSocket) <br />🧊 [集成Swagger](#集成swagger) <br />

#### **前言**

说到 .NET Core API Gateway 首先想到的应该是 Ocelot，生态十分成熟，支持 Kubernetes、Consul、Eureka等服务注册发现的中间件 支持Polly 进行 熔断、降级、重试等，功能十分的强大，但是在.NET 5问世后，作者貌似已经逐渐停止维护此项目.

由于我们项目一直在使用Ocelot作为网关 而且已经升级到 .Net 7 基于现状 我们计划重新设计开发一个网关，经过调研发现微软官方已经提供了一个反向代理的组件**YARP**

Yarp 是微软团队开发的一个反向代理**组件**， 官方出品值得信赖 👍

源码仓库：https://github.com/microsoft/reverse-proxy

文档地址 ：https://microsoft.github.io/reverse-proxy/

如果有兴趣可以添加我的QQ963922242 进一步交流

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
        "DownstreamHostAndPorts": [ "https://www.baidu.com", "https://www.jd.com" ]
      }
    ]
  }
~~~

* 运行项目观看效果把~

#### Kubernetes

Ocelot 每次负载均衡请求 Kubernertes Pod时，需要先调用一遍API Server，在我看来会对Kubernetes集群造成影响。

和Ocelot不同的是，Carp 会在项目启动的时候就把Service 信息初始化完毕，采取观察者模式监控Pod的创建与删除 动态更新Service信息 这样就避免了每次转发都需要请求API Server

需要注意的是，在Kubernetes 中需要再ServiceAccount 中增加 pods、service、watch等权限，Carp才能实时监控Service的事件信息，**下方有完整的yaml实例**

![1d7b5ed2623bf5349b8e148947bec5d](https://user-images.githubusercontent.com/54463101/228444662-a3b03a25-2a62-40e2-a068-a711de124535.png)

> 适配Kubernetes

~~~shell
Install-Package Carp.Gateway.Provider.Kubernetes
~~~

~~~C#
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

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


var app = builder.Build();

app.UseStaticFiles();

app.UseCors("CorsPolicy");

app.UseCarp(options =>
{
    options.EnableAuthentication = true; //启用权限验证
    options.CustomAuthenticationAsync.Add("Jwt", async () => //这里的 “Jwt” 对应的是配置文件中的PermissionsValidation数组中的值
    {
        //自定义鉴权逻辑
        var flag = true;
        //验证逻辑
        flag = false;
        //.....
        return await Task.FromResult(flag);
    });
    
    //可以多个
    options.CustomAuthenticationAsync.Add("Other", async () => //这里的 “Jwt” 对应的是配置文件中的PermissionsValidation数组中的值
    {
        //自定义鉴权逻辑
        var flag = true;
        //验证逻辑
        flag = false;
        //.....
        return await Task.FromResult(flag);
    });
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
        "PermissionsValidation": ["Jwt","Other"],
        "PathTemplate": "/Basics/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
      "PermissionsValidation": ["Jwt"],
        "PathTemplate": "/Business/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "登录服务集群",
        "ServiceName": "lgcenter",
        "PathTemplate": "/Login/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "日志服务的集群",
        "ServiceName": "logs",
        "PermissionsValidation": ["Jwt"],
        "PathTemplate": "/Log/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      }
    ]
  }
~~~

> Kubernetes部署yaml文件参考

~~~yaml
kind: Deployment
apiVersion: apps/v1
metadata:
  name: gateway
  namespace: dev
  labels:
    app: gateway
spec:
  replicas: 2
  selector:
    matchLabels:
      app: gateway
  template:
    metadata:
      creationTimestamp: null
      labels:
        app: gateway
    spec:
      volumes:
        - name: app-conf
          configMap:
            name: sharedsettings
            defaultMode: 420
        - name: localtime
          hostPath:
            path: /usr/share/zoneinfo/Asia/Shanghai
            type: File
        - name: tmpdir
          emptyDir:
            medium: Memory
      containers:
        - name: gateway
          image: 192.168.1.1:8000/service/gateway:dev.20231005.18.42.41
          ports:
            - containerPort: 5107
              protocol: TCP
          resources:
            limits:
              cpu: '4'
              memory: 4Gi
            requests:
              cpu: 100m
              memory: 100Mi
          volumeMounts:
            - name: app-conf
              readOnly: true
              mountPath: /app/SharedConfig/appsettings.Shared.json
              subPath: appsettings.Shared.json
            - name: localtime
              readOnly: true
              mountPath: /etc/localtime
            - name: tmpdir
              mountPath: /tmp
          terminationMessagePath: /dev/termination-log
          terminationMessagePolicy: File
          imagePullPolicy: IfNotPresent
      restartPolicy: Always
      serviceAccount: gateway
      serviceAccountName : gateway
      terminationGracePeriodSeconds: 30
      dnsPolicy: ClusterFirst
      securityContext: {}
      imagePullSecrets:
        - name: harbor-admin-secret
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
            - weight: 100
              podAffinityTerm:
                labelSelector:
                  matchExpressions:
                    - key: app
                      operator: In
                      values:
                        - gateway
                topologyKey: kubernetes.io/hostname
      schedulerName: default-scheduler
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxUnavailable: 25%
      maxSurge: 25%
  revisionHistoryLimit: 10
  progressDeadlineSeconds: 600
---
apiVersion: v1
kind: Service
metadata:
  name: gateway
  namespace: dev
  labels:
    app: gateway
spec:
  type: NodePort
  ports:
  - port: 5107
    targetPort: 5107
    nodePort: 31000
  selector:
    app: gateway
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: gateway
  namespace: dev
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  creationTimestamp: null
  name: read-endpoints
  namespace: dev
rules:
- apiGroups:
  - ""
  resources:
  - endpoints
  - pods
  - service
  verbs:
  - get
  - list
  - watch
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  creationTimestamp: null
  name: permissive-binding
  namespace: dev
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: read-endpoints
subjects:
- kind: ServiceAccount
  name: gateway
  namespace: dev
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
   options.EnableAuthentication = true; //启用权限验证
    options.CustomAuthenticationAsync.Add("Jwt", async () => //这里的 “Jwt” 对应的是配置文件中的PermissionsValidation数组中的值
    {
        //自定义鉴权逻辑
        var flag = true;
        //验证逻辑
        flag = false;
        //.....
        return await Task.FromResult(flag);
    });
    
    //可以多个
    options.CustomAuthenticationAsync.Add("Other", async () => //这里的 “Jwt” 对应的是配置文件中的PermissionsValidation数组中的值
    {
        //自定义鉴权逻辑
        var flag = true;
        //验证逻辑
        flag = false;
        //.....
        return await Task.FromResult(flag);
    });
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
         "PermissionsValidation": ["Jwt","Other"],
        "PathTemplate": "/Basics/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts" : [ "192.168.0.112:8001","192.168.0.113:8001"]

      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
         "PermissionsValidation": ["Jwt"],  //具体验证逻辑在UseCarp中间件中
        "PathTemplate": "/Business/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts" : [ "192.168.0.114:8001","192.168.0.115:8001"]
      }
    ] 
  }

~~~

> 根据域名转发

~~~json

{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Carp": {
    "Routes": [
      {
        "Descriptions": "Jd域名转发",
        "ServiceName": "Jd",
        "PathTemplate": "{**catch-all}",
        "Hosts": [ "jd.daily.com" ],
        "TransmitPathTemplate": "{**catch-all}", 
        "DownstreamHostAndPorts": [ "https://jd.com" ]
      },
      {
        "Descriptions": "Baidu域名转发",
        "ServiceName": "Baidu",
        "PathTemplate": "{**catch-all}",
        "Hosts": [ "baidu.daily.com" ],
        "TransmitPathTemplate": "{**catch-all}", //下游转发路由  
        "DownstreamHostAndPorts": ["https://baidu.com" ]
      }    
    ] 
  },
  "AllowedHosts": "*"
} 
 

~~~

#### GRPC

在Demos/Grpc中有详细的例子

#### WebSocket

~~~JSON
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },

  "Carp": {
    "Routes": [
      {
        "Descriptions": "简单的例子",
        "ServiceName": "Basics",
        "PathTemplate": "/Basics/{**catch-all}", //客户端请求路由
        "PermissionsValidation": [ "Jwt" ],
        "TransmitPathTemplate": "/Basics/{**catch-all}", //下游转发路由
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [ "192.168.1.113:31000" ]
      },
      //WebSocket转发
      {
        "Descriptions": "WebSocket服务器",
        "ServiceName": "ImServer",
        "PathTemplate": "/ImServer/{**catch-all}", 
        "TransmitPathTemplate": "/ImServer/{**catch-all}",
        "DownstreamHostAndPorts": [ "wss://192.168.1.113:30000" ]
      }
    ]
  },
  "AllowedHosts": "*"
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

