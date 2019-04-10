using System;
using System.Windows;

namespace Octgn.Installer
{
    public partial class Startup : Application
    {
        protected override void OnStartup(StartupEventArgs e) {
            var app = new App();
            App.Current = app;

            App.Current.Version = typeof(Startup)
                .Assembly
                .GetName()
                .Version
                .ToString();

            base.OnStartup(e);
        }
    }
}
