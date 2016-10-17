using System;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gateway.Admin.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.ServiceFabric.Services.Client;

namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public static class Helpers
    {
        internal static double ExponentialDelay(int failedAttempts)
        {
            const int maxDelayInSeconds = 32;
            var delayInSeconds = (1d/2d)*(Math.Pow(2d, failedAttempts));

            return maxDelayInSeconds < delayInSeconds
                ? maxDelayInSeconds
                : delayInSeconds;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static async Task<ResolvedServicePartition> ResolvedServicePartition(HttpContext context,
            IdentityModel identity, RouteInfo validRoute, ServicePartitionResolver resolver)
        {
            var partitionKey = identity.OrganizationPrefix;
            var resolvedKey = PartitionResolver.Resolve(partitionKey);

            var servicePartitionKey = validRoute.IsPartitioned
                ? new ServicePartitionKey(resolvedKey)
                : ServicePartitionKey.Singleton;

            var resolved = await resolver
                .ResolveAsync(new Uri(validRoute.ServiceUri), servicePartitionKey,
                    context.RequestAborted)
                .ConfigureAwait(false);
            return resolved;
        }

        public static byte[] SetContextRequestBody(HttpContext context)
        {
            // Request Body is a forward-only stream so it is read into memory for potential retries.
            // NOTE: This might be an issue for very big requests.
            if (!(context.Request.ContentLength > 0)) return null;

            byte[] contextRequestBody;
            using (var memoryStream = new MemoryStream())
            {
                context.Request.Body.CopyTo(memoryStream);
                contextRequestBody = memoryStream.ToArray();
            }
            return contextRequestBody;
        }

        public static string ExtractEndpoint(ResolvedServicePartition resolved, RouteInfo validRoute)
        {
            var resolvedEndpoints = resolved.Endpoints.First().Address;
            var rawAddress = JsonObject.Parse(resolvedEndpoints);

            var endpoints = rawAddress["Endpoints"] as JsonObject;

            if (endpoints == null) return string.Empty;

            var endpoint = endpoints.Count == 1
                ? endpoints.First().Value.ToString()
                : endpoints[validRoute.ListenerName] as string ?? "";
            return endpoint;
        }
    }
}