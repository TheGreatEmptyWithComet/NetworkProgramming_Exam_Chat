using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ChatClientApp
{
    public enum UserState { UserJoin, UserLeft }

    public delegate void MessageReseived(string message);
    public delegate void UserStateChanged(string userName);

    public class ChatClient : NotifyPropertyChangedHandler
    {
        private IPEndPoint localEndPoint;
        private IPEndPoint serverEndPoint;

        private bool messageWasSent = false;
        private string lastSentMessage = string.Empty;
        private IPAddress serverIp;
        public string UserName { get; set; }

        public event MessageReseived OnMessageReseived;
        public event UserStateChanged OnUserJoined;
        public event UserStateChanged OnUserLeft;


        private string logfile = "logfile.txt";

        public ChatClient()
        {
            serverIp = IPAddress.Parse("127.0.0.1");
            serverEndPoint = new IPEndPoint(serverIp, 40000);
        }

        public async Task Start()
        {
            await Task.Run(() =>
            {
                try
                {
                    using (Socket receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        receiveSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        localEndPoint = new IPEndPoint(IPAddress.Any, 30000);
                        receiveSocket.Bind(localEndPoint);

                        byte[] buffer = new byte[1024];
                        string message = string.Empty;

                        while (true)
                        {
                            int receivedBytes = receiveSocket.Receive(buffer);

                            message = Encoding.ASCII.GetString(buffer, 0, receivedBytes);

                            // Check if new user join or left chat
                            var words = message.Split("###", StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length == 2)
                            {
                                if (words[0] == UserState.UserJoin.ToString())
                                {
                                    App.Current.Dispatcher.Invoke((Action)delegate { OnUserJoined?.Invoke(words[1]); });
                                    continue;
                                }
                                else if (words[0] == UserState.UserLeft.ToString())
                                {
                                    App.Current.Dispatcher.Invoke((Action)delegate { OnUserLeft?.Invoke(words[1]); });
                                    continue;
                                }
                            }

                            // Check if received message is own sent message
                            if (messageWasSent == true && message == lastSentMessage)
                            {
                                messageWasSent = false;
                                lastSentMessage = string.Empty;
                                // do something, i.e. color text
                            }

                            OnMessageReseived?.Invoke(message);
                        }
                    }
                }
                catch { };
            });
        }

        public async Task SendMessageAsync(string message)
        {
            await Task.Run(() =>
            {
                using (Socket sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    sendSocket.SendTo(buffer, serverEndPoint);
                    messageWasSent = true;
                }
            });
        }
        public async Task NotifyUserJoinChat()
        {
            // first message - for adding new user to list
            string message = $"{UserState.UserJoin.ToString()}###{UserName}";
            await SendMessageAsync(message);

            message = $"[{DateTime.Now}]: {UserName} has joined chat!";
            await SendMessageAsync(message);
        }
        public async Task NotifyUserLeftChat()
        {
            // first message - for removing user from list
            string message = $"{UserState.UserLeft.ToString()}###{UserName}";
            SendMessageAsync(message);

            message = $"[{DateTime.Now}]: {UserName} has left chat!";
            await SendMessageAsync(message);
        }
    }
}
