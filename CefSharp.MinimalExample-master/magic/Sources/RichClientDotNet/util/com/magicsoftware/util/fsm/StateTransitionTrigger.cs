using System;

namespace com.magicsoftware.util.fsm
{
   public abstract class StateTransitionTrigger
   {
      public event EventHandler Triggered;

      /// <summary>
      /// Gets or sets a value that determines how the state machine should react
      /// to raising the trigger:
      /// If true, the state machine should immediately call MoveToNextState and, if false, the
      /// state machine should wait until MoveToNextState is invoked.
      /// </summary>
      public bool ForceImmediateTransition { get; set; }

      public StateTransitionTrigger()
      {
         ForceImmediateTransition = false;
      }

      public abstract void Setup();
      public abstract void Cleanup();

      public override string ToString()
      {
         return String.Format("{{{0}}}", GetType().Name);
      }

      protected virtual void OnTriggered()
      {
         if (Triggered != null)
            Triggered(this, new EventArgs());
      }


   }
}
