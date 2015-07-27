using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApp_OpenIDConnect_DotNet.App_Start
{
    class ConvergenceOIDCHandler : OpenIdConnectAuthenticationHandler
    {
        public static string ScopeKey = "scope";
        public static string OpenIdScope = "openid";
        private readonly ILogger _logger;

        public ConvergenceOIDCHandler(ILogger logger)
            : base(logger)
        {
            _logger = logger;
        }

        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401)
            {
                AuthenticationResponseChallenge challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);
                if (challenge == null)
                {
                    return Task.FromResult(0);
                }

                Options.Scope = OpenIdScope + " " + Options.Scope; 
                AuthenticationProperties properties = challenge.Properties;
                string scopes = null;
                if (properties.Dictionary.TryGetValue(ScopeKey, out scopes))
                {
                    Options.Scope = OpenIdScope + " " + scopes;
                }
            }

            return base.ApplyResponseChallengeAsync();
        }
    }
}
