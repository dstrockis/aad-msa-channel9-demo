using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApp_OpenIDConnect_DotNet.App_Start;
using WebApp_OpenIDConnect_DotNet.Utils;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    //[Authorize]
    public class CalendarController : Controller
    {
        public static string[] ReadScope = new[] { "https://outlook.office.com/calendars.read" };
        public static string[] WriteScope = new[] { "https://outlook.office.com/calendars.write" };

        // GET: Calendar
        public async Task<ActionResult> Index(bool authError)
        {
            return View();

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
                    ViewBag.AuthError = authError;
                    String responseString = await response.Content.ReadAsStringAsync();
                    JObject mailResponse = JObject.Parse(responseString);
                    JArray messages = mailResponse["value"] as JArray;
                    ViewBag.Mails = messages;
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

        public void GetConsent(bool write)
        {
            // Specify the scopes we need to satisfy in the challenge, space-separated.
            string scopes = ReadScope.ToString();
            if (write)
            {
                scopes += " " + WriteScope.ToString();
            }
            Dictionary<string, string> scopeDict = new Dictionary<string, string>() { { ConvergenceOIDCHandler.ScopeKey, scopes } };
            HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties(scopeDict) { RedirectUri = "/Calendar" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        public async Task<ActionResult> AddEvent(string Day, int Time, string Title)
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


                // Forms encode event
                HttpContent content = new FormUrlEncodedContent(new[] { 
                    new KeyValuePair<string, string>("Subject", Title),
                    new KeyValuePair<string, string>("Start", Day + "T" + Time.ToString() + ":00:00Z"),
                    new KeyValuePair<string, string>("End", Day + "T" + (Time+1).ToString() + ":00:00Z"),
                });

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://outlook.office.com/api/v1.0/me/events");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                request.Content = content;
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