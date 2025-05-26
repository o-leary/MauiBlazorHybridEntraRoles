using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiBlazorWeb.Services
{
	internal class AppConstants
	{

        internal const string ClientId = "";
        internal const string TenantId = "";
        public static string authority = "https://login.microsoftonline.com/" + TenantId;


        /// <summary>
        /// Scopes defining what app can access
        /// </summary>
        internal static string[] Scopes = { "api://{insert your api url}/API.Access" }; //Msal handles automatically: "offline_access" "openid", "profile", "email"
    }
}
