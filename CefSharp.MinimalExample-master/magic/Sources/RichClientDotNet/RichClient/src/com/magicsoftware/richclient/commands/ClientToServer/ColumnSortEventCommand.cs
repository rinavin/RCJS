using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// column sort command
   /// </summary>
   class ColumnSortEventCommand : EventCommand
   {
      internal int FldId { get; set; }
      internal int DitIdx { get; set; }
      internal int Direction { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public ColumnSortEventCommand() : base(InternalInterface.MG_ACT_COL_SORT)
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_DIRECTION, Direction);

         return helper.GetString();
      }
   }
}
