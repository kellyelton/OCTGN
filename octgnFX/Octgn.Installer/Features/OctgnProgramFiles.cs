using System;
using Octgn.Installer.Steps;
using System.IO;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class OctgnProgramFiles : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Files";

        public override string Description => "OCTGN Program Files required to run OCTGN.";

        public override async Task Install(Context context) {
            var resourcePath = "Octgn.Installer.InstallPackage.zip";
            var installPackageZip = Path.Combine(context.UnpackDirectory, "InstallPackage.zip");
            var unpackZipDirectory = Path.Combine(context.UnpackDirectory, "InstallPackage");

            var embeddedResourceExtractor = new EmbeddedResourceExtractor(resourcePath, installPackageZip);
            await embeddedResourceExtractor.Execute(context);

            var zipFileExtractor = new ZipFileExtractor(installPackageZip, unpackZipDirectory);
            await zipFileExtractor.Execute(context);

            await InstallChildren(context);
        }
    }
}
