using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Windows;

namespace ChatClientApp
{
    public enum MessageStatus { Usual, UserJoin, UserLeft }

    public delegate void MessageReceived(MessageObject messageObject);
    public delegate void UserJoined(List<UserObject> users);
    public delegate void UserLeft(string userId);

    public class ChatClient : NotifyPropertyChangedHandler
    {
        #region Events
        /************************************************************************************************************/
        public event MessageReceived OnMessageReceived;
        public event UserJoined OnUserJoined;
        public event UserLeft OnUserLeft;
        #endregion


        #region Field & Properties
        /************************************************************************************************************/
        private IPEndPoint localEndPoint;
        private IPEndPoint serverEndPoint;
        private MessageObject? incomingMessage;
        private byte[] buffer;

        private bool messageWasSent = false;
        private bool userIsConnected = false;
        private IPAddress serverIp;
        public UserObject User { get; set; }
        #endregion

        
        #region Constructor
        /************************************************************************************************************/
        public ChatClient()
        {
            serverIp = IPAddress.Parse("127.0.0.1");
            serverEndPoint = new IPEndPoint(serverIp, 40000);
            localEndPoint = new IPEndPoint(IPAddress.Any, 30000);

            User = new UserObject() { Id = Guid.NewGuid().ToString() };
            buffer = new byte[1024];
        }
        #endregion


        #region Methods
        /************************************************************************************************************/
        public async Task ProcessIncomingMessages()
        {
            await Task.Run(() =>
            {
                try
                {
                    using (Socket receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        receiveSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        receiveSocket.Bind(localEndPoint);

                        while (true)
                        {
                            incomingMessage = GetIncomingMessage(receiveSocket, buffer);

                            if (incomingMessage == null) continue;

                            ProcessUserJoinOrLeft();

                            ProcessMessageIsOwn();

                            ShowMessage();
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error: {e.Message}");
                };
            });
        }

        public async Task SendMessageAsync(string message, MessageStatus messageStatus)
        {
            await Task.Run(() =>
            {
                string messageAsJson = BuildMessage(message, messageStatus);

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

        private MessageObject? GetIncomingMessage(Socket receiveSocket, byte[] buffer)
        {
            int receivedBytes = receiveSocket.Receive(buffer);
            string messageAsJson = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            MessageObject? incomingMessage = JsonConvert.DeserializeObject<MessageObject>(messageAsJson);

            return incomingMessage;
        }

        private void ProcessUserJoinOrLeft()
        {
            // Check if new user join or left chat
            if (incomingMessage!.MessageStatus == MessageStatus.UserJoin)
            {
                // set user color, received from server
                if (User.Id == incomingMessage.User.Id)
                {
                    User.Color = incomingMessage.User.Color;
                    userIsConnected = true;
                }

                if (userIsConnected == false) return;

                // add new user to list
                List<UserObject>? users = JsonConvert.DeserializeObject<List<UserObject>>(incomingMessage.AllUsers);
                if (users != null)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate { OnUserJoined?.Invoke(users); });
                }
            }
            else if (incomingMessage.MessageStatus == MessageStatus.UserLeft)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { OnUserLeft?.Invoke(incomingMessage.User.Id); });
            }
        }

        private void ProcessMessageIsOwn()
        {
            if (messageWasSent == true && User.Id == incomingMessage!.User.Id)
            {
                messageWasSent = false;
                incomingMessage.IsNativeOrigin = true;
            }
        }

        private void ShowMessage()
        {
            if (userIsConnected == true)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { OnMessageReceived?.Invoke(incomingMessage!); });
            }
        }

        private string BuildMessage(string message, MessageStatus messageStatus)
        {
            // build message object
            MessageObject outgoingMessage = new MessageObject()
            {
                MessageStatus = messageStatus,
                User = User,
                Message = message,
                Time = DateTime.Now
            };

            // serialize object to json
            string messageAsJson = JsonConvert.SerializeObject(outgoingMessage, Formatting.Indented);

            return messageAsJson;
        }
        #endregion
    }
}
