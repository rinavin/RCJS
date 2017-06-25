using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.data;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// Factory to create the local commands to be executed in the client
   /// </summary>
   class LocalRunTimeCommandFactory
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      internal LocalRunTimeCommandBase CreateLocalRunTimeCommand(IClientCommand command)
      {
         if (command is ExecOperCommand)
            return CreateExecuteOperaion((ExecOperCommand)command);
         else if (command is EventCommand)
            return CreateExecuteEvent((EventCommand)command);
         else if (command is MenuCommand)
            return CreateMenuExecuteOperationCommand((MenuCommand)command);
         else if (command is UnloadCommand)
            return CreateUnloadEvent(command);

         Logger.Instance.WriteExceptionToLog(string.Format("Unsupported command '{0}'", command.ToString()));
         throw new NotImplementedException();
      }

      /// <summary>
      /// Create an open task command from a menu command
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      internal LocalRunTimeCommandBase CreateMenuExecuteOperationCommand(MenuCommand command)
      {
         // Get the relevant menu entry
         // QCR #283731. Use MenuComp on command for the relevant Ctl.
         Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx(command.MenuComp);
         ApplicationMenus applicationMenus = Manager.MenuManager.getApplicationMenus(mainProg);
         MenuEntryProgram menuEntry = applicationMenus.menuByUid(command.MenuUid) as MenuEntryProgram;

         TaskDefinitionId taskDefinitionId = new TaskDefinitionId(menuEntry.CtlIndex, menuEntry.ProgramIsn, 0, true);

         ArgumentsList argList = GetCommandArgumentList(command, menuEntry, mainProg);

         return new LocalRunTimeCommandOpenTask(taskDefinitionId) { ArgList = argList, CallingTaskTag = command.TaskTag, PathParentTaskTag = command.TaskTag };
      }

      /// <summary>
      /// get the argument list from the command - either from the operation or from the menu entry
      /// </summary>
      /// <param name="command"></param>
      /// <param name="menuEntry"></param>
      /// <param name="mainProg"></param>
      /// <returns></returns>
      private ArgumentsList GetCommandArgumentList(IClientCommand command, MenuEntryProgram menuEntry, Task mainProg)
      {
         ExecOperCommand cmd = command as ExecOperCommand;

         ArgumentsList argList = null;

         if (cmd != null && cmd.Operation != null)
            argList = cmd.Operation.GetArgList();
         else if (menuEntry.MainProgVars != null)
         {
            argList = new ArgumentsList();
            foreach (String item in menuEntry.MainProgVars)
            {
               Argument argument = new Argument();
               argument.fillDataByMainProgVars (item, mainProg);
               argList.Add(argument);
            }
         }

         return argList;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      private LocalRunTimeCommandBase CreateExecuteOperaion(ExecOperCommand command)
      {
         if (command.Operation != null)
         {
            if (command.Operation.IsLocalCall)
            {

               string calcCallingTaskTag = "";
               string pathParentTaskTag = command.Operation.getTaskTag();  // PathParentTask will be a task containing Call operation.

               // Call with a destination
               if (!String.IsNullOrEmpty(command.Operation.GetSubformControlName()))
               {
                  return new LocalRunTimeCommandCallWithDestination(command.Operation.CalledTaskDefinitionId)
                  {
                     CallingTaskTag = pathParentTaskTag,
                     PathParentTaskTag = pathParentTaskTag,
                     SubformCtrlName = command.Operation.GetSubformControlName(),
                     ArgList = command.Operation.GetArgList(),
                  };
               }
               else
               {
                  // pathParentTask must be ContextTask of PathParent Task.
                  Task pathParentTask = MGDataCollection.Instance.GetTaskByID(pathParentTaskTag) as Task;
                  calcCallingTaskTag = pathParentTask.ContextTask.getTaskTag();
                  return new LocalRunTimeCommandOpenTask(command.Operation.CalledTaskDefinitionId)
                  {
                     ArgList = command.Operation.GetArgList(),
                     ReturnValueField = command.Operation.GetReturnValueField(),
                     CallingTaskTag = calcCallingTaskTag,
                     PathParentTaskTag = pathParentTaskTag
                  };
               }

            }
         }
         else if (command.DitIdx != Int32.MinValue)
         {
            TaskDefinitionId taskDefinitionId = GetTaskIdFromCommandCtrlProp(command);

            return new LocalRunTimeCommandSelectProgram(taskDefinitionId, command);
         }

         throw new NotImplementedException();
      }

      /// <summary>
      /// Creates a command for a subform open
      /// </summary>
      /// <param name="subformCtrl"></param>
      /// <returns></returns>
      private LocalRunTimeCommandBase CreateSubformCallCommand(SubformOpenEventCommand command)
      {
         Task task = Manager.MGDataTable.GetTaskByID(command.TaskTag) as Task;
         MgForm form = task.getForm() as MgForm;
         MgControl subformCtrl = form.CtrlTab.getCtrl(command.DitIdx) as MgControl;
         Property prop = subformCtrl.getProp(PropInterface.PROP_TYPE_PRGTSK_NUM);

         return new LocalRunTimeCommandOpenTask(prop.TaskDefinitionId)
                     {
                        ArgList = subformCtrl.ArgList,
                        CallingTaskTag = task.getTaskTag(),
                        PathParentTaskTag = task.getTaskTag(),
                        SubformDitIdx = subformCtrl.getDitIdx()
                     };
      }

      /// <summary>
      /// Get the TaskDefinitionId of the task to be run, from the SelectProgram property on the command's control
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      private TaskDefinitionId GetTaskIdFromCommandCtrlProp(ExecOperCommand command)
      {
         Task task = Manager.MGDataTable.GetTaskByID(command.TaskTag) as Task;
         MgForm form = task.getForm() as MgForm;
         MgControl control = form.CtrlTab.getCtrl(command.DitIdx) as MgControl;
         Property prop = control.getProp(PropInterface.PROP_TYPE_SELECT_PROGRAM);

         return prop.TaskDefinitionId;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      private LocalRunTimeCommandBase CreateExecuteEvent(EventCommand command)
      {
         switch (command.MagicEvent)
         {
            case InternalInterface.MG_ACT_BROWSER_ESC:
            case InternalInterface.MG_ACT_EXIT:
               return new LocalRunTimeCommandCloseTask(command.TaskTag);

            case InternalInterface.MG_ACT_SUBFORM_OPEN:
               return CreateSubformCallCommand((SubformOpenEventCommand)command);

            case InternalInterface.MG_ACT_ROLLBACK:
               return new LocalRunTimeCommandRollback((RollbackEventCommand)command);

         }
         throw new NotImplementedException();

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      private LocalRunTimeCommandBase CreateUnloadEvent(IClientCommand command)
      {
         return new LocalRunTimeCommandUnload();
      }
   }
}
