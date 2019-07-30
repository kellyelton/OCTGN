using System;

namespace Octgn.Installer.Features
{
    public class DataDirectory : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "OCTGN Data Directory";

        public override string Description => "The OCTGN Data Directory. This is where all the games, cards, images, decks and user settings are stored.";
    }
}
