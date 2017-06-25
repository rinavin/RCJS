using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data
{
   /// <summary>
   /// local transaction manager 
   /// </summary>
   internal class TaskTransactionManager 
   {
      public Task task { get; set; }

      // save the counter local current open transaction      
      static internal int LocalOpenedTransactionsCount { get; set; }
      
      //The LastLocalTransactionId that was opned 
      static internal int LastOpendLocalTransactionId { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      internal TaskTransactionManager(Task task)
      {
         this.task = task;
      }

      /// <summary>
      /// return true if at lest one local transaction opned 
      /// </summary>
      /// <returns></returns>
      internal static bool IsLocalTransactionOpned
      {
         get { return TaskTransactionManager.LocalOpenedTransactionsCount > 0; }
      }

      /// <summary>
      /// find the "deferred" local transaction
      /// it is not ready deferred because:  we have one local transaction that is opened. and each time
      ///                                    transaction is commited all changes on all tasks are saved into the data base
      /// </summary>
      /// <returns></returns>
      private Transaction FindTheLocalDeferredTransactionInBranch()
      {
         Transaction localDeferredTransaction = task.DataviewManager.LocalDataviewManager.Transaction;
         Task currentTask = task;

         while (currentTask != null)
         {
            Transaction currentLocalTransaction = currentTask.DataviewManager.LocalDataviewManager.Transaction;
            if (currentLocalTransaction != null)
               localDeferredTransaction = currentLocalTransaction;

            currentTask = currentTask.ParentTask;
         }

         return localDeferredTransaction;
      }

      /// <summary>
      ///   opens a new Transaction or set reference to the existing Transaction
      /// </summary>
      internal void SetLocalTransaction()
      {
         MGDataCollection mgdTab = MGDataCollection.Instance;
         Transaction transaction = null;

         if (task.DataviewManager.LocalDataviewManager.Transaction == null)
         {
            // 1. find the first task that have local transaction in the branch.
            transaction = FindTheLocalDeferredTransactionInBranch();

            // 2. if there is not transaction opned the , get transaction begin value and create a new transaction info 
            if (transaction == null)
            {
               char transBegin =  GetTransactionBeginValue(true);
               if (AllowTransaction(transBegin, true))
               {
                  transaction = new Transaction(task, task.getTaskTag(), true);
                  PrepareTransactionProperties(transaction, true);
               }
            }

            // 3. set the transaction member on the data view manager
            task.DataviewManager.LocalDataviewManager.Transaction = transaction;
         }
      }
          /// <summary>
      /// return true if the transaction begain supported 
      /// </summary>
      /// <returns></returns>
      internal bool AllowTransaction(char transBegin, bool forLocal)
      {
         if (forLocal)
            return transBegin == ConstInterface.TRANS_TASK_PREFIX ||
                transBegin == ConstInterface.TRANS_RECORD_PREFIX;
         else
            return (transBegin == ConstInterface.TRANS_TASK_PREFIX ||
               transBegin == ConstInterface.TRANS_RECORD_PREFIX ||
                transBegin == ConstInterface.TRANS_NONE);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void PrepareTransactionProperties(Transaction transaction, bool forLocal)
      {
         char transBegin = GetTransactionBeginValue(forLocal);
         if (transaction != null && AllowTransaction(transBegin, forLocal))
            transaction.setTransBegin(transBegin);
      }
    

      /// <summary>
      /// 
      /// </summary>
      /// <param name="forLocal"></param>
      /// <returns></returns>
      internal char GetTransactionBeginValue(bool forLocal)
      {
         char transBegin = ConstInterface.TRANS_NONE;

         Property prop = task.getProp(PropInterface.PROP_TYPE_TRASACTION_BEGIN);
         if (prop != null)
         {
            string propgetValue = (forLocal ? prop.StudioValue : prop.getValue());
            // while execute offline program without connection, the main program execute as offline task.
            // property transaction begain is not relevant 
            if (propgetValue != null)
               transBegin = propgetValue[0];
         }

         return transBegin;
      }

      /// <summary>
      ///  check if local transaction is opened then close with abort 
      ///  this method is called while server send us abort on the task 
      /// </summary>
      /// <param name="openIfTransLevel"></param>
      internal ReturnResult CheckAndAbortLocalTransaction(Task checkIsOwnerTask)
      {
         ReturnResult returnResult = new ReturnResult();

         Transaction currentTransaction = task.DataviewManager.LocalDataviewManager.Transaction;
         
         //While aborting task we need to rollback the transaction 
         if (currentTransaction != null && currentTransaction.isOwner(checkIsOwnerTask))
            returnResult = checkIsOwnerTask.TaskTransactionManager.CreateAndExecuteRollbackLocalTransaction(RollbackEventCommand.RollbackType.CANCEL);
         return returnResult;
      }


      /// <summary>
      ///  check if transaction need to be open and open it 
      /// </summary>
      /// <param name="openIfTransLevel"></param>
      internal ReturnResult CheckAndOpenLocalTransaction(char openIfTransLevel)
      {
         ReturnResult returnResult = new ReturnResult();

         if (task.DataviewManager.HasLocalData)
         {
            SetLocalTransaction();
            Transaction transaction = task.DataviewManager.LocalDataviewManager.Transaction;

            //Fixed bug#308092, open the transaction only if the task is the owner of the transaction 
            if (transaction != null && transaction.isOwner(task) && (openIfTransLevel == ConstInterface.TRANS_FORCE_OPEN || transaction.getLevel() == openIfTransLevel))
            {
               IClientCommand dataViewCommand = CommandFactory.CreateDataViewCommand(task.getTaskTag(), DataViewCommandType.OpenTransaction);
               returnResult = task.DataviewManager.LocalDataviewManager.Execute(dataViewCommand);
            }
         }
         return returnResult;
      }

      /// <summary>
      ///   check if all the conditions imply a commit and do the commit return true if a commit was executed
      /// </summary>
      /// <param name = "reversibleExit">if true then the task exit is non reversible</param>
      /// <param name = "level">of the dynamicDef.transaction to commit (task / record)</param>
      internal bool checkAndCommit(bool reversibleExit, char level, bool isAbort)
      {
         char oper = isAbort
                        ? ConstInterface.TRANS_ABORT
                        : ConstInterface.TRANS_COMMIT;

         ReturnResult result = ExecuteLocalUpdatesCommand();
         // is failed the return 
         if (!result.Success)
         {
            // definition : for non Interactive and we get problem with ExecuteLocalUpdates command we need to abort the task.
            if (!task.IsInteractive)
               task.abort();
            return false;
         }

         bool ret = checkAndCommitPerDataview(task.DataviewManager.RemoteDataviewManager, reversibleExit, level, oper, ref result);
         // close the local transaction only if the remote transaction success
         if (result.Success)
            checkAndCommitPerDataview(task.DataviewManager.LocalDataviewManager, reversibleExit, level, oper, ref result);


         /// relevant for remote only
         if (!ret)
         {
            // If we modified the record but didn't flush it yet even though we should have -
            // send it now.
            if (level == ConstInterface.TRANS_RECORD_PREFIX && ((DataView)task.DataView).modifiedRecordsNumber() > 0 &&
                ((DataView)task.DataView).FlushUpdates)
            {
               try
               {
                  task.TryingToCommit = true;
                  task.CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
               }
               finally
               {
                  task.TryingToCommit = false;
               }
            }
         }
         return false;
      }

      /// <summary>
      /// execute the local updfates command 
      /// </summary>
      /// <returns></returns>
      internal ReturnResult ExecuteLocalUpdatesCommand()
      {
         // execute local command
         IClientCommand dataViewExecuteLocalUpdatesCommand = CommandFactory.CreateDataViewCommand(task.getTaskTag(), DataViewCommandType.ExecuteLocalUpdates);
         ReturnResult result = task.DataviewManager.Execute(dataViewExecuteLocalUpdatesCommand);
         return result;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dataviewmanager"></param>
      /// <param name="reversibleExit"></param>
      /// <param name="level"></param>
      /// <param name="oper"></param>
      /// <returns></returns>
      internal bool checkAndCommitPerDataview(DataviewManagerBase dataviewmanager, bool reversibleExit, char level, char oper, ref ReturnResult result)
      {
         result = new ReturnResult();

         bool returnValue = false;

         Transaction currentTransaction = dataviewmanager.Transaction;

         if (currentTransaction != null && !task.isAborting() && currentTransaction.isOpened() &&
             currentTransaction.isOwner(task) && currentTransaction.getLevel() == level)
         {

            IClientCommand cmd = CommandFactory.CreateTransactionCommand(oper, task.getTaskTag(), reversibleExit, level);
            try
            {
               task.TryingToCommit = true;
               result = dataviewmanager.Execute(cmd);
            }
            finally
            // Don't catch any exception here. Just turn off the "tryingToCommit" flag.
            {
               task.TryingToCommit = false;
            }
            returnValue = true;
         }
         return returnValue;
      }

      /// <summary>
      /// create and execute command rollback transaction by send the RollbackType
      /// </summary>
      /// <param name="rollbackType"> if it is cancel we refresh the all open tasks under the same transaction
      ///                             if it is rollback we close the all open tasks under the same transaction
      ///                               </param>
      /// <returns></returns>
      internal ReturnResult CreateAndExecuteRollbackLocalTransaction(RollbackEventCommand.RollbackType rollbackType)
      {
         ReturnResult ReturnResult = new ReturnResult();       

         // if local transaction exist on the task take it from the transaction.ownerTask ;
         string taskTag = task.DataviewManager.LocalDataviewManager.Transaction != null ? task.DataviewManager.LocalDataviewManager.Transaction.OwnerTask.getTaskTag() : task.getTaskTag();

         // execute the rollback event command on the opened local transaction
         if (TaskTransactionManager.LocalOpenedTransactionsCount > 0)
         {
            IClientCommand command = CommandFactory.CreateRollbackEventCommand(taskTag, rollbackType);
            ReturnResult = ((Task)task).DataviewManager.LocalDataviewManager.Execute(command);
         }


         return ReturnResult;
      }

      /// <summary>
      // while we don't have transaction we must faild stack on the record(same as we doing for duplicate index)
      // so we create TransactionErrorHandlingsRetry and we use this dummy transaction
      /// </summary>
      /// <param name="transBegin"></param>
      /// <returns></returns>
      internal void HandelTransactionErrorHandlingsRetry(ref char transBegin)
      {
         Task task = this.task;
         if (task.Transaction == null && transBegin == ConstInterface.TRANS_NONE)
         {
            task.TransactionErrorHandlingsRetry = new Transaction(task, task.getTaskTag(), false);
            task.TransactionErrorHandlingsRetry.setTransBegin(ConstInterface.TRANS_RECORD_PREFIX);
            transBegin = ConstInterface.TRANS_RECORD_PREFIX;
         }
      }
   }
}
