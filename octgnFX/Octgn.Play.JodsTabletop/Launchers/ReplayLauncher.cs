using System;
using System.IO;
using System.Windows;
using CommandLine;
using Octgn.DataNew;
using Octgn.Play;
using Octgn.Play.Save;

namespace Octgn.Launchers
{
    [Verb("replay-game", HelpText = "Replay a previous game")]
    public class ReplayLauncher : ILauncher
    {
        [Value(0, MetaName = "replayfile", HelpText = "Replay file", Required = true)]
        public string ReplayFile { get; set; }

        [Option('x', "devmode", Required = false, HelpText = "Enable Developer Mode")]
        public bool DevMode { get; set; }

        public void Launch() {
            ReplayReader replayReader = null;
            try {
                replayReader = ReplayReader.FromStream(File.OpenRead(ReplayFile));

                var game = DbContext.Get().GameById(replayReader.Replay.GameId);

                if (game == null) {
                    MessageBox.Show("Unable to watch replay, the game isn't installed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    return;
                }

                var gameEngine = new GameEngine(game, App.Current.Dispatcher, replayReader.Replay.User, null, replayReader, DevMode);
                gameEngine.LaunchUrl += (_, url) => {
                    App.LaunchUrl(url);
                };

                App.Current.MainWindow = App.PlayWindow = new PlayWindow(gameEngine);
                App.PlayWindow.Show();
                App.PlayWindow.Activate();
            } catch {
                replayReader?.Dispose();

                throw;
            }
        }
    }
}
