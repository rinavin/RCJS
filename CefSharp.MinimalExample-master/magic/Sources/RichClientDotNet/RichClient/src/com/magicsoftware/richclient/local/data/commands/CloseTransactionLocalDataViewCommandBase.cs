using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.rt;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// prepare command
   /// </summary>
   internal class CloseTransactionLocalDataViewCommandBase : LocalDataViewCommandBase
   {
      public TransactionModes TransactionModes { get; set; }

      public CloseTransactionLocalDataViewCommandBase(IClientCommand command)
         : base(command)
      {
         TransactionModes = TransactionModes.Abort;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         GatewayResult gatewayResult = new GatewayResult();

         Transaction transaction = DataviewManager.Transaction.OwnerTask.DataviewManager.LocalDataviewManager.Transaction;

         // closeclose the transaction only if there is transaction that open
         if (TaskTransactionManager.LocalOpenedTransactionsCount > 0)
         {
            gatewayResult = CloseTransactionInGateway();
            if (gatewayResult.Success)
            {
               //Debug.WriteLine("Commit Transaction on task--> " + LocalDataviewManager.CurrentOpenTransaction.OwnerTask);
               transaction.Opened = false;
               TaskTransactionManager.LocalOpenedTransactionsCount--;

               // there is tasks that in local transaction, we must open the transaction again
               if (TaskTransactionManager.LocalOpenedTransactionsCount > 0)
                   gatewayResult = GatewayCommandsFactory.CreateGatewayCommandOpenTransaction(LocalManager).Execute();
             }
            else
               gatewayResult.ErrorCode = GatewayErrorCode.TransactionCommit;
         }
         return gatewayResult;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      private GatewayResult CloseTransactionInGateway()
      {
         GatewayCommandBase transactionCommand = new GatewayCommandCloseTransaction(TransactionModes);
         transactionCommand.LocalManager = LocalManager;

         GatewayResult gatewayResult = transactionCommand.Execute();
         return gatewayResult;
      }
   }
}
