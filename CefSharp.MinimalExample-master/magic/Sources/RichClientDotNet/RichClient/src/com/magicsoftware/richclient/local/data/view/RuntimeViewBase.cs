using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.local.data.view.Boundaries;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.local.data.gateways;

namespace com.magicsoftware.richclient.local.data.view
{

   /// <summary>
   /// Base class for view
   /// </summary>
   internal class RuntimeViewBase
   {
      internal const int MAIN_VIEW_ID = -1;

      FieldValues currentValues = new FieldValues();
      FieldValues oldValues = new FieldValues();
      internal DataviewSynchronizer DataviewSynchronizer { get { return LocalDataviewManager.DataviewSynchronizer; } }
      internal LocalDataviewManager LocalDataviewManager { get; set; }


      /// <summary>
      /// mapping of field index in record by field index in the view
      /// </summary>
      protected Dictionary<int, int> fieldIndexInRecordByIndexInView = new Dictionary<int, int>();

      /// <summary>
      /// mapping of field index in view  by field index in the record
      /// </summary>
      protected Dictionary<int, int> fieldIndexInViewByIndexInRecord = new Dictionary<int, int>();

      /// <summary>
      /// boundaries(ranges and locates) of the view
      /// </summary>
      private ViewBoundaries viewBoundaries = new ViewBoundaries();

      internal virtual FieldValues CurrentValues
      {
         get { return currentValues; }
      }

      internal virtual FieldValues OldValues
      {
         get { return oldValues; }
      }

     

      internal int GetFieldIndexInRecordByIndexInView(int i)
      {
         return fieldIndexInRecordByIndexInView[i];
      }

      /// <summary>
      /// GetFieldIndexInViewByIndexInRecord ()
      /// </summary>
      internal int GetFieldIndexInViewByIndexInRecord(int i)
      {
         return fieldIndexInViewByIndexInRecord[i];
      }


      internal ViewBoundaries ViewBoundaries
      {
         get { return viewBoundaries; }
      }

      public virtual bool HasLocate { get { return (ViewBoundaries.HasLocate || LocalDataviewManager.HasUserLocates); } }

      public RuntimeViewBase()
      {
         viewBoundaries.RuntimeViewBase = this;
      }
      /// <summary>
      /// map field definitions
      /// </summary>
      /// <param name="field"></param>
      /// <param name="indexInRecordView"></param>
      internal virtual void MapFieldDefinition(IFieldView field, int indexInRecordView)
      {
         //map index in view to index in record and backwards 
         int indexInView = currentValues.Add(new FieldValue());
         fieldIndexInRecordByIndexInView[indexInView] = indexInRecordView;
         fieldIndexInViewByIndexInRecord[indexInRecordView] = indexInView;
      }

      /// <summary>
      /// Copy Values
      /// </summary>
      /// <param name="record"></param>
      internal void CopyValues(IRecord record)
      {
         for (int i = 0; i < CurrentValues.Count; i++)
         {
            int indexInRecord = fieldIndexInRecordByIndexInView[i];
            record.SetFieldValue(indexInRecord, CurrentValues.IsNull(i),(String)CurrentValues.GetValue(i));
         }

      }

      internal virtual GatewayResult Fetch(IRecord record)
      {
         record.setOldRec();
         DataviewSynchronizer.SetCurrentRecord(record.getId());
         return new GatewayResult();

      }

      internal virtual GatewayResult FetchCurrent(IRecord record)
      {
         return new GatewayResult();
      }
   }
}
