using System;
using System.Collections.Generic;
using Octgn.Installer.Steps;

namespace Octgn.Installer.Features
{
    public class AddOctgnToPath : Feature
    {
        public override bool IsRequired => false;

        public override bool IsVisible => true;

        public override string Name => "Add OCTGN to PATH";

        public override string Description => "This adds the OCTGN install directory to the %PATH% environmental variable.";

        public override IEnumerable<Step> GetInstallSteps(Context context) {
            yield return new AddToPath(context.InstallDirectory, Scope.Machine);
        }
    }
}
