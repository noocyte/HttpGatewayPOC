using System.Threading.Tasks;
using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using SaasKit.Multitenancy;
using WebApplication2.GatewayMiddleware;

namespace WebApplication2
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMultitenancy<Tenant, UXRiskTenantResolver>();

            services.AddTransient(x => ServicePartitionResolver.GetDefault());
            services.AddTransient<IExceptionHandler, HttpCommunicationExceptionHandler>();
            services.AddSingleton<IHttpCommunicationClientFactory, HttpCommunicationClientFactory>();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMultitenancy<Tenant>();
            app.UseMiddleware<HttpServiceGatewayMiddleware>();
            app.UseMvc();
        }
    }

    public class Tenant
    {
        public string Prefix { get; set; }
    }

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