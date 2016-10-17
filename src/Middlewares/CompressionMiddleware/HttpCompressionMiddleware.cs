using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Gateway.Admin.Middlewares.CompressionMiddleware

{
    public class HttpCompressionMiddleware
    {
        private const long MinimumLength = 2700;
        private const string ContentLength = "Content-Length";
        private const string GZipEncoding = "gzip";
        private const string ContentEncodingHeader = "Content-Encoding";
        private const string AcceptEncodingHeader = "Accept-Encoding";

        private readonly RequestDelegate _next;

        public HttpCompressionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var acceptEncoding = context.Request.Headers[AcceptEncodingHeader];
            if (acceptEncoding.ToString().IndexOf(GZipEncoding, StringComparison.CurrentCultureIgnoreCase) < 0)
            {
                await _next(context);
                return;
            }

            using (var buffer = new MemoryStream())
            {
                var body = context.Response.Body;
                context.Response.Body = buffer;
                try
                {
                    await _next(context);

                    if (buffer.Length >= MinimumLength)
                    {
                        using (var compressed = new MemoryStream())
                        {
                            using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
                            {
                                buffer.Seek(0, SeekOrigin.Begin);
                                await buffer.CopyToAsync(gzip);
                            }

                            if (compressed.Length < buffer.Length)
                            {
                                // write compressed data to response
                                context.Response.Headers.Add(ContentEncodingHeader, new[] {GZipEncoding});
                                if (context.Response.Headers[ContentLength].Count > 0)
                                {
                                    context.Response.Headers[ContentLength] = compressed.Length.ToString();
                                }

                                compressed.Seek(0, SeekOrigin.Begin);

                                await compressed.CopyToAsync(body);
                                return;
                            }
                        }
                    }

                    // write uncompressed data to response
                    buffer.Seek(0, SeekOrigin.Begin);
                    await buffer.CopyToAsync(body);
                }
                finally
                {
                    context.Response.Body = body;
                }
            }
        }
    }
}