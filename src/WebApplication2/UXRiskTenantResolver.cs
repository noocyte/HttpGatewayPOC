using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SaasKit.Multitenancy;

namespace WebApplication2
{
    public class UXRiskTenantResolver : ITenantResolver<Tenant>
    {
        public Task<TenantContext<Tenant>> ResolveAsync(HttpContext context)
        {
            var tenantContext = new TenantContext<Tenant>(
                new Tenant {Prefix = "a1234567"});

            return Task.FromResult(tenantContext);
        }
    }
}