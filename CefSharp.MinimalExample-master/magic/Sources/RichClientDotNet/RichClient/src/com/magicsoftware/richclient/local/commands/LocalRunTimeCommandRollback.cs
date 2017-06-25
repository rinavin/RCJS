using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// local command to rallback record
   /// </summary>
   class LocalRunTimeCommandRollback : LocalRunTimeCommandBase
   {
      string taskId;
      public RollbackEventCommand.RollbackType rollbackType { get; set; }

      public LocalRunTimeCommandRollback(RollbackEventCommand command)
      {
         taskId = command.TaskTag;
         rollbackType = command.Rollback;
      }

      internal override void Execute()
      {
         com.magicsoftware.richclient.tasks.MGDataCollection mgDataTab = com.magicsoftware.richclient.tasks.MGDataCollection.Instance;
         Task task = (Task)mgDataTab.GetTaskByID(taskId);
         task.TaskTransactionManager.CreateAndExecuteRollbackLocalTransaction(rollbackType);
      }
   }
}
