using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.tasks.sort
{
   //This will hold runtime sorts added on task.
   internal class SortCollection
   {
      private readonly List<Sort> _sortTab;

      /// <summary>
      ///   CTOR
      /// </summary>
      internal SortCollection()
      {
         _sortTab = new List<Sort>();
      }

      /// <summary>
      ///   parse input string and fill inner data : Vector RTSortTab
      /// </summary>
      internal void fillData(Task tsk)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag(), tsk))
         {
         }
      }

      /// <summary>
      ///   allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">possible  tag name, name of object, which need be allocated </param>
      internal bool initInnerObjects(XmlParser parser, String foundTagName, Task tsk)
      {
         if (foundTagName == null)
            return false;

         switch (foundTagName)
         {

            case ConstInterface.MG_TAG_SORTS:
               parser.setCurrIndex(
                      parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               //end of outer tag and its ">"
               break;

            case ConstInterface.MG_TAG_SORT:
               {
                  Sort sort = new Sort();
                  _sortTab.Add(sort);
                  int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());

                  if (endContext != -1 && endContext < parser.getXMLdata().Length)
                  {
                     //last position of its tag
                     String tag = parser.getXMLsubstring(endContext);
                     parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_SORT) + ConstInterface.MG_TAG_SORT.Length);

                     List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
                     // parse each column
                     InitElements(tokensVector, sort);
                     parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); //to delete "/>" too
                  }
               }
               break;
            case ConstInterface.MG_TAG_SORTS_END:
               parser.setCurrIndex2EndOfTag();
               return false;

         }
         return true;
      }

      /// <summary>
      ///   used to parse each column xml tag attributes the column represents a segment in the key
      /// </summary>
      private void InitElements(List<String> tokensVector, Sort sort)
      {
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case XMLConstants.MG_ATTR_ID: // sort segment i.e. fldIdx
                  sort.fldIdx = XmlParser.getInt(valueStr);
                  break;

               case ConstInterface.MG_ATTR_DIR: // direction of sort segment
                  sort.dir = valueStr.Equals("A") ? true : false;
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
                  break;
            }
         }
      }

      /// <summary>
      ///   get user event by its index in the vector
      /// </summary>
      /// <param name = "idx">the index of the requested event </param>
      internal Sort getSort(int idx)
      {
         if (idx < 0 || _sortTab == null || idx >= _sortTab.Count)
            return null;
         return _sortTab[idx];
      }

      /// <summary>
      ///   get size of the sort table
      /// </summary>
      protected internal int getSize()
      {
         if (_sortTab == null)
            return 0;
         return _sortTab.Count;
      }

      /// <summary>
      ///   add Sort to  Sort collection.
      /// </summary>
      /// <param name = "sort">Sort to be added </param>
      protected internal void add(Sort sort)
      {
         _sortTab.Add(sort);
      }


   }
}
