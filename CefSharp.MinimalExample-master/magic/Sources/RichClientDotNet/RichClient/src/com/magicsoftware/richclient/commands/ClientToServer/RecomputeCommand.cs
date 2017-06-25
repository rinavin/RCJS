using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class RecomputeCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      public String TaskTag { get; set; }
      internal int FldId { get; set; }
      internal bool IgnoreSubformRecompute { get; set; }

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_RECOMP; }
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
         helper.SerializeFldId(FldId);
         if (IgnoreSubformRecompute)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_IGNORE_SUBFORM_RECOMPUTE, "1");

         return helper.GetString();
      }
   }
}
