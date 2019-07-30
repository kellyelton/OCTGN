using System;
using Octgn.Installer.Steps;
using System.Collections.Generic;

namespace Octgn.Installer.Features
{
    public class OctgnProgramFiles : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Files";

        public override string Description => "OCTGN Program Files required to run OCTGN.";

        public IEnumerable<Step> GetInstallSteps() {
            // var resourcePath = "Octgn.Installer.InstallPackage.zip";
            // var outPath = "%octgntemp%\%sessionguid%\InstallPackage.zip"
            //TODO: yield return new ExtractEmbeddedResource()


            throw new NotImplementedException();
        }
    }
}
