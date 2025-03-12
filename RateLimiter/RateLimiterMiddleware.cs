using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RateLimiter
{
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimiter _limiter;

        public RateLimiterMiddleware(RequestDelegate next, RateLimiter limiter)
        {
            _next = next;
            _limiter = limiter;
        }

        public async Task Invoke(HttpContext context)
        {
            string? userId = context.Request.Headers["X-User-Id"];
            if (string.IsNullOrEmpty(userId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing X-User-Id header");
                return;
            }

            if (!_limiter.IsLimitReached(userId))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Access limit exceeded. Try again later");
                return;
            }

            await _next(context);
        }
    }
}
