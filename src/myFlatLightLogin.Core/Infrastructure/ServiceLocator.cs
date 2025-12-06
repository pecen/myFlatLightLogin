using myFlatLightLogin.Core.Services;
using System;

namespace myFlatLightLogin.Core.Infrastructure
{
    /// <summary>
    /// Simple service locator for infrastructure services.
    /// Provides access to singleton instances of core infrastructure services
    /// that need to be shared across the application.
    ///
    /// This is initialized at application startup (composition root) and provides
    /// services to DAL implementations without coupling UI to infrastructure.
    /// </summary>
    public static class ServiceLocator
    {
        private static NetworkConnectivityService? _connectivityService;
        private static SyncService? _syncService;
        private static HybridUserDal? _hybridUserDal;

        /// <summary>
        /// Gets the singleton NetworkConnectivityService instance.
        /// </summary>
        public static NetworkConnectivityService ConnectivityService
        {
            get
            {
                if (_connectivityService == null)
                    throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");
                return _connectivityService;
            }
        }

        /// <summary>
        /// Gets the singleton SyncService instance.
        /// </summary>
        public static SyncService SyncService
        {
            get
            {
                if (_syncService == null)
                    throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");
                return _syncService;
            }
        }

        /// <summary>
        /// Gets the singleton HybridUserDal instance.
        /// </summary>
        public static HybridUserDal HybridUserDal
        {
            get
            {
                if (_hybridUserDal == null)
                    throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");
                return _hybridUserDal;
            }
        }

        /// <summary>
        /// Initializes the service locator with singleton instances.
        /// This should be called once at application startup (composition root).
        /// </summary>
        public static void Initialize(
            NetworkConnectivityService connectivityService,
            SyncService syncService,
            HybridUserDal hybridUserDal)
        {
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _hybridUserDal = hybridUserDal ?? throw new ArgumentNullException(nameof(hybridUserDal));
        }

        /// <summary>
        /// Clears all registered services. Used for testing or application shutdown.
        /// </summary>
        public static void Clear()
        {
            _connectivityService = null;
            _syncService = null;
            _hybridUserDal = null;
        }
    }
}
