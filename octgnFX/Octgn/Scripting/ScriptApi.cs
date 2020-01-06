/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Octgn.Networking;
using log4net;

namespace Octgn.Scripting
{
    public abstract class ScriptApi
    {
        protected readonly static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected GameEngine GameEngine { get; }
        protected Engine ScriptEngine => GameEngine.ScriptEngine;

        protected ScriptApi(GameEngine gameEngine) {
            GameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
        }

        /// <summary>
        /// Queues action to run on the script thread
        /// synchronusly. This action will be placed at the bottom of the
        /// current queue
        /// </summary>
        /// <param name="a">Action</param>
        protected void QueueAction(Action a)
        {
            GameEngine.ScriptEngine.Invoke(a);
        }

        /// <summary>
        /// Queues action to run on the script thread
        /// synchronusly. This action will be placed at the bottom of the
        /// current queue
        /// </summary>
        protected T QueueAction<T>(Func<T> a)
        {
            return GameEngine.ScriptEngine.Invoke<T>(a);
        }

        protected void Suspend()
        {
            GameEngine.ScriptEngine.Suspend();
        }

        protected void Resume()
        {
            GameEngine.ScriptEngine.Resume();
        }

        protected Mute CreateMute()
        {
            return new Mute(GameEngine.Client, GameEngine.ScriptEngine.CurrentJob.Muted);
        }

        public void RegisterEvent(string name, IronPython.Runtime.PythonFunction derp)
        {
            ScriptEngine.RegisterFunction(name, derp);
        }

        private SynchornusNetworkCall<int> _randRequest;
        public int Random(int min, int max)
        {
            _randRequest = new SynchornusNetworkCall<int>(ScriptEngine, () => {
                GameEngine.Client.Rpc.RandomReq(min, max);
            });
            return _randRequest.Get();
        }

        public void RandomResult(int result)
        {
            _randRequest.Continuation(result);
        }

        protected class SynchornusNetworkCall<T>
        {
            private readonly Engine _engine;
            private T _result;
            private bool _gotResult;
            private Action _call;
            public SynchornusNetworkCall( Engine engine, Action call )
            {
                _engine = engine;
                _call = call;
            }

            public T Get()
            {
                Task.Factory.StartNew(RunThread);
                _engine.Suspend();
                return _result;
            }

            private void RunThread()
            {
                try
                {
                    while (!_engine.GameEngine.IsDone)
                    {
                        try
                        {
                            lock (this)
                            {
                                if (_gotResult)
                                    return;
                            }
                            _call();
                            //Program.Client.Rpc.RandomReq(_min, _max);
                            Thread.Sleep(3000);
                            lock (this)
                            {
                                if (_gotResult)
                                    return;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                finally
                {
                    if (_gotResult == false)
                        Continuation(default(T));
                }
            }

            public virtual void Continuation(T result)
            {
                lock (this)
                {
                    if (_gotResult)
                        return;
                    _gotResult = true;
                }
                _result = result;
                _engine.Resume();
            }
        }
    }
}