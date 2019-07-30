using System;

namespace Octgn.Installer.Features
{
    public class Fonts : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "Fonts";

        public override string Description => "Fonts used by OCTGN.";
    }
}
