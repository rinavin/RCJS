using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// command for various types of DataView output operation.
   /// </summary>
   class DataViewOutputCommand : DataviewCommand
   {
      internal int      Generation { get; set; }
      internal string   TaskVarNames { get; set; }
      internal int      DestinationDataSourceNumber { get; set; }
      internal string   DestinationDataSourceName { get; set; }
      internal string   DestinationColumns { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public DataViewOutputCommand(DataViewCommandType OutputCommandType)
      {
         CommandType = OutputCommandType;
      }
   }
}
