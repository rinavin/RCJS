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
   class IndexChangeEventCommand : EventCommand
   {
      internal int KeyIndex { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public IndexChangeEventCommand() : base(InternalInterface.MG_ACT_INDEX_CHANGE)
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
         helper.SerializeKeyIndex(KeyIndex);

         return helper.GetString();
      }
   }
}
