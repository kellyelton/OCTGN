using System;
using System.IO;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class CopyDirectory : Step
    {
        public DirectoryInfo Source { get; }

        public DirectoryInfo Destination { get; }

        public CopyDirectory(DirectoryInfo source, DirectoryInfo destination) {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        }

        public override async Task Execute(Context context) {
            if (!Source.Exists)
                throw new InvalidOperationException($"Source '{Source.FullName}' does not exist.");

            await Task.Run(() => Copy(Source, Destination));
        }

        protected void Copy(DirectoryInfo source, DirectoryInfo destination) {
            if (!destination.Exists) {
                CreateDirectory(destination);
            }

            foreach (var sourceDirectory in source.GetDirectories()) {
                var childDestinationString = Path.Combine(destination.FullName, sourceDirectory.Name);

                var childDestination = new DirectoryInfo(childDestinationString);

                Copy(sourceDirectory, childDestination);
            }

            foreach (var sourceFile in source.GetFiles()) {
                var destinationString = Path.Combine(destination.FullName, sourceFile.Name);

                var destinationFile = new FileInfo(destinationString);

                Copy(sourceFile, destinationFile);
            }
        }

        protected void Copy(FileInfo source, FileInfo destination) {
            source.CopyTo(destination.FullName);
        }

        protected void CreateDirectory(DirectoryInfo directory) {
            directory.Create();
        }
    }
}
