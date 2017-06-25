using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandCursorClose : GatewayCommandBase
   {
      internal override GatewayResult Execute()
      {
         Record();
         GatewayResult result = new GatewayResult();

         GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
         Debug.Assert(gatewayAdapterCursor != null);

         result.ErrorCode = GatewayAdapter.Gateway.CrsrClose(gatewayAdapterCursor);

         if (result.Success)
         {
            // TODO: 
            // Free Blobs
         }
         SetErrorDetails(result);

         return result;
      }
   }
}
