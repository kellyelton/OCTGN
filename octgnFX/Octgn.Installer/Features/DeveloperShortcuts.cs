using System;

namespace Octgn.Installer.Features
{
    public class DeveloperShortcuts : Feature
    {
        public override bool IsRequired => false;

        public override bool IsVisible => true;

        public override string Name => "Start Menu Shortcuts";

        public override string Description => "Adds shortcuts to the start menu that are useful for OCTGN game developers";
    }
}
