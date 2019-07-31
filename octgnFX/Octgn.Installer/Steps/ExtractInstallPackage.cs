using System;
using System.IO;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class ExtractInstallPackage : Step
    {
        public override async Task Execute(Context context) {
            var resourcePath = "Octgn.Installer.InstallPackage.zip";
            var installPackageZip = Path.Combine(context.UnpackDirectory, "InstallPackage.zip");
            var unpackZipDirectory = Path.Combine(context.UnpackDirectory, "InstallPackage");

            var embeddedResourceExtractor = new EmbeddedResourceExtractor(resourcePath, installPackageZip);
            await embeddedResourceExtractor.Execute(context);

            var zipFileExtractor = new ZipFileExtractor(installPackageZip, unpackZipDirectory);
            await zipFileExtractor.Execute(context);
        }
    }
}
