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
   class SubformOpenEventCommand : EventCommand
   {
      internal int DitIdx { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public SubformOpenEventCommand()
         : base(InternalInterface.MG_ACT_SUBFORM_OPEN)
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
         helper.SerializeDitIdx(DitIdx);

         return helper.GetString();
      }
   }
}
