using System;
using System.IO;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class CreateDirectory : Step
    {
        public DirectoryInfo Directory { get; }

        public CreateDirectory(string path) {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            Directory = new DirectoryInfo(path);
        }

        public override Task Execute() {
            if (!Directory.Exists) {
                Directory.Create();
            }

            return Task.Delay(0);
        }
    }
}
