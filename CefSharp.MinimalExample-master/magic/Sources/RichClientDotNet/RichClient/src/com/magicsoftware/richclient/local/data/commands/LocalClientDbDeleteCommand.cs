using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.gatewaytypes.data;
using util.com.magicsoftware.util;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// LocalClientDbDeleteCommand
   /// </summary>
   class LocalClientDbDeleteCommand: LocalDataViewCommandBase
   {
      private ClientDbDeleteCommand clientDbDelCommand;
      public LocalClientDbDeleteCommand(ClientDbDeleteCommand clientDbDelCommand)
         : base(clientDbDelCommand)
      {
         this.clientDbDelCommand = clientDbDelCommand;
      }

      /// <summary>
      /// Execute File delete gateway command.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new ReturnResult();
         bool exist = false;
         int dataSourceNumber = clientDbDelCommand.DataSourceNumber;
         string dataSourceName = clientDbDelCommand.DataSourceName;

         DataSourceDefinition dataSourceDefintion = ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.GetDataSourceDefinition(Task.getCtlIdx(), dataSourceNumber);

         //If Wrong Data Source Number is Given
         if (dataSourceDefintion == null)
         {
            Logger.Instance.WriteExceptionToLog("ClientDbDel - Invalid Data Source Number");
            result = new ReturnResult(MsgInterface.STR_CLIENT_DB_DEL_OPERATION_FAILED);
         }
         else
         {
            if (string.IsNullOrEmpty(dataSourceName))
            {
               dataSourceName = dataSourceDefintion.Name;
            }
            //Check for Table exist or not.
            if (dataSourceDefintion.CheckExist == 'N')
            {
               exist = true;
            }
            else
            {
               GatewayCommandFileExist dbFileExistCommand = GatewayCommandsFactory.CreateFileExistCommand(dataSourceName, dataSourceDefintion, LocalDataviewManager.LocalManager);
               result = dbFileExistCommand.Execute();
               exist = result.Success;
            }

            //If Table exists then go for file delete operation.
            if (exist)
            {
               GatewayCommandFileDelete dbDeleteCommand = GatewayCommandsFactory.CreateFileDeleteCommand(dataSourceName, dataSourceDefintion, LocalDataviewManager.LocalManager);

               if (!dbDeleteCommand.Execute().Success)
               {
                  Logger.Instance.WriteExceptionToLog("ClientDbDel - Cannot delete Table");
                  result = new ReturnResult(MsgInterface.STR_CLIENT_DB_DEL_OPERATION_FAILED);
               }
            }
            else
            {
               Logger.Instance.WriteExceptionToLog(string.Format("ClientDbDel - {0}", result.ErrorDescription));
               result = new ReturnResult(MsgInterface.STR_CLIENT_DB_DEL_OPERATION_FAILED);
            }
         }

         return result;
      }
   }
}
