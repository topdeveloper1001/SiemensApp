using Microsoft.AspNetCore.Authentication;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Willow.Tests.Infrastructure.Security
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Headers.TryGetValue("Authorization", out StringValues authorizationValues))
            {
                if (authorizationValues.Count > 0)
                {
                    var tokenJson = authorizationValues[0].Substring(TestAuthToken.TestScheme.Length + 1);
                    var token = JsonConvert.DeserializeObject<TestAuthToken>(tokenJson);
                    var claims = new List<Claim>();

                    claims.Add(new Claim(ClaimTypes.UserData, token.UserId.ToString()));

                    if (token.Roles != null)
                    {
                        claims.AddRange(token.Roles.Select(role => new Claim(ClaimTypes.Role, role, null, "https:///")));
                    }

                    if (!string.IsNullOrWhiteSpace(token.Auth0UserId))
                    {
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, token.Auth0UserId));
                    }

                    var identity = new ClaimsIdentity(claims.ToArray(), "test");

                    var authenticationTicket = new AuthenticationTicket(
                        new ClaimsPrincipal(identity),
                        new AuthenticationProperties(),
                        TestAuthToken.TestScheme);
                    return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
                }
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }



    }
}
