using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// open transaction
   /// </summary>
   internal class OpenTransactionLocalDataCommand  : LocalDataViewCommandBase
   {
      public OpenTransactionLocalDataCommand(DataviewCommand command)
         : base(command)
      { }


      /// <summary>
      /// open transaction
      /// if there is no transaction opened already then 
      ///     open and save it info on ClientManager.Instance.CurrentLocalTransactionOpened
      /// else
      ///     if (transaction that was opend is in my parent task)
      ///         OK
      ///     else
      ///         failed !!!
      /// 
      /// </summary>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase returnResultBase = new GatewayResult();

         Transaction transaction = DataviewManager.Transaction.OwnerTask.DataviewManager.LocalDataviewManager.Transaction;
         if (!transaction.isOpened())
         {
            // need to open in the gateway only if no opend before
             if (TaskTransactionManager.LocalOpenedTransactionsCount == 0)
                 returnResultBase = GatewayCommandsFactory.CreateGatewayCommandOpenTransaction(LocalManager).Execute();
            if (returnResultBase.Success)
            {
               transaction.Opened = true;
               TaskTransactionManager.LocalOpenedTransactionsCount++;

               //transaction.OwnerTask.TaskTransactionManager.SetCurrentOpenTransactionAsLocalTransaction();
               //Debug.WriteLine("Open Transaction on task--> " + LocalDataviewManager.CurrentOpenTransaction.OwnerTask);
            }
            
            //else
            //{
            //   // check if the open transaction is in the parent task
            //   if (!transaction.OwnerTask.isDescendentOf(LocalDataviewManager.CurrentOpenTransaction.OwnerTask))
            //      returnResultBase = new ReturnResult(MsgInterface.FMERROR_STR_TRANS_OPEN_FAILED);
            //}
         }
         return returnResultBase;         
      }
    
      
   }
}
