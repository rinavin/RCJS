using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

using System.Diagnostics;
using com.magicsoftware.util;
using System.Xml.Serialization;

namespace com.magicsoftware.gatewaytypes.data
{

   public enum DbhMask
   {
      AccessReadOnlyMask = 0,
      AccessReadWriteMask,
      DefaultNameMask,
      CheckExistMask,
      UniqueKeyMask,
      UnusableMask,
      GtPedMask,
      VirtualMask,
      ModifiedMask,
      HintMask,
      FileTypeTableMask,
      FileTypeViewMask,
      BinaryTableMask
   }

   /// <summary>
   /// Contains the data needed to create a database in the client side.
   /// Basically et is the equivalent of the server's DBH
   /// </summary>
  
   public class DataSourceDefinition : ICloneable
   {
      public const int DB_DBH_ACCESS_READONLY_MASK = (1 << 0);
      public const int DB_DBH_DEFAULT_NAME_MASK = (1 << 1);
      public const int DB_DBH_CHK_EXIST_MASK = (1 << 2);
      public const int DB_DBH_UNIQUE_KEY_MASK = (1 << 4);
      public const int DB_DBH_UNUSABLE_MASK = (1 << 5);
      public const int DB_DBH_GTPED_MASK = (1 << 6);
      public const int DB_DBH_VIRTUAL_MASK = (1 << 7);
      public const int DB_DBH_MODIFIED_MASK = (1 << 8);
      public const int DB_DBH_HINT_MASK = (1 << 9);
      public const int DB_DBH_FTYPE_TABLE_MASK = (1 << 10);
      public const int DB_DBH_BINARY_TABLE_MASK = (1 << 11);

      public DataSourceId Id { get; set; }
      
      public string Name { get; set; }
     
      public int Flags { get; set; }
      
      public string DBaseName { get; set; }
     
      public int PositionIsn { get; set; }
     
      public int ArraySize { get; set; }
     
      public char RowIdentifier { get; set; }
     
      public char CheckExist { get; set; }
     
      public char DelUpdMode { get; set; }
     
      public string FileUrl { get; set; }

     
      public List<DBField> Fields = new List<DBField>();
     
      public List<DBKey> Keys = new List<DBKey>();
     
      public List<DBSegment> Segments = new List<DBSegment>();

      private DBKey positionKey;
      public DBKey PositionKey
      {
         get
         {
            if (positionKey == null)
            {
               foreach (DBKey key in Keys)
               {
                  if (PositionIsn == key.Isn)
                  {
                     positionKey = key;
                     break;
                  }
               }
            }

            return positionKey;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public DataSourceDefinition()
      {
         Id = new DataSourceId();
      }

      public void SetMask(DbhMask mask)
      {
         switch (mask)
         {
            case DbhMask.AccessReadOnlyMask:
               Flags |= DB_DBH_ACCESS_READONLY_MASK;
               break;

            case DbhMask.AccessReadWriteMask:
               Flags &= ~DB_DBH_ACCESS_READONLY_MASK;
               break;

            case DbhMask.BinaryTableMask:
               Flags |= DB_DBH_BINARY_TABLE_MASK;
               break;

            case DbhMask.CheckExistMask:
               Flags |= DB_DBH_CHK_EXIST_MASK;
               break;

            case DbhMask.DefaultNameMask:
               Flags |= DB_DBH_DEFAULT_NAME_MASK;
               break;

            case DbhMask.FileTypeTableMask:
               Flags |= DB_DBH_FTYPE_TABLE_MASK;
               break;

            case DbhMask.FileTypeViewMask:
               Flags &= ~DB_DBH_FTYPE_TABLE_MASK;
               break;

            case DbhMask.GtPedMask:
               Flags |= DB_DBH_GTPED_MASK;
               break;

            case DbhMask.HintMask:
               Flags |= DB_DBH_HINT_MASK;
               break;

            case DbhMask.ModifiedMask:
               Flags |= DB_DBH_MODIFIED_MASK;
               break;

            case DbhMask.UniqueKeyMask:
               Flags |= DB_DBH_UNIQUE_KEY_MASK;
               break;

            case DbhMask.UnusableMask:
               Flags |= DB_DBH_UNUSABLE_MASK;
               break;

            case DbhMask.VirtualMask:
               Flags |= DB_DBH_VIRTUAL_MASK;
               break;

         }
      }

      public bool CheckMask(DbhMask mask)
      {
         switch (mask)
         {
            case DbhMask.AccessReadOnlyMask:
               return (Flags & DB_DBH_ACCESS_READONLY_MASK) != 0;

            case DbhMask.AccessReadWriteMask:
               return (Flags & DB_DBH_ACCESS_READONLY_MASK) == 0;

            case DbhMask.BinaryTableMask:
               return (Flags & DB_DBH_BINARY_TABLE_MASK) != 0;

            case DbhMask.CheckExistMask:
               return (Flags & DB_DBH_CHK_EXIST_MASK) != 0;

            case DbhMask.DefaultNameMask:
               return (Flags & DB_DBH_DEFAULT_NAME_MASK) != 0;

            case DbhMask.FileTypeTableMask:
               return (Flags & DB_DBH_FTYPE_TABLE_MASK) != 0;

            case DbhMask.FileTypeViewMask:
               return (Flags & DB_DBH_FTYPE_TABLE_MASK) == 0;

            case DbhMask.GtPedMask:
               return (Flags & DB_DBH_GTPED_MASK) != 0;

            case DbhMask.HintMask:
               return (Flags & DB_DBH_HINT_MASK) != 0;

            case DbhMask.ModifiedMask:
               return (Flags & DB_DBH_MODIFIED_MASK) != 0;

            case DbhMask.UniqueKeyMask:
               return (Flags & DB_DBH_UNIQUE_KEY_MASK) != 0;

            case DbhMask.UnusableMask:
               return (Flags & DB_DBH_UNUSABLE_MASK) != 0;

            case DbhMask.VirtualMask:
               return (Flags & DB_DBH_VIRTUAL_MASK) != 0;
         }

         return false;
      }

      /// <summary>
      /// Method to try retrieving a key, whose index is keyIndex, returning
      /// null if the keyIndex is out of bounds, instead of throwing an exception.
      /// </summary>
      /// <param name="keyIndex">The index of the key to retrieve.</param>
      /// <returns>If keyIndex is within the bounds of Keys, the method will return
      /// the DBKey from the Keys list. Otherwise, the method will return null.</returns>
      public DBKey TryGetKey(int keyIndex)
      {
         if (keyIndex < 0 || keyIndex >= Keys.Count)
            return null;

         return Keys[keyIndex];
      }

      public override string ToString()
      {
         return "{Data Source Def: \"" + Name + "\" on \"" + DBaseName + "\" (" + Id.CtlIdx + "," + Id.Isn + ")}";
      }

      public override int GetHashCode()
      {
         return ToString().GetHashCode();
      }

      public override bool Equals(object obj)
      {
         if (obj != null && obj.GetHashCode() == GetHashCode())
            return true;
         return false;
      }

      /// <summary>
      /// Create copy of DataSourceDefinition.
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      public object Clone()
      {

         DataSourceDefinition dataSourceDefinition = (DataSourceDefinition)this.MemberwiseClone();

         dataSourceDefinition.Fields = new List<DBField>();
         for (int fieldIdx = 0; fieldIdx < Fields.Count; fieldIdx++)
         {
            DBField newField = (DBField)Fields[fieldIdx].Clone();
            dataSourceDefinition.Fields.Add(newField);
         }

         dataSourceDefinition.Keys = new List<DBKey>();
         for (int keyIdx = 0; keyIdx < Keys.Count; keyIdx++)
         {
            DBKey newKey = (DBKey)Keys[keyIdx].Clone();
            for (int segmentIdx = 0; segmentIdx < newKey.Segments.Count; segmentIdx++)
            {
               DBSegment segment = (DBSegment)newKey.Segments[segmentIdx];
               segment.Field = dataSourceDefinition.Fields.Find(x => x.Isn == Keys[keyIdx].Segments[segmentIdx].Field.Isn);
            }
            dataSourceDefinition.Keys.Add(newKey);
         }

         dataSourceDefinition.Segments = new List<DBSegment>();
         for (int segmentIdx = 0; segmentIdx < Segments.Count; segmentIdx++)
         {
            DBSegment newSegment = (DBSegment)Segments[segmentIdx].Clone();
            newSegment.Field = dataSourceDefinition.Fields.Find(x => x.Isn == Segments[segmentIdx].Field.Isn);
            dataSourceDefinition.Segments.Add(newSegment);
         }

         return dataSourceDefinition;
      }
   }

}
