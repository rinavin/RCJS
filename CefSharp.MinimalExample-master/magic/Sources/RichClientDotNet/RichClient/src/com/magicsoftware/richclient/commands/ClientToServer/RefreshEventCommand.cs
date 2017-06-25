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
   class RefreshEventCommand : EventCommand
   {
      internal ViewRefreshMode RefreshMode { get; set; }
      internal bool KeepUserSort { get; set; }
      internal int CurrentRecordRow { get; set; }

      internal bool IsInternalRefresh { get; set; }

      public RefreshEventCommand(int magicEvent) : base(magicEvent)
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
         if (!IsInternalRefresh)
            helper.SerializeRefreshMode(RefreshMode);
         if (KeepUserSort)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_KEEP_USER_SORT,"1");

         return helper.GetString();
      }
   }
}
