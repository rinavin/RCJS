using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class IniputForceWriteCommand : ClientOriginatedCommand
   {
      internal string Text { get; set; }

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_INIPUT_FORCE_WRITE; }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         return " " + ConstInterface.MG_ATTR_VAL_INIPUT_PARAM + "=\"" + XmlParser.escape(Text) + "\"";
      }

      /// <summary>
      /// 
      /// </summary>
      protected override bool ShouldSerializeRecords
      {
         get
         {
            return false;
         }
      }
   }
}
