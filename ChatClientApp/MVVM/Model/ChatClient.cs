using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace ChatClientApp
{
    public enum UserState { UserJoin, UserLeft }
    public enum MessageStatus { Usual, UserJoin, UserLeft }

    public delegate void MessageReseived(MessageObject messageObject);
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
                        string messageAsJson = string.Empty;

                        while (true)
                        {
                            int receivedBytes = receiveSocket.Receive(buffer);

                            messageAsJson = Encoding.ASCII.GetString(buffer, 0, receivedBytes);

                            MessageObject? messageObject = JsonConvert.DeserializeObject<MessageObject>(messageAsJson);

                            if (messageObject == null) continue;

                            // Check if new user join or left chat
                            if (messageObject.MessageStatus == MessageStatus.UserJoin)
                            {
                                App.Current.Dispatcher.Invoke((Action)delegate { OnUserJoined?.Invoke(messageObject.UserName); });
                            }
                            else if (messageObject.MessageStatus == MessageStatus.UserLeft)
                            {
                                App.Current.Dispatcher.Invoke((Action)delegate { OnUserLeft?.Invoke(messageObject.UserName); });
                            }

                            // Check if received message is own sent message
                            if (messageWasSent == true && messageObject.Message == lastSentMessage)
                            {
                                messageWasSent = false;
                                lastSentMessage = string.Empty;
                                messageObject.IsNativeOrigin = true;
                            }

                            App.Current.Dispatcher.Invoke((Action)delegate { OnMessageReseived?.Invoke(messageObject); });
                           
                        }
                    }
                }
                catch { };
            });
        }

        public async Task SendMessageAsync(string message, MessageStatus messageStatus)
        {
            await Task.Run(() =>
            {
                // save message to catch it in incomings
                lastSentMessage = message;

                // build message object
                MessageObject messageModel = new MessageObject()
                {
                    MessageStatus = messageStatus,
                    UserName = UserName,
                    Message = message,
                    Time = DateTime.Now
                };

                // serialize object to json
                string messageAsJson = JsonConvert.SerializeObject(messageModel,Formatting.Indented);

                // send message
                using (Socket sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(messageAsJson);
                    sendSocket.SendTo(buffer, serverEndPoint);
                    messageWasSent = true;
                }
            });
        }
        public async Task NotifyUserJoinChat()
        {
            string message = $"{UserName} has joined chat!";
            await SendMessageAsync(message, MessageStatus.UserJoin);
        }
        public async Task NotifyUserLeftChat()
        {
            // first message - for removing user from list
            string message = $"{UserName} has left chat!";
            await SendMessageAsync(message, MessageStatus.UserLeft);
        }


    }
}
