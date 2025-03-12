using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RateLimiter.Models;
using System.Threading.RateLimiting;

namespace LimiterAPI.Controllers
{
    [Route("api/limiter")]
    [ApiController]
    public class RateLimiterController : ControllerBase
    {
        private readonly RateLimiter.RateLimiter _limiter;

        public RateLimiterController(RateLimiter.RateLimiter limiter)
        {
            _limiter = limiter;
        }

        [HttpPost("set-limit")]
        public IActionResult SetRateLimits([FromQuery] string user, [FromBody] List<RateLimitRule> rateLimits)
        {
            if (string.IsNullOrEmpty(user) || rateLimits == null || rateLimits.Count == 0)
            {
                return BadRequest("Invalid parameters");
            }
            _limiter.SetRateLimit(user, rateLimits);
            return Ok($"Rate limits were successfully set for user {user}");
        }

        [HttpPost("execute")]
        public async Task<IActionResult> Perform([FromBody] HttpRequestMessage request)
        {
            string? user = Request.Headers["X-User-Id"];
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("Missing X-User-Id header");
            }

            var result = await _limiter.PerformAsync(user, request);

            if (result.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return StatusCode(429, "Rate limit exceeded. Try again later.");
            }

            return Ok(result);
        }
    }
}
