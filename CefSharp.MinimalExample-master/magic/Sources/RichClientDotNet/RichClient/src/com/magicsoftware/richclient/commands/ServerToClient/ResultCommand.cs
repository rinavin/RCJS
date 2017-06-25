using System;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   /// <summary>
   /// 
   /// </summary>
   class ResultCommand : ClientTargetedCommandBase
   {
      bool _isNull;
      StorageAttribute _attr = StorageAttribute.NONE;
      String _val; 

      /// <summary>
      /// 
      /// </summary>
      /// <param name="exp"></param>
      public override void Execute(IResultValue res)
      {
         // set value to 'global' Expression variable
         if (_isNull)
            res.SetResultValue(null, StorageAttribute.NONE);
         else
         {
            if (_val == null)
               _val = "";
            res.SetResultValue(_val, _attr);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="value"></param>
      public override void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_NULL:
               _isNull = (XmlParser.getInt(value) == 1);
               break;

            case ConstInterface.MG_ATTR_PAR_ATTRS:
               _attr = (StorageAttribute)value[0];
               break;

            case XMLConstants.MG_ATTR_VALUE:
               _val = XmlParser.unescape(value);
               break;

            default:
               base.HandleAttribute(attribute, value);
               break;
         }

      }
   }
}
