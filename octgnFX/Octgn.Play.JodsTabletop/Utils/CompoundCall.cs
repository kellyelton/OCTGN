/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading;
using Octgn.Library;
using Octgn.Networking;

namespace Octgn.Utils
{
    public class CompoundCall
    {
        private Action currentCall;
        private bool running;
        private DateTime endTime;
        private int curMuted = 0;

        private readonly GameEngine _gameEngine;

        public CompoundCall(GameEngine gameEngine) {
            _gameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
        }

        public void Call(Action call)
        {
            curMuted = _gameEngine.Client.Muted;

            lock (this)
            {
                currentCall = call;
                endTime = DateTime.Now.AddSeconds(1);
                if (!running)
                {
                    running = true;
					var t = new Thread(this.RunLoop);
                    t.Name = "CompoundCall RunLoop";
                    t.Start();
                }
            }
        }

        private void RunLoop()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(5);
                    lock (this)
                    {
                        if (DateTime.Now >= endTime)
                        {
							// Do the shit
                            using (new Mute(_gameEngine.Client, curMuted))
                                X.Instance.Try(currentCall);
                            return;
                        }
                    }
                }

            }
            finally
            {
                lock (this)
                {
                    running = false;
                    currentCall = null;
                    endTime = DateTime.MaxValue;
                }
            }
        }
    }
}