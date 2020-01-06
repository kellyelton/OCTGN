/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows.Media;

namespace Octgn.Core.Play
{
    public class GameLog : INotifyCollectionChanged, IList<IGameMessage>
    {
        public bool IsReadOnly => false;
        public int Count => _messages.Count;

        public IGameMessage this[int index] {
            get {
                lock (_messages) {
                    return _messages[index];
                }
            }
            set => throw new NotSupportedException();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args) {
            CollectionChanged?.BeginInvoke(this, args, null, null);
        }

        public event EventHandler<IGameMessage> LogAdded;

        protected void OnLogAdded(IGameMessage log) {
            LogAdded?.Invoke(this, log);
        }

        private readonly List<IGameMessage> _messages = new List<IGameMessage>();
        private readonly Func<bool> _isMuted;

        public GameLog(Func<bool> isMuted) {
            _isMuted = isMuted ?? throw new ArgumentNullException(nameof(isMuted));
        }

        public IEnumerator<IGameMessage> GetEnumerator() {
            List<IGameMessage> copy;
            lock (_messages) {
                copy = _messages.ToList();
            }

            return copy.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            IEnumerable copy;
            lock (_messages) {
                copy = _messages.ToArray();
            }

            return copy.GetEnumerator();
        }

        public int IndexOf(IGameMessage item) {
            lock (_messages) {
                return _messages.IndexOf(item);
            }
        }

        public IEnumerable<IGameMessage> LogsSince(int logId) {
            lock (_messages) {
                if (logId >= _messages.Count || logId < -1) throw new ArgumentOutOfRangeException(nameof(logId));

                // Return all logs
                if (logId == -1) {
                    return _messages.ToArray();
                }

                // Return logs in range
                var logIndex = logId + 1;

                if (logIndex >= _messages.Count) return Enumerable.Empty<IGameMessage>();

                var count = _messages.Count - logIndex;

                var result = new IGameMessage[count];

                for(var i = 0; i < count; i++, logIndex++) {
                    result[i] = _messages[logIndex];
                }

                return result;
            }
        }

        private int _previousLogId = -1;

        public void Add(IGameMessage item) {
            lock (_messages) {
                if (!(item is GameMessage gameMessage)) throw new NotSupportedException($"{nameof(item)} must be a {nameof(GameMessage)}");

                gameMessage.Id = Interlocked.Increment(ref _previousLogId);

                gameMessage.IsClientMuted = _isMuted();

                _messages.Add(item);

                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item);

                OnCollectionChanged(args);

                OnLogAdded(item);
            }
        }

        public bool Contains(IGameMessage item) {
            lock (_messages) {
                return _messages.Contains(item);
            }
        }

        public void CopyTo(IGameMessage[] array, int arrayIndex) {
            lock (_messages) {
                _messages.CopyTo(array, arrayIndex);
            }
        }

        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, IGameMessage item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
        public bool Remove(IGameMessage item) => throw new NotSupportedException();

        public void PlayerEvent(IPlayPlayer player, string message, params object[] args) {
            Add(new PlayerEventMessage(player, message, args));
        }

        public void Chat(IPlayPlayer player, string message) {
            Add(new ChatMessage(player, message));
        }

        public void Warning(string message, params object[] args) {
            Add(new WarningMessage(message, args));
        }

        public void System(string message, params object[] args) {
            Add(new SystemMessage(message, args));
        }

        public void Turn(IPlayPlayer turnPlayer, int turnNumber) {
            Add(new TurnMessage(turnPlayer, turnNumber));
        }


        public void Phase(IPlayPlayer turnPlayer, string phase) {
            Add(new PhaseMessage(turnPlayer, phase));
        }

        public void GameDebug(string message, params object[] args) {
            Add(new DebugMessage(message, args));
        }

        public void Notify(string message, params object[] args) {
            Add(new NotifyMessage(message, args));
        }

        public void NotifyBar(Color color, string message, params object[] args) {
            Add(new NotifyBarMessage(color, message, args));
        }
    }
}