using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Linq;
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
                        Password = null // Never return passwords
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

                // Store user profile in Realtime Database
                var profile = new FirebaseUserProfile
                {
                    LocalId = user.Id,
                    FirebaseUid = credential.User.Uid,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Email = user.Username,
                    CreatedAt = DateTime.UtcNow.ToString("o")
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

                var profile = new FirebaseUserProfile
                {
                    LocalId = user.Id,
                    FirebaseUid = _currentUser.User.Uid,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Email = user.Username,
                    UpdatedAt = DateTime.UtcNow.ToString("o")
                };

                var dbClient = GetAuthenticatedClient(_currentUser.User.Credential.IdToken);
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
                    return new UserDto
                    {
                        Id = profile.LocalId,
                        Name = profile.Name,
                        Lastname = profile.Lastname,
                        Username = profile.Email,
                        Email = profile.Email,
                        FirebaseUid = profile.FirebaseUid
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
            _authClient?.SignOut();
            _currentUser = null;
        }

        /// <summary>
        /// Gets the currently authenticated user.
        /// </summary>
        public UserCredential GetCurrentUser()
        {
            return _currentUser;
        }

        #endregion

        #region Helper Methods

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
    }
}
