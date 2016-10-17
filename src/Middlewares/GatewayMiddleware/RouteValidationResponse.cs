using System.Net;
using Gateway.Admin.Controllers;

namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public struct RouteValidationResponse
    {
        public HttpStatusCode Status { get; private set; }
        public IdentityModel Identity { get; private set; }
        public bool IsSuccess => Status == HttpStatusCode.OK;
        public RouteInfo ValidRoute { get; private set; }

        public static RouteValidationResponse CreateSuccess(IdentityModel identity, RouteInfo result)
        {
            return new RouteValidationResponse
            {
                Identity = identity,
                Status = HttpStatusCode.OK,
                ValidRoute = result
            };
        }

        public static RouteValidationResponse CreateFailure(HttpStatusCode status)
        {
            return new RouteValidationResponse
            {
                Identity = IdentityModel.Empty,
                Status = status
            };
        }
    }
}