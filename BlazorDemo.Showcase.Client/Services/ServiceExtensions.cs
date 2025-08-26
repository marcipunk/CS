using BlazorDemo.Showcase.Client.Services;
using BlazorDemo.Showcase.Services;
using BlazorDemo.Showcase.Services.DataProviders;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;

namespace BlazorDemo.Showcase.Client.Utils {
    public static class ServiceExtensions {
        public static void AddAppServices(this IServiceCollection services) {
            services.AddScoped(sp =>
                new HttpClient {
                    BaseAddress = new Uri("https://js.devexpress.com/Demos/RwaService/api/")
                });
            services.AddDevExpressBlazor();
            services.AddScoped<SearchManager>();
            services.AddScoped<ModuleLoader>();
            services.AddScoped<ThemeManager>();
            services.AddScoped<ClipboardManager>();
            services.AddScoped<SizeModeManager>();
            services.AddScoped<ContactDataProvider>();
            services.AddScoped<WorkRequestDataProvider>();
            services.AddScoped<AnalyticDataProvider>();
            services.AddScoped<TasksDataProvider>();
            services.AddCascadingValue("NotificationCount", sp => 4);
            services.AddScoped(sp => new CascadingValueSource<SizeMode>("ParentSizeMode", SizeMode.Medium, false));
            services.AddCascadingValue(sp => sp.GetRequiredService<CascadingValueSource<SizeMode>>());
        }
    }
}
