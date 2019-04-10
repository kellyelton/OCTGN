﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Installer.Features
{
    public class Logging : Feature
    {
        public override bool IsRequired => true;

        public override bool IsVisible => false;

        public override string Name => "Logging";

        public override string Description => "Logging support in OCTGN.";

        public override IEnumerable<Feature> Children { get; } = new Feature[] {

        };

        public override Task Install(Context context) {
            throw new NotImplementedException();
        }

        public override Task Uninstall(Context context) {
            throw new NotImplementedException();
        }
    }
}
