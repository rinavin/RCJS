using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.gatewaytypes.data;
using util.com.magicsoftware.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.commands
{
   class SQLExecuteLocalDataViewCommand : LocalDataViewCommandBase
   {
      private string dataBaseName;
      private string sqlStatement;
      public StorageAttribute[] storageAttributes;
      SQLExecuteCommand sqlExecuteCommand;

      public SQLExecuteLocalDataViewCommand(SQLExecuteCommand sqlExecuteCommand)
         : base(sqlExecuteCommand)
      {
         dataBaseName = ((SQLExecuteCommand)sqlExecuteCommand).DataSourceName;
         sqlStatement = ((SQLExecuteCommand)sqlExecuteCommand).SQLStatement;
         storageAttributes = ((SQLExecuteCommand)sqlExecuteCommand).StorageAttributes;
         this.sqlExecuteCommand = sqlExecuteCommand;
      }

      /// <summary>
      /// Execute SQL command
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         GatewayResult result = new GatewayResult();
         GatewayCommandSQLExecute gatewaySqlExecuteCommand = GatewayCommandsFactory.CreateGatewayCommandSQLExecute(LocalDataviewManager.LocalManager,
            dataBaseName, sqlStatement, storageAttributes, sqlExecuteCommand.DbFields);
         result = gatewaySqlExecuteCommand.Execute();
         sqlExecuteCommand.statementReturnedValues = gatewaySqlExecuteCommand.statementReturnedValues;

         return result;
      }
   }
}
