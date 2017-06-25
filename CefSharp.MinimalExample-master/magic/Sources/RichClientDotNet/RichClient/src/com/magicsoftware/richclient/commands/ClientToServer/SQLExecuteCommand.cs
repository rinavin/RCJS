using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// Command to execute the provided SQL statement
   /// </summary>
   class SQLExecuteCommand : DataviewCommand
   {
      public string DataSourceName { get; set; }
      public string SQLStatement { get; set; }
      public StorageAttribute[] StorageAttributes { get; set; }
      public DBField[] DbFields { get; set; }
      public object[] statementReturnedValues;

      public SQLExecuteCommand()
      {
         CommandType = DataViewCommandType.SQLExecute;
      }
   }
}
