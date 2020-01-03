/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using System.Globalization;
using Octgn.Utils;

namespace Octgn.Play
{
    public sealed class Counter : INotifyPropertyChanged
    {
        #region Private fields

        private readonly byte _id;
        private int _state; // Value of this counter
        private readonly GameEngine _gameEngine;
        private readonly CompoundCall _setCounterNetworkCompoundCall;

        #endregion
        #region Public interface

        public Counter(GameEngine gameEngine, Player player, DataNew.Entities.Counter def, bool isReplay)
        {
            _gameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
            _setCounterNetworkCompoundCall = new CompoundCall(_gameEngine);

            Owner = player;
            _state = def.Start;
            Name = def.Name;
            _id = def.Id;
            Definition = def;

            if (!isReplay) {
                Player localPlayer = null;
                if (player.IsLocal) {
                    localPlayer = player;
                } else {
                    localPlayer = Player.LocalPlayer;
                }

                localPlayer.PropertyChanged += Player_PropertyChanged;

                _canChange = !localPlayer.Spectator;
            }
        }

        private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(Player.Spectator)) {
                var player = (Player)sender;

                CanChange = !player.Spectator;
            }
        }

        public Player Owner { get; }

        public string Name { get; }

        public bool CanChange {
            get => _canChange;
            set {
                if(_canChange != value) {
                    _canChange = value;
                }
            }
        }
        private bool _canChange;

        // Get or set the counter's value
        public int Value
        {
            get { return _state; }
            set { SetValue(value, Player.LocalPlayer, true, false); }
        }

        public DataNew.Entities.Counter Definition { get; }

        public static Counter Find(GameEngine gameEngine, int id)
        {
            Player p = Player.Find(gameEngine, (byte) (id >> 16));
            if (p == null || (byte) id > p.Counters.Length || (byte) id == 0)
                return null;
            return p.Counters[(byte) id - 1];
        }

        // C'tor

        public override string ToString()
        {
            return (Owner != null ? Owner.Name + "'s " : "Global ") + Name;
        }

        #endregion

        #region Implementation



        // Get the id of this counter
        internal int Id
        {
            get { return 0x02000000 | (Owner == null ? 0 : Owner.Id << 16) | _id; }
        }

        // Set the counter's value
        internal void SetValue(int value, Player who, bool notifyServer, bool isScriptChange)
        {
            var oldValue = _state;
            // Check the difference with current value
            int delta = value - _state;
            if (delta == 0) return;
            // Notify the server if needed
            if (notifyServer)
            {
                _setCounterNetworkCompoundCall.Call(() => _gameEngine.Client.Rpc.CounterReq(this, value, isScriptChange));
            }
            // Set the new value
            _state = value;
            OnPropertyChanged("Value");
            // Display a notification in the chat
            string deltaString = (delta > 0 ? "+" : "") + delta.ToString(CultureInfo.InvariantCulture);
            Program.GameMess.PlayerEvent(who, "sets {0} counter to {1} ({2})", this, value, deltaString);
            if (notifyServer || who != Player.LocalPlayer)
            {
                _gameEngine.EventProxy.OnChangeCounter_3_1_0_0(who, this, oldValue);
                _gameEngine.EventProxy.OnChangeCounter_3_1_0_1(who, this, oldValue);
            }
            if (notifyServer)
            {
                _gameEngine.EventProxy.OnCounterChanged_3_1_0_2(who, this, oldValue, isScriptChange);
            }
        }

        internal void Reset()
        {
            if (!Definition.Reset) return;
            _state = Definition.Start;
            OnPropertyChanged("Value");
        }

        #endregion

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