using Octgn.Wpf.Windows;

namespace Octgn.Controls
{
    public class DecorableWindow : DecorableWindowBase
    {
        protected override void LaunchDiagnostics() {
            Diagnostics.Instance.Show();
        }
    }
}
