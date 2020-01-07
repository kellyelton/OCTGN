/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel;
using Octgn.DataNew.Entities;
using System.Linq;

namespace Octgn.Play
{
    public sealed class Phase : INotifyPropertyChanged
    {

        internal static Phase Find(GameEngine gameEngine, byte id)
        {
            return gameEngine.AllPhases.FirstOrDefault(p => p.Id == id);
        }

        private readonly GamePhase _model;
        private readonly byte _id;
        private bool _hold;
        private bool _isActive;

        internal Phase(byte id, GamePhase model)
        {
            _id = id;
            _model = model;
            _hold = false;
        }


        internal byte Id
        {
            get { return _id; }
        }

        public bool Hold
        {
            get { return _hold; }
            set {
                if (_hold == value) return;
                _hold = value;
                OnPropertyChanged("Hold");
            }
        }

        public string Name
        {
            get { return _model.Name; }
        }

        public string Icon
        {
            get { return _model.Icon; }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}