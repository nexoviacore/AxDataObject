using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace ARMCommon.Helpers
{
    public class NetworkConnection : IDisposable
    {
        private readonly string _networkName;

        public NetworkConnection(string networkName, NetworkCredential credentials)
        {
            _networkName = networkName;

            var netResource = new NetResource
            {
                lpRemoteName = networkName
            };

            string userName = credentials.UserName;

            if (!string.IsNullOrEmpty(credentials.Domain))
            {
                userName = $@"{credentials.Domain}\{credentials.UserName}";
            }

            int result = WNetAddConnection2(
                netResource,
                credentials.Password,
                userName,
                0);

            if (result != 0)
            {
                WNetCancelConnection2(_networkName, 0, true);
                throw new IOException($"Error connecting to remote share: {networkName}. Credentials available: Yes. Error code: {result}");
            }
        }

        public void Dispose()
        {
            WNetCancelConnection2(_networkName, 0, true);
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(
            NetResource netResource,
            string password,
            string username,
            int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(
            string name,
            int flags,
            bool force);
    }

    [StructLayout(LayoutKind.Sequential)]
    public class NetResource
    {
        public int dwScope = 2;        // RESOURCE_GLOBALNET
        public int dwType = 1;         // RESOURCETYPE_DISK
        public int dwDisplayType = 3;  // RESOURCEDISPLAYTYPE_SHARE
        public int dwUsage = 0;

        public string lpLocalName = null;
        public string lpRemoteName = null;
        public string lpComment = null;
        public string lpProvider = null;
    }
}