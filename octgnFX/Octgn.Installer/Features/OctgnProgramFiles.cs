using System;
using Octgn.Installer.Steps;
using System.Collections.Generic;
using System.IO;

namespace Octgn.Installer.Features
{
    public class OctgnProgramFiles : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Files";

        public override string Description => "OCTGN Program Files required to run OCTGN.";

        public IEnumerable<Step> GetInstallSteps(Context context) {
            var resourcePath = "Octgn.Installer.InstallPackage.zip";
            var outPath = Path.Combine(context.UnpackDirectory, "InstallPackage.zip");

            yield return new ExtractEmbeddedResource(resourcePath, outPath);

            //TODO

            throw new NotImplementedException();
        }
    }
}
