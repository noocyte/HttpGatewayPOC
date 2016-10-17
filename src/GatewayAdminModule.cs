using System.Fabric;
using Microsoft.ServiceFabric.Services.Runtime;
using Ninject.Extensions.Factory;
using Ninject.Modules;

namespace Gateway.Admin
{
    public class GatewayAdminModule:NinjectModule
    {
        public override void Load()
        {
            Bind<IServiceFactory>().ToFactory();
        }
    }

    public interface IServiceFactory
    {
        T CreateStateless<T>(StatelessServiceContext context) where T : StatelessService;
    }

}