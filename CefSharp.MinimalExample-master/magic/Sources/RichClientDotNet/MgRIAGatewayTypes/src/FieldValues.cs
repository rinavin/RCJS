using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// Represents fields of the view
   /// </summary>
  
   public class FieldValues
   {
      List<FieldValue> fields = new List<FieldValue>();

      //for XML only
      public List<FieldValue> Fields
      {
         get { return fields; }
         set { fields = value; }
      }

      /// <summary>
      /// indexer
      /// </summary>
      /// <param name="i"></param>
      /// <returns></returns>
      [XmlIgnore]
      public FieldValue this[int i]
      {
         get
         {
            return fields[i];
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fieldValue"></param>
      /// <returns>index of the new field</returns>
      public int Add(FieldValue fieldValue)
      {
         fields.Add(fieldValue);
         return fields.Count - 1;
      }

      /// <summary>
      /// sets the value in the specific place (index)
      /// </summary>
      /// <param name="index"></param>
      /// <param name="value"></param>
      public void SetValue(int index, object value)
      {
         fields[index].Value = value;
      }

      /// <summary>
      /// returns the value
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public object GetValue(int index)
      {
         return fields[index].Value;
      }

      /// <summary>
      /// sets Null
      /// </summary>
      /// <param name="index"></param>
      /// <param name="isNull"></param>
      public void SetNull(int index, bool isNull)
      {
         fields[index].IsNull = isNull;
      }

      /// <summary>
      /// returns isNull
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public bool IsNull(int index)
      {
         return fields[index].IsNull;
      }

      public int Count
      {
         get
         {
            return fields.Count;
         }
      }  
 }
}
