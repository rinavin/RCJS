using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.data;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   represents a table key and all it segments
   /// </summary>
   internal class Key
   {
      internal List<FieldDef> Columns { private set; get; } // a vector of Field object representing the fields of the table 
      private readonly TableCache _table;
      private int _id;

      internal Key(TableCache ownerTable)
      {
         _table = ownerTable;
         Columns = new List<FieldDef>();
      }

      /// <summary>
      ///   parses the data of this key
      /// </summary>
      internal void FillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         // fills the attributes of the key tag itself
         FillAttributes(parser);

         // init the inner objects mainly the segments of the key - represented as columns xml tags
         while (InitInnerObjects(parser, parser.getNextTag()))
         {
         }
      }

      /// <summary>
      ///   parses the attributes of the key actually there are only two id and type (at the moment)
      /// </summary>
      private void FillAttributes(XmlParser parser)
      {
         List<String> tokensVector;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         String tag;
         String attribute;

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            tag = parser.getXMLsubstring(endContext);

            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_ATTR_KEY) + ConstInterface.MG_ATTR_KEY.Length);

            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            //parse the key attributes
            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               attribute = (tokensVector[j]);
               string valueStr = (tokensVector[j + 1]);

               if (attribute.Equals(XMLConstants.MG_ATTR_ID))
                  _id = XmlParser.getInt(valueStr);
               else if (attribute.Equals(XMLConstants.MG_ATTR_TYPE))
                  Logger.Instance.WriteDevToLog("in Key.fillAttributes() obsolete attribute (TODO: remove from server)" + attribute);
               else
                  Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
            }
            parser.setCurrIndex(++endContext); // to delete ">" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in Key.fillAttributes() out of string bounds");
      }

      /// <summary>
      ///   used to parse each column xml tag attributes the column represents a segment in the key
      /// </summary>
      private void InitElements(List<String> tokensVector)
      {
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case XMLConstants.MG_ATTR_ID:
                  Columns.Add(_table.FldsTab.getField(XmlParser.getInt(valueStr)));
                  break;

               case XMLConstants.MG_ATTR_SIZE:
                  break;

               case ConstInterface.MG_ATTR_DIR:
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
                  break;
            }
         }
      }

      /// <summary>
      ///   allocates and initialize inner object according to the found xml data
      /// </summary>
      private bool InitInnerObjects(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_COLUMN))
         {
            int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());

            if (endContext != -1 && endContext < parser.getXMLdata().Length)
            {
               //last position of its tag
               String tag = parser.getXMLsubstring(endContext);
               parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_COLUMN) + ConstInterface.MG_TAG_COLUMN.Length);

               List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
               // parse each column
               InitElements(tokensVector);
               parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); //to delete "/>" too
            }
            else
               Logger.Instance.WriteExceptionToLog("in Key.initInnerObjects() out of string bounds");
         }
         else if (foundTagName.Equals('/' + ConstInterface.MG_ATTR_KEY))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog("There is no such tag in LinksTable.initInnerObjects(): " + foundTagName);
            return false;
         }
         return true;
      }

      /// <summary>
      ///   returns the key id
      /// </summary>
      internal int GetKeyId()
      {
         return _id;
      }
   }
}
