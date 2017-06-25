using System;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandRelease : GatewayCommandBase
   {
      internal override GatewayResult Execute()
      {
         Record();
         GatewayResult result = new GatewayResult();
         GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
         if (gatewayAdapterCursor != null)
         {
            // TODO: for join cursor CrsrReleaseJoin
            result.ErrorCode = GatewayAdapter.Gateway.CrsrRelease(gatewayAdapterCursor);
            GatewayAdapter.RemoveCursor(RuntimeCursor);
         }
         SetErrorDetails(result);
         return result;
      }
   }
}
