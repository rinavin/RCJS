using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public abstract class GatewayCommandFetchBase : GatewayCommandBase
   {
      /// <summary>
      /// fm_flds_mg_4_db
      /// Converts a record from gateway to magic presentation
      /// </summary>
      protected void ConvertToRuntime(GatewayAdapterCursor gatewayAdapterCursor)
      {
         FieldValues currentRecord = gatewayAdapterCursor.CurrentRecord;
         FieldValues runtimeRecord = RuntimeCursor.RuntimeCursorData.CurrentValues;

         for (int i = 0; i < currentRecord.Count; i++)
         {
            FieldValue fieldValue = runtimeRecord[i];
            fieldValue.IsNull = currentRecord.IsNull(i);
            if (!fieldValue.IsNull)
            {
               fieldValue.Value = GatewayAdapter.StorageConvertor.ConvertGatewayToRuntimeField(gatewayAdapterCursor.Definition.FieldsDefinition[i],
                                                                                currentRecord[i].Value);
            }
         }
      }
   }
}
