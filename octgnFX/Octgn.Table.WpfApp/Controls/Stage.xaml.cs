using System.Windows;
using System.Windows.Controls;

namespace Octgn.TableTop.WpfApp.Controls
{
    public partial class Stage : UserControl
    {
        public Game Game {
            get { return (Game)GetValue(GameProperty); }
            set { SetValue(GameProperty, value); }
        }

        public static readonly DependencyProperty GameProperty =
            DependencyProperty.Register(nameof(Game), typeof(Game), typeof(Stage));


        public Stage() {
            InitializeComponent();
        }
    }
}
