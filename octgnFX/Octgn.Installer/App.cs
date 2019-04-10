using System;
using System.Threading.Tasks;

namespace Octgn.Installer
{
    public class App : ViewModelBase
    {
        public static App Current { get; set; }

        public string Version {
            get => _version;
            set => SetAndNotify(ref _version, value);
        }
        private string _version;

        public async Task OnStart() {
            // Figure out the launch configuration (Silent, uninstalling, installing, ARP, etc)


        }

        public void Shutdown() {
            throw new NotImplementedException();
        }
    }
}
