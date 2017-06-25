using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace com.magicsoftware.util.fsm
{
   public class StateMachine
   {
      //ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

      public event EventHandler NextStateChanged;
      public event EventHandler CurrentStateChanged;

      private object currentStateId;
      private object startStateId;
      private object nextStateId;
      private StateList states;
      AutoResetFlag isInStateTransition = new AutoResetFlag();

      public StateMachine(object startupStateId)
      {
         states = new StateList(this);
         this.startStateId = startupStateId;
      }

      public IEnumerable<State> States { get { return states; } }

      public State CurrentState
      {
         get
         {
            if (currentStateId == null)
               return null;
            return states[currentStateId];
         }
      }

      public void Reset()
      {
         if (CurrentState != null)
            CurrentState.Leave();
         currentStateId = null;
         nextStateId = null;
         SetNextState(startStateId);
      }

      public void MoveToNextState()
      {
         if (isInStateTransition.IsSet)
         {
            //log.WarnFormat("Already in state transition. Blocking action");
            Debug.Assert(false, "MoveToNextState is not reentrant!!!");
            return;
         }

         //log.InfoFormat("Moving from {0} to {1}", currentStateId, nextStateId);
         using (isInStateTransition.Set())
         {

            if (nextStateId == null && CurrentState.DefaultTransition != null)
               nextStateId = CurrentState.DefaultTransition.NextStateId;

            while (nextStateId != null)
            {
               if (CurrentState != null)
                  CurrentState.Leave();
               currentStateId = nextStateId;
               nextStateId = null;
               if (CurrentState != null)
                  CurrentState.Enter();
               if (CurrentStateChanged != null)
                  CurrentStateChanged(this, new EventArgs());
            }
         }
      }

      public void SetNextState(object nextStateId)
      {
         if (!object.Equals(this.nextStateId, nextStateId))
         {
            if (!states.ContainsState(nextStateId))
               throw new ArgumentException("The next state identifier " + nextStateId + " does not exist in the map.", "nextStateId");

            //log.DebugFormat("Setting next state id: {0}", nextStateId);
            this.nextStateId = nextStateId;
            if (!isInStateTransition && NextStateChanged != null)
               NextStateChanged(this, new EventArgs());
         }
      }

      public void AddState(State state)
      {
         states.Add(state);
      }

      public void AddStates(IEnumerable<State> addedStates)
      {
         states.AddRange(addedStates);
      }

      void RegisterTransition(StateTransition transition)
      {
         transition.TransitionTriggered += transition_TransitionTriggered;
      }

      void UnregisterTransitions()
      {
         foreach (var s in States)
         {
            foreach (var t in s.Transitions)
            {
               t.TransitionTriggered -= transition_TransitionTriggered;
            }
         }
      }

      void transition_TransitionTriggered(object sender, StateTransitionTriggeredEventArgs eventArgs)
      {
         SetNextState(((StateTransition)sender).NextStateId);
         if (eventArgs.TransitionTrigger.ForceImmediateTransition)
            MoveToNextState();
      }

      public static void VerifyIntegrity(StateMachine m)
      {
         var checker = new IntegrityChecker(m);
         checker.Verify(m.startStateId);
      }

      class StateList : IEnumerable<State>
      {
         private Dictionary<object, State> stateMap;

         StateMachine owner;

         public StateList(StateMachine owner)
         {
            this.owner = owner;
            stateMap = new Dictionary<object, State>();
         }

         public void Add(State item)
         {
            stateMap.Add(item.Id, item);
            foreach (var transition in item.Transitions)
            {
               owner.RegisterTransition(transition);
            }
         }

         public void AddRange(IEnumerable<State> items)
         {
            foreach (var item in items)
            {
               Add(item);
            }
         }

         public int Count { get { return stateMap.Count; } }

         public bool ContainsState(object stateId)
         {
            return stateMap.ContainsKey(stateId);
         }

         public State this[object stateId]
         {
            get { return stateMap[stateId]; }
         }

         public IEnumerator<State> GetEnumerator()
         {
            return stateMap.Values.GetEnumerator();
         }

         IEnumerator IEnumerable.GetEnumerator()
         {
            return ((IEnumerable)stateMap.Values).GetEnumerator();
         }
      }


   }

}
