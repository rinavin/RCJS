using System;
using System.Collections.Generic;

namespace com.magicsoftware.util.fsm
{
   public abstract class State
   {
      private List<StateTransition> transitions;

      public IEnumerable<StateTransition> Transitions { get { return transitions; } }
      public StateTransition DefaultTransition { get; set; }

      protected State(object stateId)
      {
         Id = stateId;
         transitions = new List<StateTransition>();
      }

      public void AddTransition(StateTransition transition)
      {
         transitions.Add(transition);
      }

      public void AddTransitions(IEnumerable<StateTransition> addedTransitions)
      {
         transitions.AddRange(addedTransitions);
      }

      protected abstract void OnEnter();
      protected abstract void OnLeave();

      public void Enter()
      {
         foreach (var transition in Transitions)
         {
            transition.Setup();
         }
         OnEnter();
      }

      public void Leave()
      {
         OnLeave();
         foreach (var transition in Transitions)
         {
            transition.Cleanup();
         }
      }

      public override string ToString()
      {
         return "{State: " + Id.ToString() + "}";
      }

      public object Id { get; private set; }
   }
}
