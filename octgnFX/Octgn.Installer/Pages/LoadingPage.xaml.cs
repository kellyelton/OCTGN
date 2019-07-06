using System;
using System.Windows.Controls;

namespace Octgn.Installer.Pages
{
    public partial class LoadingPage : UserControl
    {
        public LoadingPage() {
            InitializeComponent();
        }
    }

    public class LoadingPageViewModel : PageViewModel
    {
        public LoadingPageViewModel(App app) : base(app) {
            Button1Visible = false;

            Page = new LoadingPage();
        }
    }
}
