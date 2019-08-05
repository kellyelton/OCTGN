using System;
using System.Collections.Generic;
using Octgn.Installer.Steps;

namespace Octgn.Installer.Features
{
    public class Logging : Feature
    {
        public override bool IsRequired => true;

        public override bool IsVisible => false;

        public override string Name => "Logging";

        public override string Description => "Logging support in OCTGN.";

        public override IEnumerable<Step> GetInstallSteps(Context context) {
            throw new NotImplementedException();
        }
    }
}
