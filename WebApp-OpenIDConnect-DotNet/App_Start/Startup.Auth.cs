//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// The following using statements were added for this sample.
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin.Security.Notifications;
using System.IdentityModel.Tokens;
using System.Net.Http;
using WebApp_OpenIDConnect_DotNet.Utils;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Claims;
using WebApp_OpenIDConnect_DotNet.App_Start;

namespace WebApp_OpenIDConnect_DotNet
{
    public partial class Startup
    {
        public static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        public static string clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];
        public static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            OpenIdConnectAuthenticationOptions options = new OpenIdConnectAuthenticationOptions
            {
                ClientId = clientId,
                Authority = String.Format(CultureInfo.InvariantCulture, aadInstance, "common/v2.0"),
                RedirectUri = redirectUri,
                Scope = "openid",
                PostLogoutRedirectUri = redirectUri,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false, // Not a good idea!
                },
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthenticationFailed = OnAuthenticationFailed,
                    AuthorizationCodeReceived = OnAuthorizationCodeReceived
                }
            };

            app.Use(typeof(ConvergenceOIDCMiddleware), app, options);
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification)
        {
            // Get the scopes from the authN properties, which was encoded in the state parameter in the request.
            string scopeString = null;
            notification.AuthenticationTicket.Properties.Dictionary.TryGetValue(ConvergenceOIDCHandler.ScopeKey, out scopeString);
            char[] space = new char[] {' '};
            string[] scopes = scopeString.Split(space);

            // Get the user's info for caching
            string userObjectId = notification.AuthenticationTicket.Identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            string tenantID = notification.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenantID);

            // Get an access_token for the scopes on the request (OK b/c we're only using 1 resource)
            ClientCredential cred = new ClientCredential(clientId, clientSecret);
            var authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authority, new NaiveSessionCache(userObjectId));
            var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(notification.Code, new Uri(redirectUri), cred, scopes);
        }

        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();
            notification.Response.Redirect("/Error?message=" + notification.Exception.Message);
            return Task.FromResult(0);
        }
    }
}