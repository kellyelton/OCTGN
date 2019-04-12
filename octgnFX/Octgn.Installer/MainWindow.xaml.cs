using System;
using System.Windows;
using System.Windows.Controls;
using Octgn.Installer.Pages;

namespace Octgn.Installer
{
    public partial class MainWindow : Window
    {
        private static readonly DependencyProperty PageViewModelProperty
            = DependencyProperty.RegisterAttached(nameof(PageViewModel), typeof(PageViewModel), typeof(MainWindow), new PropertyMetadata(OnPageViewModelChanged));

        private static void OnPageViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var window = (MainWindow)d;

            var oldVm = (PageViewModel)e.OldValue;
            var newVm = (PageViewModel)e.NewValue;

            window.OnPageChanged(oldVm, newVm);
        }

        private void OnPageChanged(PageViewModel oldPage, PageViewModel newPage) {
            if (oldPage != null) {
                oldPage.Transition -= Page_Transition;
            }

            newPage.Transition += Page_Transition;

            Page = newPage.Page;
        }

        private void Page_Transition(object sender, PageTransitionEventArgs e) {
            PageViewModel = e.Page;
        }

        public PageViewModel PageViewModel {
            get => (PageViewModel)GetValue(PageViewModelProperty);
            set => SetValue(PageViewModelProperty, value);
        }

        private static readonly DependencyProperty PageProperty
            = DependencyProperty.RegisterAttached(nameof(Page), typeof(UserControl), typeof(MainWindow));

        public UserControl Page {
            get => (UserControl)GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }

        private readonly App _app;

        public MainWindow(App app) {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            InitializeComponent();

            VersionText.Text = app.Version;

            PageViewModel = new LoadingPageViewModel(_app);

            _app.Plan.StageChanged += Plan_StageChanged;
        }

        private void Plan_StageChanged(object sender, Plans.StageChangedEventArgs e) {
            switch (e.NewStage) {
                case Plans.Stage.Loading:
                    PageViewModel = new LoadingPageViewModel(_app);
                    break;
                case Plans.Stage.Terms:
                    PageViewModel = new TermsPageViewModel(_app);
                    break;
                case Plans.Stage.Progress:
                    PageViewModel = new ProgressPageViewModel(_app);
                    break;
                case Plans.Stage.ChooseMaintenance:
                    PageViewModel = new UninstallOrModifyPageViewModel(_app);
                    break;
                case Plans.Stage.Features:
                case Plans.Stage.ConfirmUninstall:
                case Plans.Stage.FinishedUninstalling:
                case Plans.Stage.FinishedInstalling:
                case Plans.Stage.FinishedWithError:
                default:
                    throw new InvalidOperationException($"Stage {e.NewStage} has not been implemented here.");
            }
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            this.DragMove();
        }

        private void Button1_Click(object sender, RoutedEventArgs e) {
            PageViewModel.Button1_Action();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            _app.Shutdown();
        }
    }
}
