using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SaasKit.Multitenancy;

namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public interface IValidateRoutes
    {
        Task<RouteValidationResponse> VerifyRequest(HttpContext context);
    }

    public class RouteValidator : IValidateRoutes
    {
        private readonly ITenantResolver<IdentityModel> _resolver;

        public RouteValidator(ITenantResolver<IdentityModel> resolver)
        {
            _resolver = resolver;
        }

        public async Task<RouteValidationResponse> VerifyRequest(HttpContext context)
        {
            var routes = RouteManager.Routes
                .Where(info => context.Request.Path.Value.StartsWith(info.PathMatcher))
                .ToList();

            // add more validation as needed
            if (!routes.Any())
                return RouteValidationResponse.CreateFailure(HttpStatusCode.NotFound);

            var route = routes.First();

            if (!route.IsOpen && !context.User.Identity.IsAuthenticated)
                return RouteValidationResponse.CreateFailure(HttpStatusCode.Unauthorized);

            var tenantContext = await _resolver.ResolveAsync(context).ConfigureAwait(false);
            if (tenantContext == null && !route.IsOpen)
                return RouteValidationResponse.CreateFailure(HttpStatusCode.Forbidden);

            if (route.IsPartitioned && tenantContext == null)
                return RouteValidationResponse.CreateFailure(HttpStatusCode.BadRequest);

            return RouteValidationResponse.CreateSuccess(tenantContext?.Tenant ?? IdentityModel.Empty, route);
        }
    }
}