/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;

namespace Octgn.Play
{
    public class Marker : INotifyPropertyChanged
    {
        internal static readonly DefaultMarkerModel[] DefaultMarkers = new[]
                                                                           {
                                                                               new DefaultMarkerModel("white",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 1)),
                                                                               new DefaultMarkerModel("blue",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 2)),
                                                                               new DefaultMarkerModel("black",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 3)),
                                                                               new DefaultMarkerModel("red",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 4)),
                                                                               new DefaultMarkerModel("green",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 5)),
                                                                               new DefaultMarkerModel("orange",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 6)),
                                                                               new DefaultMarkerModel("brown",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 7)),
                                                                               new DefaultMarkerModel("yellow",
                                                                                                      new Guid(0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 0, 0,
                                                                                                               0, 8))
                                                                           };
        private ushort _count = 1;

        internal GameEngine GameEngine => Card.GameEngine;

        public Marker(Card card, DataNew.Entities.Marker model)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Model = model;
        }

        public Marker(Card card, DataNew.Entities.Marker model, ushort count)
            : this(card, model)
        {
            _count = count;
        }

        public DataNew.Entities.Marker Model { get; }

        public ushort Count
        {
            get { return _count; }
            set
            {
                int count = _count;

                var val = value;

                if (val < count)
                    GameEngine.Client.Rpc.RemoveMarkerReq(Card, Model.Id, Model.Name, (ushort)(count - val), (ushort)count,false);
                else if (val > count)
                    GameEngine.Client.Rpc.AddMarkerReq(Card, Model.Id, Model.Name, (ushort)(val - count), (ushort)count,false);

                if (value == _count) return;

                SetCount(value);
            }
        }

        public string Description
        {
            get { return Model.Name + " (x" + Count + ")"; }
        }

        public Card Card { get; }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public override string ToString()
        {
            return Model.Name;
        }



        internal void SetCount(ushort value)
        {
            if (value == 0)
                Card.RemoveMarker(this);
            else if (value != _count)
            {
                _count = value;
                OnPropertyChanged("Count");
                OnPropertyChanged("Description");
            }
        }

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}