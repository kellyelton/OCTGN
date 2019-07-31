using System;
using System.Linq;
using System.Threading.Tasks;

namespace Octgn.Installer.Plans
{
    public abstract class Plan : ViewModelBase
    {
        public static Plan Get(Context context) {
            var isQuiet = false;

            if (context.CommandLineArguments.Contains("/quiet")) {
                isQuiet = true;
            }

            if (context.InstalledOctgn.IsInstalled) {
                if (context.InstalledOctgn.InstalledVersion == context.InstallerVersion) {
                    if (context.CommandLineArguments.Contains("/uninstall", StringComparer.CurrentCultureIgnoreCase)) {
                        return new Uninstall(context, isQuiet);
                    } else {
                        return new Maintenance(context);
                    }
                } else {
                    return new Update(context, isQuiet);
                }
            } else {
                return new Install(context, isQuiet);
            }
        }

        public bool IsQuiet { get; }

        public Context Context { get; }

        public Plan(Context context, bool isQuiet) {
            Context = context ?? throw new ArgumentNullException(nameof(context));
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

        public void Next(bool force = false) {
            if (!_canGoForward && !force)
                throw new InvalidOperationException("Can't go forward");

            OnNext();
        }

        public void Back() {
            if (!_canGoBack)
                throw new InvalidOperationException("Can't go back");

            OnBack();
        }

        public Task Run() {
            return OnRun(Context);
        }

        public void Cancel() {
            OnCancel();
        }

        protected virtual void OnStart() { }
        protected virtual void OnNext() { }
        protected virtual void OnBack() { }
        protected virtual void OnCancel() { }
        protected virtual Task OnRun(Context context) => Task.Delay(0);

        public override string ToString() {
            return this.GetType().Name;
        }
    }
}
