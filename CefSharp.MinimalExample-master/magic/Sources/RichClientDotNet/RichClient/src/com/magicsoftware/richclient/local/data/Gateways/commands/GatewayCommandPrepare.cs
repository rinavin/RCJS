using System;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandPrepare : GatewayCommandBase
   {
      internal override GatewayResult Execute()
      {
         Record();
         GatewayAdapterCursor gatewayAdapterCursor = new GatewayAdapterCursor(RuntimeCursor.CursorDefinition);
         gatewayAdapterCursor.CursorType = CursorType.Regular;

         GatewayResult result = new GatewayResult();

         DatabaseDefinition dbDefinition = (DatabaseDefinition)DbDefinition.Clone();
         UpdateDataBaseLocation(dbDefinition);

         result.ErrorCode = GatewayAdapter.Gateway.CrsrPrepare(gatewayAdapterCursor, dbDefinition);

         if (result.Success)
            GatewayAdapter.AddCursor(RuntimeCursor, gatewayAdapterCursor);
         // db_rng_val_alloc ??
         SetErrorDetails(result);
         return result;
      }
   }
}
