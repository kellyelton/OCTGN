using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using log4net;
using Octgn.DataNew.Entities;
using Octgn.Core.DataManagers;
using Octgn.Core;
using Octgn.Play;
using Octgn.Library.Utils;
using Octgn.Library;
using System.Threading.Tasks;

namespace Octgn.Launchers
{
    public class GameTableLauncher
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal int HostPort;
        internal Game HostGame;
        internal string HostUrl;

        public Task Launch(int? hostport, Guid? game)
        {
            Program.Dispatcher = Application.Current.Dispatcher;
            HostGame = GameManager.Get().GetById(game.Value);
            if (hostport == null || hostport <= 0)
            {
                this.HostPort = new Random().Next(5000, 6000);
                while (!NetworkHelper.IsPortAvailable(this.HostPort)) this.HostPort++;
            }
            else
            {
                this.HostPort = hostport.Value;
            }
            // Host a game
            return this.Host();
        }

        private async Task Host()
        {
            Program.GameSettings.UseTwoSidedTable = HostGame.UseTwoSidedTable;

            var gameName = Randomness.RandomRoomName();

            Program.GameEngine = await GameEngine.HostLocal(HostGame, gameName, "", Prefs.Nickname, true);

            Octgn.Play.Player.OnLocalPlayerWelcomed += PlayerOnOnLocalPlayerWelcomed;
            Program.GameSettings.UseTwoSidedTable = HostGame.UseTwoSidedTable;

            Dispatcher.CurrentDispatcher.Invoke(new Action(()=>Program.GameEngine.Begin()));
        }

        private void PlayerOnOnLocalPlayerWelcomed()
        {
            if (Octgn.Play.Player.LocalPlayer.Id == 1)
            {
                this.StartGame();
            }
        }

        private void StartGame()
        {
            Play.Player.OnLocalPlayerWelcomed -= this.PlayerOnOnLocalPlayerWelcomed;
            WindowManager.PlayWindow = new PlayWindow(Program.GameEngine);
            Application.Current.MainWindow = WindowManager.PlayWindow;
			WindowManager.PlayWindow.Show();
            WindowManager.PlayWindow.Closed += PlayWindowOnClosed;
        }

        private void PlayWindowOnClosed(object sender, EventArgs eventArgs)
        {
            Program.Exit();
        }
    }
}