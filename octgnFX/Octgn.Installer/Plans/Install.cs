using System;

namespace Octgn.Installer.Plans
{
    public class Install : Plan
    {
        public Install(bool isQuiet) : base(isQuiet) {
        }

        protected override void OnStart() {
            Stage = Stage.Terms;
            CanGoBack = false;
            CanGoForward = true;
        }

        protected override void OnNext() {
            switch (Stage) {
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
                case Stage.Loading:
                case Stage.ConfirmUninstall:
                case Stage.ChooseMaintenance:
                case Stage.FinishedUninstalling:
                default:
                    throw new InvalidOperationException($"Stage transition from {Stage} is not valid here.");
            }
        }

        protected override void OnBack() {
            
        }
    }
}
