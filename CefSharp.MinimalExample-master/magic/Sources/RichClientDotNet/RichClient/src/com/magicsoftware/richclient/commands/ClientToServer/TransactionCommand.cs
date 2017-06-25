using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class TransactionCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      public String TaskTag { get; set; }
      internal char Oper { get; set; }
      internal bool ReversibleExit { get; set; }
      internal char Level { get; set; }

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_TRANS; }
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_OPER, Oper);
         if (!ReversibleExit)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_REVERSIBLE, "0");
         if (Level != 0)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_TRANS_LEVEL, Level);

         return helper.GetString();

      }
   }
}
