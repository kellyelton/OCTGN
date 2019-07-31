using Octgn.Installer.Steps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public abstract class Feature : ViewModelBase
    {
        public abstract bool IsRequired { get; }

        public bool ShouldInstall {
            get => _shouldInstall;
            set {
                if(SetAndNotify(ref _shouldInstall, value)) {
                    foreach(var child in Children) {
                        if (child.IsRequired) {
                            child.ShouldInstall = true;
                        } else {
                            child.ShouldInstall = _shouldInstall;
                        }
                    }
                }
            }
        }
        private bool _shouldInstall;

        public Feature() {
            if (IsRequired)
                _shouldInstall = true;
        }

        public virtual bool IsVisible { get; } = true;

        public abstract string Name { get; }

        public abstract string Description { get; }

        public virtual IEnumerable<Feature> Children { get; } = Enumerable.Empty<Feature>();

        public virtual IEnumerable<Step> GetInstallSteps(Context context)
            => GetChildrenInstallSteps(context);

        protected IEnumerable<Step> GetChildrenInstallSteps(Context context)
            => Children.SelectMany(child => child.GetChildrenInstallSteps(context));
    }
}
