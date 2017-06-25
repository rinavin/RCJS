using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// local command to close the task
   /// </summary>
   class LocalRunTimeCommandCloseTask : LocalRunTimeCommandBase
   {
      string taskId;

      public LocalRunTimeCommandCloseTask(string taskId)
      {
         this.taskId = taskId;
      }

      internal override void Execute()
      {
         //If we are aborting the root task, close the Main Programs (execute their TS) first.
         //It is same as closing the root non-offline task in non-offline application.
         //In that case, the exit event is send to the server, which executes the TS of MPs, closes them 
         //and then sends the abort command for the root non-offline task.
         Task task = (Task)MGDataCollection.Instance.GetTaskByID(taskId);
         if (task == MGDataCollection.Instance.StartupMgData.getFirstTask())
            ClientManager.Instance.CloseMainProgramsForOfflineApplication();

         IClientCommand command = CommandFactory.CreateAbortCommand(taskId);
         base.Execute(command);
      }
   }
}
