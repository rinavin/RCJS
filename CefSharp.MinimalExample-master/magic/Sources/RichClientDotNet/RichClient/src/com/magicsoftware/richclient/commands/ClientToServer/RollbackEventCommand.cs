using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   class RollbackEventCommand : EventCommand
   {
      internal RollbackType Rollback {get; set;}

      protected override string CommandTypeAttribute
      {
         get
         {
            return base.CommandTypeAttribute;
         }
      }
      /// <summary>
      /// CTOR
      /// </summary>
      public RollbackEventCommand()
         : base(InternalInterface.MG_ACT_ROLLBACK)
      {
         Rollback = RollbackType.NONE;
      }

      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         CommandSerializationHelper helper = new CommandSerializationHelper();

         helper.SerializeTaskTag(TaskTag);
         helper.SerializeMagicEvent(MagicEvent);
         if (Rollback != RollbackType.NONE)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_ROLLBACK_TYPE,(char)Rollback);

         return helper.GetString();
      }

      internal enum RollbackType
      {
         NONE = ' ',
         CANCEL = 'C',
         ROLLBACK = 'R'
      } ;
   }
}
