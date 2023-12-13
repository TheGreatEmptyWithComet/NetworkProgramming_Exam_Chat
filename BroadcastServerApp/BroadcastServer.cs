using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ChatClientApp;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace BroadcastServerApp
{
    public enum UserState { UserJoin, UserLeft };

    public class BroadcastServer
    {
        private readonly int serverPort = 40000;
        private readonly int clientPort = 30000;

        private Socket serverSocket;
        private IPEndPoint destinationEndPoint;
        private IPEndPoint serverEndPoint;

        private List<UserObject> users = new List<UserObject>();
        private List<string> userColors;
        private int lastUserColorIndex = -1;

        public BroadcastServer()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            destinationEndPoint = new IPEndPoint(IPAddress.Broadcast, clientPort);

            serverEndPoint = new IPEndPoint(IPAddress.Any, serverPort);

            userColors = new List<string>() { "#FF892FE8", "#FF46E82F", "#FFC18D1B", "#FFE21818", "#FFE82FA6", "#FF5A2ED6", "#FF827F8A", "#FF42B339", "#FF58CAC8", "#FFB9BB07", "#FF32A27B" };
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
                int messageLength = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);

                if (clientEndPoint is IPEndPoint endPoint && endPoint.Port != serverPort)
                {
                    string messageAsJson = Encoding.UTF8.GetString(buffer, 0, messageLength);

                    MessageObject? message = JsonConvert.DeserializeObject<MessageObject>(messageAsJson);

                    if (message != null)
                    {
                        // New user join
                        if (message.MessageStatus == MessageStatus.UserJoin)
                        {
                            // set new user color
                            message.User.Color = GetNextUserColor();

                            // add new user to list
                            users.Add(message.User);

                            // add new users list to message
                            message.AllUsers = JsonConvert.SerializeObject(users);

                            // create new byte array to send
                            messageAsJson = JsonConvert.SerializeObject(message);
                            byte[] newMessageBuffer = Encoding.UTF8.GetBytes(messageAsJson);
                            buffer = newMessageBuffer;
                            messageLength = newMessageBuffer.Length;
                        }
                        else if (message.MessageStatus == MessageStatus.UserLeft)
                        {
                            UserObject user = users.Find((u) => u.Id == message.User.Id);
                            users.Remove(user);
                        }
                        
                        Console.WriteLine($"[{message.Time}]: [{message.User.Name}]: [{message.Message}]");
                    }

                    serverSocket.SendTo(buffer, 0, messageLength, SocketFlags.None, destinationEndPoint);
                }
            }
        }

        public void AtExit()
        {
            serverSocket.Dispose();
        }
        private string GetNextUserColor()
        {
            if (lastUserColorIndex < userColors.Count - 1)
            {
                ++lastUserColorIndex;
            }
            else
            {
                lastUserColorIndex = 0;
            }

            return userColors[lastUserColorIndex];
        }
    }
}
