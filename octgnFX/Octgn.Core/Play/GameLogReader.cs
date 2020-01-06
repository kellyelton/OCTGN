/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Timers;
namespace Octgn.Core.Play
{
    public sealed class GameLogReader : IDisposable
    {
        private readonly System.Timers.Timer _chatTimer;
        private readonly GameLog _gameLog;
        private Action<IReadOnlyList<IGameMessage>> onMessages;

        private readonly object NewMessageLock = new object();
        private List<IGameMessage> _newMessages = new List<IGameMessage>();

        public GameLogReader(GameLog gameLog) {
            _gameLog = gameLog ?? throw new ArgumentNullException(nameof(gameLog));

            _gameLog.LogAdded += _gameLog_LogAdded;

            _chatTimer = new System.Timers.Timer(100);
            _chatTimer.Elapsed += OnTick;
        }

        private void _gameLog_LogAdded(object sender, IGameMessage e) {
            lock (NewMessageLock) {
                _newMessages.Add(e);
            }
        }

        private bool _isStarted = false;

        public void Start(Action<IReadOnlyList<IGameMessage>> handler) {
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

        public void Reset() {
            throw new NotImplementedException();
        }

        private void OnTick(object sender, ElapsedEventArgs e) {
            lock (_chatTimer)
                _chatTimer.Enabled = false;

            try {
                IReadOnlyList<IGameMessage> newMessages;

                lock (NewMessageLock) {
                    newMessages = _newMessages;

                    _newMessages = new List<IGameMessage>();

                }
                if (newMessages.Count > 0) {
                    onMessages?.Invoke(newMessages);
                }
            } finally {
                _chatTimer.Enabled = true;
            }
        }

        public void Dispose() {
            this.Stop();

            _chatTimer.Dispose();

            _gameLog.LogAdded -= _gameLog_LogAdded;
        }
    }
}