using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   /// <summary>
   /// ResetRangeCommand
   /// </summary>
   class ResetRangeCommand : ClientTargetedCommandBase
   {
      /// <summary>
      /// Execute ResetRange command.
      /// </summary>
      /// <param name="res"></param>
      public override void  Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);

         IClientCommand command = CommandFactory.CreateDataViewCommand(TaskTag, DataViewCommandType.ResetUserRange);
         task.DataviewManager.Execute(command);
      }
   }
}
