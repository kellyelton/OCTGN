using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Octgn.Installer.Bundle.UI.Pages;
using Octgn.Installer.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Octgn.Installer.Bundle.UI
{
    public class App : BootstrapperApplication
    {
        public static App Current { get; set; }

        public App() {
            if (Current != null) throw new InvalidOperationException("Already created App");

            Current = this;

            ApplyComplete += this.OnApplyComplete;
            PlanPackageBegin += this.App_PlanPackageBegin;
            PlanComplete += this.OnPlanComplete;
            DetectComplete += App_DetectComplete;
            Error += App_Error;
            PlanRelatedBundle += App_PlanRelatedBundle;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public bool IsCancelling { get; private set; }

        public SelectedPlan Plan { get; set; }

        public Dispatcher Dispatcher { get; private set; }

        public string Version { get; private set; }

        public void Cancel() {
            IsCancelling = true;

            Dispatcher.InvokeShutdown();
        }

        private MainWindow _mainWindow;

        private bool _launchOctgn;

        protected override void Run() {
            Dispatcher = Dispatcher.CurrentDispatcher;

            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            Engine.Detect();

            Version = Engine.StringVariables["BundleVersion"];

            _launchOctgn = Environment.GetCommandLineArgs()
                .Where(x => x != null)
                .Any(x => x.Equals("/LaunchOCTGN", StringComparison.CurrentCultureIgnoreCase));

            _mainWindow = new MainWindow();
            _mainWindow.Show();

            Dispatcher.Run();

            _mainWindow.Close();

            this.Engine.Quit(0);
        }

        private bool _octgnMsiInstalled = false;
        private bool _bundleInstalled = false;

        protected override void OnDetectRelatedMsiPackage(DetectRelatedMsiPackageEventArgs args) {
            Engine.Log(LogLevel.Standard, $"OnDetectRelatedMsiPackage: {args.PackageId} {args.ProductCode} {args.Operation} {args.Version} {args.Result}");
            base.OnDetectRelatedMsiPackage(args);
        }

        protected override void OnDetectPackageComplete(DetectPackageCompleteEventArgs args) {
            Engine.Log(LogLevel.Standard, $"OnDetectPackageComplete: {args.PackageId} {args.State} {args.Status}");
            if(args.PackageId == "MainPackage" && args.State == PackageState.Present) {
                _octgnMsiInstalled = true;
                Engine.Log(LogLevel.Standard, $"OCTGN already installed.");
            }
            base.OnDetectPackageComplete(args);
        }

        protected override void OnDetectTargetMsiPackage(DetectTargetMsiPackageEventArgs args) {
            Engine.Log(LogLevel.Standard, $"OnDetectTargetMsiPackage: {args.PackageId} {args.State} {args.ProductCode} {args.Result}");
            base.OnDetectTargetMsiPackage(args);
        }

        private void App_PlanPackageBegin(object sender, PlanPackageBeginEventArgs e) {
            Engine.Log(LogLevel.Standard, "PlanPackageBeing: " + e.PackageId);
        }

        private void App_PlanRelatedBundle(object sender, PlanRelatedBundleEventArgs e) {
            Engine.Log(LogLevel.Standard, "PlanRelatedBundle");
        }

        protected override void OnDetectRelatedBundle(DetectRelatedBundleEventArgs args) {
            Engine.Log(LogLevel.Standard, "OnDetectRelatedBundle: Detected " + args.ProductCode + " " + args.Version.ToString() + " " + args.Result);

            _bundleInstalled = true;

            Engine.Log(LogLevel.Standard, "OnDetectRelatedBundle: Found Octgn bundle");
            
            base.OnDetectRelatedBundle(args);
        }

        private void App_DetectComplete(object sender, DetectCompleteEventArgs e) {
            Engine.Log(LogLevel.Standard, $"Detect complete: Action={Command.Action} Display={Command.Display} Resume={Command.Resume}");
            Engine.Log(LogLevel.Standard, $"Bundle Installed={_bundleInstalled}");
            Engine.Log(LogLevel.Standard, $"Octgn Installed={_octgnMsiInstalled}");

            try {
                switch (Command.Action) {
                    case LaunchAction.Uninstall:
                        Plan = SelectedPlan.Uninstall;
                        Engine.Plan(LaunchAction.Uninstall);
                        break;
                    case LaunchAction.Install:
                        if (Command.Resume == ResumeType.Arp) {
                            Dispatcher.BeginInvoke(new Action(() => {
                                _mainWindow.PageViewModel = new UninstallOrModifyPageViewModel();
                            }));
                        } else {
                            if (_bundleInstalled) {
                                Plan = SelectedPlan.Update;
                                Engine.Plan(LaunchAction.Install);
                            } else {
                                Plan = SelectedPlan.Install;
                                Engine.Plan(LaunchAction.Install);
                            }
                        }
                        break;
                    case LaunchAction.Modify:
                        Dispatcher.BeginInvoke(new Action(() => {
                            _mainWindow.PageViewModel = new UninstallOrModifyPageViewModel();
                        }));
                        break;
                    default: {
                            Engine.Log(LogLevel.Error, $"Unexpected LaunchAction {Command.Action} {Command.Display}");
                            throw new InvalidOperationException($"Unexpected LaunchAction {Command.Action} {Command.Display}");
                        }
                }
            } catch (Exception ex) {
                this.Engine.Log(LogLevel.Error, "App_DetectComplete:" + ex.ToString());
                MessageBox.Show("An unexpected error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Dispatcher.InvokeShutdown();
            }
        }

        public void StartPlan() {
            Engine.Log(LogLevel.Standard, "Start Plan");

            this.Engine.Apply(IntPtr.Zero);
        }

        private void OnPlanComplete(object sender, PlanCompleteEventArgs e) {
            Engine.Log(LogLevel.Standard, $"Plan Complete: {e.Status}");

            try {
                if (e.Status >= 0) {
                    Dispatcher.BeginInvoke(new Action(() => {
                        if (!WaitForOctgnToClose()) {
                            Dispatcher.InvokeShutdown();

                            return;
                        }

                        var status = e.Status;
                        switch (Plan) {
                            case SelectedPlan.Install:
                                _mainWindow.PageViewModel = new TermsPageViewModel();
                                break;
                            case SelectedPlan.Uninstall:
                                _mainWindow.PageViewModel = new ProgressPageViewModel();
                                StartPlan();
                                break;
                            case SelectedPlan.ChangeDataDirectory:
                                _mainWindow.PageViewModel = new DirectorySelectionPageViewModel();
                                break;
                            case SelectedPlan.Update:
                                _mainWindow.PageViewModel = new ProgressPageViewModel();
                                StartPlan();
                                break;
                            default:
                                throw new NotImplementedException($"RunMode {Plan} not implemented"); ;
                        }
                    }));
                } else {
                    var error = new Win32Exception(e.Status).Message;
                    var message = $"Error installing: {error}. Code: 0x{e.Status:x8}";

                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    Dispatcher.InvokeShutdown();
                }
            } catch (Exception ex) {
                this.Engine.Log(LogLevel.Error, "App_DetectComplete:" + ex.ToString());
                MessageBox.Show("An unexpected error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Dispatcher.InvokeShutdown();
            }
        }

        private void OnApplyComplete(object sender, ApplyCompleteEventArgs e) {
            Engine.Log(LogLevel.Standard, $"Apply Complete: {e.Status} {e.Result}");

            try {
                if (e.Status == 0) {
                    if (Plan == SelectedPlan.Install || Plan == SelectedPlan.ChangeDataDirectory || Plan == SelectedPlan.Update) {
                        Engine.Log(LogLevel.Standard, "Is installing or modifying");

                        if (_launchOctgn) {
                            Engine.Log(LogLevel.Standard, "Going to try and launch OCTGN");

                            var installed = InstalledOctgn.Get();

                            try {
                                if (installed.IsInstalled) {
                                    Engine.Log(LogLevel.Standard, "Going to launch OCTGN.");

                                    var path = Path.Combine(installed.InstalledDirectory.FullName, "Octgn", "OCTGN.exe");

                                    Engine.Log(LogLevel.Standard, "OCTGN Path: " + path);

                                    var psi = new ProcessStartInfo(path);
                                    psi.UseShellExecute = true;

                                    Process.Start(psi);
                                } else {
                                    this.Engine.Log(LogLevel.Error, "Error launching OCTGN, it's not installed?");
                                }
                            } catch (Exception ex) {
                                this.Engine.Log(LogLevel.Error, "Error launching OCTGN: " + ex.ToString());
                            }
                        }
                    }
                    Dispatcher.InvokeShutdown();
                } else {
                    Engine.Log(LogLevel.Error, "Apply failed: " + e.Status + " " + e.Result);
                }
            } catch (Exception ex) {
                this.Engine.Log(LogLevel.Error, "App_DetectComplete:" + ex.ToString());
                MessageBox.Show("An unexpected error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Dispatcher.InvokeShutdown();
            }
        }

        private void App_Error(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ErrorEventArgs e) {
            this.Engine.Log(LogLevel.Error, "Error:" + e.ErrorCode + ":" + e.ErrorMessage);
            MessageBox.Show(e.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Dispatcher.InvokeShutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var exception = (Exception)e.ExceptionObject;

            this.Engine.Log(LogLevel.Error, "UnhandledException:" + exception.ToString());
            MessageBox.Show("An unexpected error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Dispatcher.InvokeShutdown();
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            this.Engine.Log(LogLevel.Error, "DispatcherUnhandledException:" + e.Exception.ToString());
            MessageBox.Show("An unexpected error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Dispatcher.InvokeShutdown();
        }

        public bool IsIncompatibleOctgnInstalled() {
            //TODO: This should be able to check the registry or something, the previous installer should have left some artifact we can use. This may be unneccisary though.
            var oldPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            oldPath = Path.Combine(oldPath, "Octgn", "OCTGN");

            if (Directory.Exists(oldPath)) {
                return true;
            } return false;
        }

        public bool WaitForOctgnToClose() {
            while (true) {
                if (!IsOctgnRunning()) {
                    return true;
                }

                var result = MessageBox.Show("OCTGN is running. Please close OCTGN before you continue.", "OCTGN is running", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                switch (result) {
                    case MessageBoxResult.Cancel:
                    case MessageBoxResult.No:
                        return false;
                }
            }
        }

        public bool IsOctgnRunning() {
            foreach (var clsProcess in Process.GetProcesses()) {
                if (clsProcess.ProcessName.Equals("OCTGN", StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }
    }

    public enum SelectedPlan
    {
        Install,
        Update,
        Uninstall,
        ChangeDataDirectory
    }
}
