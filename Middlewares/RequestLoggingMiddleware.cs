using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNet8WebAPI.Middlewares
{
        public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
         //   var stopwatch = Stopwatch.StartNew();

          //  _logger.LogInformation("Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);

            await _next(context);

          //  stopwatch.Stop();
            //_logger.LogInformation("Finished handling request. Status: {StatusCode}. Time taken: {ElapsedMilliseconds} ms",
               // context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}





