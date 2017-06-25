using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandCursorGetCurrent : GatewayCommandFetchBase
   {
      internal override GatewayResult Execute()
      {
         Record();
         GatewayResult result = new GatewayResult();

         GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
         Debug.Assert(gatewayAdapterCursor != null);

         result.ErrorCode = GatewayAdapter.Gateway.CrsrGetCurr(gatewayAdapterCursor);

         ConvertToRuntime(gatewayAdapterCursor);
         RecordData();

         SetErrorDetails(result);

         return result;
      }
   }
}
