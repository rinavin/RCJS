using System.Collections.Generic;
using System.Collections.Specialized;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.application.datasources
{

   /// <summary>
   /// XML Sax handler to fill the DataSourceDefinition object
   /// </summary>
   class DataSourceDefinitionSaxHandler : MgSAXHandlerInterface
   {
      DataSourceDefinition dataSourceDefinition;
      DataSourceBuilder dataSourceBuilder;

      // List of segments that will be on the next key. The segments are a key inner objects, so we get their definition
      // before the key is created.
      List<DBSegment> segments = new List<DBSegment>();

      /// <summary>
      /// CTOR - create and activate the parsing process
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="xmlData"></param>
      public DataSourceDefinitionSaxHandler(DataSourceDefinition dataSourceDefinition, DataSourceBuilder dataSourceBuilder, 
                                            byte[] xmlData)
      {
         this.dataSourceDefinition = dataSourceDefinition;
         this.dataSourceBuilder = dataSourceBuilder;
         MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
         mgSAXHandler.parse(xmlData);
      }

      #region MgSAXHandlerInterface
      /// <summary>
      /// 
      /// </summary>
      /// <param name="elementName"></param>
      /// <param name="elementValue"></param>
      /// <param name="attributes"></param>
      public void endElement(string elementName, string elementValue, NameValueCollection attributes)
      {
         switch (elementName)
         {
            case ConstInterface.MG_TAG_KEYS:
            case ConstInterface.MG_TAG_SEGS:
            case ConstInterface.MG_TAG_FLDS:
               // closing collection - do nothing
               return;

            case ConstInterface.MG_TAG_DBH:
            case ConstInterface.MG_TAG_DBH_DATA_ID:
               // set the attributes
               dataSourceBuilder.SetAttributes(dataSourceDefinition, attributes);
               break;

            case ConstInterface.MG_TAG_FLD:
               // create the field and add it to the DataSourceDefinition
               DBField field = new DBField();
               dataSourceBuilder.SetDBFieldAttributes(field, attributes);
               dataSourceBuilder.AddField(dataSourceDefinition, field);
               break;

            case ConstInterface.MG_TAG_KEY:
               // create the key and add it to the DataSourceDefinition
               DBKey key = new DBKey();
               dataSourceBuilder.SetDBKeyAttributes(key, attributes);
               // Add the segments collection and reset the local one
               key.Segments = segments;
               segments = new List<DBSegment>();
               dataSourceBuilder.AddKey(dataSourceDefinition, key);
               break;

            case ConstInterface.MG_TAG_SEG:
               // create the segment and add it to the DataSourceDefinition
               DBSegment segment = new DBSegment();
               dataSourceBuilder.SetDBSegmentAttributes(segment, attributes, dataSourceDefinition.Fields);
               dataSourceBuilder.AddSegment(dataSourceDefinition, segment);
               break;

            case ConstInterface.MG_TAG_SEGMENT:
               // Get the segment's isn and add the segment to the local segment collection. This way it will be added
               // later to the right key
               int isn;
               IntUtil.TryParse((string)attributes.GetValues(0)[0], out isn);
               segments.Add(dataSourceDefinition.Segments.Find(x => x.Isn == isn));
               break;
         }
      }
      #endregion
   }
}
