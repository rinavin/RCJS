using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace RuntimeDesigner.Serialization
{
   /// <summary>
   /// A property item which includes also an offset to use for recalculating a new tab order
   /// </summary>
   [Serializable]
   public class TabOrderOffsetPropertyItem : PropertyItem
   {
      public string OffsetValue { get; set; }

      /// <summary>
      /// empty CTOR for serialization
      /// </summary>
      public TabOrderOffsetPropertyItem()
      {
      }

      /// <summary>
      /// CTOR
      /// </summary>
      public TabOrderOffsetPropertyItem(String key, object Value, Type type, object offsetValue)
         : base(key, Value, type)
      {
         this.OffsetValue = (string)offsetValue;
      }

      /// <summary>
      /// convert the value property into the real value
      /// </summary>
      /// <returns></returns>
      public object GetOffsetValue()
      {
         TypeConverter converter = TypeDescriptor.GetConverter(Type);

         return converter.ConvertFromString((string)OffsetValue);
      }
   }
}
