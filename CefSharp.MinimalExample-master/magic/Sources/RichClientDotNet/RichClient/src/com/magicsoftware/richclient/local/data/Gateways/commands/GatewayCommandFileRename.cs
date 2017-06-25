using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// Class for File Rename command.
   /// </summary>
   public class GatewayCommandFileRename : GatewayCommandBase
   {
      #region Properties
      public DataSourceDefinition DestinationDataSourceDefinition { get; set; }
      #endregion
      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();

         DatabaseDefinition dbDefinition = (DatabaseDefinition)DbDefinition.Clone();
         UpdateDataBaseLocation(dbDefinition);

         result.ErrorCode = GatewayAdapter.Gateway.FileRename(DataSourceDefinition, DestinationDataSourceDefinition,  dbDefinition);
         SetErrorDetails(result);

         return result;
      }
   }
}
