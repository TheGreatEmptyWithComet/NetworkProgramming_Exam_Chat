using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatClientApp
{
    public class ChatClientViewModel : NotifyPropertyChangedHandler
    {
        public ChatClient ChatClient { get; set; }

        private bool chattingIsAllowed = false;
        public bool ChattingIsAllowed
        {
            get => chattingIsAllowed;
            set
            {
                chattingIsAllowed = value;
                NotifyPropertyChanged(nameof(ChattingIsAllowed));
            }
        }


        public ObservableCollection<MessageObject> Messages { get; set; } = new ObservableCollection<MessageObject>();
        public ObservableCollection<UserObject> Users { get; set; } = new ObservableCollection<UserObject>();

        
        private string clientMessage = string.Empty;
        public string ClientMessage
        {
            get => clientMessage;
            set
            {
                clientMessage = value;
                NotifyPropertyChanged(nameof(ClientMessage));
            }
        }
        private string userName = string.Empty;
        public string UserName
        {
            get => userName;
            set
            {
                userName = value;
                NotifyPropertyChanged(nameof(UserName));
            }
        }


        public ICommand SendMessageCommand { get; set; }
        public ICommand JoinChatCommand { get; set; }
        public ICommand LeftChatCommand { get; set; }


        public ChatClientViewModel()
        {
            ChatClient = new ChatClient();
            ChatClient.Start();
            ChatClient.OnMessageReceived += (message) => Messages.Add(message);
            ChatClient.OnUserJoined += (users) => AddUserToList(users);
            ChatClient.OnUserLeft += (userId) => RemoveUserFromList(userId);

            InitCommands();
        }

        private void InitCommands()
        {
            SendMessageCommand = new RelayCommand(async () =>
            {
                if (chattingIsAllowed)
                {
                    await ChatClient.SendMessageAsync(ClientMessage, MessageStatus.Usual);
                    ClientMessage = string.Empty;
                }
            });
            JoinChatCommand = new RelayCommand(async () =>
            {
                if (!string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(ChatClient.User.Name))
                {
                    ChatClient.User.Name = UserName;
                    ChatClient.NotifyUserJoinChat();
                    UserName = string.Empty;
                    ChattingIsAllowed = true;
                }
                else
                {
                    UserName = string.Empty;
                }
            });
            LeftChatCommand = new RelayCommand(async () =>
            {
                await ChatClient.NotifyUserLeftChat();
            });
        }

        private void AddUserToList(List<UserObject> usersFromServer)
        {
            for (int i = Users.Count; i < usersFromServer.Count; ++i)
            {
                Users.Add(usersFromServer[i]);
            }
        }
        private void RemoveUserFromList(string userId)
        {
            UserObject? user = Users.Where((u) => u.Id == userId).FirstOrDefault();
            if (user != null)
            {
                Users.Remove(user);
            }
        }
    }
}
