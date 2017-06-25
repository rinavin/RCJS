using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using System.Collections.Specialized;
using System.Collections;
using System.Xml.Serialization;

namespace com.magicsoftware.gatewaytypes.data
{
   public enum SegMasks
   {
      SegDirDescendingMask = 0,
      SegDirAscendingMask,
      SegLenFieldMask,
      SegLenChanged,
   }

   /// <summary>
   /// Describes one segment in the DataSource
   /// </summary>
   public class DBSegment : ICloneable
   {
      public const int DB_SEG_DIR_DESCENDING_MASK = (1 << 0);
      public const int DB_SEG_LEN_FIELD_MASK = (1 << 1);

      public int Isn { get; set; }
      public DBField Field { get; set; }
      public int Flags { get; set; }

      public void SetMask(SegMasks mask)
      {
         switch (mask)
         {
            case SegMasks.SegDirAscendingMask:
               Flags &= ~DB_SEG_DIR_DESCENDING_MASK;
               break;
            case SegMasks.SegDirDescendingMask:
               Flags |= DB_SEG_DIR_DESCENDING_MASK;
               break;
            case SegMasks.SegLenFieldMask:
               Flags |= DB_SEG_LEN_FIELD_MASK;
               break;
            case SegMasks.SegLenChanged:
               Flags &= ~DB_SEG_LEN_FIELD_MASK;
               break;
         }
      }

      public bool CheckMask(SegMasks mask)
      {
         switch (mask)
         {
            case SegMasks.SegDirAscendingMask:
               return (Flags & DB_SEG_DIR_DESCENDING_MASK) == 0;

            case SegMasks.SegDirDescendingMask:
               return (Flags & DB_SEG_DIR_DESCENDING_MASK) != 0;

            case SegMasks.SegLenFieldMask:
               return (Flags & DB_SEG_LEN_FIELD_MASK) != 0;

            case SegMasks.SegLenChanged:
               return (Flags & DB_SEG_LEN_FIELD_MASK) == 0;
         }

         return false;
      }

      /// <summary>
      /// Create copy of the DBSegment. 
      /// </summary>
      /// <returns></returns>
      public object Clone()
      {
         DBSegment segment = (DBSegment)this.MemberwiseClone();
         //Here field of segment can not be cloned as it containes the reference of field from datasourcedefinition.
         //Caller of the clone method for segment should assign segment's field correctly. So assign null value for field.
         //TODO (Snehal) : Instead of maintaining field in the segment, we can maintain only field Isn.
         segment.Field = null;

         return segment;
      }
   }
}
