using System;

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
    }
}
