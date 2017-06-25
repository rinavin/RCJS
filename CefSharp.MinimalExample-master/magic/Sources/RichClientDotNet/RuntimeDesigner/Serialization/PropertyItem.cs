using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;

namespace RuntimeDesigner.Serialization
{
   /// <summary>
   /// Serializable Property Item
   /// </summary>   
   [Serializable]
   [XmlInclude(typeof(TabOrderOffsetPropertyItem))]
   public class PropertyItem
   {
      #region CTOR
      /// <summary>
      /// 
      /// </summary>
      internal PropertyItem()
      {

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="key"></param>
      /// <param name="Value"></param>
      /// <param name="type"></param>
      internal PropertyItem(String key, object Value, Type type)
      {
         this.Key = key;
         this.Value = Value;
         this.Type = type;

      }
      
      #endregion

      #region properties
      
      [XmlAttribute]
      public String Key { get; set; }

      public Object Value { get; set; }


      /// <summary>
      /// it is impossible to serialize Type so we found a workaround for that
      /// </summary>
      [XmlIgnore]
      private Type type;
      [XmlIgnore]
      internal Type Type
      {
         get { return type; }
         set
         {
            type = value;
            TypeName = value.AssemblyQualifiedName;
         }
      }

      public string TypeName
      {
         get { return type.AssemblyQualifiedName; }
         set { type = Type.GetType(value); }
      }
      #endregion

      /// <summary>
      /// convert the value property into the real value
      /// </summary>
      /// <returns></returns>
      public object GetValue()
      {
         TypeConverter converter = TypeDescriptor.GetConverter(type);

         return converter.ConvertFromString((string)Value);
      }
   }
}
