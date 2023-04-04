using Microsoft.AspNetCore.Mvc;

namespace Consul.Client02.Controllers
{
    [Route("app/[controller]/[action]")]
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
