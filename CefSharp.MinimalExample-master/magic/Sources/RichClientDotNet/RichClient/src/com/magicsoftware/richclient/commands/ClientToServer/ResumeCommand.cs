using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class ResumeCommand : ClientOriginatedCommand
   {
      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_RESUME; }
      }
   }
}
