using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.rt;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// prepare command
   /// </summary>
   internal class RollbackTransactionLocalDataViewCommand : LocalDataViewCommandBase
   {
      public RollbackEventCommand Command  { get; set; }

      Task ownerTransactionTask { get; set; }

      public bool StopExecWasUpdated { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="command"></param>
      public RollbackTransactionLocalDataViewCommand(RollbackEventCommand command)
         : base(command)
      {
         this.Command = command;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         GatewayResult gatewayResult = new GatewayResult();

         Transaction ownerTransaction = null;

         if (TaskTransactionManager.LocalOpenedTransactionsCount > 0)
         {
            
            gatewayResult = RollbaclTransactionInGateway();
            if (gatewayResult.Success)
            {
               if (DataviewManager.Transaction != null)
               {
                  ownerTransactionTask = DataviewManager.Transaction.OwnerTask;
                  ownerTransaction = ownerTransactionTask.DataviewManager.LocalDataviewManager.Transaction;

                  //1.  calculate the current task , the task that was execute the rollback transaction (Command.taskgTag)
                  //Debug.WriteLine("Commit Transaction on task--> " + LocalDataviewManager.CurrentOpenTransaction.OwnerTask);
                  ownerTransactionTask.CancelAndRefreshCurrentRecordAfterRollback();
               }


               GatewayCommandBase transactionCommand = GatewayCommandsFactory.CreateGatewayCommandOpenTransaction(LocalManager);
               gatewayResult = transactionCommand.Execute(); 
            
               //2.	Close\refresh all tasks that are under the current transaction  
               switch (Command.Rollback)
               {
                  case RollbackEventCommand.RollbackType.CANCEL:
                     MGDataCollection.Instance.ForEachTask(new TaskDelegate(CancelAndRefreshCurrentRecordAfterRollback), ownerTransactionTask);
                     MGDataCollection.Instance.ForEachTask(new TaskDelegate(ViewRefreshAfterRollback), ownerTransactionTask);
                     break;
                  case RollbackEventCommand.RollbackType.ROLLBACK:
                     {
                        // fixed bug#294247 rollback the value in StopExecution
                        bool saveStopExec = ClientManager.Instance.EventsManager.GetStopExecutionFlag();
                        StopExecWasUpdated = false;
                        MGDataCollection.Instance.ForEachTask(new TaskDelegate(CancelAndRefreshCurrentRecordAfterRollback), null);
                        MGDataCollection.Instance.ForEachTask(new TaskDelegate(ViewRefreshAfterRollback), null);

                        if (ownerTransactionTask != null)
                           ownerTransactionTask.AbortDirectTasks();

                        if (StopExecWasUpdated)
                           ClientManager.Instance.EventsManager.setStopExecution(saveStopExec);
                     }
                     break;
                  default:
                     break;
               }                  
            }
            else
               gatewayResult.ErrorCode = GatewayErrorCode.TransactionAbort;
         }
         return gatewayResult;
      }

      /// <summary>
      /// refresh the current record and if need refresh the view 
      /// </summary>
      /// <param name="task"></param>
      void ViewRefreshAfterRollback(Task task, object ignoreTask)
      {
         if (!task.IsSubForm &&  !task.InEndTask && !task.isAborting())
         {
            if (task != (Task)ignoreTask)
            {
               //Fixed bug #172763, we need to refresh the view and the stop execution 
               task.ViewRefreshAfterRollback(true);

               // QCR #293210. If the transaction was opened in a son, do not stop execution of the parent.
               if (ignoreTask == null || task.isDescendentOf((Task)ignoreTask))
               {
                  // QCR #441251, for local task after rollback need to stop executed (see dataview.cs, we doing the same for non offline task)
                  bool stopExec= !((task.getMode() == Constants.TASK_MODE_DELETE || !task.IsInteractive));
                  ClientManager.Instance.EventsManager.setStopExecution(stopExec);

                  StopExecWasUpdated = true;
               }
            }
         }
      }

      /// <summary>
      /// refresh the current record and if need refresh the view 
      /// </summary>
      /// <param name="task"></param>
      void CancelAndRefreshCurrentRecordAfterRollback(Task task, object ignoreTask)
      {
         if (!task.InEndTask && !task.isAborting())
         {
            if (task != (Task)ignoreTask)
               task.CancelAndRefreshCurrentRecordAfterRollback();
         }
      }

         

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      private GatewayResult RollbaclTransactionInGateway()
      {
         GatewayCommandBase TransactionCommand = new GatewayCommandCloseTransaction(TransactionModes.Abort);
         TransactionCommand.LocalManager = LocalManager;

         GatewayResult gatewayResult = TransactionCommand.Execute();
         return gatewayResult;
      }
   }
}
