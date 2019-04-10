using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class DeveloperShortcuts : Feature
    {
        public override bool IsRequired => false;

        public override bool IsVisible => true;

        public override string Name => "Start Menu Shortcuts";

        public override string Description => "Adds shortcuts to the start menu that are useful for OCTGN game developers";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {

        };

        public override Task Install(Context context) {
            throw new NotImplementedException();
        }

        public override Task Uninstall(Context context) {
            throw new NotImplementedException();
        }
    }
}
