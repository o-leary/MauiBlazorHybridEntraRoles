using Android.App;
using Android.Content.PM;
using Android.OS;
using MauiBlazorWeb.Services;

namespace MauiBlazorWeb
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // configure platform specific params
            PlatformConfig.Instance.RedirectUri = $"msal{AppConstants.ClientId}://auth";
            PlatformConfig.Instance.ParentWindow = this;
        }
    }
}
