using System;
using System.Collections.Generic;
using System.Text;
using util.com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.local.application.datasources.converter;

namespace com.magicsoftware.richclient.local.application.dataSources.converter.commands
{
   /// <summary>
   /// command for deleting table
   /// </summary>
   internal class DeleteCommand : IConvertCommand
   {
      DataSourceDefinition dataSourceDefinition;

      public DeleteCommand(DataSourceDefinition definition)
      {
         this.dataSourceDefinition = definition;
      }

      public void Execute()
      {
         Logger.Instance.WriteSupportToLog("DeleteCommand.Execute():>>>>> ", true);
         Logger.Instance.WriteSupportToLog(string.Format("DeleteCommand.Execute(): deleting table {0}", dataSourceDefinition.Name), true);

         GatewayCommandFileDelete fileDeleteCommand = GatewayCommandsFactory.CreateFileDeleteCommand(dataSourceDefinition.Name, dataSourceDefinition, ClientManager.Instance.LocalManager);
         GatewayResult result = fileDeleteCommand.Execute();

         if (!result.Success && result.ErrorCode != GatewayErrorCode.FileNotExist)
         {
            throw new DataSourceConversionFailedException(dataSourceDefinition.Name, result.ErrorDescription);
         }

         Logger.Instance.WriteSupportToLog("DeleteCommand.Execute():<<<< ", true);
      }

   }
}
   