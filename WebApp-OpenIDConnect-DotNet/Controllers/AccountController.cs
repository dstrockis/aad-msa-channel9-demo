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
using System.Web.Mvc;

// The following using statements were added for this sample.
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;
using WebApp_OpenIDConnect_DotNet.App_Start;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    public class AccountController : Controller
    {
        public void SignIn()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);

            // Specify the scopes we need to satisfy in the challenge, space-separated.
            Dictionary<string, string> scopeDict = new Dictionary<string, string>() { { ConvergenceOIDCHandler.ScopeKey, CalendarController.ReadScope[0] } };
            HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties(scopeDict) { RedirectUri = "/" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }
	}
}