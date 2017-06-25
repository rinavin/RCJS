using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.unipaas.management.data;
using System.Xml.Serialization;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// fm_crsr_update
   /// </summary>
   public class GatewayCommandCursorUpdateRecord : GatewayCommandBase
   {     

      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();
         
         Record();
         result = CheckIsFlagSet(CursorProperties.Update);

         if (result.Success)
         {
            GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
            Debug.Assert(gatewayAdapterCursor != null);

            // 2. fm_flds_mg_2_db
            ConvertToGateway(gatewayAdapterCursor);

            // 3. ///*Update/Delete Stmt chngs copying update fld list from mg_crsr into db_crsr*/???

            // 4. update the record into data view

            //If partofdatetime is set and only time field is updated then before going to gateway set IsFieldUpdated of date field to true.
            for (int idx = 0; idx < (int)gatewayAdapterCursor.Definition.FieldsDefinition.Count; idx++)
            {
               DBField fld = gatewayAdapterCursor.Definition.FieldsDefinition[idx];
               if (fld.Storage == FldStorage.TimeString && fld.PartOfDateTime != 0 && gatewayAdapterCursor.Definition.IsFieldUpdated[idx])
               {
                  int dateIdx = 0;
                  for (dateIdx = 0; dateIdx < gatewayAdapterCursor.Definition.FieldsDefinition.Count; dateIdx++)
                  {
                     if (DataSourceDefinition.Fields[gatewayAdapterCursor.Definition.FieldsDefinition[dateIdx].IndexInRecord].Isn == fld.PartOfDateTime)
                        break;
                  }

                  gatewayAdapterCursor.Definition.IsFieldUpdated[dateIdx] = true;
               }
            }

            result.ErrorCode = GatewayAdapter.Gateway.CrsrUpdate(gatewayAdapterCursor);
            SetErrorDetails(result);
         }
         return result;
      }
     
   }
}
