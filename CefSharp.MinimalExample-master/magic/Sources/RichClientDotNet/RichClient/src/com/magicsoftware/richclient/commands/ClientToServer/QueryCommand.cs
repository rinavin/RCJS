using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// base class for query commands
   /// </summary>
   abstract class QueryCommand : ClientOriginatedCommand
   {
      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_QUERY; }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         StringBuilder message = new StringBuilder();

         message.Append(" " + ConstInterface.MG_ATTR_VAL_QUERY_TYPE + "=\"");

         message.Append(SerializeQueryCommandData());

         return message.ToString();
      }

      protected abstract string SerializeQueryCommandData();

      protected override bool ShouldSerializeRecords
      {
         get
         {
            return false;
         }
      }
   }
}
