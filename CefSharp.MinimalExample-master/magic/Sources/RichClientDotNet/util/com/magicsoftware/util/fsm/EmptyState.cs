
namespace com.magicsoftware.util.fsm
{
   public class EmptyState : State
   {
      public EmptyState(object stateId)
         : base(stateId)
      {

      }

      protected override void OnEnter()
      {
      }

      protected override void OnLeave()
      {
      }
   }
}