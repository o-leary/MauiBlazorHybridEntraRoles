using MauiBlazorWeb.Models;
using MauiBlazorWeb.Services;
using MauiBlazorWeb.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class EntraAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private static IPublicClientApplication publicClientApp;
    private AuthenticationResult _authResult;
    public string LoginFailureMessage { get; set; } = "";
    public LoginStatus LoginStatus { get; set; } = LoginStatus.None;
    private MsalCacheHelper _msalCacheHelper;
    private string userIdentifier = string.Empty;

    public EntraAuthStateProvider()
    {
        Debug.WriteLine("Constructing ExternalAuthStateProvider.");
        InitializeMsalWithCache();
        if (LoginStatus == LoginStatus.Failed)
        {
            LoginStatus = LoginStatus.None; //Don't show error by default on interactive login page
        }
    }

    private async Task<IPublicClientApplication> InitializeMsalWithCache()
    {
        try
        {
            //TODO add broker for WinUI

            // Create PublicClientApplication once. Make sure that all the config parameters below are passed
            publicClientApp = PublicClientApplicationBuilder
                .Create(AppConstants.ClientId)
                .WithTenantId(AppConstants.TenantId)
                .WithLegacyCacheCompatibility(false)
                .WithExperimentalFeatures() // this is for upcoming logger
                .WithLogging(_logger, true)
#if IOS || IPADOS
                .WithIosKeychainSecurityGroup("com.o-leary.MauiBlazorWeb")
#elif ANDROID
                .WithParentActivityOrWindow(() => Platform.CurrentActivity)
#endif
                .WithRedirectUri(PlatformConfig.Instance.RedirectUri)
                .Build();

            await RegisterMsalCacheAsync(publicClientApp.UserTokenCache);

            //restore previous login
            await AttemptUserSetup();

            return publicClientApp;
        }

        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static async Task RegisterMsalCacheAsync(ITokenCache tokenCache)
    {
        Console.WriteLine(DeviceInfo.Platform.ToString());
        try
        {
#if WINDOWS
			// Configure storage properties for cross-platform
			// See https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache
			StorageCreationProperties storageProperties = new StorageCreationPropertiesBuilder("myapp_msal_cache", MsalCacheHelper.UserRootDirectory)
				.Build();

			// Create a cache helper
			MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

			// Connect the PublicClientApplication's cache with the cacheHelper.
			// This will cause the cache to persist into secure storage on the device.
			cacheHelper.RegisterCache(tokenCache);
#else
            await Task.CompletedTask;

#endif
        }
        catch (MsalCachePersistenceException ex)
        {
            Console.WriteLine(ex);
        }
        catch (MsalException ex)
        {
            Console.WriteLine(ex);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task<string> GetAccessToken()
    {
        await GetTokenSilentlyAsync();
        if(_authResult != null)
        {
            return Task.FromResult<string>(_authResult.AccessToken).Result;
        }
        else
        {
            return null;
        }
    }

    private async Task<AuthenticationResult?> GetTokenInteractivelyAsync()
    {
        try
        {
            _authResult = await publicClientApp.AcquireTokenInteractive(AppConstants.Scopes)
#if ANDROID || IOS || IPADOS
                .WithUseEmbeddedWebView(true)
#endif
                .ExecuteAsync(CancellationToken.None);

            // Store the user ID to make account retrieval easier
            userIdentifier = _authResult.Account.HomeAccountId.Identifier;
            return _authResult;
        }
        catch (MsalException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    private async Task<AuthenticationResult?> GetTokenSilentlyAsync()
    {
        try
        {
            IAccount? account = await GetUserAccountAsync();
            _authResult = account is null
                ? null
                : await publicClientApp.AcquireTokenSilent(AppConstants.Scopes, account)
                    .ExecuteAsync(CancellationToken.None);

            return _authResult;
        }
        catch (MsalUiRequiredException exception)
        {
            Console.WriteLine(exception);
            return null;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return null;
        }
    }

    private async Task<IAccount?> GetUserAccountAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(userIdentifier))
            {
                // If no saved user ID, then get the first account.
                // There should only be one account in the cache anyway.
                IEnumerable<IAccount> accounts = await publicClientApp.GetAccountsAsync();
                IAccount? account = accounts.FirstOrDefault();

                // Save the user ID so this is easier next time
                userIdentifier = account?.HomeAccountId.Identifier ?? string.Empty;
                return account;
            }

            // If there's a saved user ID use it to get the account
            return await publicClientApp.GetAccountAsync(userIdentifier);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return null;
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(currentUser));

    public Task LogInAsync()
    {
        var loginTask = LogInAsyncCore();
        NotifyAuthenticationStateChanged(loginTask);

        return loginTask;

        async Task<AuthenticationState> LogInAsyncCore()
        {
            await LoginWithEntra();
            
            return new AuthenticationState(currentUser);
        }
    }

    private async Task<ClaimsPrincipal> AttemptUserSetup()
    {
        LoginStatus = LoginStatus.Failed;

        try
        {
            IAccount? account = await GetUserAccountAsync();
            if (account != null)
            {
                LoginStatus = LoginStatus.Success;
                var user = account.GetTenantProfiles().First().ClaimsPrincipal;
                currentUser = new ClaimsPrincipal(new ClaimsIdentity(user.Claims, "Bearer"));

                //Add Roles
                var additionalIdentity = new ClaimsIdentity();
                List<Claim> roles = currentUser.FindAll("roles").ToList();
                foreach (Claim role in roles)
                {    
                    additionalIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Value));

                }
                var combinedIdentities = currentUser.Identities.ToList();
                combinedIdentities.Add(additionalIdentity);
                currentUser = new ClaimsPrincipal(combinedIdentities);

                if (currentUser.IsInRole("User"))
                {
                    Debug.WriteLine("User role is assigned");
                }
                else
                {
                    Debug.WriteLine("User role is not assigned");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"User state restore error: {ex.Message}");
        }

        var cas = new TaskCompletionSource<AuthenticationState>();
        cas.SetResult(new AuthenticationState(currentUser));
        NotifyAuthenticationStateChanged(cas.Task);

        return currentUser;
    }

    private async Task<ClaimsPrincipal> LoginWithEntra()
    {
        var tryInteractiveLogin = false;
        LoginStatus = LoginStatus.Failed;
        currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        try
        {
            _authResult = await GetTokenSilentlyAsync();
            if (_authResult != null)
            {
                LoginStatus = LoginStatus.Success;
            }
            else
            {
                tryInteractiveLogin = true;
            }
        }
        catch (MsalUiRequiredException)
        {
            tryInteractiveLogin = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MSAL Silent Error: {ex.Message}");
            tryInteractiveLogin = true;
        }

        if (tryInteractiveLogin)
        {
            try
            {
                _authResult = await GetTokenInteractivelyAsync();
                if (_authResult != null)
                {
                    LoginStatus = LoginStatus.Success;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MSAL Interactive Error: {ex.Message}");
                LoginFailureMessage = ex.Message;
            }
        }

        if (LoginStatus == LoginStatus.Success)
        {
            AttemptUserSetup();
        }
        else
        {
            Logout();
        }

        return currentUser;
    }

    public async void Logout()
    {
        IEnumerable<IAccount> accounts = await publicClientApp.GetAccountsAsync();
        foreach (IAccount account in accounts)
        {
            await publicClientApp.RemoveAsync(account).ConfigureAwait(false);
        }
        currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        userIdentifier = string.Empty;
        LoginStatus = LoginStatus.None;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(currentUser)));
    }

    private MyLogger _logger = new MyLogger();

    // Custom logger class
    private class MyLogger : IIdentityLogger
    {
        /// <summary>
        /// Checks if log is enabled or not based on the Entry level
        /// </summary>
        /// <param name="eventLogLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            //Try to pull the log level from an environment variable
            var msalEnvLogLevel = Environment.GetEnvironmentVariable("MSAL_LOG_LEVEL");

            EventLogLevel envLogLevel = EventLogLevel.Informational;
            Enum.TryParse<EventLogLevel>(msalEnvLogLevel, out envLogLevel);

            return envLogLevel <= eventLogLevel;
        }

        /// <summary>
        /// Log to console for demo purpose
        /// </summary>
        /// <param name="entry">Log Entry values</param>
        public void Log(LogEntry entry)
        {
            Debug.WriteLine(entry.Message);
        }
    }
}