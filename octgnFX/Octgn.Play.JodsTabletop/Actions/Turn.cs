/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace Octgn.Play.Actions
{
    internal sealed class Turn : ActionBase
    {
        private readonly Card _card;
        private readonly bool _up;
        private readonly Player _who;

        public Turn(Player who, Card card, bool up)
        {
            _who = who ?? throw new ArgumentNullException(nameof(who));
            if (_who.GameEngine == null) throw new InvalidOperationException($"{nameof(who)}.{nameof(Player.GameEngine)} can't be null");

            _card = card;
            _up = up;
        }

        public override void Do()
        {
            base.Do();
            _card.SetFaceUp(_up);
            _who.GameEngine.GameLog.PlayerEvent(_who,"turns '{0}' face {1}", _card, _up ? "up" : "down");

            // Turning an aliased card face up will change its id,
            // which can create bugs if one tries to execute other actions using its current id.
            // That's why scripts have to be suspended until the card is revealed.
            //if (up && card.Type.alias && Script.ScriptEngine.CurrentScript != null)
            //   card.Type.revealSuspendedScript = Script.ScriptEngine.Suspend();
        }
    }
}