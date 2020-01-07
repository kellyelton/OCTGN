/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using log4net;
using Octgn.Core;
using Octgn.Extentions;
using Octgn.Networking;
using Octgn.Play;
using Octgn.Windows;
using Octgn.Communication;
using Octgn.Annotations;

namespace Octgn.Controls
{
    public partial class PreGameLobby : IDisposable, INotifyPropertyChanged
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public event Action<object> OnClose;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool CanChangeSettings {
            get {
                if (GameEngine == null) return false;

                return GameEngine.IsHost && !GameEngine.IsReplay;
            }
        }

        public bool IsOnline {
            get {
                if(GameEngine == null) return false;

                return !GameEngine.IsLocal;
            }
        }

        protected virtual void FireOnClose(object obj) => OnClose?.Invoke(obj);

        public GameEngine GameEngine {
            get { return (GameEngine)GetValue(GameEngineProperty); }
            set { SetValue(GameEngineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GameEngine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GameEngineProperty =
            DependencyProperty.Register(nameof(GameEngine), typeof(GameEngine), typeof(PreGameLobby), new PropertyMetadata(GameEngine_Changed));

        private static void GameEngine_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var lobby = (PreGameLobby)d;

            lobby.OnPropertyChanged(nameof(CanChangeSettings));
            lobby.OnPropertyChanged(nameof(IsOnline));

            var gameEngine = (GameEngine)e.NewValue;

            var isLocal = gameEngine.IsLocal;

            if (!isLocal)
            {
                lobby.HorizontalAlignment = HorizontalAlignment.Stretch;
                lobby.VerticalAlignment = VerticalAlignment.Stretch;
                lobby.Width = Double.NaN;
                lobby.Height = Double.NaN;
            }

            if (lobby.CanChangeSettings)
            {
                lobby.skipBtn.Visibility = Visibility.Collapsed;
                lobby.descriptionLabel.Text =
                    "The following players have joined your game.\n\nClick 'Start' when everyone has joined. No one will be able to join once the game has started.";
                if (isLocal)
                {
                    if (gameEngine.Client is ClientSocket clientSocket) {
                        lobby.descriptionLabel.Text += "\n\nHosting on port: " + clientSocket.EndPoint.Port;
                        lobby.GetIps();

                        // save game/port so a new client can start up and connect
                        Prefs.LastLocalHostedGamePort = clientSocket.EndPoint.Port;
                        Prefs.LastHostedGameType = gameEngine.Definition.Id;
                    }
                }
            }
            else
            {
                lobby.descriptionLabel.Text =
                    "The following players have joined the game.\nPlease wait until the game starts, or click 'Cancel' to leave this game.";
                lobby.startBtn.Visibility = Visibility.Collapsed;
                if (gameEngine.IsReplay) {
                    lobby.skipBtn.Visibility = Visibility.Visible;
                } else {
                    lobby.skipBtn.Visibility = Visibility.Collapsed;
                }
            }
        }

        public bool StartingGame { get; private set; }

        public PreGameLobby()
        {
            InitializeComponent();

            if (this.IsInDesignMode()) return;


            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Program.ServerError -= HandshakeError;
            Player.OnLocalPlayerWelcomed -= PlayerOnOnLocalPlayerWelcomed;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Loaded -= OnLoaded;
            Player.OnLocalPlayerWelcomed += PlayerOnOnLocalPlayerWelcomed;

            Program.ServerError += HandshakeError;
            // Fix: defer the call to Program.Game.Begin(), so that the trace has
            // time to connect to the ChatControl (done inside ChatControl.Loaded).
            // Otherwise, messages notifying a disconnection may be lost
            try
            {
                if (GameEngine != null)
                    Dispatcher.BeginInvoke(new Action(() => GameEngine.Begin()));
            }
            catch (Exception)
            {
                if (Debugger.IsAttached) Debugger.Break();
            }
        }

        private void GetIps()
        {
            var task = new Task(GetLocalIps);
            task.ContinueWith(GetExternalIp);
            task.Start();
        }

        private void GetLocalIps()
        {
            try
            {
                var addr = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
                this.Dispatcher.Invoke(
                    new Action(
                        () =>
                        {
                            var paragraph = new Paragraph(new Run("--Local Ip's--")) { Foreground = Brushes.Brown };
                            foreach (var a in addr)
                            {
                                paragraph.Inlines.Add(new LineBreak());
                                paragraph.Inlines.Add(new Run(a.ToString()));
                            }
                            this.chatControl.output.Document.Blocks.Add(paragraph);
                            this.chatControl.output.ScrollToEnd();
                        }));
            }
            catch (Exception)
            {

            }
        }

        private void GetExternalIp(Task task)
        {
            try
            {
                const string Dyndns = "http://checkip.dyndns.org";
                var wc = new System.Net.WebClient();
                var utf8 = new System.Text.UTF8Encoding();
                var requestHtml = "";
                var ipAddress = "";
                requestHtml = utf8.GetString(wc.DownloadData(Dyndns));
                var fullStr = requestHtml.Split(':');
                ipAddress = fullStr[1].Remove(fullStr[1].IndexOf('<')).Trim();
                var externalIp = System.Net.IPAddress.Parse(ipAddress);
                this.Dispatcher.Invoke(
                    new Action(
                        () =>
                        {
                            var paragraph = new Paragraph(new Run("--Remote Ip--")) { Foreground = Brushes.Brown };
                            paragraph.Inlines.Add(new LineBreak());
                            paragraph.Inlines.Add(new Run(externalIp.ToString()));
                            this.chatControl.output.Document.Blocks.Add(paragraph);
                            this.chatControl.output.ScrollToEnd();
                        }));

            }
            catch (Exception)
            {

            }
        }

        private void PlayerOnOnLocalPlayerWelcomed()
        {
            if (Player.LocalPlayer.Id == 255) return;
            if (Player.LocalPlayer.Id == 1 && !GameEngine.IsReplay)
            {
                Dispatcher.BeginInvoke(new Action(() => { startBtn.Visibility = Visibility.Visible; }));
                GameEngine.Client.Rpc.Settings(GameEngine.Settings.UseTwoSidedTable, GameEngine.Settings.AllowSpectators, GameEngine.Settings.MuteSpectators);
            }
			Player.LocalPlayer.SetPlayerColor(Player.LocalPlayer.Id);
            this.StartingGame = true;
        }

        private bool calledStart = false;
        internal void Start(bool callStartGame = true)
        {
            lock (this)
            {
                if (calledStart) return;
                calledStart = true;
            }
            // Reset the InvertedTable flags if they were set and they are not used
            if (!GameEngine.Settings.UseTwoSidedTable)
                foreach (Player player in Player.AllExceptGlobal)
                    player.InvertedTable = false;
            foreach (Player player in Player.Spectators)
            {
                if (player.InvertedTable)
                    player.InvertedTable = false;
            }

            // At start the global items belong to the player with the lowest id
            if (Player.GlobalPlayer != null)
            {
                Player host = Player.AllExceptGlobal.OrderBy(p => p.Id).First();
                foreach (Octgn.Play.Group group in Player.GlobalPlayer.Groups)
                    group.Controller = host;
            }
            if (callStartGame)
            {
                GameEngine.Client.Rpc.Start(); // I believe this is for table only mode - Kelly
            }
            this.StartingGame = true;
            Back();
        }

        private void StartClicked(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            e.Handled = true;
            Start();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.StartingGame = false;
            e.Handled = true;
            Back();
        }

        private void Back()
        {
            this.FireOnClose(this);
        }

        private void HandshakeError(object sender, ServerErrorEventArgs e)
        {
            TopMostMessageBox.Show("The server returned an error:\n" + e.Message, "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
            e.Handled = true;
            Back();
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            Player.OnLocalPlayerWelcomed -= PlayerOnOnLocalPlayerWelcomed;
            if (OnClose != null)
            {
                foreach (var d in OnClose.GetInvocationList())
                {
                    OnClose -= (Action<object>)d;
                }
            }
        }

        #endregion

        private void KickPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            var sen = sender as Button;
            if (sen == null) return;
            var play = sen.DataContext as Player;
            if (play == null) return;
            if (GameEngine.IsHost == false) return;

            GameEngine.Client.Rpc.Boot(play, "The host has booted them from the game.");
        }

        private async void ProfileMouseUp(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var play = fe.DataContext as Octgn.Play.Player;
            if (play == null) return;
			await UserProfileWindow.Show(new User(play.UserId));
        }

        private void SkipClicked(object sender, RoutedEventArgs e) {
            GameEngine.ReplayEngine.FastForwardToStart();
        }
    }
}
