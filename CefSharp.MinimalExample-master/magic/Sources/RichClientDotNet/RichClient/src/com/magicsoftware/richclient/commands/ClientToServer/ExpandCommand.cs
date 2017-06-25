using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class ExpandCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      public String TaskTag { get; set; }
      internal String TreeIsNulls { get; set; } //are values of parent nodes nulls
      internal String TreePath { get; set; } //rec ids of parents of node
      internal String TreeValues { get; set; } //values of parents of node

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_EXPAND; }
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_PATH, TreePath);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_VALUES, TreeValues);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_TREE_IS_NULLS, TreeIsNulls);

         return helper.GetString();
      }
   }
}
