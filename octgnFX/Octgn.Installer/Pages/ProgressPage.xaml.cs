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

        public ProgressPageViewModel() {
            this.Page = new ProgressPage() {
                DataContext = this
            };

            Button1Text = "Cancel";

            throw new NotImplementedException();

            //switch (App.Current.Plan) {
            //    case SelectedPlan.Install:
            //        Task = "Installing...";
            //        break;
            //    case SelectedPlan.Uninstall:
            //        Task = "Uninstalling...";
            //        break;
            //    case SelectedPlan.ChangeDataDirectory:
            //        Task = "Modifying...";
            //        break;
            //    case SelectedPlan.Update:
            //        Task = "Updating...";
            //        break;
            //    default:
            //        throw new NotImplementedException($"RunMode {App.Current.Plan} not implemented");
            //}
        }

        public override void Button1_Action() {
            base.Button1_Action();

            //TODO: Bring to a 'Installation Cancelled' page.
            throw new NotImplementedException();
        }
    }
}
