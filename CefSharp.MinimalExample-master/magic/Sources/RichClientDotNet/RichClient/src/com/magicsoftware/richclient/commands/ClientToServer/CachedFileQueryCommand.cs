using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// query cached file
   /// </summary>
   class CachedFileQueryCommand : QueryCommand
   {
      internal String Text { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      protected override string SerializeQueryCommandData()
      {
         StringBuilder message = new StringBuilder();
         CommandSerializationHelper helper = new CommandSerializationHelper();

         message.Append(ConstInterface.MG_ATTR_VAL_QUERY_CACHED_FILE + "\"");

         helper.SerializeAttribute(ConstInterface.MG_ATTR_FILE_PATH, Text);

         message.Append(helper.GetString());

         return message.ToString();
      }
   }
}
