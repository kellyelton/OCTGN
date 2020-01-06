/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Timers;
namespace Octgn.Core.Play
{
    public sealed class GameLogReader : IDisposable
    {
        private readonly Timer _chatTimer;
        private readonly GameLog _gameLog;
        private int _lastLogRead = -1;
        private Action<IGameMessage[]> onMessages;

        public GameLogReader(GameLog gameLog) {
            _gameLog = gameLog ?? throw new ArgumentNullException(nameof(gameLog));

            _chatTimer = new Timer(100);
            _chatTimer.Elapsed += OnTick;
        }

        private bool _isStarted = false;

        public void Start(Action<IGameMessage[]> handler) {
            if (_isStarted) throw new InvalidOperationException("Already started.");

            onMessages = handler;

            _chatTimer.Enabled = true;

            _isStarted = true;
        }

        public void Stop() {
            if (!_isStarted) throw new InvalidOperationException("Not started.");

            onMessages = null;

            _chatTimer.Enabled = false;

            _isStarted = false;
        }

        private bool _reset;

        public void Reset() {
            _reset = true;
        }

        private void OnTick(object sender, ElapsedEventArgs e) {
            lock (_chatTimer) {
                _chatTimer.Enabled = false;
            }

            try {
                if (_reset) {
                    _lastLogRead = -1;
                    _reset = false;
                }

                var newMessages = _gameLog.LogsSince(_lastLogRead).ToArray();

                if (newMessages.Length > 0) {
                    _lastLogRead = newMessages[newMessages.Length - 1].Id;

                    onMessages?.Invoke(newMessages);
                }
            } finally {
                _chatTimer.Enabled = true;
            }
        }

        public void Dispose() {
            this.Stop();

            _chatTimer.Dispose();
        }
    }
}