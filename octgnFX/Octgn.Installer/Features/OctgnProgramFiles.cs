using System;

namespace Octgn.Installer.Features
{
    public class OctgnProgramFiles : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Files";

        public override string Description => "OCTGN Program Files required to run OCTGN.";
    }
}
