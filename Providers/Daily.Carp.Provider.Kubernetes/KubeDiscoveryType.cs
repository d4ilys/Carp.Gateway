using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daily.Carp.Provider.Kubernetes
{
    /// <summary>
    /// Kubernetes 服务发现类型
    /// </summary>
    public enum KubeDiscoveryType
    {

        /// <summary>
        /// 通过监听Pod，动态更新
        /// <remarks>使用Carp负载均衡，通过Kube-Proxy处理请求</remarks>
        /// </summary>
        EndPoint,

        /// <summary>
        /// 通过Service内部ClusterIP访问Pod
        /// <remarks>使用Kubernetes原生负载均衡，不通过Kube-Proxy处理请求</remarks>
        /// </summary>
        ClusterIP

    }
}
