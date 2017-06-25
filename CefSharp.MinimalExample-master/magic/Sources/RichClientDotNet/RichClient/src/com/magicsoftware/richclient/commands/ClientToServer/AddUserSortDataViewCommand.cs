using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.tasks.sort;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// DataView Command for Add User Sort.
   /// </summary>
   class AddUserSortDataViewCommand : DataviewCommand
   {
      internal Sort Sort { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public AddUserSortDataViewCommand()
      {
         CommandType = DataViewCommandType.AddUserSort;
      }
   }
}
