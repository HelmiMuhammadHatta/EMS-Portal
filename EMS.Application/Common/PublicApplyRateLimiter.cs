using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EMS.Application.Common;

public interface IPublicApplyRateLimiter
{
    bool AllowSubmission(string clientIp);
    int GetRemainingAttempts(string clientIp);
}

public class PublicApplyRateLimiter : IPublicApplyRateLimiter
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _ipSubmissions = new();
    private readonly TimeSpan _window = TimeSpan.FromHours(1);
    private const int MaxAttempts = 3;

    public bool AllowSubmission(string clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp)) clientIp = "unknown";

        var now = DateTime.UtcNow;
        var timestamps = _ipSubmissions.GetOrAdd(clientIp, _ => new List<DateTime>());

        lock (timestamps)
        {
            // Remove timestamps older than window
            timestamps.RemoveAll(t => now - t > _window);

            if (timestamps.Count >= MaxAttempts)
            {
                return false;
            }

            timestamps.Add(now);
            return true;
        }
    }

    public int GetRemainingAttempts(string clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp)) clientIp = "unknown";

        var now = DateTime.UtcNow;
        if (!_ipSubmissions.TryGetValue(clientIp, out var timestamps))
        {
            return MaxAttempts;
        }

        lock (timestamps)
        {
            timestamps.RemoveAll(t => now - t > _window);
            return Math.Max(0, MaxAttempts - timestamps.Count);
        }
    }
}
