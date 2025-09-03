using BlazorDemo.Showcase.Client.Utils;
using BlazorDemo.Showcase.Components;
using BlazorDemo.Showcase.Components.Account;
using BlazorDemo.Showcase.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Server; // Add this using to resolve CircuitOptions

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAppServices();

builder.Services.AddScoped<DemoData>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
builder.Services.AddScoped<CookieEvents>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.EventsType = typeof(CookieEvents);
});
builder.Services.Configure<CircuitOptions>(o => o.DetailedErrors = true);

var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => {
    options.UseSqlServer(connectionString);
    if (builder.Environment.IsDevelopment()) {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Outbound HTTP client for the external WORK API
builder.Services.AddTransient<BlazorDemo.Showcase.Services.WorkApiAuthHandler>();
builder.Services.AddHttpClient("WorkApi", client =>
{
    client.BaseAddress = new Uri("https://work.sbdw.cobra.local/");
}).AddHttpMessageHandler<BlazorDemo.Showcase.Services.WorkApiAuthHandler>();

builder.WebHost.UseStaticWebAssets();

// Ensure HTTPS is available when running without a launch profile or env URLs
builder.WebHost.ConfigureKestrel(options =>
{
    // If ASPNETCORE_URLS is not set, bind to localhost:5000 (http) and 5001 (https)
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (string.IsNullOrWhiteSpace(urls))
    {
        options.ListenLocalhost(5000); // HTTP
        try
        {
            options.ListenLocalhost(5001, lo => lo.UseHttps()); // HTTPS for Secure cookies
        }
        catch
        {
            // Dev machine may miss a dev cert; HTTP redirection will no-op
        }
    }
});

var app = builder.Build();

// Apply any pending EF Core migrations at startup (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    // Seed demo data once per application start
    var demo = scope.ServiceProvider.GetRequiredService<DemoData>();
    await demo.InitAsync();
}

string? pathBase = configuration.GetValue<string>("pathbase");
if(!string.IsNullOrEmpty(pathBase)) {
    string pathString = pathBase.StartsWith('/') ? pathBase : "/" + pathBase;
    app.UsePathBase(pathString);
}

app.UseRouting();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment()) {
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
} else {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorDemo.Showcase.Client.Components._Imports).Assembly);


app.MapAdditionalIdentityEndpoints();

// Minimal proxy for the external WORK API.
// The named HttpClient "WorkApi" attaches the Bearer token from the HttpOnly cookie via WorkApiAuthHandler.
app.MapGet("/workproxy/{**rest}", async (string? rest, HttpContext http, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("WorkApi");
    var targetPathAndQuery = (rest ?? string.Empty) + http.Request.QueryString.Value;
    var target = new Uri(client.BaseAddress!, targetPathAndQuery);

    using var upstream = await client.GetAsync(target, http.RequestAborted);
    var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/json";
    var body = await upstream.Content.ReadAsStringAsync(http.RequestAborted);
    return Results.Content(body, contentType: contentType, statusCode: (int)upstream.StatusCode);
}).AllowAnonymous();

app.Run();
