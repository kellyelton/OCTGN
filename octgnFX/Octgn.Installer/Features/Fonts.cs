using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class Fonts : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Fonts";

        public override string Description => "Fonts used by OCTGN.";

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
