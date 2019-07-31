using System;
using System.Collections.Generic;
using Octgn.Installer.Steps;

namespace Octgn.Installer.Features
{
    public class OctgnProgramFiles : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Files";

        public override string Description => "OCTGN Program Files required to run OCTGN.";

        public override IEnumerable<Step> GetInstallSteps(Context context) {
            //TODO: yield return new DeleteDirectory(context.InstallDirectory);

            yield return new CreateDirectory(context.InstallDirectory);

            throw new NotImplementedException();

            //TODO: yield return new CopyFiles(unpack\INstallPackage\Octgn, InstallDirectory)
        }
    }
}
