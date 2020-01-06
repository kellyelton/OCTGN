/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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

        private GameEngine _gameEngine;

        private async Task Host()
        {
            Program.GameSettings.UseTwoSidedTable = HostGame.UseTwoSidedTable;

            var gameName = Randomness.RandomRoomName();

            _gameEngine = await GameEngine.HostLocal(HostGame, gameName, "", Prefs.Nickname, true, Program.DeveloperMode);

            Octgn.Play.Player.OnLocalPlayerWelcomed += PlayerOnOnLocalPlayerWelcomed;
            Program.GameSettings.UseTwoSidedTable = HostGame.UseTwoSidedTable;

            Dispatcher.CurrentDispatcher.Invoke(new Action(()=>_gameEngine.Begin()));
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
            WindowManager.PlayWindow = new PlayWindow(_gameEngine);
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