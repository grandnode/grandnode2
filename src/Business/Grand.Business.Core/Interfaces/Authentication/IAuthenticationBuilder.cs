﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Grand.Business.Core.Interfaces.Authentication
{
    /// <summary>
    /// Interface to add authentication 
    /// </summary>
    public interface IAuthenticationBuilder
    {
        /// <summary>
        /// Add Authentication
        /// </summary>
        /// <param name="builder">Add Authentication builder</param>
        /// <param name="configuration">Configuration</param>
        void AddAuthentication(AuthenticationBuilder builder, IConfiguration configuration);

        /// <summary>
        /// Gets priority
        /// </summary>
        int Priority { get; }
    }
}
