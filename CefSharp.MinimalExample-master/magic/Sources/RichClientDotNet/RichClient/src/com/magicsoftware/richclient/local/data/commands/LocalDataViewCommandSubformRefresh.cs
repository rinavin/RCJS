using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// local dataview command for subform refresh command
   /// </summary>
   class LocalDataViewCommandSubformRefresh : LocalDataViewCommandViewRefresh
   {
      private bool explicitSubformRefresh;

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewCommandSubformRefresh(SubformRefreshEventCommand command)
         : base(command)
      {
         explicitSubformRefresh = command.ExplicitSubformRefresh;
      }

      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new GatewayResult();
         Task parentTask = Task.ParentTask;

         parentTask.TaskService.CopyArguments(Task, Task.ArgumentsList);
         if (explicitSubformRefresh || parentTask.ShouldRefreshSubformTask(Task))
         {
            // QCR's #419324 & #281814. Change subform task mode if it's 'AsParent' also in subform refresh.
            if (Task.ModeAsParent)
            {
               char newMode = parentTask.getMode();
               if (newMode != Task.getMode() && Task.getProp(PropInterface.PROP_TYPE_ALLOW_OPTION).getValueBoolean() &&
                  Task.CheckAllowTaskMode(newMode))
               {
                  // Defect 77371: Function SetModeAsParent does additional things for go to Create mode and for go from Create mode.
                  // Defect 82756: Do not execute parent task prefix.
                  bool performParentRecordPrefixOrg = Task.PerformParentRecordPrefix;
                  Task.PerformParentRecordPrefix = false;
                  Task.SetModeAsParent(newMode);
                  Task.PerformParentRecordPrefix = performParentRecordPrefixOrg;

               }
            }
            else
            {
               // QCR #417768. When subform refresh is executed set the original task mode if it's possible.
               // If this mode is not allowed (expression is changed), set Modify mode.
               Property.UpdateValByStudioValue(Task, PropInterface.PROP_TYPE_TASK_MODE);
               char propTaskModeValue = Task.getMode();
               if (Task.CheckAllowTaskMode(propTaskModeValue))
                  Task.setMode(propTaskModeValue);
               else
                  Task.setMode(Constants.TASK_MODE_MODIFY);
            }

            result = base.Execute();
            Task.RefreshDisplay();
         }

         return result;
      }

      /// <summary>
      /// Do not change subform task mode when subform refresh is done.
      /// QCR #153903
      /// </summary>
      protected override void SetTaskMode()
      {
      }

   }
}
