using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using BlazorDemo.Showcase.Client;

namespace BlazorDemo.Showcase.Components.Account {
    // This is a server-side AuthenticationStateProvider that uses PersistentComponentState to flow the
    // authentication state to the client which is then fixed for the lifetime of the WebAssembly application.
    internal sealed class PersistingServerAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable {
        private readonly PersistentComponentState state;
        private readonly IdentityOptions options;

        private readonly PersistingComponentStateSubscription subscription;

        private Task<AuthenticationState>? authenticationStateTask;

        public PersistingServerAuthenticationStateProvider(
            PersistentComponentState persistentComponentState,
            IOptions<IdentityOptions> optionsAccessor) {
            state = persistentComponentState;
            options = optionsAccessor.Value;

            AuthenticationStateChanged += OnAuthenticationStateChanged;
            subscription = state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
        }

        private void OnAuthenticationStateChanged(Task<AuthenticationState> task) {
            authenticationStateTask = task;
        }

        private async Task OnPersistingAsync() {
            if(authenticationStateTask is null) {
                throw new UnreachableException($"Authentication state not set in {nameof(OnPersistingAsync)}().");
            }

            var authenticationState = await authenticationStateTask;
            var principal = authenticationState.User;

            if (principal.Identity?.IsAuthenticated == true) {
                // Prefer external WORK user info if present, otherwise fall back to default Identity claims
                string? GetClaim(string type) => principal.FindFirst(type)?.Value;

                var userId = GetClaim(options.ClaimsIdentity.UserIdClaimType);
                var email = GetClaim("work:email") ?? GetClaim(options.ClaimsIdentity.EmailClaimType);
                var name = GetClaim("work:name") ?? GetClaim(options.ClaimsIdentity.UserNameClaimType);
                var role = GetClaim(options.ClaimsIdentity.RoleClaimType) ?? "Guest";

                if (userId != null && email != null && name != null && role != null) {
                    state.PersistAsJson(nameof(UserInfo), new UserInfo {
                        UserId = userId,
                        Email = email,
                        Name = name,
                        Role = role
                    });
                }
            }
        }

        public void Dispose() {
            subscription.Dispose();
            AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }
}
