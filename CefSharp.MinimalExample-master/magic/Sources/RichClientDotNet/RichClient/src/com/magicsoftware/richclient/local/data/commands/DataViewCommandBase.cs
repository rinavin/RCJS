using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// data view command base 
   /// </summary>
   internal abstract class DataViewCommandBase
   {
      /// <summary>
      /// dataview manager
      /// </summary>
      internal DataviewManagerBase DataviewManager { get; set; }

      /// <summary>
      /// execute the command
      /// </summary>
      internal abstract ReturnResultBase Execute();
   }
}
