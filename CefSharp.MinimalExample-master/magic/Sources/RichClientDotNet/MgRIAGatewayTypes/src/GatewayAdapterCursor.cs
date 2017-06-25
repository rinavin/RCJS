using System.Collections.Generic;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// It is DB_CRSR in FM (C++)
   /// </summary>
   public class GatewayAdapterCursor
   {
      public CursorDefinition  Definition { get; set; }
      public CursorType CursorType { get; set; }
      public List<RangeData>   Ranges { get; set; }
      public FieldValues       CurrentRecord { get; set; }
      public FieldValues       OldRecord { get; set; }
      public List<JOIN_TBL>    JoinTbl;            /* join conditions information */
      public DbSqlRange        SqlRng;
      public char              JoinType;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="cursorDefintion"></param>
      public GatewayAdapterCursor(CursorDefinition cursorDefintion)
      {
         Definition = cursorDefintion;
         CurrentRecord = new FieldValues();
         Ranges = new List<RangeData>();
         JoinTbl = new List<JOIN_TBL>();
         OldRecord = new FieldValues();

         // Alloc values for the current record
         foreach (DBField dbField in Definition.FieldsDefinition)
         {
            CurrentRecord.Add(new FieldValue());
            OldRecord.Add(new FieldValue());
         }
      }

      public int GetFieldIndex(DBField field)
      {
         for (int fieldIdx = 0; fieldIdx < Definition.FieldsDefinition.Count; fieldIdx++)
         {
            if (field == Definition.FieldsDefinition[fieldIdx])
               return fieldIdx;
         }

         return -1;
      }

   }
}
