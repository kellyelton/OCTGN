using System;
using System.Threading.Tasks;

namespace Octgn.Installer.Plans
{
    public class Install : Plan
    {
        public Features.Features Features { get; }

        public Install(bool isQuiet) : base(isQuiet) {
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

        protected override async Task OnRun() {
            switch (Stage) {
                case Stage.Loading:
                    Next();
                    break;
                case Stage.Terms:
                    break;
                case Stage.Features:
                    break;
                case Stage.Progress:
                    //TODO: Install
                    var context = new Context();
                    await Features.Install(context);

                    break;
                case Stage.FinishedInstalling:
                    break;
                case Stage.FinishedWithError:
                    break;
                case Stage.ConfirmUninstall:
                case Stage.ChooseMaintenance:
                case Stage.FinishedUninstalling:
                default:
                    throw new InvalidOperationException($"Can't run stage {Stage} here.");
            }
        }
    }
}
