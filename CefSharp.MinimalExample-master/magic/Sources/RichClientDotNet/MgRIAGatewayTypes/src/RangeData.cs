using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using com.magicsoftware.util;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// This class holds range Data of single field.
   /// </summary>
   public class RangeData
   {
      /// <summary>
      /// Index of the field as in the cursor on which the range applied.
      /// </summary>
      [XmlAttribute]
      public int FieldIndex { get; set; }
      public BoundaryValue Min { get; set; }
      public BoundaryValue Max { get; set; }
      [XmlAttribute]
      public int DatetimeRangeIdx { get; set; }  // == 0 - regular field
      //  > 0 - order number of the range for time part in range table (idx + 1) 
      //  < 0 - time part of the Datetime field

      /// <summary>
      /// CTOR
      /// </summary>
      public RangeData()
      {
         Min = new BoundaryValue();
         Max = new BoundaryValue();
      }

      /// <summary>
      /// Copy CTOR
      /// </summary>
      /// <param name="rangeData"></param>
      public RangeData(RangeData rangeData)
      {
         FieldIndex = rangeData.FieldIndex;
         Min = new BoundaryValue(rangeData.Min);
         Max = new BoundaryValue(rangeData.Max);
         DatetimeRangeIdx = rangeData.DatetimeRangeIdx;
      }

      public override string ToString()
      {
         return String.Format("{{Range on #{0} {1}{2}}}", FieldIndex, (Min == null ? "" : "from " + Min), (Max == null) ? "" : " to " + Max);
      }

      public override bool Equals(object obj)
      {
         RangeData other = obj as RangeData;
         if (other == null)
            return false;

         return (this.DatetimeRangeIdx == other.DatetimeRangeIdx) &&
            (this.FieldIndex == other.FieldIndex) &&
            Object.Equals(this.Max, other.Max) &&
            Object.Equals(this.Min, other.Min);
      }

      public override int GetHashCode()
      {
         var hashBuilder = new HashCodeBuilder();
         hashBuilder.Append(DatetimeRangeIdx)
            .Append(FieldIndex)
            .Append(Max)
            .Append(Min);
         return hashBuilder.HashCode;
      }
   }
}
