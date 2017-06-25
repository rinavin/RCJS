using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// ClientDbDisconnectCommand
   /// </summary>
   class ClientDbDisconnectCommand : DataviewCommand
   {
      public string DataBaseName {get; set;}

      public ClientDbDisconnectCommand(string databaseName)
      {
         DataBaseName = databaseName;
         CommandType = DataViewCommandType.DbDisconnect;
      }
   }
}
