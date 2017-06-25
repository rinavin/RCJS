using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace com.magicsoftware.util.fsm
{
   class IntegrityChecker
   {
      Dictionary<State, bool> isVisited = new Dictionary<State, bool>();
      StateMachine m;
      List<IntegrityError> errors;
      Dictionary<object, State> stateMap;

      public IntegrityChecker(StateMachine m)
      {
         this.m = m;
      }

      public void Verify(object startStateId)
      {
         errors = new List<IntegrityError>();
         stateMap = new Dictionary<object, State>();
         foreach (State s in m.States)
         {
            stateMap.Add(s.Id, s);
         }

         foreach (var state in m.States)
         {
            isVisited.Add(state, false);
         }
         Visit(stateMap[startStateId]);

         foreach (KeyValuePair<State, bool> visitState in isVisited)
         { 
            if (!visitState.Value)
               errors.Add(new IntegrityError() 
               { 
                  Description = String.Format("Could not visit node {1} when starting from state {0}", startStateId, visitState.Key) 
               });
         }

         if (errors.Count > 0)
            throw new StateMachineItegrityException(errors);
      }

      private string ToString(IEnumerable<State> iEnumerable)
      {
         StringBuilder sb = new StringBuilder();
         foreach (var s in iEnumerable)
         {
            sb.Append(s).Append(",");
         }
         return sb.ToString();
      }

      void Visit(State s)
      {
         if (!isVisited[s])
         {
            isVisited[s] = true;
            if ((s.DefaultTransition != null) && ValidateDefaultTransition(s))
               Visit(stateMap[s.DefaultTransition.NextStateId]);
            foreach (var transition in s.Transitions)
            {
               if (ValidateTransition(s, transition))
                  Visit(stateMap[transition.NextStateId]);
            }
         }
      }

      private bool ValidateDefaultTransition(State s)
      {
         return VerifyNextStateExists(s, s.DefaultTransition);
      }

      bool ValidateTransition(State s, StateTransition transition)
      {
         if (!VerifyNextStateExists(s, transition))
            return false;

         if (Count(transition.TransitionTriggers) == 0)
         {
            errors.Add(new IntegrityError() { Description = String.Format("Transition {0} does not have triggers and it is not the default transition.", transition) });
            return false;
         }

         return true;
      }

      bool VerifyNextStateExists(State s, StateTransition transition)
      {
         if (!stateMap.ContainsKey(transition.NextStateId))
         {
            errors.Add(new IntegrityError() { Description = String.Format("Transition target state key {1} does not exist, in transition from state {0}", s, transition.NextStateId) });
            return false;
         }

         return true;
      }

      int Count(IEnumerable collection)
      {
         if (collection is ICollection)
         {
            return ((ICollection)collection).Count;
         }

         int count = 0;
         foreach (object o in collection)
         {
            count++;
         }
         return count;
      }
   }

   public class IntegrityError
   {
      public string Description { get; set; }
   }

   public class StateMachineItegrityException : Exception
   {
      public List<IntegrityError> Errors { get; private set; }

      public StateMachineItegrityException(string error)
         : base(error)
      {
      }

      public StateMachineItegrityException(List<IntegrityError> errors)
         : base("State machine integrity is flawed.")
      {
         this.Errors = errors;
      }
   }
}

