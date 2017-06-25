using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// general class for event commands
   /// </summary>
   class EventCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      public String TaskTag { get; set; }
      internal int MagicEvent { get; private set; }
      internal int ClientRecId { get; set; }

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_EVENT; }
      }

      public EventCommand(int magicEvent)
      {
         MagicEvent = magicEvent;
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

         return helper.GetString();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      protected override bool ShouldSerialize
      {
         get
         {
            if (TaskTag != null && MGDataCollection.Instance.GetTaskByID(TaskTag) != null &&
                 !((Task)MGDataCollection.Instance.GetTaskByID(TaskTag)).KnownToServer)
               return false;

            return true;
         }
      }
   }
}
