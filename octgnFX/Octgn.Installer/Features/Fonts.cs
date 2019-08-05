using System;
using System.Collections.Generic;
using Octgn.Installer.Steps;

namespace Octgn.Installer.Features
{
    public class Fonts : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Fonts";

        public override string Description => "Fonts used by OCTGN.";

        public override IEnumerable<Step> GetInstallSteps(Context context) {
            throw new NotImplementedException();
        }
    }
}
