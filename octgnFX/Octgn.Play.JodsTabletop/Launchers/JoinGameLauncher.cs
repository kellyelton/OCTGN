using CommandLine;
using Octgn.Communication;
using Octgn.Core;
using Octgn.Core.DataManagers;
using Octgn.Play;
using System;
using System.Net;
using System.Windows;

namespace Octgn.Launchers
{
    [Verb("join-game", HelpText = "Join an existing game")]
    public class JoinGameLauncher : ILauncher
    {
        [Value(0, MetaName = "server", HelpText = "Game Host (domain/ip:port)", Required = true)]
        public string Server { get; set; }

        [Value(1, MetaName = "gameid", HelpText = "Game Id (Guid)", Required = true)]
        public Guid GameId { get; set; }

        [Value(2, MetaName = "nickname", HelpText = "Players Nickname", Required = true)]
        public string Nickname { get; set; }

        [Option('p', "password", Required = false, HelpText = "Hosted Game Password")]
        public string Password { get; set; }

        [Option('s', "spectator", Required = false, HelpText = "Is the Player a Spectator?")]
        public bool Spectator { get; set; }

        [Option('x', "devmode", Required = false, HelpText = "Enable Developer Mode")]
        public bool DevMode { get; set; }

        public IPEndPoint ServerEndPoint {
            get {
                if (string.IsNullOrWhiteSpace(Server)) {
                    var port = Prefs.LastLocalHostedGamePort;

                    return new IPEndPoint(IPAddress.Loopback, port);
                }

                return ParseHost(Server);
            }
        }

        public void Launch() {
            var host = ServerEndPoint;

            var game = GameManager.Get().GetById(GameId);
            User octgnUser = null;

            var app = (App)Application.Current;

            var gameEngine = GameEngine.Join(app.Dispatcher, game, octgnUser, Nickname, Password, Spectator, host.Address, host.Port, DevMode).Result;

            app.MainWindow = App.PlayWindow = new PlayWindow(gameEngine);
            App.PlayWindow.Show();
            App.PlayWindow.Activate();
        }

        private IPEndPoint ParseHost(string host) {
            var parts = host.Split(new char[1] { ':' }, 2);
            if (parts.Length != 2) throw new FormatException($"{host} is not a valid host");

            var ip = IPAddress.Parse(parts[0]);

            var port = int.Parse(parts[1]);

            if (port <= 0) throw new FormatException($"port {port} is invalid");

            return new IPEndPoint(ip, port);
        }
    }
}
