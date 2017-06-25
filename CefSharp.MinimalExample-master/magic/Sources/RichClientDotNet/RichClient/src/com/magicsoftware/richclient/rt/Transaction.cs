using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.rt
{
   internal class Transaction
   {
      private readonly string _transId;

      private char _afterTransRetry = ConstInterface.RECOVERY_NONE;
                   //holds the recovery type of a failed transaction so we can ask if we are after retry.

      internal bool Opened { get; set; }
      internal Task OwnerTask {get; set;}
      private char _transBegin = (char) (0);

      // it is for relevant for local transaction, so it will be easy to debug 
      public int LocalTransactionId { get; set; }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "task">the task who owns this transaction </param>
      /// <param name="setTransId"></param>
      internal Transaction(Task task, string setTransId, bool isLocalTransaction)
      {
         OwnerTask = task;
         _transId = setTransId;
         _transBegin = ConstInterface.TRANS_TASK_PREFIX;

         // for local, set the id (we don't want to add a new class for now)
         if (isLocalTransaction)
            LocalTransactionId = ++TaskTransactionManager.LastOpendLocalTransactionId;
      }

      /// <summary>
      ///   returns true if the given task is the owner of this transaction
      /// </summary>
      /// <param name = "task">the task to check </param>
      internal bool isOwner(Task task)
      {
         return task == OwnerTask;
      }

      /// <summary>
      ///   close the transaction
      /// </summary>
      internal void close()
      {
         Opened = false;
      }

      /// <summary>
      ///   opens the transaction - should be called when an update occured
      /// </summary>
      internal void open()
      {
         Opened = true;
      }

      /// <summary>
      ///   returns true if the transaction was opened
      /// </summary>
      internal bool isOpened()
      {
         return Opened;
      }

      /// <summary>
      ///   signalls wheather we are now in the process of retrying a failed transaction
      /// </summary>
      internal void setAfterRetry(char val)
      {
         _afterTransRetry = val;
      }

      /// <summary>
      ///   return true if we are after a transaction retry
      /// </summary>
      internal bool getAfterRetry()
      {
         return _afterTransRetry != ConstInterface.RECOVERY_NONE;
         ;
      }

      /// <summary>
      ///   return true if we are after a transaction with recovery RECOVERY_RETRY
      /// </summary>
      internal bool getAfterRetry(char recovery)
      {
         return _afterTransRetry == recovery;
      }

      /// <summary>
      ///   returns the transaction level (Task/Record)
      /// </summary>
      internal char getLevel()
      {
         return _transBegin;
      }

      /// <summary>
      ///   sets the level of the Transaction
      /// </summary>
      internal void setTransBegin(char val)
      {
         _transBegin = val;
      }

      /// <summary>
      ///   gets transId
      /// </summary>
      internal string getTransId()
      {
         return _transId;
      }

      /// <summary>
      ///   sets Owner task
      /// </summary>
      internal void setOwnerTask(Task task)
      {
         OwnerTask = task;
      }
   }
}