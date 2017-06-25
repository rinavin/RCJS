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
   class NonReversibleExitEventCommand : EventCommand
   {
      internal bool CloseSubformOnly { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public NonReversibleExitEventCommand() : base(InternalInterface.MG_ACT_EXIT)
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_REVERSIBLE, "0");
         helper.SerializeCloseSubformOnly(CloseSubformOnly);

         return helper.GetString();
      }
   }
}
