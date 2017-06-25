using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.exp
{
   /// <summary>
   ///   data for <exptable> ...</exptable> tag
   /// </summary>
   internal class ExpTable
   {
      private readonly List<Expression> _exps = new List<Expression>(); //of Expression

      /// <summary>
      ///   All data inside <exptable> ...</exptable>
      ///   Function for filling own fields, allocate memory for inner objescts.
      ///   Parsing the input String.
      /// </summary>
      /// <param name = "task">to parent task</param>
      internal void fillData(Task task)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag(), task))
         {
         }
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">possible  tag name, name of object, which need be allocated</param>
      /// <param name = "task">to parent Task</param>
      private bool initInnerObjects(XmlParser parser, String foundTagName, Task task)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_EXP))
         {
            Expression expression = new Expression();
            expression.fillData(task);
            _exps.Add(expression);
         }
         else if (foundTagName.Equals(ConstInterface.MG_TAG_EXPTABLE))
         {
            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) +
                                   1); //end of outer tad and its ">"
         }
         else if (foundTagName.Equals('/' + ConstInterface.MG_TAG_EXPTABLE))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog("There is no such tag in ExpTable.initInnerObjects(): " +
                                                        foundTagName);
            return false;
         }
         return true;
      }

      /// <summary>
      ///   get Expression by its ID
      /// </summary>
      /// <param name = "id">the id of the expression </param>
      /// <returns> reference to Expression with id=ID </returns>
      internal Expression getExpById(int id)
      {
         Expression exp;

         if (id < 0)
            return null;

         for (int i = 0;
              i < _exps.Count;
              i++)
         {
            exp = _exps[i];
            if (exp.getId() == id)
               return exp;
         }
         return null;
      }

      /// <summary>
      ///   get a Expression by its index
      /// </summary>
      /// <param name = "idx">is the id of the requested field </param>
      /// <returns> a reference to the field </returns>
      internal Expression getExp(int idx)
      {
         Expression exp = null;

         if (idx >= 0 && idx < _exps.Count)
            exp = _exps[idx];

         return exp;
      }

      /// <summary>
      ///   return the number of Expressions in the table
      /// </summary>
      /// <returns> number of fields in the table </returns>
      internal int getSize()
      {
         int count = 0;

         if (_exps != null)
            count = _exps.Count;

         return count;
      }
   }
}
