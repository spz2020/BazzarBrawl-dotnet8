using Supercell.Laser.Server.Networking.Session;
using System.Net;
using System.Net.Sockets;

namespace Supercell.Laser.Server.Networking
{
    public static class TCPGateway
    {
        private static List<Connection> ActiveConnections;
        private static Socket Socket;
        private static Thread Thread;
        private static ManualResetEvent AcceptEvent;

        private static Dictionary<IPAddress, ConnectionAttemptInfo> ConnectionAttempts;
        private static HashSet<IPAddress> BlockedIPs;

        private const int MaxAttempts = 3;
        private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(5);

        public static void Init(string host, int port)
        {
            ActiveConnections = new List<Connection>();
            ConnectionAttempts = new Dictionary<IPAddress, ConnectionAttemptInfo>();
            BlockedIPs = new HashSet<IPAddress>();

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            Socket.Listen(100);

            AcceptEvent = new ManualResetEvent(false);

            Thread = new Thread(TCPGateway.Update);
            Thread.Start();
        }

        private static void Update()
        {
            while (true)
            {
                AcceptEvent.Reset();
                Socket.BeginAccept(new AsyncCallback(OnAccept), null);
                AcceptEvent.WaitOne();
            }
        }

        private static void OnAccept(IAsyncResult ar)
        {
            AcceptEvent.Set();
            try
            {
                Socket client = Socket.EndAccept(ar);
                IPAddress clientIP = ((IPEndPoint)client.RemoteEndPoint).Address;

                if (BlockedIPs.Contains(clientIP))
                {
                    client.Close();
                    return;
                }

                if (!RegisterConnectionAttempt(clientIP))
                {
                    // todo
                }

                Connection connection = new Connection(client);
                ActiveConnections.Add(connection);
                Logger.Print($"New connection! IP: {clientIP}");
                Connections.AddConnection(connection);
                client.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            }
            catch (Exception ex)
            {
                Logger.Print("Unhandled exception during OnAccept: " + ex.Message);
            }
        }

        private static void OnReceive(IAsyncResult ar)
        {
            Connection connection = (Connection)ar.AsyncState;
            if (connection == null) return;

            try
            {
                int r = connection.Socket.EndReceive(ar);
                if (r <= 0)
                {
                    Logger.Print("Client disconnected.");
                    RemoveConnection(connection);
                    return;
                }

                connection.Memory.Write(connection.ReadBuffer, 0, r);
                if (connection.Messaging.OnReceive() != 0)
                {
                    RemoveConnection(connection);
                    return;
                }

                connection.Socket.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            }
            catch (SocketException)
            {
                RemoveConnection(connection);
            }
            catch (Exception exception)
            {
                Logger.Print("Unhandled exception: " + exception + ", trace: " + exception.StackTrace);
                RemoveConnection(connection);
            }
        }

        private static void RemoveConnection(Connection connection)
        {
            ActiveConnections.Remove(connection);
            if (connection.MessageManager.HomeMode != null)
            {
                Sessions.Remove(connection.Avatar.AccountId);
            }
            connection.Close();
        }

        private static bool IsBlocked(IPAddress ip)
        {
            if (ConnectionAttempts.TryGetValue(ip, out var attemptInfo))
            {
                if (DateTime.UtcNow < attemptInfo.BlockUntil)
                {
                    return true;
                }

                attemptInfo.AttemptCount = 0;
                attemptInfo.BlockUntil = DateTime.MinValue;
            }
            return false;
        }

        private static bool RegisterConnectionAttempt(IPAddress ip)
        {
            if (BlockedIPs.Contains(ip)) return false;

            if (!ConnectionAttempts.ContainsKey(ip))
            {
                ConnectionAttempts[ip] = new ConnectionAttemptInfo();
            }

            var attemptInfo = ConnectionAttempts[ip];

            if (DateTime.UtcNow - attemptInfo.LastAttempt < AttemptWindow)
            {
                attemptInfo.AttemptCount++;
            }
            else
            {
                attemptInfo.AttemptCount = 1;
            }

            attemptInfo.LastAttempt = DateTime.UtcNow;

            if (attemptInfo.AttemptCount > MaxAttempts)
            {
                attemptInfo.BlockUntil = DateTime.UtcNow + BlockDuration;
                return false;
            }

            return true;
        }



        public static void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch (Exception)
            {
                ;
            }
        }

        private class ConnectionAttemptInfo
        {
            public DateTime LastAttempt { get; set; } = DateTime.MinValue;
            public int AttemptCount { get; set; } = 0;
            public DateTime BlockUntil { get; set; } = DateTime.MinValue;
        }
    }
}
