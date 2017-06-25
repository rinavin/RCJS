using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using com.magicsoftware.util;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// This class hold value details of field selected in task.
   /// </summary>

   public class FieldValue
   {
      public Object Value { get; set; }
      [XmlAttribute]
      public bool IsNull { get; set; }

      public override string ToString()
      {
         return String.Format("{{FieldValue: {0}}}", (IsNull || Value == null) ? "{NULL}" : Value.ToString());
      }

      public override bool Equals(object obj)
      {
         FieldValue other = obj as FieldValue;
         if (other == null)
            return false;

         if (this.IsNull)
            return other.IsNull;

         return (object.Equals(this.Value, other.Value));
      }

      public override int GetHashCode()
      {
         // This might not work properly for blobs.
         var hashBuilder = new HashCodeBuilder();
         hashBuilder.Append((IsNull || Value == null) ? 0 : Value.GetHashCode());
         return hashBuilder.HashCode;
      }

      public FieldValue Clone()
      {
         return (FieldValue)MemberwiseClone();
      }
   }
}
