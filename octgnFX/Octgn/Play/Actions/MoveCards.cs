using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using log4net;

namespace Octgn.Play.Actions
{
    internal class MoveCards : ActionBase
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal Card[] Cards;
        internal bool[] FaceUp;
        internal Group From;
        internal int[] Idx;
        internal Group To;
        internal Player Who;
        internal int[] X, Y;

        internal static event EventHandler Done;
        internal static event EventHandler Doing;

        public MoveCards(Player who, Card[] cards, Group to, int[] idx, bool[] faceUp, Caller caller) : base(caller)
        {
            Who = who;
            Cards = cards;
            To = to;
            From = cards[0].Group;
            Idx = idx;
            FaceUp = faceUp;
            X = new int[cards.Length];
            Y = new int[cards.Length];
        }

        public MoveCards(Player who, Card[] card, int[] x, int[] y, int[] idx, bool[] faceUp, Caller caller) : base(caller) {
            Who = who;
            Cards = card;
            To = Program.GameEngine.Table;
            From = card[0].Group;
            X = x;
            Y = y;
            Idx = idx;
            FaceUp = faceUp;
        }

        private IEnumerable<MoveCard> CreateMoveCardActions() {
            for(var i = 0; i < Cards.Length; i++) {
                var card = Cards[i];

                var oldGroup = card.Group;
                var oldIndex = card.GetIndex();
                var oldX = (int)card.X;
                var oldY = (int)card.Y;
                var oldHighlight = card.HighlightColorString;
                var oldMarkers = card.MarkersString;
                var oldFaceUp = card.FaceUp;
                var oldFilter = card.FilterColorString;
                var oldAlternate = card.Alternate();

                if ((oldGroup != To) || (oldX != X[i]) || (oldY != Y[i]) || (oldIndex != Idx[i])) {
                    yield return new MoveCard(Caller) {
                        Card = card,
                        Player = Who,
                        From = oldGroup,
                        To = To,
                        Index = Idx[i],
                        OldHighlight = oldHighlight,
                        OldIndex = oldIndex,
                        OldMarkers = oldMarkers,
                        OldFaceUp = oldFaceUp,
                        OldFilter = oldFilter,
                        OldAlternate = oldAlternate,
                        OldX = oldX,
                        OldY = oldY,
                        X = X[i],
                        Y = Y[i]
                    };
                }
            }
        }

        public override void Do()
        {
            if (Doing != null) Doing(this, EventArgs.Empty);

            var moveCardActions = CreateMoveCardActions().ToArray();

            if (Caller != Caller.Script) {
                var cards = new Card[moveCardActions.Length];
                var tos = new Group[moveCardActions.Length];
                var indexes = new int[moveCardActions.Length];
                var xs = new int[moveCardActions.Length];
                var ys = new int[moveCardActions.Length];

                for(var i = 0;i< moveCardActions.Length; i++) {
                    cards[i] = moveCardActions[i].Card;
                    tos[i] = moveCardActions[i].To;
                    indexes[i] = moveCardActions[i].Index;
                    xs[i] = moveCardActions[i].X;
                    ys[i] = moveCardActions[i].Y;
                }

                if(Program.GameEngine.EventProxy.OnCardsMoving_3_1_0_2(cards, tos, indexes, xs, ys, FaceUp)) {
                    // Script handled the event, bail out.
                    return;
                }
            }

            if (Caller != Caller.Network) {
                Program.Client.Rpc.MoveCardReq(Cards.Select(x => x.Id).ToArray(), To, Idx, FaceUp, Caller == Caller.Script);
            }

            base.Do();

            Debug.WriteLine("Moving " + Cards.Length + " cards from " + From + " to " + To);

            foreach(var card in moveCardActions) {
                card.Do();
            }

            if (moveCardActions.Length > 0)
            {
                var cards = new Card[moveCardActions.Length];
                var oldGroups = new Group[moveCardActions.Length];
                var tos = new Group[moveCardActions.Length];
                var oldIndexes = new int[moveCardActions.Length];
                var indexes = new int[moveCardActions.Length];
                var oldX = new int[moveCardActions.Length];
                var oldY = new int[moveCardActions.Length];
                var x = new int[moveCardActions.Length];
                var y = new int[moveCardActions.Length];
                var oldHighlights = new string[moveCardActions.Length];
                var oldMarkers = new string[moveCardActions.Length];
                var oldFaceUps = new bool[moveCardActions.Length];
                var oldFilters = new string[moveCardActions.Length];
                var oldAlternates = new string[moveCardActions.Length];

                Program.GameEngine.EventProxy.OnMoveCards_3_1_0_0(Who, cards, oldGroups, tos, oldIndexes, indexes, oldX, oldY, x, y, oldHighlights, oldMarkers, Caller == Caller.Script);
                if (Caller == Caller.Script)
                {
                    Program.GameEngine.EventProxy.OnScriptedMoveCards_3_1_0_1(Who, cards, oldGroups, tos, oldIndexes, indexes, oldX, oldY, x, y, oldHighlights, oldMarkers, oldFaceUps);
                    Program.GameEngine.EventProxy.OnScriptedCardsMoved_3_1_0_2(Who, cards, oldGroups, tos, oldIndexes, oldX, oldY, oldHighlights, oldMarkers, oldFaceUps, oldFilters, oldAlternates);
                }
                else
                {
                    Program.GameEngine.EventProxy.OnMoveCards_3_1_0_1(Who, cards, oldGroups, tos, oldIndexes, indexes, oldX, oldY, x, y, oldHighlights, oldMarkers, oldFaceUps);
                    Program.GameEngine.EventProxy.OnCardsMoved_3_1_0_2(Who, cards, oldGroups, tos, oldIndexes, oldX, oldY, oldHighlights, oldMarkers, oldFaceUps, oldFilters, oldAlternates);
                }
            }

            if (Done != null) Done(this, EventArgs.Empty);
        }
    }
}