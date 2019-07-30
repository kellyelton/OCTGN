﻿using System;

namespace Octgn.Installer.Features
{
    public class AddOctgnToPath : Feature
    {
        public override bool IsRequired => false;

        public override bool IsVisible => true;

        public override string Name => "Add OCTGN to PATH";

        public override string Description => "This adds the OCTGN install directory to the %PATH% environmental variable.";
    }
}
