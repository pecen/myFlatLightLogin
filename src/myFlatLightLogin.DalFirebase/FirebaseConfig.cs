namespace myFlatLightLogin.DalFirebase
{
    /// <summary>
    /// Firebase configuration settings.
    /// Replace these placeholder values with your actual Firebase project credentials.
    /// Get these from: Firebase Console -> Project Settings -> General -> Your apps -> Web app
    /// </summary>
    public static class FirebaseConfig
    {
        /// <summary>
        /// Firebase Web API Key.
        /// Get from: Firebase Console -> Project Settings -> General -> Web API Key
        /// </summary>
        public const string ApiKey = "YOUR_FIREBASE_API_KEY_HERE";

        /// <summary>
        /// Firebase Realtime Database URL.
        /// Format: https://YOUR-PROJECT-ID.firebaseio.com/
        /// Get from: Firebase Console -> Realtime Database -> Data tab (URL at top)
        /// </summary>
        public const string DatabaseUrl = "https://YOUR-PROJECT-ID.firebaseio.com/";

        /// <summary>
        /// Firebase Authentication domain.
        /// Format: YOUR-PROJECT-ID.firebaseapp.com
        /// </summary>
        public const string AuthDomain = "YOUR-PROJECT-ID.firebaseapp.com";
    }
}
