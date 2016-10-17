using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public static class HttpResponseMessageExtensions
    {
        private static readonly List<string> IgnoredResponseHeaders = new List<string>
        {
            "Connection",
            "Date",
            "Server",
            "Transfer-Encoding"
        };

        public static async Task CopyToCurrentContext(this HttpResponseMessage source, HttpContext context)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Response.StatusCode = (int) source.StatusCode;

            foreach (var header in source.Headers.Where(header => !IgnoredResponseHeaders.Contains(header.Key)))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in source.Content.Headers.Where(header => !IgnoredResponseHeaders.Contains(header.Key)))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.AddResponseProxyHeaders();

            // todo huh ?
            if (context.Response.Body == null)
            {
                context.Response.StatusCode = 666;
                return;
            }

            await source.Content.CopyToAsync(context.Response.Body).ConfigureAwait(false);
        }

        public static void AddResponseProxyHeaders(this HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.45
            // http://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/RequestAndResponseBehaviorCustomOrigin.html
            context.Response.Headers.Add("Via", "1.1 " + Environment.MachineName);
        }
    }
}