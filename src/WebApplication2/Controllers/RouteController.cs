using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.GatewayMiddleware;

namespace WebApplication2.Controllers
{
    [Route("route")]
    public class RouteController : Controller
    {
        // GET /route
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(RouteManager.Routes);
        }


        // POST /route
        [HttpPost]
        public IActionResult Post([FromBody] IEnumerable<RouteInfo> routes)
        {
            RouteManager.Routes = routes;
            return Ok(RouteManager.Routes);
        }
    }
}