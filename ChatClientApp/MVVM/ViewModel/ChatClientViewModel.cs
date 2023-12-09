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


        //public ObservableCollection<MessageModel> Messages { get; set; } = new ObservableCollection<MessageModel>();
        public ObservableCollection<string> Users { get; set; } = new ObservableCollection<string>();

        public string Messages { get; set; } = string.Empty;

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
            ChatClient.OnMessageReseived += (message) => { Messages += (message + Environment.NewLine); NotifyPropertyChanged(nameof(Messages)); };
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
                    string message = $"[{DateTime.Now}]: [{ChatClient.UserName}]: {ClientMessage}";
                    await ChatClient.SendMessageAsync(message);
                    ClientMessage = string.Empty;
                }
            });
            JoinChatCommand = new RelayCommand(async () =>
            {
                if (!string.IsNullOrEmpty(UserName))
                {
                    ChatClient.UserName = UserName;
                    ChatClient.NotifyUserJoinChat();
                    UserName = string.Empty;
                    ChatingIsAllowed = true;
                }
            });
            LeftChatCommand = new RelayCommand(async () =>
            {
                await ChatClient.NotifyUserLeftChat();
            });
        }

        private void AddUserTolist(string userName)
        {
            //App.Current.Dispatcher.Invoke((Action)delegate { Users.Add(userName); });
            Users.Add(userName);
        }
        private void RemoveUserFromlist(string userName)
        {
            //App.Current.Dispatcher.Invoke((Action)delegate {  });
            Users.Remove(userName);
        }
    }
}
