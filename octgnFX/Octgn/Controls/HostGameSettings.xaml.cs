using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using Octgn.Core;
using Octgn.Core.DataManagers;
using Octgn.Library.Exceptions;
using Octgn.ViewModels;

using log4net;

using UserControl = System.Windows.Controls.UserControl;
using Octgn.Communication;
using Octgn.Library;

namespace Octgn.Controls
{

    public partial class HostGameSettings : UserControl,IDisposable
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public event Action<object, DialogResult> OnClose;
        protected virtual void FireOnClose(object sender, DialogResult result)
        {
            this.OnClose?.Invoke(sender, result);
        }

        public static DependencyProperty ErrorProperty = DependencyProperty.Register(
            "Error", typeof(String), typeof(HostGameSettings));

        public bool HasErrors { get; private set; }
        public string Error
        {
            get { return this.GetValue(ErrorProperty) as String; }
            private set { this.SetValue(ErrorProperty, value); }
        }

        public bool IsLocalGame { get; private set; }
        public string Gamename { get; private set; }
        public string Password { get; private set; }
        public string Username { get; set; }
        public bool Specators { get; set; }
        public DataNew.Entities.Game Game { get; private set; }
        public bool SuccessfulHost { get; private set; }

        public GameEngine GameEngine { get; private set; }

        private Decorator Placeholder;
        private Guid lastHostedGameType;

        public ObservableCollection<DataGameViewModel> Games { get; private set; }

        public HostGameSettings()
        {
            InitializeComponent();
            Specators = true;
            Games = new ObservableCollection<DataGameViewModel>();
            Program.LobbyClient.Connected += LobbyClient_Connected;
            Program.LobbyClient.Disconnected += LobbyClient_Disconnected;
            TextBoxGameName.Text = Prefs.LastRoomName ?? Randomness.RandomRoomName();
            CheckBoxIsLocalGame.IsChecked = !Program.LobbyClient.IsConnected;
            CheckBoxIsLocalGame.IsEnabled = Program.LobbyClient.IsConnected;
            LabelIsLocalGame.IsEnabled = Program.LobbyClient.IsConnected;
            lastHostedGameType = Prefs.LastHostedGameType;
            if (GameManager.Get().GameCount == 1)
            {
                lastHostedGameType = GameManager.Get().Games.First().Id;
            }
            TextBoxUserName.Text = (Program.LobbyClient.IsConnected == false
                || Program.LobbyClient.User == null
                || Program.LobbyClient.User.DisplayName == null) ? Prefs.Nickname : Program.LobbyClient.User.DisplayName;
			Program.OnOptionsChanged += ProgramOnOptionsChanged;
            TextBoxUserName.IsReadOnly = Program.LobbyClient.IsConnected;
            if (Program.LobbyClient.IsConnected)
                PasswordGame.IsEnabled = SubscriptionModule.Get().IsSubscribed;
            else {
                PasswordGame.IsEnabled = true;
            }
            StackPanelIsLocalGame.Visibility = Prefs.EnableAdvancedOptions ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ProgramOnOptionsChanged()
        {
            StackPanelIsLocalGame.Visibility = Prefs.EnableAdvancedOptions ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LobbyClient_Disconnected(object sender, DisconnectedEventArgs args)
        {
            Dispatcher.Invoke(new Action(() =>
                {
                    CheckBoxIsLocalGame.IsChecked = true;
                    CheckBoxIsLocalGame.IsEnabled = false;
                    LabelIsLocalGame.IsEnabled = false;
                    TextBoxUserName.IsReadOnly = false;
                }));
        }

        private void LobbyClient_Connected(object sender, ConnectedEventArgs args)
        {
            Dispatcher.Invoke(new Action(() =>
                {
                    CheckBoxIsLocalGame.IsChecked = false;
                    CheckBoxIsLocalGame.IsEnabled = true;
                    LabelIsLocalGame.IsEnabled = true;
                    TextBoxUserName.IsReadOnly = true;
                    TextBoxUserName.Text = Program.LobbyClient.User.DisplayName;
                }));

        }

        void RefreshInstalledGameList()
        {
            if (Games == null)
                Games = new ObservableCollection<DataGameViewModel>();
            var list = GameManager.Get().Games.Select(x => new DataGameViewModel(x)).ToList();
            Games.Clear();
            foreach (var l in list)
                Games.Add(l);
        }

        void ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TextBoxGameName.Text))
                this.SetError("You must enter a game name");
            else if (ComboBoxGame.SelectedIndex == -1) this.SetError("You must select a game");
            else
            {
                if(String.IsNullOrWhiteSpace(PasswordGame.Password))
                    this.SetError();
                else
                {
                    if(PasswordGame.Password.Contains(":,:") || PasswordGame.Password.Contains("=") || PasswordGame.Password.Contains("-") || PasswordGame.Password.Contains(" "))
                        this.SetError("The password has invalid characters");
                    else
                        this.SetError();
                }
            }
        }

        void SetError(string error = "")
        {
            this.HasErrors = !string.IsNullOrWhiteSpace(error);
            Error = error;
            ErrorMessageBorder.Visibility = HasErrors ? Visibility.Visible : Visibility.Collapsed;
        }

        #region LobbyEvents

        #endregion

        #region Dialog
        public void Show(Decorator placeholder)
        {
            Placeholder = placeholder;
            this.RefreshInstalledGameList();

            if (lastHostedGameType != Guid.Empty)
            {
                var game = GameManager.Get().Games.FirstOrDefault(x => x.Id == lastHostedGameType);
                if (game != null)
                {
                    var model = Games.FirstOrDefault(x => x.Id == game.Id);
                    if (model != null) this.ComboBoxGame.SelectedItem = model;
                }
            }

            placeholder.Child = this;
        }

        public void Close()
        {
            Close(DialogResult.Abort);
        }

        private void Close(DialogResult result)
        {
            Program.OnOptionsChanged -= ProgramOnOptionsChanged;
            IsLocalGame = CheckBoxIsLocalGame.IsChecked ?? false;
            Gamename = TextBoxGameName.Text;
            Password = PasswordGame.Password;
            if (ComboBoxGame.SelectedIndex != -1)
                Game = (ComboBoxGame.SelectedItem as DataGameViewModel).GetGame();
            Placeholder.Child = null;
            this.FireOnClose(this, result);
        }

        void StartWait()
        {
            BorderHostGame.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
        }

        void EndWait()
        {
            BorderHostGame.IsEnabled = true;
            ProgressBar.Visibility = Visibility.Hidden;
            ProgressBar.IsIndeterminate = false;
        }

        #endregion

        #region UI Events
        private void ButtonCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close(DialogResult.Cancel);
        }

        private async void ButtonHostGameStartClick(object sender, RoutedEventArgs e)
        {
            this.ValidateFields();
            if (this.HasErrors) return;

            var error = "";
            try {
                this.StartWait();
                this.Game = (ComboBoxGame.SelectedItem as DataGameViewModel).GetGame();
                this.Gamename = TextBoxGameName.Text;
                this.Password = PasswordGame.Password;
                this.Username = TextBoxUserName.Text;
                var isLocalGame = CheckBoxIsLocalGame?.IsChecked ?? false;

                if (isLocalGame) {
                    GameEngine = await GameEngine.HostLocal(Dispatcher, Game, Gamename, Password, Program.LobbyClient?.User, Username, Specators, Program.DeveloperMode);
                } else {
                    Username = Program.LobbyClient.User.DisplayName;

                    GameEngine = await GameEngine.HostOnline(Dispatcher, Program.LobbyClient, Game, Gamename, Password, Specators, Program.DeveloperMode);
                }

                GameEngine.LaunchUrl += (_, url) => {
                    Program.LaunchUrl(url);
                };

                SuccessfulHost = true;

                if (isLocalGame) {
                    Prefs.Nickname = Username;
                }

                Prefs.LastRoomName = this.Gamename;
                Prefs.LastHostedGameType = this.Game.Id;

            } catch (Exception ex) {
                if (ex is UserMessageException) {
                    error = ex.Message;
                } else error = "There was a problem, please try again.";
                Log.Warn("Start Game Error", ex);
                SuccessfulHost = false;
            } finally {
                if (!string.IsNullOrWhiteSpace(error))
                    this.SetError(error);
                this.EndWait();
                if(SuccessfulHost)
                    this.Close(DialogResult.OK);
            }
        }

        private void ButtonRandomizeGameNameClick(object sender, RoutedEventArgs e)
        {
            TextBoxGameName.Text = Randomness.GrabRandomJargonWord() + " " + Randomness.GrabRandomNounWord();
        }

        private void ButtonRandomizeUserNameClick(object sender, RoutedEventArgs e)
        {
            if (Program.LobbyClient.IsConnected == false)
                TextBoxUserName.Text = Randomness.GrabRandomJargonWord() + "-" + Randomness.GrabRandomNounWord();
        }
        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (OnClose != null)
            {
                foreach (var d in OnClose.GetInvocationList())
                {
                    OnClose -= (Action<object, DialogResult>)d;
                }
            }
            Program.LobbyClient.Connected -= LobbyClient_Connected;
            Program.LobbyClient.Disconnected -= LobbyClient_Disconnected;
        }

        #endregion

        private void CheckBoxIsLocalGame_OnChecked(object sender, RoutedEventArgs e)
        {
            PasswordGame.IsEnabled = true;
        }

        private void CheckBoxIsLocalGame_OnUnchecked(object sender, RoutedEventArgs e)
        {
            PasswordGame.IsEnabled = SubscriptionModule.Get().IsSubscribed;
        }

        private void CheckBoxSpectators_OnChecked(object sender, RoutedEventArgs e)
        {
            Specators = true;
        }

        private void CheckBoxSpectators_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Specators = false;
        }
    }
}
