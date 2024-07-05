#### 目录

🍧 [**概述**](#概述)  <br />
✨ [Quick Start](#quick-start) <br />
☁️ [集成Kubernetes](#kubernetes) <br />🎭 [Kubernetes无感升级](#Kubernetes实现用户无感升级) <br />🍢 [集成Consul](#consul) <br />
⚓ [普通代理模式](#普通代理模式) <br />🥨 [错误重试](#错误重试) <br />🎡 [权限验证](#权限验证) <br />🎉 [GRPC](#GRPC) <br />👍 [WebSocket](#WebSocket) <br />🧊 [集成Swagger](#集成swagger) <br />

#### **概述**

Carp.Gateway 是.NET下生态的网关 基于微软的Yarp实现

支持**Kubernetes**、Consul

#### Quick Start 

* 创建 .NET 8.0 WebAPI

* NuGet 安装Carp.Gateway

~~~c#
Install-Package Carp.Gateway
~~~

* Program.cs

~~~C#
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args)；

builder.Services.AddCarp();  

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

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

Ocelot 每次负载均衡请求时，需要先调用一遍API Server查询EndPoint，这样会浪费一部分性能。

和Ocelot不同的是，Carp 会在项目启动的时候就把Service信息初始化完毕，采取观察者模式监控Pod的创建与删除 动态更新Service信息 或者 直接使用ClusterIP直接进行访问，这样就避免了每次转发都需要请求API Server

需要注意的是，在Kubernetes 中需要再ServiceAccount 中增加 pods、service、watch等权限，Carp才能实时监控Service的事件信息，**下方有完整的yaml实例**

![1d7b5ed2623bf5349b8e148947bec5d](https://user-images.githubusercontent.com/54463101/228444662-a3b03a25-2a62-40e2-a068-a711de124535.png)

> 适配Kubernetes

~~~shell
Install-Package Carp.Gateway.Provider.Kubernetes
~~~

~~~C#
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarp().AddKubernetes();

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();

app.UseCarp();

app.Run();
~~~

> Kubernetes服务发现类型说明

**在Carp中Kubernetes服务发现支持两种方式**

1.ClusterIP(默认方式)

ClusterIP提供了一种在集群内部进行服务发现和负载均衡的机制。

~~~c#
builder.Services.AddCarp().AddKubernetes(KubeDiscoveryType.ClusterIP);
~~~

2.Endpoint

Endpoint = PodId + ContainerPort，Carp会将一个Service中的所有的Endpoint交给Yarp进行管理，当Pod发生变化时（例如滚动更新时），Carp会实时更新Yarp配置

~~~c#
builder.Services.AddCarp().AddKubernetes(KubeDiscoveryType.EndPoint);
~~~

> 配置文件

~~~json
 "Carp": {
    "Kubernetes": {
      "Namespace": "dev"
    },
    "Routes": [
      {
        "Descriptions": "基础服务集群",
        "ServiceName": "basics",
        "PathTemplate": "/basics/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
        "PathTemplate": "/business/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "登录服务集群",
        "ServiceName": "lgcenter",
        "PathTemplate": "/login/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http"
      },
      {
        "Descriptions": "日志服务的集群",
        "ServiceName": "logs",
        "PermissionsValidation": ["Jwt"],
        "PathTemplate": "/log/{**catch-all}",
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

#### Kubernetes实现用户无感升级

在K8S中我们在部署更新服务的时候，旧的Pod会被Kill，新的Pod会生成并逐步替换。但是在这个工作过程中旧的Pod在被Kill时可能还会有一些流量会被调度到该Pod，会导致一些请求出现错误 一般为502，为了解决这个问题，我们引入两个动作。

> 就绪探针

Pod在启动完毕后，我们为了确保容器正确启动并可以接收请求，会暴漏一个api，该api接收K8S的心跳探测，如果成功并状态码返回200 则代表该Pod已经可以处理流量

~~~yaml
readinessProbe:
  httpGet:
    path: /api/health/index
    port: 5000
    scheme: HTTP
  initialDelaySeconds: 15
  timeoutSeconds: 5
  periodSeconds: 30
  successThreshold: 1
  failureThreshold: 3
~~~

> preStop钩子

容器在被下达Kill的命令后，仍然可以处理一段时间的请求，这个是至关重要的，解决了容器在退出的过程中的一瞬间流量被命中后无法处理的情况

~~~yaml
lifecycle:
  preStop:
    exec:
      command:
        - /bin/sh
        - '-c'
        - sleep 10
~~~

#### Consul

~~~c#
using Com.Ctrip.Framework.Apollo;
using Com.Ctrip.Framework.Apollo.Core;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCarp().AddConsul();  //添加Consul支持

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();

app.UseCarp();

app.MapControllers();

app.Run();
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
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarp(); 

var app = builder.Build();

app.UseCarp();

app.Run();
~~~

~~~json
"Carp": {
    "Namespace": "dev",
    "Routes": [
      {
        "Descriptions": "基础服务集群",
        "ServiceName": "basics",
        "PathTemplate": "/basics/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts" : [ "192.168.0.112:8001","192.168.0.113:8001"]

      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
        "PathTemplate": "/business/{**catch-all}",
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
        "PathTemplate": "/basics/{**catch-all}", //客户端请求路由
        "TransmitPathTemplate": "/Basics/{**catch-all}", //下游转发路由
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [ "192.168.1.113:31000" ]
      },
      //WebSocket转发
      {
        "Descriptions": "WebSocket服务器",
        "ServiceName": "ImServer",
        "PathTemplate": "/imServer/{**catch-all}", 
        "TransmitPathTemplate": "/imServer/{**catch-all}",
        "DownstreamHostAndPorts": [ "wss://192.168.1.113:30000" ]
      }
    ]
  },
  "AllowedHosts": "*"
}
~~~

#### 权限验证

~~~c#
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
    options.CustomAuthenticationAsync.Add("Signature", async () => //这里的 “Signature” 对应的是配置文件中的PermissionsValidation数组中的值
    {
        //自定义鉴权逻辑
        var flag = true;
        //验证逻辑
        flag = false;
        //.....
        return await Task.FromResult(flag);
    });
});
~~~

~~~json
 "Carp": {
    "Routes": [
      {
        "Descriptions": "基础服务集群",
        "ServiceName": "basics",
        "PermissionsValidation": ["Jwt","Signature"],  //验证Jwt和Signature
        "PathTemplate": "/Basics/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [ "192.168.1.113:31000" ]
      },
      {
        "Descriptions": "主业务服务集群",
        "ServiceName": "business",
         "PermissionsValidation": ["Signature"], // 只验证Signature
        "PathTemplate": "/Business/{**catch-all}",
        "LoadBalancerOptions": "PowerOfTwoChoices",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [ "192.168.1.113:32000" ]
      }
    ]
  }
~~~



#### 错误重试

> 状态码大于400才会触发重试

~~~json
{
  "Carp": {
    "Routes": [
      {
        "Descriptions": "简单的例子",
        "ServiceName": "Basics",
        "PathTemplate": "/Basics/{**catch-all}", 
        "TransmitPathTemplate": "{**catch-all}", 
        "DownstreamHostAndPorts": [ "https://jd.com", "https://xxx.aasd.casd", "https://xxx.aasasd.casd", "https://xxx.aassssasd.casd" ],
        "RetryPolicy": {    //重试策略
          "RetryCount": 2,  //默认3次，重试次数
          "RetryOnStatusCodes": [ "5xx","404" ]  //可以不配置，默认5xx
        }
      }  
    ] 
  }
}
~~~

#### 限流

> 在ASP.NET Core中已经内置了限流中间件

[ASP.NET Core 中的速率限制中间件 | Microsoft Learn](https://learn.microsoft.com/zh-cn/aspnet/core/performance/rate-limit?view=aspnetcore-8.0)

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

~~~c#

using Daily.Carp.Extension;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Data;
using Daily.Carp;
using IGeekFan.AspNetCore.Knife4jUI;

var builder = WebApplication.CreateBuilder(args);

//添加Carp配置
builder.Services.AddCarp().AddKubernetes();

builder.Services.AddControllers();

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

app.UseKnife4UI(c =>
{
    c.Authentication = true; //开启鉴权
    c.Password = "123456";   //设置密码
    //配置服务swagger信息
    c.SwaggerEndpoint("basics/swagger/v1/swagger.json", "Basics API");
    c.SwaggerEndpoint("Business/swagger/v1/swagger.json", "Business API");
});

app.MapControllers();

app.Run();

~~~

~~~json

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
            "PathTemplate": "/basics/{**catch-all}",
            "LoadBalancerOptions": "PowerOfTwoChoices",
            "DownstreamScheme": "http"
          },
          {
            "Descriptions": "主业务服务集群",
            "ServiceName": "business",
            "PathTemplate": "/business/{**catch-all}",
            "LoadBalancerOptions": "PowerOfTwoChoices",
            "DownstreamScheme": "http"
          }
        ]
      }
}
~~~

![image](https://github.com/luoyunchong/IGeekFan.AspNetCore.Knife4jUI/assets/54463101/d011c6c1-e782-49e3-95d0-9de35a2f9fe4)

