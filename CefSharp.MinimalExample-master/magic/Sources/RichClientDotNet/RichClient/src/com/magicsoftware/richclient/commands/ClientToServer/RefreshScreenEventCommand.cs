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
   class RefreshScreenEventCommand : EventCommand
   {
      internal int TopRecIdx { get; set; }
      internal ViewRefreshMode RefreshMode { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public RefreshScreenEventCommand() : base(InternalInterface.MG_ACT_RT_REFRESH_SCREEN)
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_OBJECT, TopRecIdx);
         helper.SerializeRefreshMode(RefreshMode);

         return helper.GetString();
      }
   }
}
