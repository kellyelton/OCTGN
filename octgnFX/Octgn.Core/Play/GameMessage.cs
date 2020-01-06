/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Windows.Media;

namespace Octgn.Core.Play
{
    public abstract class GameMessage : IGameMessage
    {
        public bool IsClientMuted { get; internal set; }

        public bool IsMuted => CanMute && IsClientMuted;

        public abstract bool CanMute { get; }

        public int Id { get; internal set; }
        public DateTime Timestamp { get; private set; }
        public IPlayPlayer From { get; private set; }
        public string Message { get; private set; }
        public object[] Arguments { get; private set; }

        private readonly bool isClientMuted = false;

        protected GameMessage(IPlayPlayer from, string message, params object[] args)
        {
            Timestamp = DateTime.Now;
            From = from;
            Message = message;
            Arguments = args ?? new object[0];
        }
    }

    public class PlayerEventMessage : GameMessage
    {
        public PlayerEventMessage(IPlayPlayer @from, string message, params object[] args)
            : base(@from, message, args)
        {
        }

        public override bool CanMute{get{return true;}}
    }

    public class ChatMessage : GameMessage
    {
        public override bool CanMute { get { return false; } }
        public ChatMessage(IPlayPlayer @from, string message, params object[] args)
            : base(@from, message, args)
        {
        }
    }

    public class WarningMessage : GameMessage
    {
        public override bool CanMute { get { return false; } }
        public WarningMessage(string message, params object[] args)
			:base(BuiltInPlayer.Warning,message, args)
        {

        }
    }

    public class SystemMessage : GameMessage
    {
        public override bool CanMute { get { return false; } }
        public SystemMessage(string message, params object[] args)
			:base(BuiltInPlayer.System,message, args)
        {

        }
    }

    public class NotifyMessage : GameMessage
    {
        public override bool CanMute { get { return false; } }
        public NotifyMessage(string message, params object[] args)
            : base(BuiltInPlayer.Notify, message, args)
        {

        }
    }

    public class TurnMessage : GameMessage
    {
        public override bool CanMute { get { return false;} }
        public int TurnNumber { get; private set; }
        public IPlayPlayer ActivePlayer { get; set; }
        public TurnMessage(IPlayPlayer turnPlayer, int turnNum)
			:base(BuiltInPlayer.Turn,"Turn {0}: ", new object[]{turnNum})
        {
            TurnNumber = turnNum;
            ActivePlayer = turnPlayer;
        }
    }

    public class PhaseMessage : GameMessage
    {
        public override bool CanMute { get { return false; } }
        public string Phase { get; private set; }
        public IPlayPlayer ActivePlayer { get; set; }
        public PhaseMessage(IPlayPlayer turnPlayer, string phase)
            : base(BuiltInPlayer.Turn, "{0}: ", new object[] { phase })
        {
            Phase = phase;
            ActivePlayer = turnPlayer;
        }
    }


    public class DebugMessage : GameMessage
    {
        public override bool CanMute { get { return false; } }
        public DebugMessage(string message, params object[] args)
			:base(BuiltInPlayer.Debug,message, args)
        {

        }
    }

    public class NotifyBarMessage : GameMessage
    {
        public override bool CanMute { get { return false; } }
        public Color MessageColor { get; private set; }
        public NotifyBarMessage(Color messageColor, string message, params object[] args)
			:base(BuiltInPlayer.NotifyBar,message, args)
        {
            MessageColor = messageColor;
        }
    }
}