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

        private bool chatingIsAllowed = false;
        public bool ChatingIsAllowed
        {
            get => chatingIsAllowed;
            set
            {
                chatingIsAllowed = value;
                NotifyPropertyChanged(nameof(ChatingIsAllowed));
            }
        }


        public ObservableCollection<MessageObject> Messages { get; set; } = new ObservableCollection<MessageObject>();
        public ObservableCollection<string> Users { get; set; } = new ObservableCollection<string>();

        //public string Messages { get; set; } = string.Empty;

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
            ChatClient.OnMessageReseived += (message) => Messages.Add(message);
            ChatClient.OnUserJoined += (userName) => AddUserTolist(userName);
            ChatClient.OnUserLeft += (userName) => RemoveUserFromlist(userName);

            InitCommands();
        }

        private void InitCommands()
        {
            SendMessageCommand = new RelayCommand(async () =>
            {
                if (chatingIsAllowed)
                {
                    await ChatClient.SendMessageAsync(ClientMessage, MessageStatus.Usual);
                    ClientMessage = string.Empty;
                }
            });
            JoinChatCommand = new RelayCommand(async () =>
            {
                if (!string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(ChatClient.UserName))
                {
                    ChatClient.UserName = UserName;
                    ChatClient.NotifyUserJoinChat();
                    UserName = string.Empty;
                    ChatingIsAllowed = true;
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

        private void AddUserTolist(string userName)
        {
            Users.Add(userName);
        }
        private void RemoveUserFromlist(string userName)
        {
            Users.Remove(userName);
        }
    }
}
