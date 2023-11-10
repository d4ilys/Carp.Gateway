using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Simpleness.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        public string Index()
        {
            return "1111";
        }
    }
}
