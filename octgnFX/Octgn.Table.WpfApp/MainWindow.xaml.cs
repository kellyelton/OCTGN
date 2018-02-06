using System.Windows;

namespace Octgn.TableTop.WpfApp
{
    public partial class MainWindow : Window
    {
        public Game Game {
            get { return (Game)GetValue(GameProperty); }
            set { SetValue(GameProperty, value); }
        }

        public static readonly DependencyProperty GameProperty =
            DependencyProperty.Register(nameof(Game), typeof(Game), typeof(MainWindow));

        public MainWindow() {
            InitializeComponent();

            Game = new Game("GAME", "Test Game");
            Game.Add(new Player("Player:0", "Player A", true));
            Game.Add(new Player("Player:1", "Player B", true));

            Game.Add(new Table("MainTable", "My Table"));
        }
    }
}
