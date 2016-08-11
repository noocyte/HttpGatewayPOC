using System.Collections.Generic;

namespace WebApplication2.GatewayMiddleware
{
    public class RouteInfo
    {
        public string PathMatcher { get; set; }
        public bool IsPartitioned { get; set; }
        public string ServiceUri { get; set; }
        public string ListenerName { get; set; }
    }

    public static class RouteManager
    {
        static RouteManager()
        {
            Routes = new List<RouteInfo>();
            //{
            //    new RouteInfo
            //    {
            //        IsPartitioned = false,
            //        ServiceUri = "fabric:/Api/ApiService",
            //        ListenerName = "",
            //        PathMatcher = "entity/template"
            //    }
            //};

        }
        public static IEnumerable<RouteInfo> Routes { get; set; }
    }
}