using System;
using System.Collections.Generic;
using Octgn.Installer.Steps;

namespace Octgn.Installer.Features
{
    public class DataDirectory : Feature
    {
        public override bool IsRequired => true;

        public override string Name => "OCTGN Data Directory";

        public override string Description => "The OCTGN Data Directory. This is where all the games, cards, images, decks and user settings are stored.";

        public override IEnumerable<Step> GetInstallSteps(Context context) {
            yield return new CreateDirectory(context.DataDirectory);
        }
    }
}
