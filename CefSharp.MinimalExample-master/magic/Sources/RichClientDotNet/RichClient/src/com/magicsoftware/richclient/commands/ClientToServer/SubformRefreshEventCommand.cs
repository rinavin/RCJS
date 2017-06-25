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
   class SubformRefreshEventCommand : RefreshEventCommand
   {
      internal string SubformTaskTag { get; set; }
      internal bool ExplicitSubformRefresh { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public SubformRefreshEventCommand()
         : base(InternalInterface.MG_ACT_SUBFORM_REFRESH)
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
         helper.SerializeRefreshMode(RefreshMode);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_SUBFORM_TASK, SubformTaskTag);

         return helper.GetString();
      }
   }
}
