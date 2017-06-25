using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// open the transaction in the gateway
   /// </summary>
   public class GatewayCommandOpenTransaction : GatewayCommandBase
   {
      internal override GatewayResult Execute()
      {
         GatewayResult gatewayResult = new GatewayResult();

         // must set the DatabaseType, the getter of property GatewayAdapter is used it.
         this.DatabaseType = GatewaysManager.DATA_SOURCE_DATATYPE_LOCAL;
         bool hasDataSourceDefinition = GatewayAdapter.HasDataSourceDefinition();

         // only if there is data source opened then open transaction
         if (hasDataSourceDefinition)
         {
             this.DataSourceDefinition = GatewayAdapter.GetFirstDataSourceDefinition();
             Record();

             // save the Data base definition on static so while close the transaction we will use it
             GatewayAdapter.TransactionWasOpned = hasDataSourceDefinition;

             // call the gateway command 
             gatewayResult.ErrorCode = GatewayAdapter.Gateway.Trans((int)TransactionModes.OpenWrite);
             SetErrorDetails(gatewayResult);
         }

         return gatewayResult;
      }
   }
}
