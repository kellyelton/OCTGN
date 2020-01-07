/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace Octgn.Play.Actions
{
    public class CreateCard : ActionBase
    {
        private readonly bool _deletesWhenLeavesGroup;
        private readonly bool _faceUp;
        private readonly int _id;
        private readonly DataNew.Entities.Card _model;
        private readonly Player _owner;
        private readonly int _x;
        private readonly int _y;
        internal Card Card;

        public CreateCard(Player owner, int id,bool faceUp, DataNew.Entities.Card model, int x, int y,
                          bool deletesWhenLeavesGroup)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _id = id;
            _faceUp = faceUp;
            _deletesWhenLeavesGroup = deletesWhenLeavesGroup;
            _model = model;
            _x = x;
            _y = y;
        }

        internal static event EventHandler Done;

        public override void Do()
        {
            base.Do();

            Card =
                new Card(_owner, _id, _model, _model.Size.Name)                    {X = _x, Y = _y, DeleteWhenLeavesGroup = _deletesWhenLeavesGroup};
            Card.SetFaceUp(_faceUp);
            _owner.GameEngine.Table.AddAt(Card, _owner.GameEngine.Table.Count);

            if (Done != null) Done(this, EventArgs.Empty);
        }
    }
}