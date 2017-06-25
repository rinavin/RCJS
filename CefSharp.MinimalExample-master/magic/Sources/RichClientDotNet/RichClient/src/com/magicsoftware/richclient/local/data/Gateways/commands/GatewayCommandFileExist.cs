using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// Class for File Exist command.
   /// </summary>
   public class GatewayCommandFileExist : GatewayCommandBase
   {
      #region Properties
      public String FileName { get; set; }
      #endregion

      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();

         DatabaseDefinition dbDefinition = (DatabaseDefinition)DbDefinition.Clone();
         UpdateDataBaseLocation(dbDefinition);

         result.ErrorCode = GatewayAdapter.Gateway.FilExist(DataSourceDefinition, dbDefinition, FileName);

         SetErrorDetails(result);

         return result;
      }

      protected override void SetErrorDetails(GatewayResult result)
      {
         base.SetErrorDetails(result);

         if (!result.Success)
         {
            result.ErrorParams[0] = FileName;
         }
      }
   }
}
