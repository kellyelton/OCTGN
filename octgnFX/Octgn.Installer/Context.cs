using Octgn.Installer.Tools;
using System;
using System.IO;

namespace Octgn.Installer
{
    public class Context
    {
        public Guid SessionId { get; } = Guid.NewGuid();

        public InstalledOctgn InstalledOctgn { get; }

        public Version InstallerVersion { get; }

        public string[] CommandLineArguments { get; }

        public string InstallDirectory { get; }

        public string UnpackDirectory { get; }

        public string UnpackedInstallPackageDirectory { get; }

        public string UnpackedInstallerDirectory { get; }

        public string DataDirectory { get; }

        public Context(InstalledOctgn installedOctgn, Version installerVersion, string[] commandLineArgs) {
            InstalledOctgn = installedOctgn ?? throw new ArgumentNullException(nameof(installedOctgn));
            InstallerVersion = installerVersion ?? throw new ArgumentNullException(nameof(installerVersion));
            CommandLineArguments = commandLineArgs ?? throw new ArgumentNullException(nameof(commandLineArgs));

            UnpackDirectory = Environment.ExpandEnvironmentVariables($"%TEMP%\\Octgn\\Installers\\{SessionId:N}");

            UnpackedInstallPackageDirectory = Path.Combine(UnpackDirectory, "InstallPackage");

            UnpackedInstallerDirectory = Path.Combine(UnpackedInstallPackageDirectory, "Octgn");
        }
    }
}
