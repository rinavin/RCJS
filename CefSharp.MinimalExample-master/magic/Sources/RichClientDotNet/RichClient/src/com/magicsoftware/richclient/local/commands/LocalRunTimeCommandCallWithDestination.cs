using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using System;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// local command to run a call operation with a destination property
   /// </summary>
   class LocalRunTimeCommandCallWithDestination : LocalRunTimeCommandOpenTask
   {
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      internal LocalRunTimeCommandCallWithDestination(TaskDefinitionId taskDefinitionId) : base (taskDefinitionId)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      internal override void Execute()
      {
         Task parentTask = Manager.MGDataTable.GetTaskByID(CallingTaskTag) as Task;
         MgForm parentForm = parentTask.getForm() as MgForm;

         MgControl destSubformCtrl = parentForm.getSubFormCtrlByName(SubformCtrlName);
         // Subform task is not allowed to be opened before the first record prefix of the parent.
         // In Task Prefix or in the first Record Prefix do not execute call but put the task from the call on the subform control property.
         // So after the first record prefix the task from the call operation will be loaded into the subform control.
         if (destSubformCtrl != null && !((Task)destSubformCtrl.getForm().getTask()).AfterFirstRecordPrefix)
         {
            // QCR #310113. If the subform was loaded, now it must be closed.
            Task subformTaskToRemove = destSubformCtrl.getSubformTask();
            if (subformTaskToRemove != null)
               subformTaskToRemove.stop();

            SetCalledTaskDefinitionId(destSubformCtrl);
            SetCalledSubformType(destSubformCtrl);
            destSubformCtrl.ArgList = ArgList;

            // TODO: Task Suffix
         }
         else
            base.Execute();
      }

      /// <summary>
      /// Set subform type on the property of the subform control
      /// </summary>
      /// <param name="destSubformCtrl"></param>
      private void SetCalledSubformType(MgControl destSubformCtrl)
      {
         SubformType subformType = TaskDefinitionId.IsProgram == true ? SubformType.Program : SubformType.Subtask;
         String mgNumString = DisplayConvertor.Instance.toNum(((int)subformType).ToString(), null, 0);
         destSubformCtrl.setProp(PropInterface.PROP_TYPE_SUBFORM_TYPE, mgNumString);
      }

      /// <summary>
      /// Set TaskDefinitionId on the property of the subform control
      /// </summary>
      /// <param name="destSubformCtrl">subform control</param>
      private void SetCalledTaskDefinitionId(MgControl destSubformCtrl)
      {
         // If 'Connect to' property was 'none'
         if (!destSubformCtrl.checkIfExistProp(PropInterface.PROP_TYPE_PRGTSK_NUM))
            destSubformCtrl.setProp(PropInterface.PROP_TYPE_PRGTSK_NUM, null);

         Property prop = destSubformCtrl.getProp(PropInterface.PROP_TYPE_PRGTSK_NUM);
         prop.SetTaskDefinitionId(TaskDefinitionId);
      }

   }
}
