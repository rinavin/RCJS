using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using com.magicsoftware.util.fsm;

namespace Tests.UniUtils
{
   static class StateArtifactsFactories
   {
      internal static State CreateState(object stateId)
      {
         State target = new TestState(stateId);
         return target;
      }

      internal static void CreateStatePrivateObject(object stateId, out State target, out PrivateObject privateObj)
      {
         target = CreateState(stateId);
         privateObj = new PrivateObject(target);
      }

      internal static StateTransition CreateTransition(object nextStateId)
      {
         TestTransition tt = new TestTransition(nextStateId);
         return tt;
      }

      internal static void CreateTransitionPrivateObject(object nextStateId, out StateTransition transition, out PrivateObject privateObj)
      {
         transition = CreateTransition(nextStateId);
         privateObj = new PrivateObject(transition);
      }

      internal static List<StateTransition> CreateTransitionList(params string[] nextStateIds)
      {
         var list = new List<StateTransition>();
         foreach (var id in nextStateIds)
         {
            list.Add(CreateTransition(id));
         }
         return list;
      }

      internal static StateTransitionTrigger CreateTrigger()
      {
         return new TestTrigger();
      }

      internal static void CreateStateTransitionTriggerPrivateObject(out StateTransitionTrigger trigger, out PrivateObject privateObject)
      {
         trigger = CreateTrigger();
         privateObject = new PrivateObject(trigger);
      }

      internal static List<StateTransitionTrigger> CreateTriggerList(int count)
      {
         var list = new List<StateTransitionTrigger>();
         while (count > 0)
         {
            list.Add(CreateTrigger());
            count--;
         }

         return list;
      }
   }

   class TestState : State
   {
      public bool OnEnterInvoked { get; private set; }
      public bool OnLeaveInvoked { get; private set; }

      public TestState(object stateId)
         : base(stateId)
      {
         OnEnterInvoked = false;
         OnLeaveInvoked = false;
      }

      protected override void OnEnter()
      {
         OnEnterInvoked = true;
      }

      protected override void OnLeave()
      {
         OnLeaveInvoked = true;
      }
   }

   class TestTransition : StateTransition
   {
      public bool SetupInvoked { get; private set; }
      public bool CleanupInvoked { get; private set; }

      public TestTransition(object nextStateId)
         : base(nextStateId)
      {
         SetupInvoked = false;
         CleanupInvoked = false;
      }

      protected override void OnSetup()
      {
         SetupInvoked = true;
      }

      protected override void OnCleanup()
      {
         CleanupInvoked = true;
      }
   }

   class TestTrigger : StateTransitionTrigger
   {
      public bool SetupInvoked { get; private set; }
      public bool CleanupInvoked { get; private set; }

      bool isActive;

      public TestTrigger()
      {
         ResetInternalState();
      }

      public void ResetInternalState()
      {
         isActive = false;
         SetupInvoked = false;
         CleanupInvoked = false;
      }

      public override void Setup()
      {
         SetupInvoked = true;
         isActive = true;
      }

      public override void Cleanup()
      {
         CleanupInvoked = true;
         isActive = false;
      }

      public void TriggerNow()
      {
         if (!isActive)
            Assert.Fail("Trying to trigger an inactive trigger. Make sure to setup the state, transition or trigger.");
         OnTriggered();
      }
   }


}
