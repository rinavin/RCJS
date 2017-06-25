using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   class ResetSortCommand : ClientTargetedCommandBase
   {
      public override void  Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);

         IClientCommand command = CommandFactory.CreateDataViewCommand(TaskTag, DataViewCommandType.ResetUserSort);
         task.DataviewManager.Execute(command);
      }
   }
}
