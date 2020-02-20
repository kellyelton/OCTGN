using CommandLine;
using Octgn.DeckBuilder;
using Octgn.Library.Exceptions;
using System.IO;
using System.Windows;

namespace Octgn.Launchers
{
    [Verb("open-deck", HelpText = "Open a deck")]
    public class OpenDeckLauncher : ILauncher
    {
        [Value(0, MetaName = "path", HelpText = "Path to the deck file", Required = true)]
        public string Path { get; set; }

        public void Launch() {
            if (!File.Exists(Path)) throw new UserMessageException("Deck File '{Path}' not found");

            var deck = new MetaDeck(Path);

            var win = new DeckBuilderWindow(deck, true);
            win.Show();

            Application.Current.MainWindow = win;
        }
    }

}
