using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Consul.Client.Controllers
{
    [Route("basics/[controller]/[action]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public string Index()
        {
            return $"Success-{_configuration["port"]}";
        }
    }
}
