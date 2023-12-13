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

    public delegate void MessageReceived(MessageObject messageObject);
    public delegate void UserJoined(List<UserObject> users);
    public delegate void UserLeft(string userId);

    public class ChatClient : NotifyPropertyChangedHandler
    {
        private IPEndPoint localEndPoint;
        private IPEndPoint serverEndPoint;

        private bool messageWasSent = false;
        private IPAddress serverIp;
        public UserObject User { get; set; }

        public event MessageReceived OnMessageReceived;
        public event UserJoined OnUserJoined;
        public event UserLeft OnUserLeft;

        
        public ChatClient()
        {
            serverIp = IPAddress.Parse("127.0.0.1");
            serverEndPoint = new IPEndPoint(serverIp, 40000);

            User = new UserObject() { Id = Guid.NewGuid().ToString() };
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
                                // set user color, received from server
                                if (User.Id == messageObject.User.Id)
                                {
                                    User.Color = messageObject.User.Color;
                                }

                                // add new user to list
                                List<UserObject>? users = JsonConvert.DeserializeObject<List<UserObject>>(messageObject.AllUsers);
                                if (users != null)
                                {
                                    App.Current.Dispatcher.Invoke((Action)delegate { OnUserJoined?.Invoke(users); });
                                }
                            }
                            else if (messageObject.MessageStatus == MessageStatus.UserLeft)
                            {
                                App.Current.Dispatcher.Invoke((Action)delegate { OnUserLeft?.Invoke(messageObject.User.Id); });
                            }

                            // Check if received message is own sent message
                            if (messageWasSent == true && User.Id == messageObject.User.Id)
                            {
                                messageWasSent = false;
                                messageObject.IsNativeOrigin = true;
                            }

                            App.Current.Dispatcher.Invoke((Action)delegate { OnMessageReceived?.Invoke(messageObject); });
                           
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
                // build message object
                MessageObject messageObject = new MessageObject()
                {
                    MessageStatus = messageStatus,
                    User = User,
                    Message = message,
                    Time = DateTime.Now
                };

                // serialize object to json
                string messageAsJson = JsonConvert.SerializeObject(messageObject,Formatting.Indented);

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
            string message = $"{User.Name} has joined chat!";
            await SendMessageAsync(message, MessageStatus.UserJoin);
        }
        public async Task NotifyUserLeftChat()
        {
            string message = $"{User.Name} has left chat!";
            await SendMessageAsync(message, MessageStatus.UserLeft);
        }


    }
}
