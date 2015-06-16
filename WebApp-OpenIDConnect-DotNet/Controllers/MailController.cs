using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApp_OpenIDConnect_DotNet.Utils;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    [Authorize]
    public class MailController : Controller
    {
        // GET: Mail
        public async Task<ActionResult> Index()
        {
            AuthenticationResult result = null;

            try
            {
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                string authority = String.Format(CultureInfo.InvariantCulture, Startup.aadInstance, tenantID);
                AuthenticationContext authContext = new AuthenticationContext(authority, new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.clientSecret);
                result = await authContext.AcquireTokenSilentAsync(Startup.outlookScopes, credential, UserIdentifier.AnyUser);

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://outlook.office365.com/api/v1.0/me/messages");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
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
                return new RedirectResult("/Error?message=An Error Occurred Reading Mail: " + ee.Message + " You might need to log out and log back in.");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=An Error Occurred Reading Mail: " + ex.Message);
            }

        }
    }
}