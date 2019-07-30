using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class OctgnProgram : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "OCTGN";

        public override string Description => "The main OCTGN program.";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {
            new DeckFileSupport(),
            new Fonts(),
            new Logging()
        };

        public override async Task Install(Context context) {
            await InstallChildren(context);

            throw new NotImplementedException();
        }

        public override async Task Uninstall(Context context) {
            await UninstallChildren(context);

            throw new NotImplementedException();
        }
    }
}
