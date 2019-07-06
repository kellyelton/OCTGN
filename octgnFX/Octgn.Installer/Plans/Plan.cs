using Octgn.Installer.Tools;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Octgn.Installer.Plans
{
    public abstract class Plan : ViewModelBase
    {
        public static Plan Get(InstalledOctgn installedOctgn, Version installerVersion, string[] commandLineArgs) {
            var isQuiet = false;

            if (commandLineArgs.Contains("/quiet")) {
                isQuiet = true;
            }

            if (installedOctgn.IsInstalled) {
                if (installedOctgn.InstalledVersion == installerVersion) {
                    if (commandLineArgs.Contains("/uninstall", StringComparer.CurrentCultureIgnoreCase)) {
                        return new Uninstall(isQuiet);
                    } else {
                        return new Maintenance();
                    }
                } else {
                    return new Update(isQuiet);
                }
            } else {
                return new Install(isQuiet);
            }
        }

        public bool IsQuiet { get; private set; }

        public Plan(bool isQuiet) {
            IsQuiet = isQuiet;
        }

        public bool CanGoBack {
            get => _canGoBack;
            protected set => SetAndNotify(ref _canGoBack, value);
        }
        private bool _canGoBack;

        public bool CanGoForward {
            get => _canGoForward;
            protected set => SetAndNotify(ref _canGoForward, value);
        }
        private bool _canGoForward;

        public Stage Stage {
            get => _stage;
            protected set {
                var oldStage = _stage;
                if(SetAndNotify(ref _stage, value)) {
                    StageChanged?.Invoke(this, new StageChangedEventArgs(oldStage, _stage));
                }
            }
        }
        private Stage _stage;

        public event EventHandler<StageChangedEventArgs> StageChanged;

        private bool _isStarted = false;

        public void Start() {
            if (_isStarted) throw new InvalidOperationException("Already Started");
            _isStarted = true;

            OnStart();
        }

        public void Next() {
            if (!_canGoForward)
                throw new InvalidOperationException("Can't go forward");

            OnNext();
        }

        public void Back() {
            if (!_canGoBack)
                throw new InvalidOperationException("Can't go back");

            OnBack();
        }

        public Task Run() {
            return OnRun();
        }

        public void Cancel() {
            OnCancel();
        }

        protected virtual void OnStart() { }
        protected virtual void OnNext() { }
        protected virtual void OnBack() { }
        protected virtual void OnCancel() { }
        protected virtual Task OnRun() => Task.Delay(0);

        public override string ToString() {
            return this.GetType().Name;
        }
    }
}
