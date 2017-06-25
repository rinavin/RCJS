using System;

namespace com.magicsoftware.util.fsm
{
   public class StateTransitionTriggeredEventArgs : EventArgs
   {
      public StateTransitionTrigger TransitionTrigger { get; private set; }

      public StateTransitionTriggeredEventArgs(StateTransitionTrigger transitionTrigger)
      {
         TransitionTrigger = transitionTrigger;
      }
   }
}
