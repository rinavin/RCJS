using System;

namespace com.magicsoftware.richclient.rt
{
   /// <summary> this class represents one entry in the execution stack</summary>
   internal class ExecutionStackEntry
   {
      /// <summary> constructor for the execution stack entry - gets values for the 3 members</summary>
      /// <param name="intaskId"> </param>
      /// <param name="inhandlerId"> </param>
      /// <param name="inoperIdx"> </param>
      internal ExecutionStackEntry(String intaskId, String inhandlerId, int inoperIdx)
      {
         TaskId = intaskId;
         HandlerId = inhandlerId;
         OperIdx = inoperIdx;
      }

      internal String TaskId { get; private set; } // task id of the operation
      internal String HandlerId { get; private set; } // handler id of the operation
      internal int OperIdx { get; private set; } // idx of the operation

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj)
      {
         ExecutionStackEntry executionStackEntry = obj as ExecutionStackEntry;
         if (executionStackEntry == null)
            return false;

         return (String.Equals(executionStackEntry.HandlerId, HandlerId) &&
                 String.Equals(executionStackEntry.TaskId, TaskId) &&
                 executionStackEntry.OperIdx == OperIdx);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         return base.GetHashCode();
      }

   }
}