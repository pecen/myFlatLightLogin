using Serilog;
using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service for monitoring network connectivity status.
    /// </summary>
    public class NetworkConnectivityService
    {
        private static readonly ILogger _logger = Log.ForContext<NetworkConnectivityService>();
        private bool _isOnline;

        /// <summary>
        /// Event raised when network connectivity changes.
        /// </summary>
        public event EventHandler<bool> ConnectivityChanged;

        public NetworkConnectivityService()
        {
            _isOnline = CheckConnectivity();
            _logger.Information("NetworkConnectivityService initialized - IsOnline: {IsOnline}", _isOnline);

            // Listen for network changes
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
        }

        /// <summary>
        /// Gets whether the device is currently online.
        /// </summary>
        public bool IsOnline => _isOnline;

        /// <summary>
        /// Checks if the device has internet connectivity.
        /// </summary>
        public bool CheckConnectivity()
        {
            try
            {
                // First check if any network is available
                bool basicCheck = NetworkInterface.GetIsNetworkAvailable();
                _logger.Debug("Basic network available: {BasicCheck}", basicCheck);

                if (!basicCheck)
                    return false;

                // Additional check: look for active network interfaces with gateway
                // This helps detect when WiFi is off but other interfaces (Bluetooth, VPN) are still up
                bool hasActiveInterface = false;
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in interfaces)
                {
                    // Skip loopback and tunnel interfaces
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    // Check if interface is up and has an IP address
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        var ipProps = ni.GetIPProperties();

                        // Check if interface has a gateway (indicates internet connectivity)
                        if (ipProps.GatewayAddresses.Count > 0)
                        {
                            _logger.Debug("Active interface found: {InterfaceName} ({InterfaceType})",
                                ni.Name, ni.NetworkInterfaceType);
                            hasActiveInterface = true;
                            break;
                        }
                    }
                }

                _logger.Debug("Has active interface with gateway: {HasActiveInterface}", hasActiveInterface);
                return hasActiveInterface;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CheckConnectivity error");
                return false;
            }
        }

        /// <summary>
        /// Checks if the device can reach Firebase servers.
        /// Uses HTTP request instead of ping to avoid ICMP blocking issues.
        /// </summary>
        public async Task<bool> CanReachFirebaseAsync()
        {
            if (!CheckConnectivity())
                return false;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                // Try to reach Firebase Auth REST API endpoint
                // This is a real endpoint that responds to HEAD/GET requests
                var response = await httpClient.GetAsync("https://identitytoolkit.googleapis.com/");

                // Any response (even 400/404) means we can reach Firebase servers
                // We just want to know if the network path is working
                _logger.Debug("Firebase reachability test - Status: {StatusCode}", response.StatusCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Firebase reachability test failed: {Message}", ex.Message);
                return false;
            }
        }

        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            UpdateConnectivityStatus(e.IsAvailable);
        }

        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            UpdateConnectivityStatus(CheckConnectivity());
        }

        private void UpdateConnectivityStatus(bool isOnline)
        {
            if (_isOnline != isOnline)
            {
                _logger.Information("Connectivity changed: {OldStatus} -> {NewStatus}", _isOnline, isOnline);
                _isOnline = isOnline;
                ConnectivityChanged?.Invoke(this, _isOnline);
            }
        }

        /// <summary>
        /// Cleanup event subscriptions.
        /// </summary>
        public void Dispose()
        {
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
        }
    }
}
