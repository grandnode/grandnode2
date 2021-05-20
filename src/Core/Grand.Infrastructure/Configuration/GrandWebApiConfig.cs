using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Infrastructure.Configuration
{
    public class GrandWebApiConfig
    {
        public static string Scheme => "GrandWebBearerScheme";
        public bool Enabled { get; set; }
        public string SecretKey { get; set; }
        public bool ValidateIssuer { get; set; }
        public string ValidIssuer { get; set; }
        public bool ValidateAudience { get; set; }
        public string ValidAudience { get; set; }
        public bool ValidateLifetime { get; set; }
        public bool ValidateIssuerSigningKey { get; set; }
        public int ExpiryInMinutes { get; set; }
        public int RefreshTokenExpiryInMinutes { get; set; }
    }
}
