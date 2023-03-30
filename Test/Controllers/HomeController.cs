using Daily.Carp.Internel;
using Daily.Carp.Yarp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yarp.ReverseProxy.Configuration;

namespace Test.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        //[Authorize]
        [HttpGet]
        public object GetAll()
        {
            var proxyConfigProvider = CarpApp.ServiceProvider.GetService<IProxyConfigProvider>();
            var readOnlyList = proxyConfigProvider.GetConfig().Clusters;
            var routeConfigs = proxyConfigProvider.GetConfig().Routes;
            return new
            {
                routeConfigs, readOnlyList
            };
        }
        [HttpGet]
        public object Test()
        {
            return "1";
        }

        [HttpGet]
        public object Update()
        {
            var proxyConfigProvider = ServiceDiscovery.GetService<CarpProxyConfigProvider>();
            var readOnlyList = proxyConfigProvider.GetConfig().Clusters;
            var routeConfigs = proxyConfigProvider.GetConfig().Routes;
            return new
            {
                routeConfigs,
                readOnlyList
            };
        }
    }
}