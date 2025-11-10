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
        public const string ApiKey = "AIzaSyAyrrr5EVSChCNWIZEPJxL7-C7rE-UUig4";

        /// <summary>
        /// Firebase Realtime Database URL.
        /// Format: https://YOUR-PROJECT-ID.firebaseio.com/. Actually it seems like Google have changed the format to
        /// https://YOUR-PROJECT-ID.REGION.firebasedatabase.app/ (my amendment)
        /// Get from: Firebase Console -> Realtime Database -> Data tab (URL at top)
        /// </summary>
        public const string DatabaseUrl = "https://myflatlightlogin-default-rtdb.europe-west1.firebasedatabase.app/";

        /// <summary>
        /// Firebase Authentication domain.
        /// Format: YOUR-PROJECT-ID.firebaseapp.com.
        /// </summary>
        public const string AuthDomain = "myflatlightlogin.firebaseapp.com";
    }
}
