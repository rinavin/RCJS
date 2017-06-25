using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using System.Collections.Specialized;
using System.Collections;
using System.Xml.Serialization;

namespace com.magicsoftware.gatewaytypes.data
{
   public enum KeyMasks
   {
      DuplicateKeyModeMask,
      UniqueKeyModeMask,
      OneWayKeyMask,
      TwoWayKeyMask,
      KeyRangeFull,
      KeyRangeQuick,
      KeyTypeVirtual,
      KeyTypeReal,
      KeyClusteredMask,
      KeyDropReIndex,
      KeyHintMask,
      KeySortMagicMask,
      KeyValidMask,
      KeySortMask,
      KeyPrimaryMask,
      KeySystemPrimaryMask,
      MagicKeyMask
   }

   /// <summary>
   /// Describes one key in the DataSource
   /// </summary>
   public class DBKey : ICloneable
   {
      public const int DB_KEY_MODE_DUPLICATE_MASK = (1 << 0);
      public const int DB_KEY_ORDER_ONEWAY_MASK = (1 << 1);
      public const int DB_KEY_RANGE_FULL_MASK = (1 << 2);
      public const int DB_KEY_TYPE_VIRTUAL_MASK = (1 << 3);
      public const int DB_KEY_CLUSTERED_MASK = (1 << 4);
      public const int DB_KEY_DROP_REIDX_MASK = (1 << 5);
      public const int DB_KEY_HINT_MASK = (1 << 6);
      public const int DB_KEY_SORT_MAGIC_MASK = (1 << 7);
      public const int DB_KEY_VALID_MASK = (1 << 8);
      public const int DB_KEY_SORT_MASK = (1 << 9);
      public const int DB_KEY_PRIMARY_MASK = (1 << 10);
      public const int DB_KEY_SYS_PRIMARY_MASK = (1 << 11);
      public const int DB_KEY_MAGIC_KEY_MASK = (1 << 12);
       
      public string KeyDBName { get; set; }
       
      public int Isn { get; set; }
      
      public int Flags { get; set; }
     
      public List<DBSegment> Segments = new List<DBSegment>();

      public void SetMask(KeyMasks mask)
      {
         switch (mask)
         {
            case KeyMasks.DuplicateKeyModeMask:
               Flags |= DB_KEY_MODE_DUPLICATE_MASK;
               break;

            case KeyMasks.UniqueKeyModeMask:
               Flags &= ~DB_KEY_MODE_DUPLICATE_MASK;
               break;

            case KeyMasks.OneWayKeyMask:
               Flags |= DB_KEY_ORDER_ONEWAY_MASK;
               break;

            case KeyMasks.TwoWayKeyMask:
               Flags &= ~DB_KEY_ORDER_ONEWAY_MASK;
               break;

            case KeyMasks.KeyRangeFull:
               Flags |= DB_KEY_RANGE_FULL_MASK;
               break;

            case KeyMasks.KeyRangeQuick:
               Flags &= ~DB_KEY_RANGE_FULL_MASK;
               break;

            case KeyMasks.KeyTypeVirtual:
               Flags |= DB_KEY_TYPE_VIRTUAL_MASK;
               break;

            case KeyMasks.KeyTypeReal:
               Flags &= ~DB_KEY_TYPE_VIRTUAL_MASK;
               break;

            case KeyMasks.KeyClusteredMask:
               Flags |= DB_KEY_CLUSTERED_MASK;
               break;

            case KeyMasks.KeyDropReIndex:
               Flags |= DB_KEY_DROP_REIDX_MASK;
               break;

            case KeyMasks.KeyHintMask:
               Flags |= DB_KEY_HINT_MASK;
               break;

            case KeyMasks.KeySortMagicMask:
               Flags |= DB_KEY_SORT_MAGIC_MASK;
               break;

            case KeyMasks.KeyValidMask:
               Flags |= DB_KEY_VALID_MASK;
               break;

            case KeyMasks.KeySortMask:
               Flags |= DB_KEY_SORT_MASK;
               break;

            case KeyMasks.KeyPrimaryMask:
               Flags |= DB_KEY_PRIMARY_MASK;
               break;

            case KeyMasks.KeySystemPrimaryMask:
               Flags |= DB_KEY_SYS_PRIMARY_MASK;
               break;

            case KeyMasks.MagicKeyMask:
               Flags |= DB_KEY_MAGIC_KEY_MASK;
               break;

         }
      }

      public bool CheckMask(KeyMasks mask)
      {
         switch (mask)
         {
            case KeyMasks.DuplicateKeyModeMask:
               return (Flags & DB_KEY_MODE_DUPLICATE_MASK) != 0;

            case KeyMasks.UniqueKeyModeMask:
               return (Flags & DB_KEY_MODE_DUPLICATE_MASK) == 0;

            case KeyMasks.OneWayKeyMask:
               return (Flags & DB_KEY_ORDER_ONEWAY_MASK) != 0;

            case KeyMasks.TwoWayKeyMask:
               return (Flags & DB_KEY_ORDER_ONEWAY_MASK) == 0;

            case KeyMasks.KeyRangeFull:
               return (Flags & DB_KEY_RANGE_FULL_MASK) != 0;

            case KeyMasks.KeyRangeQuick:
               return (Flags & DB_KEY_RANGE_FULL_MASK) == 0;

            case KeyMasks.KeyTypeVirtual:
               return (Flags & DB_KEY_TYPE_VIRTUAL_MASK) != 0;

            case KeyMasks.KeyTypeReal:
               return (Flags & DB_KEY_TYPE_VIRTUAL_MASK) == 0;

            case KeyMasks.KeyClusteredMask:
               return (Flags & DB_KEY_CLUSTERED_MASK) != 0;

            case KeyMasks.KeyDropReIndex:
               return (Flags & DB_KEY_DROP_REIDX_MASK) != 0;

            case KeyMasks.KeyHintMask:
               return (Flags & DB_KEY_HINT_MASK) != 0;

            case KeyMasks.KeySortMagicMask:
               return (Flags & DB_KEY_SORT_MAGIC_MASK) != 0;

            case KeyMasks.KeyValidMask:
               return (Flags & DB_KEY_VALID_MASK) != 0;

            case KeyMasks.KeySortMask:
               return (Flags & DB_KEY_SORT_MASK) != 0;

            case KeyMasks.KeyPrimaryMask:
               return (Flags & DB_KEY_PRIMARY_MASK) != 0;

            case KeyMasks.KeySystemPrimaryMask:
               return (Flags & DB_KEY_SYS_PRIMARY_MASK) != 0;

            case KeyMasks.MagicKeyMask:
               return (Flags & DB_KEY_MAGIC_KEY_MASK) != 0;

         }
         return false;
      }

      /// <summary>
      /// Create copy of the DBKey. 
      /// </summary>
      /// <returns></returns>
      public object Clone()
      {
         DBKey key = (DBKey)this.MemberwiseClone();
         key.Segments = new List<DBSegment>();

         for (int segmentIdx = 0; segmentIdx < Segments.Count; segmentIdx++)
         {
            DBSegment segment = (DBSegment)Segments[segmentIdx].Clone();
            key.Segments.Add(segment);
         }

         return key;
      }

   }
}
