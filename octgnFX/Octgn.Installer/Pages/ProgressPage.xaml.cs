using Octgn.Installer.Plans;
using System;
using System.Windows.Controls;

namespace Octgn.Installer.Pages
{
    public partial class ProgressPage : UserControl
    {
        public ProgressPage() {
            InitializeComponent();
        }
    }

    public class ProgressPageViewModel : PageViewModel
    {
        public string Status {
            get => _status;
            set => SetAndNotify(ref _status, value);
        }
        private string _status;

        public int Progress {
            get => _progress;
            set => SetAndNotify(ref _progress, value);
        }
        private int _progress;

        public string Task {
            get => _task;
            set => SetAndNotify(ref _task, value);
        }
        private string _task;

        public ProgressPageViewModel(App app) : base(app) {
            this.Page = new ProgressPage() {
                DataContext = this
            };

            Button1Text = "Cancel";

            if(App.Plan is Install installPlan) {
                Task = "Installing...";
            } else {
                throw new NotImplementedException($"Plan {App.Plan} not implemented");
            }
        }

        public override void Button1_Action() {
            base.Button1_Action();

            //TODO: Bring to a 'Installation Cancelled' page.
            throw new NotImplementedException();
        }
    }
}
