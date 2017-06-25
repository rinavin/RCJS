using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class BrowserEscEventCommand : EventCommand
   {
      internal bool ExitByMenu { get; set; }
      internal bool CloseSubformOnly { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public BrowserEscEventCommand() : base(InternalInterface.MG_ACT_BROWSER_ESC)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         CommandSerializationHelper helper = new CommandSerializationHelper();

         helper.SerializeTaskTag(TaskTag);
         helper.SerializeMagicEvent(MagicEvent);
         if(ExitByMenu)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_EXIT_BY_MENU, "1");

         helper.SerializeAttribute(ConstInterface.MG_ATTR_OBJECT, "1");
         helper.SerializeCloseSubformOnly(CloseSubformOnly);

         return helper.GetString();
      }
   }
}
