using Microsoft.AspNetCore.Http;

namespace DotNet8WebAPI.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[CorrelationIdHeader] = correlationId;
            }

            context.Response.Headers[CorrelationIdHeader] = correlationId;
            await _next(context);
        }
    }
}
