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
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
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
