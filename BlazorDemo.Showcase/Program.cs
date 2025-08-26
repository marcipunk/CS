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
builder.Services.ConfigureApplicationCookie(o =>
{
    o.EventsType = typeof(CookieEvents);
});
builder.Services.Configure<CircuitOptions>(o => o.DetailedErrors = true);

var configuration = builder.Configuration;
builder.Services.AddDbContext<ApplicationDbContext>((provider, options) => {
    var accessor = provider.GetRequiredService<IHttpContextAccessor>();
    var context = accessor?.HttpContext;
    if(context != null) {
        string? dataKey = context.Request.Cookies["DemoDataKey"];
        if(dataKey == null) {
            dataKey = Guid.NewGuid().ToString();
            context.Response.Cookies.Append("DemoDataKey", dataKey);
        }
        options.UseInMemoryDatabase(dataKey);
    }
    // Use the following lines of code to configures the context to connect to a SQL Server database.
    // var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    // options.UseSqlServer(connectionString);
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

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

app.UseAuthorization();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorDemo.Showcase.Client.Components._Imports).Assembly);


app.MapAdditionalIdentityEndpoints();

app.Run();
