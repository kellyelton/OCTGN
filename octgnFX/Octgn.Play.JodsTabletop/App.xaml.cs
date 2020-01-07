/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Octgn.Play;
using System;
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

        protected override void OnStartup(StartupEventArgs e) {
            //TODO: Get from command args
            log4net.GlobalContext.Properties["gameid"] = "12345";
            IsReleaseTest = false;
        }
    }
}
