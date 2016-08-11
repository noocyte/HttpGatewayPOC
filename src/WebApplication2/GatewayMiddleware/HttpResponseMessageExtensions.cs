﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebApplication2.GatewayMiddleware
{
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Response headers which are either set by the framework or shouldn't be copied.
        /// </summary>
        private static readonly List<string> IgnoredResponseHeaders = new List<string>
        {
            "Connection",
            "Date",
            "Server",
            "Transfer-Encoding"
        };

        /// <summary>
        /// Copies all details from the source to the current response.
        /// </summary>
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

            await source.Content.CopyToAsync(context.Response.Body);
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