using System;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public abstract class Step
    {
        public abstract Task Execute();
    }
}
