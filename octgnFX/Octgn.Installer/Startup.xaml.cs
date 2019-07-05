﻿using Octgn.Installer.Plans;
using Octgn.Installer.Tools;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Octgn.Installer
{
    public partial class Startup : Application
    {
        protected override async void OnStartup(StartupEventArgs e) {
            var version = typeof(Startup)
                .Assembly
                .GetName()
                .Version;

            var installedOctgn = await Task.Run(() => InstalledOctgn.Get());

            var plan = Plan.Get(installedOctgn, version, Environment.GetCommandLineArgs());

            var app = new App(version, installedOctgn, plan);

            await app.OnStart();

            base.OnStartup(e);
        }
    }
}