using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace myFlatLightLogin.DalFirebase
{
    /// <summary>
    /// Firebase implementation of IUserDal using Firebase Authentication and Realtime Database.
    /// </summary>
    public class UserDal : IUserDal
    {
        private readonly FirebaseAuthClient _authClient;
        private UserCredential? _currentUser;

        public UserDal()
        {
            // Initialize Firebase Authentication
            var authConfig = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
            };
            _authClient = new FirebaseAuthClient(authConfig);
        }

        /// <summary>
        /// Creates an authenticated FirebaseClient using the provided auth token.
        /// </summary>
        private FirebaseClient GetAuthenticatedClient(string authToken)
        {
            var options = new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(authToken)
            };
            return new FirebaseClient(FirebaseConfig.DatabaseUrl, options);
        }

        #region IUserDal Implementation

        /// <summary>
        /// Fetches user by ID. Note: Firebase uses string UIDs, so this converts int to lookup.
        /// For proper Firebase implementation, use FetchByFirebaseUidAsync instead.
        /// </summary>
        public UserDto Fetch(int id)
        {
            // This is a synchronous wrapper - not ideal for Firebase
            // In practice, you should use async methods
            return FetchAsync(id).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Inserts (registers) a new user with Firebase Authentication and stores profile in Realtime DB.
        /// </summary>
        public bool Insert(UserDto user)
        {
            return InsertAsync(user).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates user profile in Firebase Realtime Database.
        /// </summary>
        public bool Update(UserDto user)
        {
            return UpdateAsync(user).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes user from Firebase Authentication and Realtime Database.
        /// </summary>
        public bool Delete(int id)
        {
            return DeleteAsync(id).GetAwaiter().GetResult();
        }

        #endregion

        #region Async Methods (Recommended for Firebase)

        /// <summary>
        /// Fetches user asynchronously by ID.
        /// Note: With Firebase security rules, this only works for the currently authenticated user.
        /// </summary>
        private async Task<UserDto> FetchAsync(int id)
        {
            try
            {
                if (_currentUser?.User == null)
                    throw new InvalidOperationException("No authenticated user. Please sign in first.");

                // With security rules, we can only fetch the current user's data
                var dbClient = GetAuthenticatedClient(_currentUser.User.Credential.IdToken);
                var profile = await dbClient
                    .Child("users")
                    .Child(_currentUser.User.Uid)
                    .OnceSingleAsync<FirebaseUserProfile>();

                if (profile != null && profile.LocalId == id)
                {
                    return new UserDto
                    {
                        Id = profile.LocalId,
                        Name = profile.Name,
                        Lastname = profile.Lastname,
                        Username = profile.Email,
                        Password = null, // Never return passwords
                        RegistrationDate = profile.CreatedAt,
                        Role = (UserRole)profile.Role // Convert integer to enum
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Registers a new user with Firebase Authentication and stores profile.
        /// Updates the UserDto with the Firebase UID upon successful creation.
        /// </summary>
        private async Task<bool> InsertAsync(UserDto user)
        {
            try
            {
                // Create user with Firebase Authentication
                var credential = await _authClient.CreateUserWithEmailAndPasswordAsync(
                    user.Username, // Using Username as Email
                    user.Password);

                if (credential?.User == null)
                    return false;

                // IMPORTANT: Update the DTO with the Firebase UID so it can be stored in SQLite
                user.FirebaseUid = credential.User.Uid;

                // Store user profile in Realtime Database
                var profile = new FirebaseUserProfile
                {
                    LocalId = user.Id,
                    FirebaseUid = credential.User.Uid,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Email = user.Username,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    Role = (int)user.Role // Store role as integer
                };

                // Use authenticated client to store user profile
                var dbClient = GetAuthenticatedClient(credential.User.Credential.IdToken);
                await dbClient
                    .Child("users")
                    .Child(credential.User.Uid)
                    .PutAsync(profile);

                _currentUser = credential;
                return true;
            }
            catch (FirebaseAuthException ex)
            {
                throw new Exception($"Firebase authentication failed: {GetFriendlyErrorMessage(ex)}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to insert user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates user profile in Firebase Realtime Database.
        /// </summary>
        private async Task<bool> UpdateAsync(UserDto user)
        {
            try
            {
                if (_currentUser?.User == null)
                    throw new InvalidOperationException("No authenticated user");

                var dbClient = GetAuthenticatedClient(_currentUser.User.Credential.IdToken);

                // Fetch existing profile to preserve CreatedAt
                var existingProfile = await dbClient
                    .Child("users")
                    .Child(_currentUser.User.Uid)
                    .OnceSingleAsync<FirebaseUserProfile>();

                var profile = new FirebaseUserProfile
                {
                    LocalId = user.Id,
                    FirebaseUid = _currentUser.User.Uid,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Email = user.Username,
                    CreatedAt = existingProfile?.CreatedAt ?? DateTime.UtcNow.ToString("o"), // Preserve CreatedAt or set if missing
                    UpdatedAt = DateTime.UtcNow.ToString("o"),
                    Role = (int)user.Role // Store role as integer
                };

                await dbClient
                    .Child("users")
                    .Child(_currentUser.User.Uid)
                    .PutAsync(profile);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes user from Firebase.
        /// Note: With Firebase security rules, this only works for the currently authenticated user.
        /// </summary>
        private async Task<bool> DeleteAsync(int id)
        {
            try
            {
                if (_currentUser?.User == null)
                    throw new InvalidOperationException("No authenticated user. Please sign in first.");

                // With security rules, we can only delete the current user's data
                var dbClient = GetAuthenticatedClient(_currentUser.User.Credential.IdToken);
                var profile = await dbClient
                    .Child("users")
                    .Child(_currentUser.User.Uid)
                    .OnceSingleAsync<FirebaseUserProfile>();

                // Only allow deletion if the ID matches the current user
                if (profile != null && profile.LocalId == id)
                {
                    // Delete from Realtime Database
                    await dbClient
                        .Child("users")
                        .Child(_currentUser.User.Uid)
                        .DeleteAsync();

                    // Note: Deleting from Firebase Authentication requires admin SDK
                    // or calling the delete method on the user account
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete user: {ex.Message}", ex);
            }
        }

        #endregion

        #region Additional Firebase Methods

        /// <summary>
        /// Authenticates a user with email and password.
        /// </summary>
        public async Task<UserDto> SignInAsync(string email, string password)
        {
            try
            {
                _currentUser = await _authClient.SignInWithEmailAndPasswordAsync(email, password);

                if (_currentUser?.User == null)
                    return null;

                // Fetch user profile from database with authentication
                var dbClient = GetAuthenticatedClient(_currentUser.User.Credential.IdToken);
                var profile = await dbClient
                    .Child("users")
                    .Child(_currentUser.User.Uid)
                    .OnceSingleAsync<FirebaseUserProfile>();

                if (profile != null)
                {
                    // Migration: If Role field is not explicitly set (default 0), update the profile
                    // This ensures existing users without Role field get it added to Firebase
                    bool needsRoleUpdate = false;
                    if (profile.Role == 0 && string.IsNullOrEmpty(profile.UpdatedAt))
                    {
                        // User created before Role field existed, update with default role
                        profile.Role = 0; // Explicitly set to User role
                        profile.UpdatedAt = DateTime.UtcNow.ToString("o");
                        needsRoleUpdate = true;
                    }

                    // Update profile in Firebase if needed (adds Role field for legacy users)
                    if (needsRoleUpdate)
                    {
                        await dbClient
                            .Child("users")
                            .Child(_currentUser.User.Uid)
                            .PutAsync(profile);
                    }

                    return new UserDto
                    {
                        Id = profile.LocalId,
                        Name = profile.Name,
                        Lastname = profile.Lastname,
                        Username = profile.Email,
                        Email = profile.Email,
                        FirebaseUid = profile.FirebaseUid,
                        FirebaseAuthToken = _currentUser.User.Credential.IdToken, // Store auth token for authenticated API calls
                        RegistrationDate = profile.CreatedAt,
                        Role = (UserRole)profile.Role // Convert integer to enum
                    };
                }

                return null;
            }
            catch (FirebaseAuthException ex)
            {
                throw new Exception(GetFriendlyErrorMessage(ex), ex);
            }
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        public void SignOut()
        {
            // Only sign out if there's actually a user signed in
            if (_currentUser != null)
            {
                _authClient?.SignOut();
                _currentUser = null;
            }
        }

        /// <summary>
        /// Gets the currently authenticated user.
        /// </summary>
        public UserCredential GetCurrentUser()
        {
            return _currentUser;
        }

        /// <summary>
        /// Updates the current user's password in Firebase Authentication.
        /// Requires an active authenticated session.
        /// </summary>
        /// <param name="newPassword">New password (plain text)</param>
        /// <returns>True if password was updated successfully</returns>
        public async Task<bool> UpdatePasswordAsync(string newPassword)
        {
            try
            {
                if (_currentUser?.User == null)
                    throw new InvalidOperationException("No authenticated user. Please sign in first.");

                // Use Firebase Authentication REST API to update password
                await ChangePasswordViaRestApiAsync(_currentUser.User.Credential.IdToken, newPassword);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates user password using old password for authentication.
        /// Used for syncing password changes made offline.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="oldPassword">Old password (for re-authentication)</param>
        /// <param name="newPassword">New password to set</param>
        /// <returns>True if password was updated successfully</returns>
        public async Task<bool> UpdatePasswordWithOldPasswordAsync(
            string email,
            string oldPassword,
            string newPassword)
        {
            try
            {
                // 1. Sign in with old password to get fresh credentials
                var credential = await _authClient.SignInWithEmailAndPasswordAsync(email, oldPassword);

                if (credential?.User == null)
                    return false;

                // 2. Update to new password using Firebase REST API
                await ChangePasswordViaRestApiAsync(credential.User.Credential.IdToken, newPassword);

                // 3. Update current user session
                _currentUser = credential;

                return true;
            }
            catch (FirebaseAuthException ex)
            {
                throw new Exception($"Failed to update password: {GetFriendlyErrorMessage(ex)}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update password: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Changes user password using Firebase Authentication REST API.
        /// </summary>
        /// <param name="idToken">User's ID token</param>
        /// <param name="newPassword">New password to set</param>
        private async Task ChangePasswordViaRestApiAsync(string idToken, string newPassword)
        {
            using (var httpClient = new HttpClient())
            {
                var requestUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={FirebaseConfig.ApiKey}";

                var requestBody = new
                {
                    idToken = idToken,
                    password = newPassword,
                    returnSecureToken = true
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var friendlyError = ParseFirebaseRestApiError(errorContent);
                    throw new Exception(friendlyError);
                }
            }
        }

        /// <summary>
        /// Parses Firebase REST API error JSON and returns a user-friendly message.
        /// </summary>
        private string ParseFirebaseRestApiError(string jsonError)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonError))
                {
                    if (doc.RootElement.TryGetProperty("error", out JsonElement errorElement))
                    {
                        if (errorElement.TryGetProperty("message", out JsonElement messageElement))
                        {
                            string errorCode = messageElement.GetString() ?? "";

                            return errorCode switch
                            {
                                "TOKEN_EXPIRED" => "TOKEN_EXPIRED",  // Special case - will be handled by caller
                                "INVALID_ID_TOKEN" => "Your session is invalid. Please sign in again.",
                                "USER_NOT_FOUND" => "User account not found.",
                                "INVALID_PASSWORD" => "The password provided is invalid.",
                                "WEAK_PASSWORD" => "Password is too weak. Please use at least 6 characters.",
                                "EMAIL_EXISTS" => "An account with this email already exists.",
                                "EMAIL_NOT_FOUND" => "No account found with this email address.",
                                "INVALID_EMAIL" => "The email address is invalid.",
                                "MISSING_PASSWORD" => "Please provide a password.",
                                "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many unsuccessful attempts. Please try again later.",
                                "USER_DISABLED" => "This account has been disabled.",
                                "OPERATION_NOT_ALLOWED" => "This operation is not allowed.",
                                _ => $"Authentication error: {errorCode}"
                            };
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, return a generic message
            }

            return "An error occurred while updating your password. Please try again.";
        }

        /// <summary>
        /// Converts Firebase authentication exceptions to user-friendly messages.
        /// </summary>
        private string GetFriendlyErrorMessage(FirebaseAuthException ex)
        {
            return ex.Reason switch
            {
                AuthErrorReason.InvalidEmailAddress => "The email address is invalid.",
                AuthErrorReason.WrongPassword => "The password is incorrect.",
                AuthErrorReason.UserNotFound => "No account found with this email address.",
                AuthErrorReason.EmailExists => "An account with this email already exists.",
                AuthErrorReason.WeakPassword => "Password is too weak. Please use at least 6 characters.",
                AuthErrorReason.TooManyAttemptsTryLater => "Too many unsuccessful attempts. Please try again later.",
                AuthErrorReason.UserDisabled => "This account has been disabled.",
                AuthErrorReason.OperationNotAllowed => "Email/password sign-in is not enabled.",
                _ => $"Authentication error: {ex.Message}"
            };
        }

        #endregion
    }

    /// <summary>
    /// Firebase user profile model for Realtime Database.
    /// </summary>
    internal class FirebaseUserProfile
    {
        public int LocalId { get; set; }
        public string FirebaseUid { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }

        /// <summary>
        /// User's role. 0 = User (default), 1 = Admin
        /// </summary>
        public int Role { get; set; } = 0; // Default to User role
    }
}
