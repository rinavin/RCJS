using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// local command to run a program from the select-program property of a control
   /// </summary>
   class LocalRunTimeCommandSelectProgram : LocalRunTimeCommandOpenTask
   {
      MgControl mgControl;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      /// <param name="command"></param>
      public LocalRunTimeCommandSelectProgram(TaskDefinitionId taskDefinitionId, IClientCommand command)
         : base(taskDefinitionId)
      {
         ExecOperCommand cmd = command as ExecOperCommand;
         Debug.Assert(cmd != null);

         Task task = Manager.MGDataTable.GetTaskByID(cmd.TaskTag) as Task;
         MgForm form = task.getForm() as MgForm;
         mgControl = form.CtrlTab.getCtrl(cmd.DitIdx) as MgControl;
         
         CallingTaskTag = cmd.TaskTag;
         PathParentTaskTag = CallingTaskTag;

         ForceModal = true;
      }

      /// <summary>
      /// 
      /// </summary>
      internal override void Execute()
      {
         Field field = mgControl.getField() as Field;

         // get the value directly from the control
         string value = Manager.GetCtrlVal(mgControl);
         // get the MgValue - if the value is an index in a list, get the displayed string from the list
         value = mgControl.getMgValue(value);

         // set the value on the field
         field.setValueAndStartRecompute(value, value == null, false, false, false);

         // create an argument list with the field
         Argument argument = new Argument(field);
         ArgList = new ArgumentsList(argument);

         base.Execute();
      }
   }
}
