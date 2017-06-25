using System;
using System.Collections.Generic;

namespace com.magicsoftware.util.fsm
{
   public class StateTransition
   {
      public event EventHandler<StateTransitionTriggeredEventArgs> TransitionTriggered;

      List<StateTransitionTrigger> transitionTriggers;

      public StateTransition(object nextStateId)
      {
         NextStateId = nextStateId;
         transitionTriggers = new List<StateTransitionTrigger>();
      }

      public IEnumerable<StateTransitionTrigger> TransitionTriggers
      {
         get
         {
            return transitionTriggers;
         }
      }

      public object NextStateId { get; private set; }

      public void Setup()
      {
         SetupTriggers();
         OnSetup();
      }

      public void Cleanup()
      {
         CleanupTriggers();
         OnCleanup();
      }

      protected void SetupTriggers()
      {
         ForEachTrigger(new Action<StateTransitionTrigger>(trigger => trigger.Setup()));
      }

      protected void CleanupTriggers()
      {
         ForEachTrigger(new Action<StateTransitionTrigger>(trigger => trigger.Cleanup()));
      }

      protected void DetachFromTriggers()
      {
         ForEachTrigger(new Action<StateTransitionTrigger>(trigger => { trigger.Triggered -= trigger_Triggered; }));
      }

      protected virtual void OnSetup()
      {}

      protected virtual void OnCleanup()
      {}

      protected virtual void OnTriggered(StateTransitionTrigger trigger)
      {
         if (TransitionTriggered != null)
            TransitionTriggered(this, new StateTransitionTriggeredEventArgs(trigger));
      }

      public void AddTrigger(StateTransitionTrigger trigger)
      {
         transitionTriggers.Add(trigger);
         trigger.Triggered += trigger_Triggered;
      }

      public void AddTriggers(IEnumerable<StateTransitionTrigger> triggers)
      {
         transitionTriggers.AddRange(triggers);
         foreach (var trigger in triggers)
         {
            trigger.Triggered += trigger_Triggered;
         }
      }

      void trigger_Triggered(object sender, EventArgs e)
      {
         OnTriggered((StateTransitionTrigger)sender);
      }

      void ForEachTrigger(Action<StateTransitionTrigger> action)
      {
         foreach (var trigger in TransitionTriggers)
         {
            action(trigger);
         }
      }

      public override string ToString()
      {
         return String.Format("{{{0}: to {1}}}", GetType().Name, NextStateId);
      }
   }
}
