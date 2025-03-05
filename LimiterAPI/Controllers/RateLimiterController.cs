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
        public async Task<IActionResult> Perform([FromBody] Func<Task<object>> func)
        {
            string? user = Request.Headers["X-User_Id"];
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("Missing X-User_Id header");
            }

            var (allowed, result) = await _limiter.Perform(user, func);

            if (!allowed)
            {
                return StatusCode(429, "Rate limit exceeded. Try again later.");
            }

            return Ok(result);
        }
    }
}
