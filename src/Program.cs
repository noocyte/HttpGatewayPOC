using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;
using Ninject;

namespace Gateway.Admin
{
    public class Program
    {
        // Entry point for the application.
        public static void Main(string[] args)
        {
            var kernel = new StandardKernel(new GatewayAdminModule());

            ServiceRuntime.RegisterServiceAsync("AdminType", context =>
            {
                var factory = kernel.Get<IServiceFactory>();
                var service = factory.CreateStateless<GatewayService>(context);
                return service;
            })
                .GetAwaiter()
                .GetResult();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}