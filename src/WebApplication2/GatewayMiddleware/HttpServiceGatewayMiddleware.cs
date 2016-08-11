using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using SaasKit.Multitenancy;

namespace WebApplication2.GatewayMiddleware
{
    public class HttpServiceGatewayMiddleware
    {
        private readonly IHttpCommunicationClientFactory _httpCommunicationClientFactory;
        private readonly RequestDelegate _next;
        private readonly ITenantResolver<Tenant> _resolver;

        public HttpServiceGatewayMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IHttpCommunicationClientFactory httpCommunicationClientFactory, ITenantResolver<Tenant> resolver)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (httpCommunicationClientFactory == null)
                throw new ArgumentNullException(nameof(httpCommunicationClientFactory));

            _next = next;
            _httpCommunicationClientFactory = httpCommunicationClientFactory;
            _resolver = resolver;
        }

        public async Task Invoke(HttpContext context)
        {
            byte[] contextRequestBody = null;
            try
            {
                // if this is a /route request - lets just continue
                if (context.Request.Path.StartsWithSegments("/route"))
                {
                    await _next(context);
                    return;
                }

                var route = RouteManager.Routes
                    .Where(info => context.Request.Path.Value.StartsWith(info.PathMatcher))
                    .DefaultIfEmpty(CreateFallThroughRoute())
                    .First();

                var tenantContext = await _resolver.ResolveAsync(context);

                var servicePartitionClient = CreateServicePartitionClient(tenantContext.Tenant, route);

                // Request Body is a forward-only stream so it is read into memory for potential retries.
                // NOTE: This might be an issue for very big requests.
                if (context.Request.ContentLength > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        context.Request.Body.CopyTo(memoryStream);
                        contextRequestBody = memoryStream.ToArray();
                    }
                }

                var response = await servicePartitionClient.InvokeWithRetryAsync(
                    client => ExecuteServiceCallAsync(client, context, contextRequestBody));

                await response.CopyToCurrentContext(context);
            }
            catch (HttpResponseException ex)
            {
                // as soon as we get a response from the service, we don't treat it as an error from the gateway.
                // For this reason, we forward faulty responses to the caller 1:1.
                //  _logger.LogWarning("Service returned non retryable error. Reason: {Reason}", "HTTP " + ex.Response.StatusCode);
                await ex.Response.CopyToCurrentContext(context);
            }
        }

        private static RouteInfo CreateFallThroughRoute()
        {
            return new RouteInfo
            {
                IsPartitioned = false,
                ServiceUri = "fabric:/Api/AdminApiService"
            };
        }

        private static async Task<HttpResponseMessage> ExecuteServiceCallAsync(HttpCommunicationClient client,
            HttpContext context, byte[] contextRequestBody)
        {
            // create request and copy all details

            var req = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri(context.Request.Path + context.Request.QueryString, UriKind.Relative)
            };

            if (contextRequestBody != null)
            {
                req.Content = new ByteArrayContent(contextRequestBody);
            }

            req.CopyHeadersFromCurrentContext(context);
            req.AddProxyHeaders(context);

            // execute request
            var response = await client.HttpClient.SendAsync(req, context.RequestAborted);

            // cases in which we want to invoke the retry logic from the ClientFactory
            var statusCode = (int) response.StatusCode;
            if ((statusCode >= 500 && statusCode < 600) || statusCode == (int) HttpStatusCode.NotFound)
            {
                throw new HttpResponseException("Service call failed", response);
            }

            return response;
        }

        private ServicePartitionClient<HttpCommunicationClient> CreateServicePartitionClient(Tenant currentTenant,
            RouteInfo route)
        {
            var servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                communicationClientFactory: _httpCommunicationClientFactory,
                serviceUri: new Uri(route.ServiceUri),
                partitionKey: route.IsPartitioned ? new ServicePartitionKey(currentTenant.Prefix) : null,
                listenerName: string.IsNullOrWhiteSpace(route.ListenerName) ? null : route.ListenerName);

            return servicePartitionClient;
        }
    }
}