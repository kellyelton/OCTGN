using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class DeckFileSupport : Feature
    {
        public override bool IsRequired => false;

        public override string Name => ".08d(OCTGN Deck File) Support";

        public override string Description => "Allows 08d(OCTGN Deck) files to be launched by double clicking them.";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {

        };

        public DeckFileSupport() {
            ShouldInstall = true;
        }

        public override Task Install(Context context) {
            throw new NotImplementedException();
        }

        public override Task Uninstall(Context context) {
            throw new NotImplementedException();
        }
    }
}
