using System;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class AbortNonOfflineTasksCommand : ClientOriginatedCommand
   {
      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_ABORT_NON_OFFLINE_TASKS; }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         CommandSerializationHelper helper = new CommandSerializationHelper();

         helper.SerializeAttribute(ConstInterface.MG_ATTR_SHOULD_PRE_PROCESS, "Y");

         return helper.GetString();
      }
   }
}
