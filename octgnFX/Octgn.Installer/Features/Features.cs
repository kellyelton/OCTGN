using System;
using System.Collections.Generic;

namespace Octgn.Installer.Features
{
    public class Features : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Features";

        public override string Description => "All the OCTGN features";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {
            new DotNet47Framework(),
            new OctgnProgram(),
            new DataDirectory(),
            new DeveloperFeatures()
        };
    }
}
