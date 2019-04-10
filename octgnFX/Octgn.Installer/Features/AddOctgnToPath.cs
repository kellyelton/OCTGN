using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class AddOctgnToPath : Feature
    {
        public override bool IsRequired => false;

        public override bool IsVisible => true;

        public override string Name => "Add OCTGN to PATH";

        public override string Description => "This adds the OCTGN install directory to the %PATH% environmental variable.";

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
