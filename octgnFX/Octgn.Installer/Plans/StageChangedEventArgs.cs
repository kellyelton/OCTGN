using System;

namespace Octgn.Installer.Plans
{
    public class StageChangedEventArgs : EventArgs
    {
        public StageChangedEventArgs(Stage oldStage, Stage newStage) {
            OldStage = oldStage;
            NewStage = newStage;
        }

        public Stage OldStage { get; }

        public Stage NewStage { get; }
    }
}
