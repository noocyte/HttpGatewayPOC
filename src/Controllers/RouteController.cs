using System.Collections.Generic;
using Gateway.Admin.Middlewares.GatewayMiddleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Admin.Controllers
{
    [AllowAnonymous]
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