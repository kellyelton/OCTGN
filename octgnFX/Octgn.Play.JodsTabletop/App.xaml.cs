/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Octgn.Communication;
using Octgn.Core.DataManagers;
using Octgn.Play;
using System;
using System.Net;
using System.Windows;

namespace Octgn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsReleaseTest { get; private set; }

        public static bool IsDeveloperMode { get; private set; }

        public static PlayWindow PlayWindow { get; private set; }

        public static Guid GameDefinitionId { get; private set; }

        protected override void OnStartup(StartupEventArgs e) {
            //TODO: Get from command args
            log4net.GlobalContext.Properties["gameid"] = "12345";
            IsReleaseTest = false;

            string nickname = null;
            string password = null;
            bool spectator = false;
            IPEndPoint host = null;

            var os = new Mono.Options.OptionSet()
            {
                    {"g|game=", x => GameDefinitionId = Guid.Parse(x)},
                    {"n|nickname=", x => nickname = x},
                    {"p|password=", x => password = x},
                    {"s|spectator", x => spectator = true},
                    {"h|host", x => host = ParseHost(x)},
                    {"x|devmode", x => IsDeveloperMode = true}
                };

            os.Parse(e.Args);

            var game = GameManager.Get().GetById(GameDefinitionId);
            User octgnUser = null;

            var gameEngine = GameEngine.Join(Dispatcher, game, octgnUser, nickname, password, spectator, host.Address, host.Port, IsDeveloperMode).Result;

            App.PlayWindow = new PlayWindow(gameEngine);
            App.PlayWindow.Show();
            App.PlayWindow.Activate();
        }

        private IPEndPoint ParseHost(string host) {
            var parts = host.Split(new char[1] { ';' }, 2);
            if (parts.Length != 2) throw new FormatException($"{host} is not a valid host");

            var ip = IPAddress.Parse(parts[0]);

            var port = int.Parse(parts[1]);

            if (port <= 0) throw new FormatException($"port {port} is invalid");

            return new IPEndPoint(ip, port);
        }
    }
}
