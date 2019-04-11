using Octgn.Installer.Plans;
using Octgn.Installer.Tools;
using System;
using System.Threading.Tasks;

namespace Octgn.Installer
{
    public class App : ViewModelBase
    {
        public string Version => _version.ToString();
        private readonly Version _version;

        public InstalledOctgn InstalledOctgn { get; }
        public Plan Plan { get; }

        public App(Version version, InstalledOctgn installedOctgn, Plan plan) {
            _version = version;
            InstalledOctgn = installedOctgn ?? throw new ArgumentNullException(nameof(installedOctgn));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        public async Task OnStart() {
            // Figure out the launch configuration (Silent, uninstalling, installing, ARP, etc)

            if (!Plan.IsQuiet) {
                var window = new MainWindow(this);
                window.Show();
            } else {
                Plan.StageChanged += Plan_StageChanged;
            }

            Plan.Start();
        }

        private void Plan_StageChanged(object sender, StageChangedEventArgs e) {
            
        }

        public void Shutdown() {
            throw new NotImplementedException();
        }
    }
}
