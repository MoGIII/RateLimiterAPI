using RateLimiter.Models;
using System.Collections.Concurrent;

namespace RateLimiter
{

    /// <summary>
    /// Class RateLimiter implements a rate limiter using the 
    /// sliding window approach.
    /// This approach won't block the user completly until the fixed time has passed
    /// and will allow smoother spread of the requests over time. 
    /// </summary>
    public class RateLimiter
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _requestLogs = new();

        private readonly ConcurrentDictionary<string, List<RateLimitRule>> _userRateLimits = new();

        /// <summary>
        /// Set the rate limits that the user requests for his request
        /// </summary>
        /// <param name="userId">String that represents the user</param>
        /// <param name="window">The time frame</param>
        /// <param name="maxRequests">Max amount of requests for given window</param>
        public void SetRateLimit(string userId, List<RateLimitRule> limitRules)
        {
            _userRateLimits[userId] = limitRules;
        }

        /// <summary>
        /// Method to check if the rate limit was reached in any of the request queues.
        /// </summary>
        /// <param name="id">the requester</param>
        /// <param name="waitAmount">The amount of time left for the earliest request to expiret</param>
        /// <returns>true if limit was reached, else false</returns>
        public bool IsLimitReached(string id)
        {
            //If no rate limits set - allow request
            if (!_userRateLimits.ContainsKey(id))
            {
                return true;

            }

            DateTime now = DateTime.UtcNow;

            if (!_requestLogs.ContainsKey(id))
            {
                _requestLogs[id] = new List<DateTime>();
            }

            var timestamps = _requestLogs[id];
            var rateLimits = _userRateLimits[id];

            foreach (var rule in rateLimits)
            {
                timestamps.RemoveAll(stamp => stamp < now - rule.WindowSize);
            }

            foreach (var rule in rateLimits)
            {
                int requestCount = timestamps.Count(stamp => stamp >= now - rule.WindowSize);
                if (requestCount >= rule.MaxRequests)
                {
                    return false;
                }
            }

            timestamps.Add(now);
            return true;
        }

        /// <summary>
        /// Invoke the task if the limit was not reached
        /// </summary>
        public async Task<(bool Allowed, T? Result)> Perform<T>(string userId, Func<Task<T>> function)
        {
            if (!IsLimitReached(userId))
            {
                return (false, default);
            }

            T result = await function();
            return (true, result);
        }
    }
}
