using System.Windows;
using MessageBox = System.Windows.MessageBox;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Octgn.Launchers
{
    public class TableLauncher : UpdatingLauncher
    {
        private readonly int? hostPort;
        private readonly Guid? gameId;
        private readonly Dispatcher _dispatcher;

        public TableLauncher(Dispatcher dispatcher, int? hostport, Guid? gameid) {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            this.hostPort = hostport;
            this.gameId = gameid;
            if (this.gameId == null) {
                MessageBox.Show("You must supply a GameId with -g=GUID on the command line.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown = true;
            }
        }

        public override Task BeforeUpdate() {
            return Task.CompletedTask;
        }

        public override async Task AfterUpdate() {
            try {
                await new GameTableLauncher().Launch(_dispatcher, this.hostPort, this.gameId);
            } catch (Exception e) {
                this.Log.Warn("Couldn't host/join table mode", e);
                this.Shutdown = true;
                Program.Exit();
            }
        }
    }
}