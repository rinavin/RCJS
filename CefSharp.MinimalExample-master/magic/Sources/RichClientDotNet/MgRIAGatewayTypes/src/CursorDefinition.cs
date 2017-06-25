using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using System.Xml.Serialization;

namespace com.magicsoftware.gatewaytypes
{
  
   public class CursorDefinition
   {

      private DataSourceDefinition dataSourceDefinition;
      
      [XmlIgnore]
      public DataSourceDefinition DataSourceDefinition
      {
         get { return dataSourceDefinition; }
         set
         {
            dataSourceDefinition = value;
            DataSourceId = dataSourceDefinition == null ? null : dataSourceDefinition.Id;
         }
      }

      //for serialization
      public DataSourceId DataSourceId { get; set; }


      public List<int> FieldIsns { get; set; }
      
      List<DBField> fieldsDefinition;
      
      [XmlIgnore]
      public List<DBField> FieldsDefinition
      {
         get { return fieldsDefinition; }
         set
         {
            fieldsDefinition = value;
            if (value != null)
            {
               FieldIsns = new List<int>();
               foreach (var item in fieldsDefinition)
               {
                  FieldIsns.Add(item.Isn);
               }
            }
         }
      }

      public int LimitTo { get; set; }

      public DbPos CurrentPosition { get; set; }

      public DbPos StartPosition { get; set; }
      public CursorMode CursorMode { get; set; } //Why do we need this? Do we have different modes ?

      private DBKey key;  // key = 0 - N-1,  nokey = NULL_CHAR */
      [XmlIgnore]
      public DBKey Key
      {
         get { return key; }
         set
         {
            key = value;
            KeyIsn = key == null ?  -1 : key.Isn ;
         }
      }
                   
      //for serialization
      public int KeyIsn { get; set; }

      public CursorProperties CursorProperties { get; set; }

      public Order Direction { get; set; }

      public int Blobs { get; set; }  //Blobs Count.

      public List<bool> IsFieldUpdated { get; set; }

      public List<bool> DifferentialUpdate { get; set; }

      /// <summary>
      /// set flags
      /// </summary>
      /// <param name="flag"></param>
      public void SetFlag(CursorProperties flag)
      {
         CursorProperties |= flag;
      }

      /// <summary>
      /// clear flag
      /// </summary>
      /// <param name="flag"></param>
      public void ClearFlag(CursorProperties flag)
      {
         CursorProperties &= ~flag;
      }

      /// <summary>
      /// check the flag
      /// </summary>
      /// <param name="flag"></param>
      public bool IsFlagSet(CursorProperties flag)
      {
         return ((CursorProperties & flag) != 0);
      }


      /// <summary>
      /// set flag value according to value
      /// </summary>
      /// <param name="flag"></param>
      /// <param name="value"></param>
      public void SetFlagValue(CursorProperties flag, bool value)
      {
         if (value)
            SetFlag(flag);
         else
            ClearFlag(flag);
      }
   }
}
