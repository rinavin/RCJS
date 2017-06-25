using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandFileClose : GatewayCommandBase
   {
      internal override GatewayResult Execute()
      {
         Record();
         RTDataSource rtDataSource = GatewayAdapter.GetDataSource(DataSourceDefinition);
         GatewayResult result = new GatewayResult();

         // TODO: Access (HDLINFO hdl_info_tbl_.datatbl)

         if (rtDataSource != null)
         {
            if (rtDataSource.IsLast)
               result.ErrorCode = GatewayAdapter.Gateway.FileClose(DataSourceDefinition);

            if (result.Success)
               rtDataSource.Close();
            SetErrorDetails(result);
         }
         return result;
      }
   }
}
