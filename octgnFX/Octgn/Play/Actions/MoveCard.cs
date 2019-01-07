using System;

namespace Octgn.Play.Actions
{
    public class MoveCard : ActionBase
    {
        public Player Player { get; set; }
        public Card Card { get; set; }
        public Group To { get; set; }
        public Group From { get; set; }
        public bool FaceUp { get; set; }
        public int OldIndex { get; set; }
        public int Index { get; set; }
        public int OldX { get; set; }
        public int OldY { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string OldHighlight { get; set; }
        public string OldMarkers { get; set; }
        public bool OldFaceUp { get; set; }
        public string OldFilter { get; set; }
        public string OldAlternate { get; set; }

        public MoveCard(Caller caller) : base(caller) {

        }

        public override void Do() {
            base.Do();

            bool shouldSee = Card.FaceUp, shouldLog = true;

            // Move the Card
            if (Card.Group == To) {
                shouldLog = false;
                if ((Card.X != X) || (Card.Y != Y)) Card.CardMoved = true;
                Card.X = X;
                Card.Y = Y;
                if (To.Cards.IndexOf(Card) != Index) {
                    if (To.Ordered)
                        Program.GameMess.PlayerEvent(Player, "reorders {0}", To);
                    Card.SetIndex(Index);
                }
            } else {
                Card.Group.Remove(Card);
                if (Card.DeleteWhenLeavesGroup) //nonpersisting Cards will be deleted
                    Card.Group = null;
                //TODO Card.Delete();
                else {
                    Card.CardMoved = true;
                    Card.SwitchTo(Player);
                    Card.SetFaceUp(FaceUp);//FaceUp may be false - it's one of the constructor parameters for this
                    Card.SetOverrideGroupVisibility(false);
                    Card.X = X;
                    Card.Y = Y;
                    To.AddAt(Card, Index);
                }
            }

            // Should the Card be named in the log ?
            shouldSee |= Card.FaceUp;
            // Prepare the message
            if (shouldLog)
                Program.GameMess.PlayerEvent(Player, "moves '{0}' to {2}{1}",
                                         shouldSee ? Card.Type : (object)"Card",
                                         To, To is Pile && Index > 0 && Index + 1 == To.Count ? "the bottom of " : "");

            Program.GameEngine.EventProxy.OnMoveCard_3_1_0_0(Player, Card, From, To, OldIndex, Index, OldX, OldY, X, Y, Caller == Caller.Script);

            if (Caller == Caller.Script)
            {
                Program.GameEngine.EventProxy.OnScriptedMoveCard_3_1_0_1(Player, Card, From, To, OldIndex, Index, OldX, OldY, X, Y, OldFaceUp, OldHighlight, OldMarkers);
            }
            else
            {
                Program.GameEngine.EventProxy.OnMoveCard_3_1_0_1(Player, Card, From, To, OldIndex, Index, OldX, OldY, X, Y, OldFaceUp, OldHighlight, OldMarkers);
            }

            if (Caller != Caller.Network) {
                Program.Client.Rpc.CardSwitchTo(Player.LocalPlayer, Card, Card.Alternate());
            }
        }
    }
}