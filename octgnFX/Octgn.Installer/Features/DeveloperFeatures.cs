using System;
using System.Collections.Generic;

namespace Octgn.Installer.Features
{
    public class DeveloperFeatures : Feature
    {
        public override bool IsRequired => false;

        public override bool IsVisible => true;

        public override string Name => "Developer Features";

        public override string Description => "Various features useful for game developers.";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {
            new AddOctgnToPath(),
            new DeveloperShortcuts()
        };
    }
}
