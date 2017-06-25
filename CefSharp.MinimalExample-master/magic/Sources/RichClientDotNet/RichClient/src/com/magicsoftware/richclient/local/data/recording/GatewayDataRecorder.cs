using System;
using System.Collections.Generic;
using System.Text;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.gateways.commands;
using System.Xml.Serialization;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.local.data.recording
{
   /// <summary>
   /// Data Recorder.
   /// </summary>
   public class GatewayDataRecorder : RecorderBase<GatewayAdapterCursor>
   {
      public GatewayRecordsList records;

      public GatewayDataRecorder()
      {
         records = new GatewayRecordsList(); 
      }

      public class GatewayRecord
      {
         public GatewayRecord()
         {
            FieldValues = new List<FieldData>();
         }

         public List<FieldData> FieldValues { get; set; }

      }
      
      public class FieldData
      {
         [XmlAttribute]
         public String Name { get; set; }
         [XmlText]
         public String Value { get; set; }

         [XmlAttribute]
         public bool IsNull { get; set; }

         public FieldData(string name, string value, bool isNull)
         {
            this.Name = name;
            this.Value = value;
            this.IsNull = isNull;
         }
         public FieldData()
         {

         }

      }

      [XmlRootAttribute("GatewayRecords")]
      public class GatewayRecordsList : List<GatewayRecord> { }
      
      ClassSerializer serializer = new ClassSerializer();
      
      /// <summary>
      /// filename to save the commands
      /// </summary>
      public override string FileName
      {
         get
         {
            return serializer.FileName;
         }
         set
         {
            serializer.FileName = value;
         }
      }

      public string XMLString
      {
         get
         {
            return serializer.XMLString;
         }
         set
         {
            serializer.XMLString = value;
         }
      }
      protected override void Add(GatewayAdapterCursor gatewayAdapterCursor)
      {
         GatewayRecord record = new GatewayRecord();
         for ( int fldIdx = 0; fldIdx < gatewayAdapterCursor.Definition.FieldsDefinition.Count; fldIdx++)
         {
            DBField field = gatewayAdapterCursor.Definition.FieldsDefinition[fldIdx];
            if (field.IsBlob())
            {
               if (field.IsBinaryBlob())
               {
                  string blobData = string.Empty;
                  if (!gatewayAdapterCursor.CurrentRecord[fldIdx].IsNull)
                  {
                     string blobDataWithPrefix = BlobType.createFromBytes((byte[])(((GatewayBlob)gatewayAdapterCursor.CurrentRecord[fldIdx].Value).Blob), BlobType.CONTENT_TYPE_BINARY);
                     blobData = BlobType.getString(blobDataWithPrefix);
                  }
                  
                  record.FieldValues.Add(new FieldData(field.DbName,
                                                       blobData,
                                                       gatewayAdapterCursor.CurrentRecord[fldIdx].IsNull));
               }
               else
               {
                  record.FieldValues.Add(new FieldData(field.DbName,
                                                      (((GatewayBlob)gatewayAdapterCursor.CurrentRecord[fldIdx].Value).Blob).ToString(),
                                                      gatewayAdapterCursor.CurrentRecord[fldIdx].IsNull));
               }
            }
            else
            {
               record.FieldValues.Add(new FieldData(gatewayAdapterCursor.Definition.FieldsDefinition[fldIdx].DbName,
                                                    gatewayAdapterCursor.CurrentRecord[fldIdx].Value == null ? null : gatewayAdapterCursor.CurrentRecord[fldIdx].Value.ToString(), 
                                                    gatewayAdapterCursor.CurrentRecord[fldIdx].IsNull));
            }
         }
         records.Add(record);
      }

      public override void Save()
      {
         serializer.SerializeToFile(records);
      }

      public override object Load()
      {
         throw new NotImplementedException();
      }
   }
}
