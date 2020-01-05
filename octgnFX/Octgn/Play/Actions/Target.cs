using System;
using System.Diagnostics;

namespace Octgn.Play.Actions
{
    internal sealed class Target : ActionBase
    {
        internal bool DoTarget;
        internal Card FromCard, ToCard;
        internal Player Who;
        internal bool IsScriptChange;

        public Target(Player who, Card fromCard, Card toCard, bool doTarget, bool isScriptChange)
        {
            Who = who;
            FromCard = fromCard;
            ToCard = toCard;
            DoTarget = doTarget;
            IsScriptChange = isScriptChange;
        }

        internal static event EventHandler CreatingArrow;
        internal static event EventHandler DeletingArrows;

        public override void Do()
        {
            base.Do();
            if (DoTarget)
            {
                if (ToCard == null) SingleTarget();
                else ArrowTarget();
            }
            else
                ClearTarget();
        }

        private void SingleTarget()
        {
            FromCard.SetTargetedBy(Who);
            Who.GameEngine.GameLog.PlayerEvent(Who, "targets '{0}'", FromCard);
            Who.GameEngine.EventProxy.OnTargetCard_3_1_0_0(Who, FromCard, true);
            Who.GameEngine.EventProxy.OnTargetCard_3_1_0_1(Who, FromCard, true);
            Who.GameEngine.EventProxy.OnCardTargeted_3_1_0_2(Who, FromCard, true, IsScriptChange);
        }

        private void ArrowTarget()
        {
            if (CreatingArrow != null) CreatingArrow(this, EventArgs.Empty);
            Who.GameEngine.GameLog.PlayerEvent(Who, "targets '{1}' with '{0}'", FromCard, ToCard);
            Who.GameEngine.EventProxy.OnTargetCardArrow_3_1_0_0(Who, FromCard, ToCard, true);
            Who.GameEngine.EventProxy.OnTargetCardArrow_3_1_0_1(Who, FromCard, ToCard, true);
            Who.GameEngine.EventProxy.OnCardArrowTargeted_3_1_0_2(Who, FromCard, ToCard, true, IsScriptChange);
        }

        private void ClearTarget()
        {
            if (FromCard.TargetsOtherCards && DeletingArrows != null)
            {
                DeletingArrows(this, EventArgs.Empty);
                Who.GameEngine.EventProxy.OnTargetCardArrow_3_1_0_0(Who, FromCard, ToCard, false);
                Who.GameEngine.EventProxy.OnTargetCardArrow_3_1_0_1(Who, FromCard, ToCard, false);
                Who.GameEngine.EventProxy.OnCardArrowTargeted_3_1_0_2(Who, FromCard, ToCard, false, IsScriptChange);
            }

            if (FromCard.TargetedBy != null)
            {
                FromCard.SetTargetedBy(null);
                Who.GameEngine.EventProxy.OnTargetCard_3_1_0_0(Who, FromCard, false);
                Who.GameEngine.EventProxy.OnTargetCard_3_1_0_1(Who, FromCard, false);
                Who.GameEngine.EventProxy.OnCardTargeted_3_1_0_2(Who, FromCard, false, IsScriptChange);
            }
        }
    }
}