using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;

namespace com.magicsoftware.richclient.local.data.view
{
   /// <summary>
   /// Source view definition for a data control.
   /// </summary>
   class DataControlSourceViewDefinition : IDataSourceViewDefinition
   {
      public DataSourceReference TaskDataSource
      {
         get;
         private set;
      }

      public List<DBField> DbFields
      {
         get;
         private set;
      }

      public DBKey DbKey
      {
         get;
         private set;
      }

      public Order RecordsOrder
      {
         get { return Order.Ascending; }
      }

      public bool CanInsert
      {
         get { return false; }
      }

      public bool CanDelete
      {
         get { return false; }
      }

      public int BoundControlId
      {
         get;
         private set;
      }

      public IRangeDataBuilder RangeDataBuilder { get; set; }

      public DataControlSourceViewDefinition(DataSourceReference dataSourceReference, int keyIndex, int valueFieldIndex, int displayFieldIndex, int boundControlId)
      {
         TaskDataSource = dataSourceReference;
         DbKey = dataSourceReference.DataSourceDefinition.TryGetKey(keyIndex);
         DbFields = new List<DBField>();
         DBField f = dataSourceReference.DataSourceDefinition.Fields[displayFieldIndex];
         DbFields.Add(f);
         if (valueFieldIndex == -1)
            valueFieldIndex = displayFieldIndex;
         f = dataSourceReference.DataSourceDefinition.Fields[valueFieldIndex];
         DbFields.Add(f);
         BoundControlId = boundControlId;
         RangeDataBuilder = null;
      }

      /// <summary>
      /// Attempt to locate a field in this definition's field list. If the field
      /// is not found, it will be added to the field list. Either way, the field's
      /// index in the view will be returned.
      /// </summary>
      /// <param name="dBField">The field to find or add.</param>
      /// <returns>The index of the field in the view (0-based).</returns>
      internal int FindOrAddField(DBField dBField)
      {
         int fieldIndex = DbFields.IndexOf(dBField);
         if (fieldIndex == -1)
         {
            fieldIndex = DbFields.Count;
            DbFields.Add(dBField);
         }
         return fieldIndex;
      }
   }
}
