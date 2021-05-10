using Grand.Domain.Configuration;
using System.Collections.Generic;

namespace Grand.Domain.Security
{
    public class SecuritySettings : ISettings
    {       
        /// <summary>
        /// Gets or sets a list of admin area allowed IP addresses
        /// </summary>
        public List<string> AdminAreaAllowedIpAddresses { get; set; }
    }
}