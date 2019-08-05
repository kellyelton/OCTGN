using System;
using System.Collections.Generic;
using System.IO;
using Octgn.Installer.Steps;

namespace Octgn.Installer.Features
{
    public class OctgnProgramFiles : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Files";

        public override string Description => "OCTGN Program Files required to run OCTGN.";

        public override IEnumerable<Step> GetInstallSteps(Context context) {
            var unpackDirectory = new DirectoryInfo(context.UnpackedInstallerDirectory);

            var installDirectory = new DirectoryInfo(context.InstallDirectory);

            yield return new DeleteDirectory(context.InstallDirectory);

            yield return new CopyDirectory(unpackDirectory, installDirectory);
        }
    }
}
