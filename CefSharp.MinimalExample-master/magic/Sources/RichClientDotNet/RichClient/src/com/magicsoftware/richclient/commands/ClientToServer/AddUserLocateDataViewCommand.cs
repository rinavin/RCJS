using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// DataView Command for Add user locates.
   /// </summary>
   class AddUserLocateDataViewCommand : AddUserRangeDataviewCommand
   {
      /// <summary>
      /// CTOR
      /// </summary>
      public AddUserLocateDataViewCommand()
      {
         CommandType = DataViewCommandType.AddUserLocate;
      }
   }
}
