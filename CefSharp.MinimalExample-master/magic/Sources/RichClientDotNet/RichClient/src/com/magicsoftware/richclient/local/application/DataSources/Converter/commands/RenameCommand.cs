using System;
using System.Collections.Generic;
using System.Text;
using util.com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;

using com.magicsoftware.richclient.local.application.datasources.converter;

namespace com.magicsoftware.richclient.local.application.dataSources.converter.commands
{
   /// <summary>
   /// command for renaming table to new name
   /// </summary>
   internal class RenameCommand : IConvertCommand
   {
      DataSourceDefinition fromDataSourceDefinition;
      DataSourceDefinition toDataSourceDefinition;

      public RenameCommand(DataSourceDefinition fromDefinition, DataSourceDefinition toDefinition)
      {
         this.fromDataSourceDefinition = fromDefinition;
         this.toDataSourceDefinition = toDefinition;
      }

      public void Execute()
      {
         Logger.Instance.WriteSupportToLog("RenameCommand.Execute():>>>>> ", true);
         Logger.Instance.WriteSupportToLog(string.Format("RenameCommand.Execute(): renaming table {0} to {1}", 
                                           fromDataSourceDefinition.Name, toDataSourceDefinition.Name), true);

         GatewayCommandFileRename fileRenameCommand = GatewayCommandsFactory.CreateFileRenameCommand(fromDataSourceDefinition, toDataSourceDefinition, ClientManager.Instance.LocalManager);
         GatewayResult result = fileRenameCommand.Execute();

         if (!result.Success)
         {
            throw new DataSourceConversionFailedException(toDataSourceDefinition.Name, result.ErrorDescription);
         }

         Logger.Instance.WriteSupportToLog("RenameCommand.Execute():<<<< ", true);
      }
   }
}
