using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   /// <summary>
   /// ResetLocateCommand
   /// </summary>
   class ResetLocateCommand : ClientTargetedCommandBase
   {
      /// <summary>
      /// Execute ResetLocate command.
      /// </summary>
      /// <param name="res"></param>
      public override void  Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);

         IClientCommand command = CommandFactory.CreateDataViewCommand(TaskTag, DataViewCommandType.ResetUserLocate);
         task.DataviewManager.Execute(command);
      }
   }
}
