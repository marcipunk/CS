using System.Net.Http.Headers;

namespace BlazorDemo.Showcase.Services;

public class WorkApiAuthHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext;
        if (http != null && http.Request.Cookies.TryGetValue("work_token", out var token) && !string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}