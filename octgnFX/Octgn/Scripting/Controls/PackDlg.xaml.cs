/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew.Entities;

namespace Octgn.Scripting.Controls
{
    public partial class PackDlg
    {
        public IEnumerable<Set> Sets { get; set; }

        [Obsolete("Only to be used in designer")]
        public PackDlg() {
            InitializeComponent();
        }

        private readonly GameEngine _gameEngine;

        public PackDlg(GameEngine gameEngine)
        {
            _gameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
            Sets = _gameEngine.Definition.Sets().Where(x => x.Packs.Count() > 0).OrderBy(x => x.Name).ToArray();
            InitializeComponent();
            Owner = WindowManager.PlayWindow;
            setsCombo.SelectionChanged += setsCombo_SelectionChanged;
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

       private void StartClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (packsCombo.SelectedItem != null)
                DialogResult = true;
        }

        public Pack GetPack()
        {
            ShowDialog();
            if (packsCombo.SelectedItem == null) return null;
            return (Pack)packsCombo.SelectedItem;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Close();
        }

        private void setsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            packsCombo.SelectedIndex = 0;
        }

    }
}