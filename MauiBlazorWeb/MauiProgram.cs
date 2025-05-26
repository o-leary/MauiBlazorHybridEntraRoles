using MauiBlazorWeb.Services;
using MauiBlazorWeb.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Maui.LifecycleEvents;
using System.Security.Claims;

namespace MauiBlazorWeb
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
#if ANDROID
                .ConfigureLifecycleEvents(events =>
                {

                    events.AddAndroid(platform =>
                    {
                        platform.OnActivityResult((activity, rc, result, data) =>
                        {
                            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(rc, result, data);
                        });
                    });

                })
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            //Register needed elements for authentication:
            // This is the core functionality
            builder.Services.AddAuthorizationCore();
            // This is our custom provider
            builder.Services.AddSingleton<EntraAuthStateProvider>();
            // Use our custom provider when the app needs an AuthenticationStateProvider
            builder.Services.AddScoped<AuthenticationStateProvider>(s
                => (EntraAuthStateProvider)s.GetRequiredService<EntraAuthStateProvider>());

            // Add device-specific services used by the MauiBlazorWeb.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddScoped<IWeatherService, WeatherService>();

            //return builder.Build();

            var host = builder.Build();

            return host;
        }
    }
}
