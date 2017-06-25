using System.Diagnostics;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   class ClientRefreshCommand : ClientTargetedCommandBase
   {
      public static bool ClientIsInRollbackCommand { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="res"></param>
      public override void Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);
         Debug.Assert(task.IsSubForm);

         // Fixed Defect#:77149
         // if we in rollback command and we get clientRefresh command from server ,
         // first we need to rollback the local transaction and then we need to do refresh to the view .
         // the refresh to the view will be handled in rollback command 
         if (ClientIsInRollbackCommand)
            return;

         if (task.AfterFirstRecordPrefix && !task.InEndTask)
         {
            // QCR #284789. When the subform execute it's recompute, we do not needed to execute it's view refresh,
            // we need execute only it's subforms refresh. It will be done in recompute.
            if (((DataView)task.DataView).getCurrRec().InRecompute)
               task.ExecuteClientSubformRefresh = true;
            else
            {
               // Do not execute RP & RS of the parent, because it was done already.
               task.PerformParentRecordPrefix = false;
               ((Task)task).getParent().SubformRefresh(task, true);
               task.PerformParentRecordPrefix = true;
            }
         }
      }
   }
}
