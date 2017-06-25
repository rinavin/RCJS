using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// fm_crsr_delete
   /// </summary>
   public class GatewayCommandCursorDeleteRecord : GatewayCommandBase
   {
      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();

         Record();
         result = CheckIsFlagSet(CursorProperties.Delete);

         if (result.Success)
         {
            GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
            Debug.Assert(gatewayAdapterCursor != null);

            result.ErrorCode = GatewayAdapter.Gateway.CrsrDelete(gatewayAdapterCursor);
            SetErrorDetails(result);
         }
         return result;
      }    
   }
}
