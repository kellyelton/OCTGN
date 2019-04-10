using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public abstract class Feature : ViewModelBase
    {
        public abstract bool IsRequired { get; }

        public bool ShouldInstall {
            get => _shouldInstall;
            set => SetAndNotify(ref _shouldInstall, value);
        }
        private bool _shouldInstall;

        public Feature() {
            if (IsRequired)
                _shouldInstall = true;
        }

        public virtual bool IsVisible { get; } = true;

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract IEnumerable<Feature> Children { get; }

        public virtual async Task Install(Context context) {
            foreach(var child in Children) {
                await child.Install(context);
            }
        }

        public virtual async Task Uninstall(Context context) {
            foreach (var child in Children) {
                await child.Uninstall(context);
            }
        }
    }
}
