using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using System.Collections.Specialized;
using System.Collections;
using System.Xml.Serialization;

namespace com.magicsoftware.gatewaytypes.data
{
   /// <summary>
   /// Describes one field in the DataSource
   /// </summary>
   public class DBField : ICloneable
   {
      
      public int Isn { get; set; }
     
      public int IndexInRecord { get; set; }
     
      public char Attr { get; set; }
     
      public bool AllowNull { get; set; }
     
      public bool DefaultNull { get; set; }
      
      public FldStorage Storage { get; set; }
      public bool ShouldSerializeStorage()
      {
          return Storage != 0;
      }
      public int Length { get; set; }
     
      public DatabaseDefinitionType DataSourceDefinition { get; set; }
      public bool ShouldSerializeDataSourceDefinition()
      {
          return DataSourceDefinition != 0;
      }

      public string Name { get; set; }

      public char DiffUpdate { get; set; }
     
      public int Dec { get; set; }
     
      public int Whole { get; set; }
     
      public long PartOfDateTime { get; set; }
     
      public bool DefaultStorage { get; set; }
     
      public BlobContent BlobContent { get; set; }
       /// <summary>
       /// xml serializer does not know how to serialize 0 values if they are not defined in the enum
       /// tell the serializer do not serialize it
       /// </summary>
       /// <returns></returns>
      public bool ShouldSerializeBlobContent()
      {
          return BlobContent != 0;
      }
      public string Picture { get; set; }
     
      public string DbDefaultValue { get; set; }
     
      public string DbInfo { get; set; }
      
      public string DbName { get; set; }
     
      public string DbType { get; set; }
     
      public string UserType { get; set; }

      public string NullDisplay { get; set; }

      public String DefaultValue { get; set; }

      public bool IsBlob()
      {
         if (Storage == FldStorage.Blob || Storage == FldStorage.AnsiBlob ||
             Storage == FldStorage.UnicodeBlob)
            return true;
         else
            return false;
      }

      public bool IsBinaryBlob()
      {
         if (Storage == FldStorage.Blob)
            return true;
         else
            return false;
      }

      public bool IsUnicode()
      {
         if (Storage == FldStorage.UnicodeZString)
            return true;
         else
            return false;
      }

      public bool IsString()
      {
         if (Storage == FldStorage.AlphaString || Storage == FldStorage.AlphaZString
            || Storage == FldStorage.DateString || Storage == FldStorage.TimeString)
            return true;
         else
            return false;
      }

      public bool IsNumber()
      {
         if (Storage == FldStorage.NumericFloat || Storage == FldStorage.NumericSigned)
            return true;
         else
            return false;
      }

      public int StorageFldSize()
      {
         return Length;
      }

      public override string ToString()
      {
         return "{DBField: " + DbName + ", #" + IndexInRecord + ", " + Picture + "}";
      }

      /// <summary>
      /// Create copy of the DbField. 
      /// </summary>
      /// <returns></returns>
      public object Clone()
      {
         return this.MemberwiseClone();
      }
   }
}
