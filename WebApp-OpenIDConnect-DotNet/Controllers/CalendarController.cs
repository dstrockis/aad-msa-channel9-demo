using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebApp_OpenIDConnect_DotNet.App_Start;
using WebApp_OpenIDConnect_DotNet.Utils;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    public class CalendarController : Controller
    {
        public static string[] ReadScope = new[] { "https://outlook.office.com/calendars.read" };
        public static string[] WriteScope = new[] { "https://outlook.office.com/calendars.readwrite" };

        // GET: Calendar
        public async Task<ActionResult> Index(string authError)
        {
            if (!Request.IsAuthenticated)
            {
                // Specify the scopes we need to satisfy in the challenge, space-separated.
                Dictionary<string, string> scopeDict = new Dictionary<string, string>() { { ConvergenceOIDCHandler.ScopeKey, ReadScope[0] } };
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties(scopeDict) { RedirectUri = "/Calendar" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return new HttpUnauthorizedResult();
            }


            // TODO - Get Tokens, Call O365
            AuthenticationResult result = null;

            try
            {
                // Get an access_token
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
                string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                string authority = String.Format(CultureInfo.InvariantCulture, Startup.aadInstance, tenantID);
                AuthenticationContext authContext = new AuthenticationContext(authority, new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.clientSecret);
                result = await authContext.AcquireTokenSilentAsync(ReadScope, credential, UserIdentifier.AnyUser);

                // Read the user's calendar for this week
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://outlook.office.com/api/v1.0/me/calendarview?startDateTime=2015-07-26T01:00:00Z&endDateTime=2015-08-01T23:00:00Z");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.AuthError = true;
                    if (string.IsNullOrEmpty(authError))
                    {
                        ViewBag.AuthError = false;
                    }
                    String responseString = await response.Content.ReadAsStringAsync();
                    JObject mailResponse = JObject.Parse(responseString);
                    JArray messages = mailResponse["value"] as JArray;
                    ViewBag.Events = messages;
                    return View();
                }

                return new RedirectResult("/Error?message=An Error Occurred Reading Mail: " + response.StatusCode);
            }
            catch (AdalException ee)
            {
                return new RedirectResult("/GetConsent?write=false");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=An Error Occurred Reading Mail: " + ex.Message);
            }

        }







        // TODO - Challenge for more consent!
        [Authorize]
        public void GetConsent(bool write)
        {
            // Specify the scopes we need to satisfy in the challenge, space-separated.
            string scopes = ReadScope[0];
            if (write)
            {
                scopes += " " + WriteScope[0];
            }
            Dictionary<string, string> scopeDict = new Dictionary<string, string>() { { ConvergenceOIDCHandler.ScopeKey, scopes } };
            HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties(scopeDict) { RedirectUri = "/Calendar" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }





        // TODO - The same token acquisition & O365 Call
        [Authorize]
        public async Task<ActionResult> AddEvent(string Day, string Time, string Title)
        {
            AuthenticationResult result = null;

            try
            {
                // Get an access_token
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
                string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                string authority = String.Format(CultureInfo.InvariantCulture, Startup.aadInstance, tenantID);
                AuthenticationContext authContext = new AuthenticationContext(authority, new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.clientSecret);
                result = await authContext.AcquireTokenSilentAsync(WriteScope, credential, UserIdentifier.AnyUser);

                // Create POST Data
                int time = Int32.Parse(Time);
                string json = new JavaScriptSerializer().Serialize(new
                {
                    Subject = Title,
                    Start = Day + "T" + time.ToString() + ":00:00Z",
                    End = Day + "T" + (time + 1).ToString() + ":00:00Z"
                });
                
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://outlook.office.com/api/v1.0/me/events");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                { 
                    return Redirect("/Calendar/Index?authError=true");
                }

                return new RedirectResult("/Error?message=An Error Occurred Reading Mail: " + response.StatusCode);
            }
            catch (AdalException ee) 
            {
               return Redirect("/Calendar/Index?authError=true");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=An Error Occurred Reading Mail: " + ex.Message);
            }
        }
    }
}