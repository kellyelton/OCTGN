/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using System.Windows;

namespace Octgn.Windows
{
    public partial class GameLog : Window
    {
        public GameEngine GameEngine {
            get { return (GameEngine)GetValue(GameEngineProperty); }
            set { SetValue(GameEngineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GameEngine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GameEngineProperty =
            DependencyProperty.Register(nameof(GameEngine), typeof(GameEngine), typeof(GameLog), new PropertyMetadata(null));

        private bool realClose = false;

        [Obsolete("Used only for design mode")]
        public GameLog() {
            InitializeComponent();
        }

        public GameLog(GameEngine gameEngine)
        {
            GameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));

            InitializeComponent();

            this.Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            if (!realClose)
            {
                this.Visibility = Visibility.Hidden;
                cancelEventArgs.Cancel = true;
            }
        }

        public void RealClose()
        {
            realClose = true;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ChatControl.Save();
        }
    }
}
