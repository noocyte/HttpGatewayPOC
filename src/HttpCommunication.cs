using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace Gateway.Admin
{
    public sealed class HttpCommunication<TStartup> : ICommunicationListener where TStartup : class
    {
        private IWebHost _webHost;
        private readonly string _endpointName;

        public HttpCommunication(string endpointName)
        {
            _endpointName = endpointName;
        }

        void ICommunicationListener.Abort()
        {
            _webHost?.Dispose();
        }

        Task ICommunicationListener.CloseAsync(CancellationToken cancellationToken)
        {
            _webHost?.Dispose();

            return Task.FromResult(true);
        }

        Task<string> ICommunicationListener.OpenAsync(CancellationToken cancellationToken)
        {
            var endpoint = FabricRuntime.GetActivationContext().GetEndpoint(_endpointName);

            string serverUrl = $"{endpoint.Protocol}://+:{endpoint.Port}";

            _webHost = new WebHostBuilder()
                .UseWebListener()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<TStartup>()
                .UseUrls(serverUrl)
                .Build();

            _webHost.Start();
            var resultAddress = serverUrl.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);
            return Task.FromResult(resultAddress);
        }
    }
}