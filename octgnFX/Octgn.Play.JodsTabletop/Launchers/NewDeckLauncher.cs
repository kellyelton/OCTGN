using CommandLine;
using Octgn.DeckBuilder;
using System;
using System.Windows;

namespace Octgn.Launchers
{
    [Verb("new-deck", HelpText = "Create a new deck")]
    public class NewDeckLauncher : ILauncher
    {
        [Value(0, MetaName = "gameid", HelpText = "Game Id (Guid)", Required = false)]
        public Guid GameId { get; set; }

        public NewDeckLauncher() { }

        public void Launch() {
            var win = new DeckBuilderWindow(null, true);
            win.Show();

            Application.Current.MainWindow = win;
        }
    }

}
