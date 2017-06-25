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
   class ComputeEventCommand : EventCommand
   {
      internal bool Subforms{ get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public ComputeEventCommand() : base(InternalInterface.MG_ACT_COMPUTE)
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
         if(Subforms)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_OBJECT, "99999");

         return helper.GetString();
      }
   }
}
