using System.Collections.Specialized;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.application.datasources
{
   /// <summary>
   /// XML Sax handler for parsing the DataSource definitions file
   /// </summary>
   class DataSourceDefinitionManagerSaxHandler : MgSAXHandlerInterface
   {
      DataSourceDefinitionManager dataSourceDefinitionManager;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="xmlData"></param>
      /// <param name="ddm"></param>
      public DataSourceDefinitionManagerSaxHandler(byte[] xmlData, DataSourceDefinitionManager dataSourceDefinitionManager)
      {
         this.dataSourceDefinitionManager = dataSourceDefinitionManager;
         MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
         mgSAXHandler.parse(xmlData);

      }

      #region MgSAXHandlerInterface
      public void endElement(string elementName, string elementValue, NameValueCollection attributes)
      {
         if (elementName.Equals(ConstInterface.MG_TAG_DBH_DATA_ID))
         {
            DataSourceDefinition dataSourceDefinition = new DataSourceDefinition();
            dataSourceDefinitionManager.DataSourceBuilder.SetAttributes(dataSourceDefinition, attributes);
            dataSourceDefinitionManager.DataSourceDefinitions.Add(dataSourceDefinition.Id, dataSourceDefinition);
         }
      }
      #endregion
   }
}
