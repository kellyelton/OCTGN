using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Octgn.Installer.Steps;

namespace Octgn.Installer.Features
{
    public class DeckFileSupport : Feature
    {
        public override bool IsRequired => false;

        public override string Name => ".08d(OCTGN Deck File) Support";

        public override string Description => "Allows 08d(OCTGN Deck) files to be launched by double clicking them.";

        public DeckFileSupport() {
            ShouldInstall = true;
        }

        public override IEnumerable<Step> GetInstallSteps(Context context) {
            var installPath = Path.Combine(context.InstallDirectory, "Octgn.exe");

            yield return new SetRegistryValue(Registry.LocalMachine, "Software\\Classes\\.o8d", null, "octgn.o8d");
            yield return new SetRegistryValue(Registry.LocalMachine, "Software\\Classes\\octgn.o8d", null, "OCTGN Deck File");
            yield return new SetRegistryValue(Registry.LocalMachine, "Software\\Classes\\octgn.o8d\\DefaultIcon", null, installPath);
            yield return new SetRegistryValue(Registry.LocalMachine, "Software\\Classes\\octgn.o8d\\shell\\open\\command", null, $"\"{installPath}\" \"%1\"");
        }
    }
}
