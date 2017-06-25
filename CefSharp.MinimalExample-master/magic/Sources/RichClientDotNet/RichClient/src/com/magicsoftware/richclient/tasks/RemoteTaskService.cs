using System;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.util;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.tasks
{
   internal class RemoteTaskService : TaskServiceBase
   {
      /// <summary>
      /// !!
      /// </summary>
      /// <param name="defaultValue"></param>
      /// <returns></returns>
      internal override string GetTaskTag(string defaultValue)
      {     
         return defaultValue;
      }
    
      /// <summary>
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal override DataviewManagerBase GetDataviewManagerForVirtuals(Task task)
      {
         return task.DataviewManager.RemoteDataviewManager;
      }

       /// <summary>
      /// Prepare Task before execute 
      /// </summary>
      /// <param name="task"></param>
      internal override ReturnResult PrepareTask(Task task)
      {
         // QCR #443197. For non-offline task with a local data it's dataview isn't retrieved yet.
         if (!task.DataviewManager.HasLocalData)
            task.DataViewWasRetrieved = true;
         ReturnResult result = base.PrepareTask(task);

         // Update the refresh rule for Display List property, so it will always be refreshed only 
         // after the form owning the control is refreshed. This will ensure a single refresh cycle on DcValues, occurring
         // only after the form has been re-sent from the server.
         SetPropertyRefreshRule(task, PropInterface.PROP_TYPE_DISPLAY_LIST, new RefreshOnlyAfterFormIsRefreshedRule(task.getForm()));

         // When running as a modal task, force the window to be modal
         if (task.getMGData().IsModal && task.getForm() != null)
            task.getForm().ConcreteWindowType = WindowType.Modal;

         return result;
      }     

      /// <summary>
      /// get the event task id 
      /// </summary>
      /// <param name="originalTaskId"></param>
      /// <param name="task"></param>
      /// <param name="evt"></param>
      /// <returns></returns>
      internal override String GetEventTaskId(Task task, String originalTaskId, events.Event evt)
      {
         return originalTaskId;
      }

      internal override bool ShouldEvaluatePropertyLocally(int propId)
      {
         return false;
      }

      /// <summary>
      /// in connected tasks, task prefix is always executed by the server
      /// </summary>
      /// <param name="task"></param>
      internal override void InitTaskPrefixExecutedFlag(Task task)
      {
         task.TaskPrefixExecuted = true;
      }

      /// <summary>
      /// set the tookit parent task
      /// for remote subtask this send from server in tag MG_ATTR_TOOLKIT_PARENT_TASK = "toolkit_parent_task"; 
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal override void SetToolkitParentTask(Task task)
      {
      }

      /// <summary>
      /// When the subform is closed before call with a destination, we need to clean all parent recomputes.
      /// Recomputes are send from the server together with a new opened subform.
      /// </summary>
      /// <param name="parentTask"></param>
      /// <param name="subformTask"></param>
      internal override void RemoveRecomputes(Task parentTask, Task subformTask)
      {
         ((FieldsTable)parentTask.DataView.GetFieldsTab()).resetRecomp();
      }

      /// <summary>
      /// set the arguments on the task parameters
      /// </summary>
      /// <param name="task"></param>
      /// <param name="args"></param>
      internal override void CopyArguments(Task task, ArgumentsList args)
      {
         // If the parent has local data and the subform is opened, pass and copy the arguments.
         // If the parent and subform tasks are offline tasks, subform tasks are opened after the first RP
         // of the parent and receive their arguments.
         // If the parent and subform tasks are remote tasks, arguments are passed  from the server.
         // But if the parent has local data and an argument is a local, it receive its value only
         // after parent TP, when the parent gets local data. In this case we need to pass arguments.
         if (task.IsSubForm && (task.ParentTask.DataviewManager.HasLocalData || task.DataviewManager.HasLocalData))
         {
            args = ((MgControl)task.getForm().getSubFormCtrl()).ArgList;
            task.CopyArguments(args);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      internal override void HandleRollbackLocalTransactionForCancelAction(Task task)
      {
         task.TaskTransactionManager.CreateAndExecuteRollbackLocalTransaction(RollbackEventCommand.RollbackType.CANCEL);
      }

      /// <summary>
      /// get the owner transaction
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal override Task GetOwnerTransactionTask(Task task)
      {
         Task OwnerTransactionTask = task;

         if (task.DataviewManager.RemoteDataviewManager.Transaction != null)
            OwnerTransactionTask = task.DataviewManager.RemoteDataviewManager.Transaction.OwnerTask;

         return OwnerTransactionTask;
      }

   }
}
