using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class MenuCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      public String TaskTag { get; set; }
      internal int MenuUid { get; set; }
      internal int MenuComp { get; set; }

      internal string MenuPath { get; set; }
      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_MENU; }
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_MNUUID, MenuUid);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_MNUCOMP, MenuComp);

         //serialize  menu path
         helper.SerializeAttribute(ConstInterface.MG_ATTR_MNUPATH, MenuPath);

         return helper.GetString();
      }
   }
}
