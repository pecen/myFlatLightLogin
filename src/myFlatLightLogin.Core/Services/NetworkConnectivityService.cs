using Serilog;
using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service for monitoring network connectivity status.
    /// Uses actual internet reachability tests to handle VM environments properly.
    /// </summary>
    public class NetworkConnectivityService : IDisposable
    {
        private static readonly ILogger _logger = Log.ForContext<NetworkConnectivityService>();
        private bool _isOnline;
        private readonly Timer _connectivityCheckTimer;
        private readonly object _lock = new();
        private bool _disposed;

        /// <summary>
        /// Event raised when network connectivity changes.
        /// </summary>
        public event EventHandler<bool> ConnectivityChanged;

        public NetworkConnectivityService()
        {
            // Do initial connectivity check
            _isOnline = CheckConnectivityWithReachabilityTest();
            _logger.Information("NetworkConnectivityService initialized - IsOnline: {IsOnline}", _isOnline);

            // Listen for network changes
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;

            // Start periodic connectivity check (every 10 seconds) for VM environments
            // where network events may not fire properly when host loses connectivity
            _connectivityCheckTimer = new Timer(
                OnConnectivityCheckTimer,
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Gets whether the device is currently online.
        /// </summary>
        public bool IsOnline => _isOnline;

        /// <summary>
        /// Checks if the device has internet connectivity by testing actual reachability.
        /// </summary>
        public bool CheckConnectivity()
        {
            return CheckConnectivityWithReachabilityTest();
        }

        /// <summary>
        /// Performs actual internet connectivity test using HTTP request.
        /// </summary>
        private bool CheckConnectivityWithReachabilityTest()
        {
            try
            {
                // First do a quick check if any network is available
                bool basicCheck = NetworkInterface.GetIsNetworkAvailable();
                if (!basicCheck)
                {
                    _logger.Debug("Basic network check failed - no network available");
                    return false;
                }

                // Do actual internet reachability test
                // This handles VM environments where virtual adapters appear "up"
                return TestInternetReachability();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CheckConnectivityWithReachabilityTest error");
                return false;
            }
        }

        /// <summary>
        /// Tests actual internet connectivity using a lightweight HTTP request.
        /// </summary>
        private bool TestInternetReachability()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(3);

                // Use Google's connectivity check endpoint (very lightweight)
                // Alternative: Microsoft's connectivity check at http://www.msftconnecttest.com/connecttest.txt
                var task = httpClient.GetAsync("http://www.gstatic.com/generate_204");
                task.Wait();

                var response = task.Result;
                bool isReachable = response.IsSuccessStatusCode || (int)response.StatusCode == 204;
                _logger.Debug("Internet reachability test - Status: {StatusCode}, Reachable: {IsReachable}",
                    response.StatusCode, isReachable);
                return isReachable;
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                _logger.Debug("Internet reachability test - Timed out (no connection)");
                return false;
            }
            catch (AggregateException ex) when (ex.InnerException is HttpRequestException)
            {
                _logger.Debug("Internet reachability test - HTTP error (no connection): {Message}",
                    ex.InnerException.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Debug("Internet reachability test - Error: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks if the device can reach Firebase servers.
        /// Uses HTTP request instead of ping to avoid ICMP blocking issues.
        /// </summary>
        public async Task<bool> CanReachFirebaseAsync()
        {
            if (!_isOnline)
            {
                _logger.Debug("CanReachFirebaseAsync: IsOnline is false");
                return false;
            }

            try
            {
                _logger.Debug("CanReachFirebaseAsync: Attempting HTTP request to Firebase...");
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                // Try to reach Firebase Auth REST API endpoint
                var response = await httpClient.GetAsync("https://identitytoolkit.googleapis.com/");

                // Any response (even 400/404) means we can reach Firebase servers
                _logger.Information("Firebase reachability test - Status: {StatusCode}, Success: true", response.StatusCode);
                return true;
            }
            catch (TaskCanceledException)
            {
                _logger.Warning("Firebase reachability test failed: Request timed out after 5 seconds");
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.Warning("Firebase reachability test failed: HTTP error - {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Warning("Firebase reachability test failed: Unexpected error - {Message}", ex.Message);
                return false;
            }
        }

        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            _logger.Debug("Network availability changed event - IsAvailable: {IsAvailable}", e.IsAvailable);
            // Do actual reachability test instead of trusting the event
            UpdateConnectivityStatus(CheckConnectivityWithReachabilityTest());
        }

        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            _logger.Debug("Network address changed event");
            // Do actual reachability test
            UpdateConnectivityStatus(CheckConnectivityWithReachabilityTest());
        }

        private void OnConnectivityCheckTimer(object state)
        {
            if (_disposed) return;

            // Periodic check for VM environments
            bool currentStatus = CheckConnectivityWithReachabilityTest();
            if (currentStatus != _isOnline)
            {
                _logger.Debug("Periodic connectivity check detected change: {OldStatus} -> {NewStatus}",
                    _isOnline, currentStatus);
                UpdateConnectivityStatus(currentStatus);
            }
        }

        private void UpdateConnectivityStatus(bool isOnline)
        {
            lock (_lock)
            {
                if (_isOnline != isOnline)
                {
                    _logger.Information("Connectivity changed: {OldStatus} -> {NewStatus}",
                        _isOnline ? "Online" : "Offline",
                        isOnline ? "Online" : "Offline");
                    _isOnline = isOnline;
                    ConnectivityChanged?.Invoke(this, _isOnline);
                }
            }
        }

        /// <summary>
        /// Cleanup event subscriptions and timer.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _connectivityCheckTimer?.Dispose();
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
        }
    }
}
