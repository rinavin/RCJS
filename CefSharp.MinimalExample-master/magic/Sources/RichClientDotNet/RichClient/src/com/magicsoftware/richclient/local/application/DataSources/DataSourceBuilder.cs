using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.sources;
using com.magicsoftware.unipaas.management.data;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.application.datasources
{
   internal delegate byte[] DataSourceReaderDelegate(String sourceUrl);

   /// <summary>
   /// Builder for a data source definition
   /// </summary>
   internal class DataSourceBuilder
   {
      internal DataSourceReaderDelegate DataSourceReader { get; set; }

      internal DataSourceDefinition Build()
      {
         DataSourceDefinition dataSourceDefinition = new DataSourceDefinition();

         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         // separate the DBH xml and pass it to the sax parser
         int endContext = parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_DBH_END, parser.getCurrIndex()) + ConstInterface.MG_TAG_DBH_END.Length;
         endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, endContext) + XMLConstants.TAG_CLOSE.Length;
         string xml = parser.getXMLsubstring(endContext);

         // Activate the sax parser
         new DataSourceDefinitionSaxHandler(dataSourceDefinition, this, Encoding.UTF8.GetBytes(xml));

         // skip to after the DBH
         parser.setCurrIndex(endContext + XMLConstants.TAG_CLOSE.Length);

         return dataSourceDefinition;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attributes"></param>
      public void SetAttributes(DataSourceDefinition dataSourceDefinition, NameValueCollection attributes)
      {
         IEnumerator enumerator = attributes.GetEnumerator();
         while (enumerator.MoveNext())
         {
            String attr = (String)enumerator.Current;
            setAttribute(dataSourceDefinition, attr, attributes[attr]);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="valueStr"></param>
      /// <returns></returns>
      protected virtual bool setAttribute(DataSourceDefinition dataSourceDefinition, string attribute, string valueStr)
      {
         int tmp;
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_ISN:
               IntUtil.TryParse(valueStr, out tmp);
               dataSourceDefinition.Id.Isn = tmp;
               break;
            case XMLConstants.MG_ATTR_CTL_IDX:
               IntUtil.TryParse(valueStr, out tmp);
               dataSourceDefinition.Id.CtlIdx = tmp;
               break;
            case ConstInterface.MG_ATTR_NAME:
               dataSourceDefinition.Name = XmlParser.unescape(valueStr);
               break;
            case ConstInterface.MG_ATTR_FLAGS:
               IntUtil.TryParse(valueStr, out tmp);
               dataSourceDefinition.Flags = tmp;
               break;
            case ConstInterface.MG_ATTR_DBASE_NAME:
               dataSourceDefinition.DBaseName = valueStr == null ? null : valueStr.ToUpper();
               break;
            case ConstInterface.MG_ATTR_POSITION_ISN:
               IntUtil.TryParse(valueStr, out tmp);
               dataSourceDefinition.PositionIsn = tmp;
               break;
            case ConstInterface.MG_ATTR_ARRAY_SIZE:
               IntUtil.TryParse(valueStr, out tmp);
               dataSourceDefinition.ArraySize = tmp;
               break;
            case ConstInterface.MG_ATTR_ROW_IDENTIFIER:
               dataSourceDefinition.RowIdentifier = valueStr[0];
               break;
            case ConstInterface.MG_ATTR_CHECK_EXIST:
               dataSourceDefinition.CheckExist = valueStr[0];
               break;
            case ConstInterface.MG_ATTR_DEL_UPD_MODE:
               dataSourceDefinition.DelUpdMode = valueStr[0];
               break;
            case ConstInterface.MG_ATTR_DBH_DATA_URL:
               dataSourceDefinition.FileUrl = XmlParser.unescape(valueStr);

               Debug.Assert(DataSourceReader != null);
               byte[] buf = DataSourceReader(dataSourceDefinition.FileUrl);
               new DataSourceDefinitionSaxHandler(dataSourceDefinition, this, buf);
               break;
            default:
               return false;
         }
         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="field"></param>
      internal void AddField(DataSourceDefinition dataSourceDefinition, DBField field)
      {
         field.IndexInRecord = dataSourceDefinition.Fields.Count;
         dataSourceDefinition.Fields.Add(field);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="key"></param>
      internal void AddKey(DataSourceDefinition dataSourceDefinition, DBKey key)
      {
         dataSourceDefinition.Keys.Add(key);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="segment"></param>
      internal void AddSegment(DataSourceDefinition dataSourceDefinition, DBSegment segment)
      {
         dataSourceDefinition.Segments.Add(segment);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="tokensVector"></param>
      internal void SetDBFieldAttributes(DBField dbField, NameValueCollection attributes)
      {
         IEnumerator enumerator = attributes.GetEnumerator();
         while (enumerator.MoveNext())
         {
            String attr = (String)enumerator.Current;
            setDBFieldAttribute(dbField, attr, attributes[attr]);
         }

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="valueStr"></param>
      /// <returns></returns>
      protected virtual bool setDBFieldAttribute(DBField dbField, string attribute, string valueStr)
      {
         int tmp;
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_ISN:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.Isn = tmp;
               break;
            case ConstInterface.MG_ATTR_ATTR:
               dbField.Attr = valueStr[0];
               break;
            case ConstInterface.MG_ATTR_ALLOW_NULL:
               dbField.AllowNull = valueStr[0] == '1';
               break;
            case ConstInterface.MG_ATTR_DEFAULT_NULL:
               dbField.DefaultNull = valueStr[0] == '1';
               break;
            case ConstInterface.MG_ATTR_STORAGE:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.Storage = (FldStorage)tmp;
               break;
            case ConstInterface.MG_ATTR_LENGTH:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.Length = tmp;
               break;
            case ConstInterface.MG_ATTR_DATASOURCE_DEFINITION:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.DataSourceDefinition = (DatabaseDefinitionType)tmp;
               break;
            case ConstInterface.MG_ATTR_DIFF_UPDATE:
               dbField.DiffUpdate = valueStr[0];
               break;
            case ConstInterface.MG_ATTR_DEC:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.Dec = tmp;
               break;
            case ConstInterface.MG_ATTR_WHOLE:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.Whole = tmp;
               break;
            case ConstInterface.MG_ATTR_PART_OF_DATETIME:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.PartOfDateTime = tmp;
               break;
            case ConstInterface.MG_ATTR_DEFAULT_STORAGE:
               dbField.DefaultStorage = valueStr[0] == '1';
               break;
            case ConstInterface.MG_ATTR_CONTENT:
               IntUtil.TryParse(valueStr, out tmp);
               dbField.BlobContent = (BlobContent)BlobType.ParseContentType(tmp);
               break;
            case ConstInterface.MG_ATTR_PICTURE:
               dbField.Picture = XmlParser.unescape(valueStr);
               break;
            case ConstInterface.MG_ATTR_DB_DEFAULT_VALUE:
               dbField.DbDefaultValue = XmlParser.unescape(valueStr);
               break;
            case ConstInterface.MG_ATTR_FLD_DB_INFO:
               dbField.DbInfo = XmlParser.unescape(valueStr);
               break;
            case ConstInterface.MG_ATTR_DB_NAME:
               dbField.DbName = XmlParser.unescape(valueStr);
               break;
            case ConstInterface.MG_ATTR_DB_TYPE:
               dbField.DbType = XmlParser.unescape(valueStr);
               break;
            case ConstInterface.MG_ATTR_USER_TYPE:
               dbField.UserType = XmlParser.unescape(valueStr);
               break;
            case ConstInterface.MG_ATTR_NULL_DISPLAY:
               dbField.NullDisplay = XmlParser.unescape(valueStr);
               break;

            case XMLConstants.MG_ATTR_DEFAULTVALUE:
               dbField.DefaultValue = valueStr;
               if (dbField.Attr == (char)StorageAttribute.ALPHA || dbField.Attr == (char)StorageAttribute.UNICODE)
               {
                  dbField.DefaultValue = XmlParser.unescape(valueStr);
                  dbField.DefaultValue = StrUtil.padStr(dbField.DefaultValue, dbField.Length);
               }
               else if (dbField.DefaultValue.Length == 0 && dbField.Attr != (char)StorageAttribute.BLOB &&
                        dbField.Attr != (char)StorageAttribute.BLOB_VECTOR)
                  dbField.DefaultValue = null;
               else if (dbField.Attr == (char)StorageAttribute.BLOB)
               {
                  dbField.DefaultValue = BlobType.createFromString(dbField.DefaultValue, (char)dbField.BlobContent);
               }
               break;

            case ConstInterface.MG_ATTR_FIELD_NAME:
               dbField.Name = XmlParser.unescape(valueStr);
               break;

            default:
               return false;
         }
         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attributes"></param>
      internal void SetDBKeyAttributes(DBKey dbKey, NameValueCollection attributes)
      {
         IEnumerator enumerator = attributes.GetEnumerator();
         while (enumerator.MoveNext())
         {
            String attr = (String)enumerator.Current;
            setDBKeyAttribute(dbKey, attr, attributes[attr]);
         }

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="valueStr"></param>
      /// <returns></returns>
      protected virtual bool setDBKeyAttribute(DBKey dbKey, string attribute, string valueStr)
      {
         int tmp;
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_KEY_DB_NAME:
               dbKey.KeyDBName = XmlParser.unescape(valueStr);
               break;

            case ConstInterface.MG_ATTR_ISN:
               IntUtil.TryParse(valueStr, out tmp);
               dbKey.Isn = tmp;
               break;
            case ConstInterface.MG_ATTR_FLAGS:
               IntUtil.TryParse(valueStr, out tmp);
               dbKey.Flags = tmp;
               break;

            default:
               return false;
         }
         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attributes"></param>
      /// <param name="dbFields"></param>
      internal void SetDBSegmentAttributes(DBSegment dbSegment,  NameValueCollection attributes, List<DBField> dbFields)
      {
         IEnumerator enumerator = attributes.GetEnumerator();
         while (enumerator.MoveNext())
         {
            String attr = (String)enumerator.Current;
            setDBSegmentAttribute(dbSegment, attr, attributes[attr], dbFields);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="valueStr"></param>
      /// <param name="dbFields"></param>
      /// <returns></returns>
      protected virtual bool setDBSegmentAttribute(DBSegment dbSegment, string attribute, string valueStr, List<DBField> dbFields)
      {
         int tmp;
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_FLAGS:
               IntUtil.TryParse(valueStr, out tmp);
               dbSegment.Flags = tmp;
               break;

            case ConstInterface.MG_ATTR_ISN:
               IntUtil.TryParse(valueStr, out tmp);
               dbSegment.Isn = tmp;
               break;
            case ConstInterface.MG_ATTR_FLD_ISN:
               int fldIsn;
               IntUtil.TryParse(valueStr, out fldIsn);
               dbSegment.Field = dbFields.Find(x => x.Isn == fldIsn);
               break;

            default:
               return false;
         }
         return true;
      }

   }
}
