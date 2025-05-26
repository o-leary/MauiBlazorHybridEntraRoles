using Foundation;
using MauiBlazorWeb.Services;
using UIKit;

namespace MauiBlazorWeb
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // configure platform specific params
            PlatformConfig.Instance.RedirectUri = $"msal{AppConstants.ClientId}://auth";

            return base.FinishedLaunching(application, launchOptions);
        }
    }
}
