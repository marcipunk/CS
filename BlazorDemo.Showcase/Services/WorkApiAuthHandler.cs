using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace BlazorDemo.Showcase.Services;

public class WorkApiAuthHandler(IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext;
        string? token = null;
        if (http != null)
        {
            // Prefer cookie if present
            if (!http.Request.Cookies.TryGetValue("work_token", out token) || string.IsNullOrEmpty(token))
            {
                // Fallback to a short-lived cached token right after login (bridges the first request race)
                var userId = http.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    memoryCache.TryGetValue($"work_token:{userId}", out token);
                }
                // If the user principal isn't populated yet, try a global bridge key set at login
                if (string.IsNullOrEmpty(token))
                {
                    memoryCache.TryGetValue("work_token:latest", out token);
                }
            }
        }
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}