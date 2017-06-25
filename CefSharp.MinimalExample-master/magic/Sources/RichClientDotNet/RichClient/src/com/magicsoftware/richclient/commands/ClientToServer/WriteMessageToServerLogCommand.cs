using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// Command which will write error messages into server log file.
   /// </summary>
   class WriteMessageToServerLogCommand : EventCommand
   {
      public string ErrorMessage { get; set; }

      public WriteMessageToServerLogCommand() : base(InternalInterface.MG_ACT_WRITE_ERROR_TO_SERVER_LOG)
      {

      }
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         CommandSerializationHelper helper = new CommandSerializationHelper();

         helper.SerializeTaskTag(TaskTag);
         helper.SerializeMagicEvent(MagicEvent);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_ERROR_MESSAGE, ErrorMessage);

         return helper.GetString();
      }
   }
}
