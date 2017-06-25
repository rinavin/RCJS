using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.unipaas.management.data;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// fm_crsr_insert
   /// </summary>
   public class GatewayCommandCursorInsertRecord : GatewayCommandBase
   {

    
      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();

         Record();
         result = CheckIsFlagSet(CursorProperties.Insert);
         
         if (result.Success)
         {
            GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
            Debug.Assert(gatewayAdapterCursor != null);

            // fm_flds_mg_2_db
            ConvertToGateway(gatewayAdapterCursor);
            result.ErrorCode = GatewayAdapter.Gateway.CrsrInsert(gatewayAdapterCursor);
            SetErrorDetails(result);
         }
         return result;
      }
     
   }
}
