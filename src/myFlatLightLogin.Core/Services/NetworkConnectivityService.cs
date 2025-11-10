using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service for monitoring network connectivity status.
    /// </summary>
    public class NetworkConnectivityService
    {
        private bool _isOnline;

        /// <summary>
        /// Event raised when network connectivity changes.
        /// </summary>
        public event EventHandler<bool> ConnectivityChanged;

        public NetworkConnectivityService()
        {
            _isOnline = CheckConnectivity();
            Console.WriteLine($"[NetworkConnectivityService] Initialized - IsOnline: {_isOnline}");

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
                Console.WriteLine($"[NetworkConnectivityService] Basic network available: {basicCheck}");

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
                            Console.WriteLine($"[NetworkConnectivityService] Active interface found: {ni.Name} ({ni.NetworkInterfaceType})");
                            hasActiveInterface = true;
                            break;
                        }
                    }
                }

                Console.WriteLine($"[NetworkConnectivityService] Has active interface with gateway: {hasActiveInterface}");
                return hasActiveInterface;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkConnectivityService] CheckConnectivity error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the device can reach Firebase servers.
        /// </summary>
        public async Task<bool> CanReachFirebaseAsync()
        {
            if (!CheckConnectivity())
                return false;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("firebase.google.com", 3000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
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
                Console.WriteLine($"[NetworkConnectivityService] Connectivity changed: {_isOnline} -> {isOnline}");
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
