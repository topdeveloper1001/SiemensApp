using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SiemensApp.Domain
{
    public class AuthenticationOptions
    {
        public string Endpoint { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public static AuthenticationOptions Create(SiteConfiguration siteConfiguration)
        {
            return new AuthenticationOptions
            {
                Endpoint = siteConfiguration.Url,
                Username = siteConfiguration.UserName,
                Password = siteConfiguration.Password
            };
        }
    }
}
