using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class RecomputeUnitDataviewCommand : DataviewCommand
   {  
      /// <summary>
      /// id of fetched link
      /// </summary>
      internal RecomputeId UnitId { get; set; }
      
      /// <summary>
      /// client id
      /// </summary>
      internal int ClientRecId { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public RecomputeUnitDataviewCommand()
      {
         CommandType = DataViewCommandType.RecomputeUnit;
      }
   }
}
