using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class EvaluateCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      public String TaskTag { get; set; }
      internal int ExpIdx { get; set; }
      internal StorageAttribute ExpType { get; set; }
      internal int LengthExpVal { get; set; }
      internal Task MprgCreator { get; set; }


      /// <summary>
      /// CTOR
      /// </summary>
      public EvaluateCommand()
      {
         LengthExpVal = Int32.MinValue;
      }

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_EVAL; }
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_EXP_IDX, ExpIdx);
         if (ExpType != StorageAttribute.NONE)
         {
            String maxDigits = "";
            if (LengthExpVal > 0)
               maxDigits += LengthExpVal;

            helper.SerializeAttribute(ConstInterface.MG_ATTR_EXP_TYPE, (char)ExpType + maxDigits);
         }
         if (MprgCreator != null)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_MPRG_SOURCE, MprgCreator.getTaskTag());

         return helper.GetString();
      }
   }
}
