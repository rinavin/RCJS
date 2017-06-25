using System;

namespace com.magicsoftware.unipaas.management.tasks
{
   public interface IMGDataTable
   {
      /// <summary>searches all the MGData objects till it finds the task with the given id</summary>
      /// <param name = "id">the id of the requested task</param>
      ITask GetTaskByID(String id);

      /// <summary>returns main program for given ctl index</summary>
      /// <param name="contextID">active/target context</param>
      /// <param name="ctlIdx">ctl index</param>
      /// <returns></returns>
      ITask GetMainProgByCtlIdx(Int64 contextID, int ctlIdx);
   }
}
