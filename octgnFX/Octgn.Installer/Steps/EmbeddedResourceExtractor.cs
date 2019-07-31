using System;
using System.IO;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class EmbeddedResourceExtractor : Step
    {
        public string ResourcePath { get; }

        public FileInfo ExtractFile { get; }

        public EmbeddedResourceExtractor(string resourcePath, string extractFilePath) {
            if (string.IsNullOrWhiteSpace(nameof(resourcePath))) throw new ArgumentNullException(nameof(resourcePath));
            if (string.IsNullOrWhiteSpace(extractFilePath)) throw new ArgumentNullException(nameof(extractFilePath));

            ResourcePath = resourcePath;

            ExtractFile = new FileInfo(extractFilePath);
        }

        public override async Task Execute(Context context) {
            if (ExtractFile.Exists) {
                ExtractFile.Delete();
            }

            if (!ExtractFile.Directory.Exists) {
                ExtractFile.Directory.Create();
            }

            var ass = typeof(EmbeddedResourceExtractor).Assembly;

            using (var stream = ass.GetManifestResourceStream(ResourcePath))
            using (var outStream = ExtractFile.Open(FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)) {
                await stream.CopyToAsync(outStream);

                await outStream.FlushAsync();
            }
        }
    }
}
