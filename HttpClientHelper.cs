using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using Microsoft.AspNet.Identity;
using Moq;
using SHL.Bewaking.Data.Models;
using SHL.Common;
using SHL.Common.Constants;

namespace Helpers
{
    public static class HttpClientHelper
    {
        public static void MockPrincipal(Mock<HttpContextBase> httpContext, List<Claim> claims = null)
        {
            var identity = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            
            if (claims != null && claims.All(c => c.Type != ClaimTypes.NameIdentifier))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "TestUser"));
            }
            if (claims != null && claims.All(c => c.Type != ClaimTypes.Name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, "TestUser"));
            }

            if (claims != null)
                claims.ForEach(x => identity.AddClaim(x));

            httpContext.Setup(x => x.User).Returns(() => new ClaimsPrincipal(identity));
        }
    }
}
