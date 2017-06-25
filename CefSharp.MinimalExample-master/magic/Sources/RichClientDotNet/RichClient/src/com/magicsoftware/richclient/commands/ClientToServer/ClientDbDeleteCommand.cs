using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// Command to delete the local DataSource using data source index.
   /// </summary>
   class ClientDbDeleteCommand : DataviewCommand
   {
      public int DataSourceNumber { get; set; }
      public string DataSourceName { get; set; }

      public ClientDbDeleteCommand()
      {
         CommandType = DataViewCommandType.DbDelete;
      }
   }
}
