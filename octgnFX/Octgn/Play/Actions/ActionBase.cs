using System;

namespace Octgn.Play.Actions
{
    public class ActionBase
    {
        public Caller Caller { get; }

        public ActionBase(Caller caller) {
            Caller = caller;
        }

        public virtual void Do()
        {
            //History.Record(this);
        }
    }

    public class Caller
    {
        public Player Player { get; }
        public CallSource Source { get; }
        public bool IsScriptInvoked { get; }

        public Caller(Player player, CallSource source, bool isScriptInvoked) {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Source = source;
            IsScriptInvoked = isScriptInvoked;
        }
    }

    public enum CallSource
    {
        Local,
        Network
    }
}