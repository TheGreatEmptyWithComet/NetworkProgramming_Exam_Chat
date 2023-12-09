using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BroadcastServerApp
{
    public enum UserState { UserJoin, UserLeft };

    public class BroadcastServer
    {
        private readonly int serverPort = 40000;

        private Socket serverSocket;
        private IPEndPoint destinationEndPoint;
        private IPEndPoint serverEndPoint;
        private string messageExcludePattern = $"^({UserState.UserJoin.ToString()}|{UserState.UserLeft.ToString()})";

        public BroadcastServer()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            destinationEndPoint = new IPEndPoint(IPAddress.Broadcast, 30000);

            serverEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
        }

        public void Start()
        {
            serverSocket.EnableBroadcast = true;
            serverSocket.Bind(serverEndPoint);

            Console.WriteLine("Broadcast server is running");

            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[1024];

            while (true)
            {
                int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);

                if (clientEndPoint is IPEndPoint endPoint && endPoint.Port != serverPort)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    bool messageIsService = Regex.IsMatch(message, messageExcludePattern);
                    if (!messageIsService)
                    {
                        Console.WriteLine(message);
                    }
                    serverSocket.SendTo(buffer, 0, receivedBytes, SocketFlags.None, destinationEndPoint);
                }
            }
        }

        public void AtExit()
        {
            serverSocket.Dispose();
        }
    }
}
