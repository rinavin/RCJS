using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class GlobalParamsQueryCommand : QueryCommand
   { 
      protected override string SerializeQueryCommandData()
      {
         return ConstInterface.MG_ATTR_VAL_QUERY_GLOBAL_PARAMS + "\"";
      }
   }
}
