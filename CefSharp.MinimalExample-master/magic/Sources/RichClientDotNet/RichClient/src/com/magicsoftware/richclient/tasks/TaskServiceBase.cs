using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.data;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.richclient.gui;
using System.Diagnostics;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.tasks
{
   internal abstract class TaskServiceBase
   {
      /// <summary>
      /// </summary>
      /// <param name="defaultValue"></param>
      /// <returns></returns>
      internal abstract string GetTaskTag(string defaultValue);

      /// <summary>
      /// creates initial record
      /// </summary>
      /// <param name="task"></param>
      internal virtual void CreateFirstRecord(Task task)
      {
         // QCR #298593 Frames are different from subforms
         task.AfterFirstRecordPrefix = true;
      }

      /// <summary>
      /// execute the task prefix
      /// </summary>
      /// <param name="task"></param>
      internal virtual ReturnResult ExecuteTaskPrefix(Task task) 
      {
         return task.TaskTransactionManager.CheckAndOpenLocalTransaction(ConstInterface.TRANS_TASK_PREFIX);
      }

      /// <summary>
      /// !!
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal abstract DataviewManagerBase GetDataviewManagerForVirtuals(Task task);

      /// <summary>
      /// set the arguments on the task parameters
      /// </summary>
      /// <param name="task"></param>
      /// <param name="args"></param>
      internal abstract void CopyArguments(Task task, ArgumentsList args);

      /// <summary>
      /// update changes in called task to fields in arguments from calling task
      /// </summary>
      /// <param name="task"></param>
      /// <param name="args"></param>
      internal virtual void UpdateArguments(Task task, ArgumentsList args) { }

      /// <summary>
      /// Prepare Task before execution
      /// </summary>
      /// <param name="task"></param>
      internal virtual ReturnResult PrepareTask(Task task)
      {
         // Subform with a local data is not cached
         if (task.IsSubForm && (task.DataviewManager.HasLocalData || task.ParentTask.DataviewManager.HasLocalData))
         {
            MgControlBase subformControl= task.getForm().getSubFormCtrl();
            Property propIsCached = subformControl.GetComputedProperty(PropInterface.PROP_TYPE_IS_CACHED);
            propIsCached.setValue("0");
         }
         SetChunkSize(task);
         return ReturnResult.SuccessfulResult;
      }

      protected void SetPropertyRefreshRule(Task task, int property, IPropertyRefreshRule rule)
      {
         if (task.getForm() == null)
            return;

         task.getForm().SetPropertyRefreshRule(property, rule);
      }



      /// <summary>
      /// Set Path Parent Task 
      /// </summary>
      /// <param name="task"></param>
      internal virtual void SetPathParentTask(Task task)
      {
      }

      /// <summary>
      /// Set Task Mode 
      /// </summary>
      /// <param name="task"></param>
      internal virtual ReturnResult SetTaskMode(Task task)
      {
         return ReturnResult.SuccessfulResult;
      }
      

      /// <summary>
      /// calculate chunk size
      /// </summary>
      /// <param name="task"></param>
      internal virtual void SetChunkSize(Task task)
      {
         task.DataviewManager.LocalDataviewManager.MaxRecordsInView = ((DataView)task.DataView).getChunkSize();
      }

      /// <summary>
      /// prepare the property open task window
      /// </summary>
      /// <param name="task"></param>
      internal static void PreparePropOpenTaskWindow(Task task)
      {
         Property propOpenTaskWindow = task.getProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_OPEN_TASK_WINDOW);
         bool propOpenTaskWindowValue = false;

         // for main program that isn't internal application , we are not use the form, the open window is false
         // the same we doing for remote program in tsk_open.cpp 
         if (!(task.isMainProg() && task.getCtlIdx() != 0))
            propOpenTaskWindowValue = propOpenTaskWindow.getValueBoolean();

         task.SetOpenWin(propOpenTaskWindowValue);
      }

      /// <summary>
      /// prepare the main display
      /// </summary>
      /// <param name="task"></param>
      internal static ReturnResult PreparePropMainDisplay(Task task)
      {

         task.ComputeMainDisplay();
         bool mainDisplayIsLegal = task.FormIsLegal();
         ReturnResult result = (mainDisplayIsLegal ? ReturnResult.SuccessfulResult : new ReturnResult(MsgInterface.BRKTAB_STR_ERR_FORM));
         if (result.Success && !task.TaskService.IsFrameAllowed(task))
         {
            MgForm form = task.getForm() as MgForm;
            if (form != null && form.IsFrameSet)
               result = new ReturnResult(MsgInterface.CHK_ERR_OFFLINE_NOT_SUPPORT_FRAME_INTERFACE);
         }

         return result;
      }

      /// <summary>
      /// </summary>
      /// <param name="task"></param>
      /// <param name="returnValField"></param>
      internal virtual void UpdateReturnValue(Task task, Field returnValField) { }

      /// <summary>
      /// !!
      /// </summary>
      /// <param name="task"></param>
      /// <param name="originalTaskId"></param>
      /// <param name="evt"></param>
      /// <returns></returns>
      internal abstract String GetEventTaskId(Task task, String originalTaskId, Event evt);

      /// <summary>
      /// Returns a boolean indicating whether the property whose id is propId, should
      /// be reevaluated locally, on the client side.
      /// </summary>
      /// <param name="propId">The identifier of the property in question.</param>
      /// <returns>
      /// The implementing method should return <code>true</code> if the property
      /// should be evaluated on the client side. Otherwise it should return <code>false</code>.
      /// </returns>
      internal abstract bool ShouldEvaluatePropertyLocally(int propId);

      /// <summary>
      /// initialize the TaskPrefixExecuted flag on the task - on connected tasks it is always executed on the server, while
      /// in offline tasks it is executed as part of the task start process
      /// </summary>
      /// <param name="task"></param>
      internal abstract void InitTaskPrefixExecutedFlag(Task task);

      /// <summary>
      /// Show the form. For connected tasks, will do nothing.
      /// </summary>
      /// <param name="task"></param>
      internal virtual void OpenForm(Task task, bool callByDestSubForm) { }

      /// <summary>
      /// Frames form is not supported in an offline task
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal virtual bool IsFrameAllowed(Task task)
      {
         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      internal virtual void DoFirstRefreshTable(Task task) { }

      /// <summary>
      /// Execute RP and open subforms. For connected tasks does nothing.
      /// </summary>
      /// <param name="task"></param>
      internal virtual void EnterFirstRecord(Task task) { }

      /// <summary>
      /// Sets subform control for the new opened subform task and
      /// sets subform task tag on the parent subform control
      /// </summary>
      /// <param name="parentTask"></param>
      /// <param name="subformControl"></param>
      internal virtual void PrepareForSubform(Task parentTask, MgControl subformControl) { }

      /// <summary>
      /// Creates exit command, adds it to commands for execution and execute it if it's needed
      /// </summary>
      /// <param name="task"></param>
      /// <param name="reversibleExit"></param>
      /// <param name="subformDestination"></param>
      internal virtual void Exit(Task task, bool reversibleExit, bool subformDestination)
      {
         task.Exit(reversibleExit, subformDestination);
      }

      /// <summary>
      /// set the tookit parent task
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal abstract void SetToolkitParentTask(Task task);

      /// <summary>
      /// When the subform is closed before call with a destination, we need to remove parent recomputes.
      /// </summary>
      /// <param name="parentTask"></param>
      /// <param name="subformTask"></param>
      internal abstract void RemoveRecomputes(Task parentTask, Task subformTask);

      /// <summary>
      /// Sets RefreshOnVars array and adds the corresponding recomputes for the parent
      /// </summary>
      internal virtual void AddSubformRecomputes(Task task) { }

      /// <summary>
      /// check if event can be executed during task close
      /// </summary>
      /// <param name="evt"></param>
      /// <returns></returns>
      internal virtual bool AllowEventExecutionDuringTaskClose(RunTimeEvent evt) { return false; }

      internal virtual void BeforeRollback(Task task) { }

      internal virtual void HandleRollbackLocalTransactionForCancelAction(Task task) { }

      internal virtual Task GetOwnerTransactionTask(Task task) { return null; }
   }
}
