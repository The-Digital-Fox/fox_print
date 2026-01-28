using System;
using System.Collections.Generic;

namespace FoxPrint
{
    /// <summary>
    /// FoxNest API environment
    /// </summary>
    public enum FoxNestEnvironment
    {
        /// <summary>
        /// Local development environment (http://localhost:3007)
        /// </summary>
        Local,

        /// <summary>
        /// Development environment (https://dev-go-v2.thedigitalfox.com)
        /// </summary>
        Development,

        /// <summary>
        /// UAT/Staging environment (https://uat-go-v2.thedigitalfox.com)
        /// </summary>
        Uat,

        /// <summary>
        /// Production environment (https://go.thedigitalfox.com)
        /// </summary>
        Production
    }

    /// <summary>
    /// Provides URL mappings for FoxNest environments
    /// </summary>
    public static class FoxNestEnvironments
    {
        private static readonly Dictionary<FoxNestEnvironment, string> EnvironmentUrls = new Dictionary<FoxNestEnvironment, string>
        {
            { FoxNestEnvironment.Local, "http://localhost:3007" },
            { FoxNestEnvironment.Development, "https://dev-go-v2.thedigitalfox.com" },
            { FoxNestEnvironment.Uat, "https://uat-go-v2.thedigitalfox.com" },
            { FoxNestEnvironment.Production, "https://go.thedigitalfox.com" }
        };

        /// <summary>
        /// Get the base URL for the specified environment
        /// </summary>
        /// <param name="environment">The target environment</param>
        /// <returns>The base URL for the environment</returns>
        public static string GetUrl(FoxNestEnvironment environment)
        {
            if (EnvironmentUrls.TryGetValue(environment, out string url))
            {
                return url;
            }

            throw new ArgumentException($"Unknown environment: {environment}", nameof(environment));
        }

        /// <summary>
        /// Local development environment URL
        /// </summary>
        public static string Local => EnvironmentUrls[FoxNestEnvironment.Local];

        /// <summary>
        /// Development environment URL
        /// </summary>
        public static string Development => EnvironmentUrls[FoxNestEnvironment.Development];

        /// <summary>
        /// UAT/Staging environment URL
        /// </summary>
        public static string Uat => EnvironmentUrls[FoxNestEnvironment.Uat];

        /// <summary>
        /// Production environment URL
        /// </summary>
        public static string Production => EnvironmentUrls[FoxNestEnvironment.Production];
    }
}
