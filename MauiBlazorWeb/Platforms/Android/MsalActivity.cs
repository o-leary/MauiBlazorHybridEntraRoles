// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Android.App;
using Android.Content;
using MauiBlazorWeb.Services;
using Microsoft.Identity.Client;

namespace MauiAppBasic.Platforms.Android.Resources
{
    [Activity(Exported = true)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
        DataHost = "auth",
        DataScheme = $"msal{AppConstants.ClientId}")]
    public class MsalActivity : BrowserTabActivity
    {
    }
}