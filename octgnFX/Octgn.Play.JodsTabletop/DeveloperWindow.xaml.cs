/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Windows;

namespace Octgn.Play
{
    public partial class DeveloperWindow : Window
    {
        public GameEngine GameEngine {
            get { return (GameEngine)GetValue(GameEngineProperty); }
            set { SetValue(GameEngineProperty, value); }
        }

        public static readonly DependencyProperty GameEngineProperty =
            DependencyProperty.Register(nameof(GameEngine), typeof(GameEngine), typeof(DeveloperWindow), new PropertyMetadata(null));

        [Obsolete("Used only for designer")]
        public DeveloperWindow() {
            this.InitializeComponent();
        }

        public DeveloperWindow(GameEngine gameEngine) {
            GameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
        }

        private void ButtonReloadScriptsClick(object sender, RoutedEventArgs e) {
            GameEngine.ScriptEngine.ReloadScripts();
        }
    }
}
