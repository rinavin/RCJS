using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util.fsm;

namespace com.magicsoftware.util.fsm
{
   public class ManualTransitionTrigger : StateTransitionTrigger
   {

      public override void Setup()
      {

      }

      public override void Cleanup()
      {
         
      }

      public void TriggerNow()
      {
         OnTriggered();
      }
   }
}
