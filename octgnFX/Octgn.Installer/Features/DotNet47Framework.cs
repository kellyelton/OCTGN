using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class DotNet47Framework : Feature
    {
        public override bool IsRequired => true;

        public override string Name => ".Net 4.7 Framework";

        public override string Description => "The Microsoft .Net 4.7 Framework. This is the framework used to build OCTGN.";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {

        };

        public override Task Install(Context context) {
            throw new NotImplementedException();
        }
    }
}
