using System;

namespace com.magicsoftware.unipaas
{
   /// <summary>
   /// functionality required by the GUI namespace from the FlowMonitorQueue class.
   /// </summary>
   public interface IFlowMonitorQueue
   {
      ///<param name="contextID"></param>
      /// <param name = "newTaskMode">new task mode for the task</param>
      void addTaskCngMode(Int64 contextID, char newTaskMode);
   }
}
