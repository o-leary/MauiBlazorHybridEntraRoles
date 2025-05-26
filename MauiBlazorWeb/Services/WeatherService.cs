using MauiBlazorWeb.Shared.Services;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MauiBlazorWeb.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly EntraAuthStateProvider _authenticationStateProvider;

        public WeatherService(EntraAuthStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
        {
            var forecasts = Array.Empty<WeatherForecast>();
            try
            {
                var httpClient = HttpClientHelper.GetHttpClient();
                var weatherUrl = HttpClientHelper.WeatherUrl;


                var accessToken = await _authenticationStateProvider.GetAccessToken();

                if (accessToken is null)
                {
                    throw new Exception("Could not retrieve access token to get weather forecast.");
                }

                var scheme = "Bearer";

                if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(scheme))
                {
                    //Output access token for inspection
                    //Debug.WriteLine($"Token: {accessToken}");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, accessToken);
                    forecasts = (await httpClient.GetFromJsonAsync<WeatherForecast[]>(weatherUrl)) ?? [];
                }
                else
                {
                    Debug.WriteLine("Token or scheme is null or empty.");
                    _authenticationStateProvider.Logout();
                }
            }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Debug.WriteLine("API Request was unauthorized.");
                    // When you have things working uncomment the following line to force logout on disallowed
                    //_authenticationStateProvider.Logout();
                }
                Debug.WriteLine($"HTTP Request error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }

            return forecasts;
        }
    }
}
