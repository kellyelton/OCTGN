using System;
using System.IO;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class DeleteDirectory : Step
    {
        public DirectoryInfo Directory { get; }

        public DeleteDirectory(string path) {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            Directory = new DirectoryInfo(path);
        }

        public override async Task Execute(Context context) {
            if (!Directory.Exists) return;

            Directory.Delete(true);

            await Task.Delay(100);

            Directory.Refresh();

            while (Directory.Exists) {
                await Task.Delay(100);

                Directory.Refresh();
            }
        }
    }
}
