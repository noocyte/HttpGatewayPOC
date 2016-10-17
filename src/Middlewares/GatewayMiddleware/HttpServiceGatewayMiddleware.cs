using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Polly;

namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public class HttpServiceGatewayMiddleware
    {
        private static readonly HttpClient Client;
        private readonly ServicePartitionResolver _resolver;
        private readonly IValidateRoutes _routeValidator;

        static HttpServiceGatewayMiddleware()
        {
            Client = new HttpClient();
        }

        public HttpServiceGatewayMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IValidateRoutes routeValidator, ServicePartitionResolver resolver)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _routeValidator = routeValidator;
            _resolver = resolver;
        }

        public async Task Invoke(HttpContext context)
        {
            var validationResponse = await _routeValidator.VerifyRequest(context).ConfigureAwait(false);

            if (!validationResponse.IsSuccess)
            {
                var statusCode = (int) validationResponse.Status;
                context.Response.StatusCode = statusCode;
                return;
            }

            var contextRequestBody = Helpers.SetContextRequestBody(context);
            var identity = validationResponse.Identity;
            var validRoute = validationResponse.ValidRoute;

            var resolved = await Helpers
                .ResolvedServicePartition(context, identity, validRoute, _resolver)
                .ConfigureAwait(false);

            var endpoint = Helpers.ExtractEndpoint(resolved, validRoute);

            var policy = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(message => (int) message.StatusCode >= 500)
                .RetryAsync(5, (delegateResult, retryCount, c) =>
                {
                    if (delegateResult.Result?.StatusCode != HttpStatusCode.ServiceUnavailable)
                    {
                        return Task.Delay(TimeSpan.FromSeconds(Helpers.ExponentialDelay(retryCount)),
                            context.RequestAborted);
                    }

                    return _resolver
                        .ResolveAsync(resolved, context.RequestAborted)
                        .ContinueWith(task => endpoint = Helpers.ExtractEndpoint(task.Result, validRoute));
                });

            var response = await policy
                .ExecuteAsync(() =>
                    MakeServiceCallAsync(endpoint, context, contextRequestBody, identity))
                .ConfigureAwait(false);

            await response.CopyToCurrentContext(context).ConfigureAwait(false);
        }


        private static async Task<HttpResponseMessage> MakeServiceCallAsync(string endpoint,
            HttpContext context, byte[] contextRequestBody, IdentityModel identity)
        {
            var uriString = endpoint + context.Request.Path +
                            context.Request.QueryString;
            var req = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri(uriString, UriKind.Absolute)
            };

            if (contextRequestBody != null)
            {
                req.Content = new ByteArrayContent(contextRequestBody);
            }

            req.CopyHeadersFromCurrentContext(context);
            req.AddProxyHeaders(context);
            var identityModelSafeString = Helpers.Base64Encode(JsonConvert.SerializeObject(identity));
            req.Headers.Add("x-identity-model", identityModelSafeString);

            // execute request
            var response = await Client.SendAsync(req, context.RequestAborted).ConfigureAwait(false);
            return response;
        }
    }
}