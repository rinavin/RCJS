using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   /// <summary>
   /// Represents an attribute on an XML tag with name and value. This
   /// class also provides methods to convert the value to a type other
   /// than plain string.
   /// </summary>
   public class XmlAttribute
   {
      public string Name { get; private set; }
      public string Value { get; private set; }

      public XmlAttribute(string name, string value)
      {
         this.Name = name;
         this.Value = value;
      }

      public int ValueAsInt
      {
         get
         {
            return Int32.Parse(Value);
         }
      }

      public bool ValueAsBoolean
      {
         get
         {
            return Value[0] == '1';
         }
      }

      public override string ToString()
      {
         return Name + "=" + Value;
      }
   }
}
