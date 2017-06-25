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
   class LocateQueryEventCommand : EventCommand
   {
      internal int FldId { get; set; }
      internal int DitIdx { get; set; }
      internal string IncrmentalSearchString { get; set; }
      internal bool ResetIncrementalSearch { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public LocateQueryEventCommand() : base(InternalInterface.MG_ACT_RTO_LOCATE)
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
         helper.SerializeFldId(FldId);
         helper.SerializeMagicEvent(MagicEvent);
         helper.SerializeDitIdx(DitIdx);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_SEARCH_STR, IncrmentalSearchString);

         if (ResetIncrementalSearch)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_RESET_SEARCH, "1");

         return helper.GetString();
      }
   }
}
