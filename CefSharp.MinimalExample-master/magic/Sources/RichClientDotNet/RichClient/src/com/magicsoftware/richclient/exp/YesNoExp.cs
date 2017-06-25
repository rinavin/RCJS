using System;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.exp
{
   /// <summary>
   ///   YesNoExp class represent a Yes/No/Expression combination
   /// </summary>
   internal class YesNoExp
   {
      private Expression _exp;
      private bool _val;

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "defaultVal">the default value of this item</param>
      internal YesNoExp(bool defaultVal)
      {
         _val = defaultVal;
         _exp = null;
      }

      /// <summary>
      ///   set the value of the Yes/No/Expression
      /// </summary>
      /// <param name = "task">the task to look for expression</param>
      /// <param name = "strVal">the string that contains the Yes/No/Expression</param>
      internal void setVal(Task task, String strVal)
      {
         switch (strVal[0])
         {
            case 'Y':
               _val = true;
               break;

            case 'N':
               _val = false;
               break;

            default:
               int expId = XmlParser.getInt(strVal);
               if (task != null)
                  _exp = task.getExpById(expId);
               break;
         }
      }

      /// <summary>
      ///   returns the value of the Yes/No/Expression
      /// </summary>
      internal bool getVal()
      {
         if (_exp != null)
            return DisplayConvertor.toBoolean(_exp.evaluate(StorageAttribute.BOOLEAN, 0));
         return _val;
      }

      /// <summary>
      ///   returns true if an expression exists and it is a server side one
      /// </summary>
      internal bool isServerExp()
      {
         if (_exp != null)
            return _exp.computedByServerOnly();
         return false;
      }

      /// <summary>
      ///   returns true if an expression exists and it is a client side one
      /// </summary>
      internal bool isClientExp()
      {
         if (_exp != null)
            return _exp.computedByClientOnly();
         return false;
      }
   }
}
