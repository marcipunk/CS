using Microsoft.JSInterop;

namespace BlazorDemo.Showcase.Client.Services;

public interface IAuthTokenStore
{
    string? AccessToken { get; }
    Task SetTokenAsync(string? token, bool rememberMe, IJSRuntime js);
    Task LoadFromStorageAsync(IJSRuntime js);
}

public class AuthTokenStore : IAuthTokenStore
{
    private const string LocalStorageKey = "wjwtToken";
    public string? AccessToken { get; private set; }

    public async Task SetTokenAsync(string? token, bool rememberMe, IJSRuntime js)
    {
        AccessToken = token;
        if (rememberMe && !string.IsNullOrEmpty(token))
        {
            await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, token);
        }
        else
        {
            await js.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
        }
    }

    public async Task LoadFromStorageAsync(IJSRuntime js)
    {
        try
        {
            AccessToken = await js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
        }
        catch
        {
            AccessToken = null;
        }
    }
}
