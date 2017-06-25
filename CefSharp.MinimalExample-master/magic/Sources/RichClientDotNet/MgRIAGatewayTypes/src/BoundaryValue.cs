using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;
using com.magicsoftware.util;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// This class holds Boundary Values required for range.
   /// </summary>
   public class BoundaryValue
   {
      [XmlAttribute]
      public RangeType Type { get; set; }
      public FieldValue Value { get; set; }
      [XmlAttribute]
      public bool Discard { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public BoundaryValue()
      {
         Value = new FieldValue();
      }

      /// <summary>
      /// Copy CTOR
      /// </summary>
      /// <param name="boundaryValue"></param>
      public BoundaryValue(BoundaryValue boundaryValue)
      {
         Type = boundaryValue.Type;
         Value = new FieldValue();
         Value.Value = boundaryValue.Value.Value;
         Value.IsNull = boundaryValue.Value.IsNull;
         Discard = boundaryValue.Discard;
      }

      public override string ToString()
      {
         return String.Format("{{BoundaryValue: {0}, {1}}}", Type, Value);
      }

      public override bool Equals(object obj)
      {
         BoundaryValue other = obj as BoundaryValue;
         if (other == null)
            return false;

         return (this.Discard == other.Discard) &&
            (this.Type == other.Type) &&
            (object.Equals(this.Value, other.Value));
      }

      public override int GetHashCode()
      {
         var hashBuilder = new HashCodeBuilder();
         hashBuilder.Append(Discard ? 1 : 2)
            .Append((int)Type)
            .Append(Value == null ? 0 : Value.GetHashCode());
         return hashBuilder.HashCode;
      }
   }
}
