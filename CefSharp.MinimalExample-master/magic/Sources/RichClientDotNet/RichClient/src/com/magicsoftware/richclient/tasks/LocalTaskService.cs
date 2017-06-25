using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using com.magicsoftware.richclient.data;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.util;
using NUM_TYPE = com.magicsoftware.unipaas.management.data.NUM_TYPE;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.local.commands;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;


namespace com.magicsoftware.richclient.tasks
{

   internal class LocalTaskService : TaskServiceBase
   {
      // number to be used for task tag. start from a very large number, so offline tasks will not have the same tag as connected tasks
      protected static uint _lastedTaskTag = ConstInterface.INITIAL_OFFLINE_TASK_TAG;

      /// <summary>
      /// !!
      /// </summary>
      /// <param name="defaultValue"></param>
      /// <returns></returns>
      internal override string GetTaskTag(string defaultValue)
      {
         ++_lastedTaskTag;

         return _lastedTaskTag.ToString();
      }


      /// <summary>
      /// set the tookit parent task
      /// for remote subtask this send from server in tag MG_ATTR_TOOLKIT_PARENT_TASK = "toolkit_parent_task"; 
      /// and for local we need to calculate it 
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal override void SetToolkitParentTask(Task task)
      {
         // If it is subtask then we need to save ParentTask as the calling task that is toolkit task
         if (task.IsSubtask)
            task.StudioParentTask = task.PathParentTask;
      }

      /// <summary>
      /// </summary>
      /// <param name="task"></param>
      internal override void CreateFirstRecord(Task task)
      {
         if (task.HasFields)
            ((DataView)task.DataView).addFirstRecord();
      }

      /// <summary>
      /// !!
      /// </summary>
      /// <param name="task"></param>
      internal override ReturnResult ExecuteTaskPrefix(Task task)
      {
         ReturnResult returnResult = base.ExecuteTaskPrefix(task);

         if (returnResult.Success)
         {
            ClientManager.Instance.EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_TASK_PREFIX);
            task.TaskPrefixExecuted = true;
         }

         return returnResult;
      }

      /// <summary>
      /// get dataview Manager for virtuals
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal override DataviewManagerBase GetDataviewManagerForVirtuals(Task task)
      {
         return task.DataviewManager.LocalDataviewManager;
      }

      /// <summary>
      /// set the arguments on the task parameters
      /// </summary>
      /// <param name="task"></param>
      /// <param name="args"></param>
      internal override void CopyArguments(Task task, ArgumentsList args)
      {
         task.CopyArguments(args);
      }

      /// <summary>
      /// update changes in called task to fields in arguments from calling task
      /// </summary>
      /// <param name="task"></param>
      /// <param name="args"></param>
      internal override void UpdateArguments(Task task, ArgumentsList args)
      {
         FieldsTable fieldsTable = task.DataView.GetFieldsTab() as FieldsTable;

         int numberOfArguments = (args != null) ? args.getSize() : 0;
         int currentArgIndex = 0; // index of currently handled argument
         bool taskHasParameters = ((DataView)task.DataView).ParametersExist();

         if (numberOfArguments > 0)
         {
            int dvPos = ((DataView)task.DataView).GetRecordMainIdx();
            int dvSize = ((DataView)task.DataView).GetRecordMainSize();

            // Go over the fields, find the fields which had arguments set to
            for (int i = dvPos; (i < dvSize) && (currentArgIndex < numberOfArguments); i++)
            {
               Field field = fieldsTable.getField(i) as Field;

               if (!field.IsForArgument(taskHasParameters))
                  continue;

               // get the argument
               Argument arg = args.getArg(currentArgIndex);
               currentArgIndex++;

               // ignore skipped arguments
               if (arg.skipArg())
                  continue;

               // ignore arguments which are not fields
               Field argField = arg.getField();
               if (argField == null)
                  continue;

               SetArgumentToField(field, argField);
            }
         }
      }

      /// <summary>
      /// Update the field from the argument with value from executed task's field
      /// </summary>
      /// <param name="sourceField"></param>
      /// <param name="targetField"></param>
      void SetArgumentToField(Field sourceField, Field targetField)
      {
         String value = Argument.convertArgs(sourceField.getValue(false), sourceField.getType(), targetField.getType());

         if (sourceField.IsModifiedAtLeastOnce())
         {
            //If argument is modified then only perform setValueAndStartRecompute().
            bool valsEqual = ExpressionEvaluator.mgValsEqual(value, sourceField.isNull(), sourceField.getType(), targetField.getValue(false), targetField.isNull(), targetField.getType());

            if (!valsEqual)
            {
               targetField.setValueAndStartRecompute(value, false, true, true, false);
               targetField.updateDisplay();
            }
         }
      }

      /// <summary>
      /// Removes those parent task recomputes that were arguments for the subform.
      /// We want do it when the subform is closed before a Call with a destination.
      /// </summary>
      /// <param name="parentTask"></param>
      /// <param name="subformTask"></param>
      internal override void RemoveRecomputes(Task parentTask, Task subformTask)
      {
         FieldsTable fieldsTable = parentTask.DataView.GetFieldsTab() as FieldsTable;
         Field field;

         if (subformTask != null && subformTask.RefreshOnVars != null)
         {
            foreach (int fieldIdx in subformTask.RefreshOnVars)
            {
               field = fieldsTable.getField(fieldIdx) as Field;
               field.RemoveSubformFromRecompute(subformTask);
            }
         }
      }

      /// <summary>
      /// Sets RefreshOnVars array and adds the corresponding recomputes for the parent
      /// </summary>
      internal override void AddSubformRecomputes(Task task)
      {
         if (task.ArgumentsList != null && !String.IsNullOrEmpty(task.ArgumentsList.RefreshOnString))
         {
            task.SetRefreshOnVars(task.ArgumentsList.RefreshOnString);
            AddRecomputes(task);
         }
      }

      /// <summary>
      /// Adda recomputes for the parent task if this task is a subform and has arguments from the parent.
      /// </summary>
      private void AddRecomputes(Task task)
      {
         // QCR #423434.
         if (task.ParentTask == null)
            return;

         FieldsTable fieldsTable = task.ParentTask.DataView.GetFieldsTab() as FieldsTable;
         Field field;

         foreach (int fieldIdx in task.RefreshOnVars)
         {
            field = fieldsTable.getField(fieldIdx) as Field;
            field.AddSubformRecompute(task);
         }
      }

      /// set modality of command and task according to task form type and the force-modal info
      /// </summary>
      /// <param name="task"></param>
      void SetIsModal(Task task)
      {
         MGData mgdata = task.getMGData();

         if (mgdata.ForceModal)
         {
            // force the task to run modal
            mgdata.IsModal = true;
            // change the form type to be modal
            if (task.getForm() != null)
               task.getForm().ConcreteWindowType = WindowType.Modal;
         }
         else
         {
            // set the IsModel according to the calculated window type
            if (!task.IsSubForm && task.getForm() != null)
               mgdata.IsModal = (task.getForm().ConcreteWindowType == WindowType.Modal);
         }
      }

      /// <summary>
      /// return TRUE if this window type is support by the   Rich Client
      /// </summary>
      /// <param name="windowType"></param>
      /// <returns></returns>
      static bool IsValidWindowType(WindowType windowType)
      {
         List<WindowType> validTypes = new List<WindowType>(){ WindowType.Sdi, WindowType.ChildWindow, WindowType.Floating, WindowType.Modal,
                                                                WindowType.Tool, WindowType.FitToMdi, WindowType.MdiChild, WindowType.MdiFrame };
         return validTypes.Contains(windowType);
      }

      /// <summary>
      /// same as we doing in the server see tsk_open.cpp RichClientIsValidWindowType
      /// </summary>
      /// <param name="task"></param>
      private ReturnResult CheckIsValidWindowType(Task task)
      {
         ReturnResult result = ReturnResult.SuccessfulResult;

         if (task.getForm() != null)
         {
            WindowType windowType = task.getForm().ConcreteWindowType;
            bool isValidWindowType = IsValidWindowType(windowType);

            // get the main program of the internal           

            MGDataCollection mgDataTab = MGDataCollection.Instance;
            Task mainPrgTask = mgDataTab.GetMainProgByCtlIdx(0);

            if (isValidWindowType)
            {
               // if we try to open FitToMdi || MdiChild , the main program should have open window
               if (windowType == WindowType.FitToMdi || windowType == WindowType.MdiChild)
               {
                  if (!(mainPrgTask != null && mainPrgTask.isOpenWin()))
                  {
                     result = new ReturnResult(MsgInterface.DN_ERR_MDI_FRAME_ISNOT_OPENED);
                  }
               }
            }
            else
            {
               result = new ReturnResult(MsgInterface.DITDEF_STR_ERR_WINDOW_TYPE);
            }
         }
         return result;
      }

      /// <summary>
      /// Prepare Task before 
      /// </summary>
      /// <param name="task"></param>
      internal override ReturnResult SetTaskMode(Task task)
      {
         char orgTaskMode = task.getMode();
         task.getProp(PropInterface.PROP_TYPE_TASK_MODE).ResetComputedOnceFlag();

         // set the studio value form property mode, so in dataview command we are asking about it 
          ReturnResult result = PrepareTaskMode(task);
         Record rec = ((DataView)task.DataView).getCurrRec();
         if (orgTaskMode != task.getMode() && rec != null)
         {
            
            if (task.getMode() == Constants.TASK_MODE_CREATE)
            {
               rec.setNewRec();
               rec.setMode(com.magicsoftware.unipaas.management.data.DataModificationTypes.Insert);
            }
            else
            {
               rec.setOldRec();
               rec.setMode(com.magicsoftware.unipaas.management.data.DataModificationTypes.None);
            }
         }
         return result;
      }

      /// <summary>
      /// Prepare Task before execution
      /// </summary>
      /// <param name="task"></param>
      internal override ReturnResult PrepareTask(Task task)
      {

         // init transaction 
         Property.UpdateValByStudioValue(task, PropInterface.PROP_TYPE_TRASACTION_BEGIN);


         ReturnResult result;
         // 4. TODO: transaction begin : see appSerializer.cpp line ~455
         Property.UpdateValByStudioValue(task, PropInterface.PROP_TYPE_TASK_PROPERTIES_OPEN_TASK_WINDOW);

         // 1. Note TaskTag of this task :
         //    calculate the tag task (we save the last task tag that was send from the remote , 
         //     and in the local just increment the count

         // 5. set the value to _openWin
         result = task.PrepareTaskForm();
         if (!result.Success)
            return result;

         // Update the refresh rule for Display List property, so it will always be refreshed when 
         // any of its dependencies are updated.
         SetPropertyRefreshRule(task, PropInterface.PROP_TYPE_DISPLAY_LIST, new AlwaysRefreshRule());

         // update the form with the subform control from the parent task
         if (task.SubformControl != null)
            ((MgForm)task.getForm()).setSubFormCtrl(task.SubformControl);

         result = CheckIsValidWindowType(task);
         if (!result.Success)
            return result;

         // PrepareTask should be after the form is initialized for the chunk size to be initialized properly
         result = base.PrepareTask(task);
         if (!result.Success)
            return result;

         SetIsModal(task);

         // set the trigger task , depends on the IsModal & main display         
         task.HandleTriggerTask();

         // set the PreviouslyActiveTaskId , depends on the IsModal & main display
         task.HandlePreviouslyActiveTaskId();

         // for handlers table for each item calculate the control from the control name
         task.HandlersTab.CalculateControlFormControlName();

         task.initCtrlVerifyHandlerBits();

         // 3. transaction mode
         Property propTransMode = task.getProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_TRANSACTION_MODE);
         if (propTransMode != null)
         {
            TransMode orgStudioValueTrasnMode = (TransMode)propTransMode.StudioValue[0];
            //char newValuePropTransMode = orgValue.CompareTo(TransMode.None) == 0 ? TransMode.None  : TransMode.Physical;
            TransMode newValuePropTransMode = orgStudioValueTrasnMode == TransMode.None ? TransMode.None : TransMode.Physical;

            task.setProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_TRANSACTION_MODE, (char)newValuePropTransMode);
         }

         // 6. set the value for all properties that compute once from studio value.
         Property.UpdateValByStudioValue(task, PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_EVENTS);
         Property.UpdateValByStudioValue(task, PropInterface.PROP_TYPE_PRELOAD_VIEW);
         Property.UpdateValByStudioValue(task, PropInterface.PROP_TYPE_TABBING_CYCLE);

         return result;
      }

      /// <summary>
      /// prepare the task init mode
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      ReturnResult PrepareTaskMode(Task task)
      {
         ReturnResult result = ReturnResult.SuccessfulResult;

         Property.UpdateValByStudioValue(task, PropInterface.PROP_TYPE_TASK_MODE);

         // If task is a subform and it's mode is as parent mode, set indication on the task.
         if (task.IsSubForm)
         {
            Property taskMode = task.getProp(PropInterface.PROP_TYPE_TASK_MODE);
            if (taskMode.StudioValue[0] == 'P')
               task.ModeAsParent = true;
         }

         char propTaskModeValue = task.getMode();

         if (propTaskModeValue == 'P') // AS PARENT
         {
            char parentTaskMode = task.PathParentTask.getMode();
            task.setMode(parentTaskMode);
            propTaskModeValue = parentTaskMode;
         }

         bool modeIsAllowed = task.CheckAllowTaskMode(propTaskModeValue);
         if (!modeIsAllowed)
            result = new ReturnResult(MsgInterface.RT_STR_MODE_NOTALLOWED);
         else
         {
            task.setOriginalTaskMode(propTaskModeValue);

            // On non interactive rich client task, the force suffix is always true. excepte for mode Delete
            if (!task.IsInteractive)
               task.setProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, propTaskModeValue == 'D' ? "0":"1");
         }
         return result;
      }

      /// <summary>
      /// Get the parent of a task: This method takes into consideration the possibility
      /// that the parent task is not necessarily the "calling" task, as the case may be
      /// when loading the initial task - whose parent is the main program.
      /// </summary>
      /// <param name="task">The task whose parent should be retrieved.</param>
      /// <returns>The execution-wise parent of the task.</returns>
      Task GetParentTask(Task task)
      {
         // Main programs have no parent.
         if (task.isMainProg())
            return null;

         // fixed bug #:297896, 
         // If the task is activated by a 'call' operation, the client manager will
         // hold the calling task as the '_parentTask'. In that case, this is the calling task.
         if (task.getParent() != null)
            return task.getParent();

         // was remarked-> need to be check if we need it
         // if (ClientManager.Instance.getCurrTask() != null)
         //    return ClientManager.Instance.getCurrTask();

         // At this point we assume that the parameter is the startup task, which is not being
         // called but rather activated by the command processor. In this case the parent task
         // is the main program of CTL 0.
         return MGDataCollection.Instance.GetMainProgByCtlIdx(0);
      }

      /// <summary>
      /// update the return value returned from the task
      /// </summary>
      /// <param name="task">exiting task</param>
      /// <param name="returnValField">field to be updated with the tasks return value</param>
      internal override void UpdateReturnValue(Task task, Field returnValField)
      {
         if (task.RetrunValueExp == 0 || returnValField == null)
            return;

         StorageAttribute vecCellAttribute = StorageAttribute.NONE;
         if (returnValField.getType() == StorageAttribute.BLOB_VECTOR)
            vecCellAttribute = returnValField.getCellsType();

         // evaluate the expression
         bool wasEvaluated;
         String retVal = task.EvaluateExpression(task.RetrunValueExp, returnValField.getType(), returnValField.getSize(),
                                                 false, vecCellAttribute, true, out wasEvaluated);
         if (wasEvaluated)
            returnValField.setValueAndStartRecompute(retVal, false, true, true, false);
         else
         {
            returnValField.invalidate(true, Field.CLEAR_FLAGS);
            returnValField.compute(false);
         }

         returnValField.updateDisplay();
      }

      /// <summary>
      /// !!
      /// </summary>
      /// <param name="task"></param>
      internal override void SetChunkSize(Task task)
      {
         ((DataView)task.DataView).CalculateChunkSizeExp();
         base.SetChunkSize(task);
      }

      /// <summary>
      /// get event task id
      /// </summary>
      /// <param name="task"></param>
      /// <param name="evt"></param>
      /// <returns></returns>
      internal override String GetEventTaskId(Task task, String originalTaskId, events.Event evt)
      {
         Task eventTask = task.GetAncestorTaskByTaskDefinitionId(evt.OwnerTaskDefinitionId);
         return eventTask.getTaskTag();
      }

      internal override bool ShouldEvaluatePropertyLocally(int propId)
      {
         return true;
      }

      /// <summary>
      /// in local tasks, task prefix will be executed later locally 
      /// </summary>
      /// <param name="task"></param>
      internal override void InitTaskPrefixExecutedFlag(Task task)
      {
         task.TaskPrefixExecuted = false;
      }

      /// <summary>
      /// Show the form now
      /// </summary>
      /// <param name="task"></param>
      internal override void OpenForm(Task task, bool callByDestSubForm)
      {
         MgForm form = task.getForm() as MgForm;
         if (form != null)
         {
            // remove the form from the list of forms to be opened, so this task's dataview will be initialized
            // when we want it to
            ClientManager.Instance.CreatedForms.remove(form);

            // open other forms, to ensure correct modality relationships
            ClientManager.Instance.OpenForms(callByDestSubForm);

            // open this form
            Manager.OpenForm(form);
         }
      }

      /// <summary>
      /// Frames form is not supported in an offline task
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal override bool IsFrameAllowed(Task task)
      {
         return false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      internal override void DoFirstRefreshTable(Task task)
      {
         MgForm form = task.getForm() as MgForm;

         if (form != null)
            Manager.DoFirstRefreshTable(form);
      }

      /// <summary>
      /// Execute RP and open subforms.
      /// </summary>
      /// <param name="task"></param>
      internal override void EnterFirstRecord(Task task)
      {
         if (task.getForm() != null && !task.isMainProg())
         {
            ((MgForm)task.getForm()).IgnoreFirstRecordCycle = true;
            task.SubformExecMode = SubformExecModeEnum.FIRST_TIME;

            ClientManager.Instance.EventsManager.pushNewExecStacks();
            ClientManager.Instance.EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX, task.IsSubForm);
            if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag())
            {
               task.AfterFirstRecordPrefix = true;
               Callsubforms(task);

               if (task.IsSubForm)
               {
                  task.getForm().executeLayout();
                  ClientManager.Instance.EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
               }
            }

            task.SubformExecMode = SubformExecModeEnum.SET_FOCUS;
            ClientManager.Instance.EventsManager.popNewExecStacks();
         }
         else
            base.EnterFirstRecord(task);
      }

      /// <summary>
      /// This function finds all subform controls and opens their tasks.
      /// </summary>
      /// <param name="task"></param>
      private void Callsubforms(Task task)
      {
         IList<MgControlBase> list = task.getForm().CtrlTab.GetControls(IsSubformWithTask);
         foreach (MgControl ctrl in list)
         {
            // may be the subform task was opened from call with destination from the RP of the previous subform
            if (ctrl.getSubformTask() == null)
            {
               IClientCommand cmd = CommandFactory.CreateSubformOpenCommand(task.getTaskTag(), ctrl.getDitIdx());
               task.getMGData().CmdsToServer.Add(cmd);
               task.CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.ONLY_COMMANDS);
            }
         }
      }

      /// <summary>
      /// predicate for open subforms
      /// </summary>
      /// <param name="ctrl"></param>
      /// <returns></returns>
      private bool IsSubformWithTask(MgControlBase ctrl)
      {
         bool isLoadTask = (ctrl.Type == MgControlType.CTRL_TYPE_SUBFORM &&
                             (SubformType)ctrl.getProp(PropInterface.PROP_TYPE_SUBFORM_TYPE).getValueInt() != SubformType.None);

         // QCR #177047. If the subform is visible or it's RefreshWhenHidden is 'Y' load the subform task.
         if (isLoadTask && !ctrl.isVisible() && !ctrl.checkProp(PropInterface.PROP_TYPE_REFRESH_WHEN_HIDDEN, false))
         {
            // QCR #282991.
            // It related to nested subforms. When the parent subform is invisible and it's RefreshWhenHidden = Y, 
            // we load the parent subform. But the nested subform is loaded according its RefreshWhenHidden property.
            // If the nested subform has no visibility expression, so we load it together with its parent (RefreshWhenHidden property doesn't affect).
            // If the nested subform has a visibility expression, so we do not load this subform (RefreshWhenHidden = No).
            // In this case the subform is loaded when its control becomes visible or by SubformRefresh event. 
            // QCR #315894. The subform may become visible also if it has such a parent control as a group control, a tab control, a combo box.
            Property visibleProperty = ctrl.getProp(PropInterface.PROP_TYPE_VISIBLE);
            if (ctrl.hasContainer() || ctrl.getLinkedParent(false) != null || visibleProperty.isExpression())
               isLoadTask = false;
         }

         return isLoadTask;
      }

      /// <summary>
      /// Sets subform control for the new opened subform task and
      /// sets subform task tag on the parent subform control
      /// </summary>
      /// <param name="parentTask"></param>
      /// <param name="subformControl"></param>
      internal override void PrepareForSubform(Task parentTask, MgControl subformControl)
      {
         List<Task> subformTaskList = MGDataCollection.Instance.GetTasks(t => !t.isStarted() && !t.isMainProg());
         Debug.Assert(subformTaskList.Count == 1, "Only 1 subform can be opened each time");

         Task subformTask = subformTaskList[0];
         subformControl.setSubformTaskId(subformTask.getTaskTag());
         subformTask.SubformControl = subformControl;
      }

      /// <summary>
      /// Creates exit command, adds it to commands for execution and execute it.
      /// It isn't needed for a subform. A subform doesn't receive CMD_TYPE_ABORT.
      /// CMD_TYPE_ABORT is sent to MgData, i.e. Parent task.
      /// </summary>
      /// <param name="task"></param>
      /// <param name="reversibleExit"></param>
      /// <param name="subformDestination"></param>
      internal override void Exit(Task task, bool reversibleExit, bool subformDestination)
      {
         if (!task.IsSubForm)
            base.Exit(task, reversibleExit, subformDestination);

      }

      /// <summary>
      /// Variable event should be enabled during task close in offline, since server will not execute it for us,
      /// as it is performed for non offline programs
      /// </summary>
      /// <param name="evt"></param>
      /// <returns></returns>
      internal override bool AllowEventExecutionDuringTaskClose(events.RunTimeEvent evt)
      {
         return evt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                evt.getInternalCode() == InternalInterface.MG_ACT_VARIABLE;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      internal override void BeforeRollback(Task task)
      {
         // The cancel handled here will not caused a rollback since it was raised by rollback. (rollb already done in server).
         ClientManager.Instance.EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_CANCEL, EventSubType.CancelWithNoRollback);
      }

      /// <summary>
      /// get the owner transaction      
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal override Task GetOwnerTransactionTask(Task task)
      {
         Task OwnerTransactionTask = task;

         if (task.DataviewManager.LocalDataviewManager.Transaction != null)
            OwnerTransactionTask = task.DataviewManager.LocalDataviewManager.Transaction.OwnerTask;

         return OwnerTransactionTask;
      }
   }
}
