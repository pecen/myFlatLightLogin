using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace myFlatLightLogin.DalFirebase
{
    /// <summary>
    /// Firebase configuration settings loaded from appsettings.json or User Secrets.
    /// This provides secure storage of Firebase credentials without hardcoding them in source code.
    /// </summary>
    public static class FirebaseConfig
    {
        private static readonly Lazy<IConfiguration> _configuration = new Lazy<IConfiguration>(BuildConfiguration);

        /// <summary>
        /// Firebase Web API Key.
        /// Get from: Firebase Console -> Project Settings -> General -> Web API Key
        /// </summary>
        public static string ApiKey => GetConfigValue("Firebase:ApiKey");

        /// <summary>
        /// Firebase Realtime Database URL.
        /// Format: https://YOUR-PROJECT-ID.firebaseio.com/ or
        /// https://YOUR-PROJECT-ID.REGION.firebasedatabase.app/
        /// Get from: Firebase Console -> Realtime Database -> Data tab (URL at top)
        /// </summary>
        public static string DatabaseUrl => GetConfigValue("Firebase:DatabaseUrl");

        /// <summary>
        /// Firebase Authentication domain.
        /// Format: YOUR-PROJECT-ID.firebaseapp.com
        /// </summary>
        public static string AuthDomain => GetConfigValue("Firebase:AuthDomain");

        /// <summary>
        /// Builds the configuration from appsettings.json and User Secrets.
        /// </summary>
        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // Add User Secrets in development
#if DEBUG
            builder.AddUserSecrets<FirebaseConfigSecrets>();
#endif

            return builder.Build();
        }

        /// <summary>
        /// Gets a configuration value and validates it's not empty.
        /// </summary>
        private static string GetConfigValue(string key)
        {
            var value = _configuration.Value[key];

            if (string.IsNullOrWhiteSpace(value) ||
                value.Contains("YOUR_") ||
                value.Contains("YOUR-PROJECT-ID"))
            {
                throw new InvalidOperationException(
                    $"Firebase configuration '{key}' is not set. " +
                    $"Please configure your Firebase credentials in appsettings.json or User Secrets. " +
                    $"See FIREBASE_SETUP.md for instructions.");
            }

            return value;
        }
    }

    /// <summary>
    /// Marker class for User Secrets. This class is never instantiated.
    /// </summary>
    internal class FirebaseConfigSecrets { }
}
