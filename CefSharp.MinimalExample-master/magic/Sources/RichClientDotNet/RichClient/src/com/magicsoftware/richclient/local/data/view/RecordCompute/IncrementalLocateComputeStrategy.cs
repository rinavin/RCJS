using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using System.Diagnostics;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// check record according to incremental locate data
   /// This strategy should be executed only for non-real fields
   /// </summary> 
   class IncrementalLocateComputeStrategy : ComputeUnitStrategyBase
   {
      IFieldView fieldView;
      object minValue;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="dataviewSynchronizer"></param>
      /// <param name="recordComputer"></param>
      /// <param name="fieldView"></param>
      /// <param name="value"></param>
      public IncrementalLocateComputeStrategy(IFieldView fieldView, string minValue)
      {
         Debug.Assert(fieldView.IsVirtual || fieldView.IsLink);

         this.fieldView = fieldView;
         switch (fieldView.StorageAttribute)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
            case StorageAttribute.NUMERIC:
               this.minValue = minValue;
               break;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="record"></param>
      /// <param name="checkRange"></param>
      /// <returns></returns>
      internal override UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions)
      {
         UnitComputeResult result = new UnitComputeResult();

         string fieldValue = fieldView.ValueFromCurrentRecord;

         switch (fieldView.StorageAttribute)
         {
            case StorageAttribute.NUMERIC:
               {
                  NUM_TYPE fetchedValue = new NUM_TYPE(fieldValue);
                  NUM_TYPE minimumValue = new NUM_TYPE(DisplayConvertor.Instance.toNum((string)minValue, new PIC(fieldView.Picture, StorageAttribute.NUMERIC, 0), 0));

                  if (NUM_TYPE.num_cmp(fetchedValue, minimumValue) < 0)
                     result.ErrorCode = UnitComputeErrorCode.LocateFailed;
               }
               break;

            case StorageAttribute.UNICODE:
            case StorageAttribute.ALPHA:
               {
                  //QCR # 444514 : Check whether given Incremental Locate String is in between min and max of fetched value. 
                  //               Locate gets successful when fetched record is greater than or equal to search Incremental Locate String value.
                  string minimumValue = minValue.ToString().PadRight(fieldView.Length, char.MinValue);
                  string maximumValue = minValue.ToString().PadRight(fieldView.Length, char.MaxValue);

                  if (String.Compare(fieldValue, (string)minimumValue, StringComparison.Ordinal) < 0)
                     result.ErrorCode = UnitComputeErrorCode.LocateFailed;
                  else if (String.Compare(fieldValue, (string)maximumValue, StringComparison.Ordinal) > 0)
                     result.ErrorCode = UnitComputeErrorCode.LocateFailed;
               }
               break;
         }

         return result;
      }
   }
}
