using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.sources;

namespace com.magicsoftware.richclient.local.application.Databases
{
   /// <summary>
   /// manages the database definition objects - parse the server data, create the elements and hold the DatabaseDefinition dictionary
   /// </summary>
   class DatabaseDefinitionsManager
   {
      // dictionary of database definitions
      internal Dictionary<String, DatabaseDefinition> databaseDefinitions = new Dictionary<String, DatabaseDefinition>();
      
      // URL of the cached file containing the database definitions data
      String databaseDefinitionsUrl;

      internal DatabaseDefinition this[String databaseName]
      {
         get
         {
            if (databaseDefinitions.ContainsKey(databaseName))
               return databaseDefinitions[databaseName];
            return null;
         }
      }

      internal void Add(String key, DatabaseDefinition value)
      {
         databaseDefinitions.Add(key, value);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void FillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (true)
         {
            if (!InitInnerObjects(parser, parser.getNextTag()))
               break;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="foundTagName"></param>
      /// <returns></returns>
      private bool InitInnerObjects(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         switch (foundTagName)
         {
            case ConstInterface.MG_TAG_DATABASES_HEADER:
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               return true;

            case ConstInterface.MG_TAG_DATABASE_INFO:
               FillDataInternal(parser);
               return true;

            case ConstInterface.MG_TAG_DATABASES_END:
            default:
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               return false;
         }
      }

      /// <summary>
      /// get the file url attribute from the XML parsing
      /// </summary>
      internal void FillUrl()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser; 
         parser.setCurrIndex(parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_DATABASE_URL, parser.getCurrIndex()) + ConstInterface.MG_TAG_DATABASE_URL.Length + 1);

         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
         Debug.Assert(tokensVector[0].Equals(XMLConstants.MG_ATTR_VALUE));

         // Set the file url value
         databaseDefinitionsUrl = XmlParser.unescape(tokensVector[1]);

         // get the file from server to client 
         GetDatabaseDefinitions();

         parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length);
      }

      /// <summary>
      /// used when the data is passed in the response string and not as a cached file
      /// </summary>
      void FillDataInternal(XmlParser parser)
      {
         // create a database definitions xml buffer
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1;
         string xml = parser.getXMLsubstring(endContext);

         // Activate the sax parser
         new DatabaseDefinitionsSaxParser(Encoding.UTF8.GetBytes(xml), this);

         // skip to after the DBH
         parser.setCurrIndex(endContext + XMLConstants.TAG_CLOSE.Length);
      }

      /// <summary>
      /// get DataSource file from the server and parse it
      /// </summary>
      void GetDatabaseDefinitions()
      {
         byte[] buf = ApplicationSourcesManager.GetInstance().ReadSource(databaseDefinitionsUrl, true);
         new DatabaseDefinitionsSaxParser(buf, this);
      }
   }
}
