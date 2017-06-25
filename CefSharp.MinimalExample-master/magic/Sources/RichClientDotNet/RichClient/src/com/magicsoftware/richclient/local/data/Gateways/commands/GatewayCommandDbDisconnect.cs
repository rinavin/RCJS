using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// GatewayCommandDbDisconnect
   /// </summary>
   public class GatewayCommandDbDisconnect : GatewayCommandBase
   {
      private string tableName = string.Empty;

      /// <summary>
      /// Execute DbDisconnect command.
      /// </summary>
      /// <returns></returns>
      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();

         try
         {
            if (DbDefinition != null)
            {
               DatabaseDefinition dbDefinition = (DatabaseDefinition)DbDefinition.Clone();
               UpdateDataBaseLocation(dbDefinition);

               result.ErrorCode = GatewayAdapter.Gateway.DbDisconnect(dbDefinition.Location, out tableName);
            }
            else
            {
               result.ErrorCode = GatewayErrorCode.DatasourceNotExist;
            }
         }
         catch
         {
            throw new NotImplementedException();
         }

         SetErrorDetails(result);

         return result;
      }

      protected override void SetErrorDetails(GatewayResult result)
      {
         base.SetErrorDetails(result);

         if (result.ErrorCode == GatewayErrorCode.DatasourceOpen)
         {
            result.ErrorParams[0] = ", data source: " + tableName;
         }
      }
   }
}
