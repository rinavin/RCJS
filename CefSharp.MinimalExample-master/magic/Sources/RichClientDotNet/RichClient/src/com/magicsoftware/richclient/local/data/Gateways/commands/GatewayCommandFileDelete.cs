using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// Class for File Delete Command.
   /// </summary>
   public class GatewayCommandFileDelete : GatewayCommandBase
   {
      #region Properties
      public String FileName { get; set; }
      #endregion

      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();

         DatabaseDefinition dbDefinition = (DatabaseDefinition)DbDefinition.Clone();
         UpdateDataBaseLocation(dbDefinition);

         result.ErrorCode = GatewayAdapter.Gateway.FileDelete(DataSourceDefinition, dbDefinition, FileName);
         
         SetErrorDetails(result);

         return result;
      }
   }
}
