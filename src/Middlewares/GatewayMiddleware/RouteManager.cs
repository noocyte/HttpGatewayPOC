using System.Collections.Generic;
using Gateway.Admin.Controllers;

namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public static class RouteManager
    {
        static RouteManager()
        {
            Routes = new List<RouteInfo>();
        }

        public static IEnumerable<RouteInfo> Routes { get; set; }
    }
}