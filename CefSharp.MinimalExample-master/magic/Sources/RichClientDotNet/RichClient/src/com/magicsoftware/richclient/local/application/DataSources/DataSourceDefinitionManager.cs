using System;
using System.Collections.Generic;
using System.Diagnostics;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using util.com.magicsoftware.util;
using System.Xml.Serialization;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.sources;
using com.magicsoftware.richclient.local.application.datasources.converter;

namespace com.magicsoftware.richclient.local.application.datasources
{
   /// <summary>
   /// Manage the collection of DataSource definitions
   /// </summary>
   class DataSourceDefinitionManager
   {
      internal Dictionary<DataSourceId, DataSourceDefinition> DataSourceDefinitions = new Dictionary<DataSourceId, DataSourceDefinition>();

      string dataDefinitionIdsUrl;

      DataSourceBuilder builder = new DataSourceBuilder();
      internal DataSourceBuilder DataSourceBuilder
      {
         get { return builder; }
      }

      /// <summary>
      /// 
      /// </summary>
      internal void FillData()
      {
         while (true)
         {
            string nextTag = ClientManager.Instance.RuntimeCtx.Parser.getNextTag();

            if (!InitInnerObjects(nextTag))
               break;
         }
      }

      /// <summary>
      ///  Returns the dataSourceDefinition according to it's id
      /// </summary>
      /// <param name="dataSourceId"></param>
      /// <returns></returns>
      internal DataSourceDefinition GetDataSourceDefinition(DataSourceId dataSourceId)
      {
         if (DataSourceDefinitions.ContainsKey(dataSourceId))
         {
            return DataSourceDefinitions[dataSourceId];
         }
         return null;
      }

      /// <summary>
      ///  Returns the dataSourceDefinition according to taskCtlIdx & realIdx.
      /// </summary>
      /// <param name="taskCtlIdx"></param>
      /// <param name="realIdx"></param>
      /// <returns></returns>
      internal DataSourceDefinition GetDataSourceDefinition(int taskCtlIdx, int realIdx)
      {
         DataSourceId dataSourceId = ClientManager.Instance.GetDataSourceId(taskCtlIdx, realIdx);

         if (dataSourceId != null)
         {
            return (ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.GetDataSourceDefinition(dataSourceId));
         }
         else
            return null;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="foundTagName"></param>
      /// <returns></returns>
      private bool InitInnerObjects(String foundTagName)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;

         if (foundTagName == null)
            return false;

         switch (foundTagName)
         {
            case ConstInterface.MG_TAG_DBHS:
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               return true;

            case ConstInterface.MG_TAG_DBH:
            case ConstInterface.MG_TAG_DBH_DATA_ID:
               DataSourceDefinition dataSourceDefinition = builder.Build();
               DataSourceDefinitions.Add(dataSourceDefinition.Id, dataSourceDefinition);
               return true;

            case ConstInterface.MG_TAG_DBH_END:
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               return true;

            case ConstInterface.MG_TAG_DBH_DATA_IDS_URL:
               // If data includes only file url
               FillUrl();
               return true;

            case ConstInterface.MG_TAG_DBHS_END:
            default:
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               return false;
         }
      }

      /// <summary>
      /// get the file url attribute from the XML parsing
      /// </summary>
      private void FillUrl()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         parser.setCurrIndex(parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_DBH_DATA_IDS_URL, parser.getCurrIndex()) + ConstInterface.MG_TAG_DBH_DATA_IDS_URL.Length + 1);

         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
         Debug.Assert(tokensVector[0].Equals(XMLConstants.MG_ATTR_VALUE));
         // Set the file url value
         dataDefinitionIdsUrl = XmlParser.unescape(tokensVector[1]);
         // get the file from server to client 
         GetDataSourceDefinitions();

         parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length);
      }

      /// <summary>
      /// Reads contents of a datasource indicated by sourceUrl using ApplicationSourcesManager
      /// </summary>
      /// <param name="sourceUrl"></param>
      /// <returns></returns>
      private byte[] GetDataSourceBuffer(String sourceUrl)
      {
         return ApplicationSourcesManager.GetInstance().ReadSource(sourceUrl, true);
      }

      /// <summary>
      /// Builds data repository from sources and handle conversion of data sources
      /// </summary>
      private void GetDataSourceDefinitions()
      {
         // read new sources from remote
         byte[] buf = ApplicationSourcesManager.GetInstance().ReadSource(dataDefinitionIdsUrl, true);
         this.DataSourceBuilder.DataSourceReader = GetDataSourceBuffer;
         new DataSourceDefinitionManagerSaxHandler(buf, this);

         if (CommandsProcessorManager.SessionStatus == CommandsProcessorManager.SessionStatusEnum.Remote)
         {
            // handle data source conversion if modified definition have any change related to database
            DataSourceConverter dataSourceConverter = ClientManager.Instance.LocalManager.DataSourceConverter = new DataSourceConverter();
            dataSourceConverter.NewDataSourceRepositoryContents = buf;
            dataSourceConverter.HandleRepositoryChanges(this, dataDefinitionIdsUrl);
         }
      }
   }
}
