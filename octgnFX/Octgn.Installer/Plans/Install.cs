using Octgn.Installer.Steps;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Octgn.Installer.Plans
{
    public class Install : Plan
    {
        public Features.Features Features { get; }

        public Install(Context context, bool isQuiet) : base(context, isQuiet) {
            Features = new Features.Features();

            Stage = Stage.Loading;

            CanGoForward = true;
            CanGoBack = false;
        }

        protected override void OnNext() {
            switch (Stage) {
                case Stage.Loading:
                    Stage = Stage.Terms;
                    CanGoBack = false;
                    CanGoForward = true;
                    break;
                case Stage.Terms:
                    Stage = Stage.Features;
                    CanGoBack = true;
                    CanGoForward = true;
                    break;
                case Stage.Features:
                    Stage = Stage.Progress;
                    CanGoBack = false;
                    CanGoForward = false;
                    break;
                case Stage.Progress:
                    Stage = Stage.FinishedInstalling;
                    CanGoBack = false;
                    CanGoForward = false;
                    break;
                case Stage.FinishedInstalling:
                case Stage.FinishedWithError:
                case Stage.ConfirmUninstall:
                case Stage.ChooseMaintenance:
                case Stage.FinishedUninstalling:
                default:
                    throw new InvalidOperationException($"Stage transition from {Stage} is not valid here.");
            }
        }

        protected override void OnBack() {

        }

        protected override async Task OnRun(Context context) {
            switch (Stage) {
                case Stage.Loading:
                    Next();
                    break;
                case Stage.Terms:
                    break;
                case Stage.Features:
                    break;
                case Stage.Progress:
                    var installSteps = Features.GetInstallSteps(context).ToList();

                    //TODO: Maybe to fail if OCTGN is running and force a retry to check again

                    installSteps.Insert(0, new ExtractInstallPackage());

                    foreach(var step in installSteps) {
                        await step.Execute(context);
                    }

                    Next(force: true);
                    break;
                case Stage.FinishedInstalling:
                    break;
                case Stage.FinishedWithError:
                case Stage.ConfirmUninstall:
                case Stage.ChooseMaintenance:
                case Stage.FinishedUninstalling:
                default:
                    throw new InvalidOperationException($"Can't run stage {Stage} here.");
            }
        }
    }
}
