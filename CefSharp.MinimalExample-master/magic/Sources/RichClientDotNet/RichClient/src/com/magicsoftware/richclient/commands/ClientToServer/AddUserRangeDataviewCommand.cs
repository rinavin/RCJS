using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// command for the RangeAdd function
   /// </summary>
   class AddUserRangeDataviewCommand : DataviewCommand
   {
      internal UserRange Range { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public AddUserRangeDataviewCommand()
      {
         CommandType = DataViewCommandType.AddUserRange;
      }
   }
}
