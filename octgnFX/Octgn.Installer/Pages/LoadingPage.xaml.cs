using System;
using System.Windows.Controls;

namespace Octgn.Installer.Pages
{
    public partial class LoadingPage : UserControl
    {
        public LoadingPage() {
            InitializeComponent();

            Loaded += LoadingPage_Loaded;
        }

        private async void LoadingPage_Loaded(object sender, System.Windows.RoutedEventArgs e) {
            Loaded -= LoadingPage_Loaded;

            await App.Current.OnStart();
        }
    }

    public class LoadingPageViewModel : PageViewModel
    {
        public LoadingPageViewModel() {
            Button1Visible = false;

            Page = new LoadingPage();
        }
    }
}
