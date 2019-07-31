using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class ZipFileExtractor : Step
    {
        public FileInfo ZipPath { get; }

        public DirectoryInfo ExtractTo { get; }

        public ZipFileExtractor(string zipPath, string extractTo) {
            if (string.IsNullOrWhiteSpace(nameof(zipPath))) throw new ArgumentNullException(nameof(zipPath));
            if (string.IsNullOrWhiteSpace(extractTo)) throw new ArgumentNullException(nameof(extractTo));

            ZipPath = new FileInfo(zipPath);

            ExtractTo = new DirectoryInfo(extractTo);
        }

        public override async Task Execute(Context context) {
            if (!ZipPath.Exists) {
                throw new InvalidOperationException($"Zip File {ZipPath.FullName} does not exist.");
            }

            if (!ExtractTo.Exists) {
                ExtractTo.Create();
            }

            using (var zipFile = ZipFile.Open("asdf", ZipArchiveMode.Read)) {
                foreach (var entry in zipFile.Entries) {
                    var newPath = Path.Combine(ExtractTo.FullName, entry.FullName);

                    entry.ExtractToFile(newPath, true);
                }
            }
        }
    }
}
