using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// close the transaction in the gateway
   /// </summary>
   public class GatewayCommandCloseTransaction : GatewayCommandBase
   {
      /// <summary>
      /// for command close transaction or rollback transaction the command that send to the gateway is the same 
      /// command with diffrent mode 
      /// Close  : TransactionModes.Commit
      /// Rollback : TransactionModes.Abort
      /// </summary>
      public TransactionModes TransactionModes { get; set; }

      /// <summary>
      /// 
      /// </summary>
      internal GatewayCommandCloseTransaction()
      {
         this.TransactionModes = TransactionModes.Commit;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="transactionModes"></param>
      internal GatewayCommandCloseTransaction(TransactionModes transactionModes)
      {
         this.TransactionModes = transactionModes;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override GatewayResult Execute()
      {
         GatewayResult gatewayResult = new GatewayResult();

         // must set the DatabaseType, the getter of property GatewayAdapter is used it.
         this.DatabaseType = GatewaysManager.DATA_SOURCE_DATATYPE_LOCAL;

         //check if TransactionWasOpned
         if (GatewayAdapter.TransactionWasOpned)
         {
            Record();

            // call the gateway command 
            gatewayResult.ErrorCode = GatewayAdapter.Gateway.Trans((int)TransactionModes);
            
            SetErrorDetails(gatewayResult);

            // reset the static member
            if (gatewayResult.Success)
               GatewayAdapter.TransactionWasOpned = false;
         }

         return gatewayResult;
      }
   }
}
