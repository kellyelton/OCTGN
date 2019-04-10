using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class DataDirectory : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "OCTGN Data Directory";

        public override string Description => "The OCTGN Data Directory. This is where all the games, cards, images, decks and user settings are stored.";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {

        };

        public override Task Install(Context context) {
            throw new NotImplementedException();
        }
    }
}
