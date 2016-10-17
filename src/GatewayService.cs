using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Admin.Controllers;
using Gateway.Admin.Middlewares.GatewayMiddleware;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Gateway.Admin
{
    internal sealed class GatewayService : StatelessService
    {
        private CancellationToken _cancellationToken;

        public GatewayService(StatelessServiceContext context)
            : base(context)
        {
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(_ => new HttpCommunication<StartupExternal>("External"), "external"),
                new ServiceInstanceListener(_ => new HttpCommunication<StartupInternal>("Internal"), "internal")
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            var routes = await GetRoutesAsync().ConfigureAwait(false);
            RouteManager.Routes = routes;
        }

        private async Task<IEnumerable<RouteInfo>> GetRoutesAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(_cancellationToken);

            // get all the routes!
            var service = ServiceProxy.Create<IRouterService>(new Uri("fabric:/ApplicationName/ServiceName-Router"));
            var routesResponse = await service.GetRoutes().ConfigureAwait(false);
            return routesResponse;
        }
    }

    public interface IRouterService : IService
    {
        Task<IEnumerable<RouteInfo>> GetRoutes();
    }
}