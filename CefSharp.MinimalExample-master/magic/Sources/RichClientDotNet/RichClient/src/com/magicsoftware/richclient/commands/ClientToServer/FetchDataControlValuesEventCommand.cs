using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;

namespace RichClient.src.com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// FetchDataControlValuesEventCommand
   /// </summary>
   class FetchDataControlValuesEventCommand : EventCommand
   {
      internal string ControlName {get;set;}
      public FetchDataControlValuesEventCommand() : base(InternalInterface.MG_ACT_FETCH_DATA_CONTROL_VALUES)
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_CONTROL_NAME, ControlName);
         return helper.GetString();
      }
   }
}
