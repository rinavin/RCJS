using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// strategy for computing of the field
   /// </summary>
   internal class ComputeFieldStrategy : ComputeUnitStrategyBase
   {
      internal IFieldView Field { get; set; }
      internal bool CheckLocate { get; set; }
      internal bool ShouldComputeOnFetch { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="record"></param>
      /// <param name="checkRange"> true if we need to check range</param>
      /// <param name="recompute"> true if need to recompute</param>
      /// <param name="computeInitExpressions"> true if we need to compute reals</param>
      /// <returns></returns>
      internal override UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions)
      {
         UnitComputeResult result = new UnitComputeResult();

         // performance improvement QCR #178301
         // Compute is expensive operation on mobile platforms, need to prevent unnesessary compute
         // value of real fields are already fetched from the database, so no need to compute them in most of the cases
         // computeInitExpressions will be true when we do need to compute them, like in create line

        

         if (computeInitExpressions || ShouldComputeOnFetch)
            Field.Compute(false);
         else
         {
            Field.TakeValFromRec();
         }

         //If toolkit locates Or User locates present then check locates to get correct start position.
         if (CheckLocate || 
            ((Field.IsVirtual || Field.IsLink) && DataviewSynchronizer.DataviewManager.UserLocates.Count > 0 && DataviewSynchronizer.DataviewManager.TaskViews.UseUserLocates))
         {
            result = CheckLocates();
         }

         if (result.Success && checkRange && Field.ShouldCheckRangeInCompute)
            result = CheckRange();


         return result;
      }

      /// <summary>
      /// check field's range
      /// </summary>
      /// <returns></returns>
      internal UnitComputeResult CheckRange()
      {
         UnitComputeResult result = new UnitComputeResult();
         Boundary boundary = Field.Range;
         if (boundary != null)
         {
            boundary.compute(false);
            if (!boundary.checkRange(Field.ValueFromCurrentRecord, Field.IsNullFromCurrentRecord))
               result.ErrorCode = UnitComputeErrorCode.RangeFailed;
         }

         // check the user ranges
         if (result.Success && (Field.IsVirtual || Field.IsLink))
         {
            List<UserRange> userRanges = DataviewSynchronizer.DataviewManager.UserRanges;
            if (userRanges != null)
            {
               // go over all user ranges, check those with the right field id
               foreach (UserRange userRangeData in userRanges)
               {
                  if (userRangeData.veeIdx - 1 == Field.Id)
                     result = CheckUserRange(Field, userRangeData);
               }
            }
         }
         return result;
      }


      /// <summary>
      /// check field's locates
      /// </summary>
      /// <returns></returns>
      internal UnitComputeResult CheckLocates()
      {
         UnitComputeResult result = new UnitComputeResult();
         Boundary boundary = Field.Locate;

         // If toolkit locates and user locates both are present, then after view refresh user locates should be used and not toolkit locate.
         // DataviewSynchronizer.DataviewManager.TaskViews.UseUserLocates flag is set in view refresh command and once start position is calculated 
         // then reset this flag.
         if (DataviewSynchronizer.DataviewManager.TaskViews.UseUserLocates)
         {
            // check the user ranges
            if (result.Success && (Field.IsVirtual || Field.IsLink))
            {
               List<UserRange> userLocates = DataviewSynchronizer.DataviewManager.UserLocates;
               if (userLocates != null)
               {
                  // go over all user ranges, check those with the right field id
                  foreach (UserRange userRangeData in userLocates)
                  {
                     if (userRangeData.veeIdx - 1 == Field.Id)
                     {
                        result = CheckUserRange(Field, userRangeData);
                        if (!result.Success)
                        {
                           result.ErrorCode = UnitComputeErrorCode.LocateFailed;
                        }
                     }
                     
                  }
               }
            }
         }
         else
         {
            if (boundary != null)
            {
               // Compute of locate should search for the range of data only if it done on the main datasource and not for link.
               // Locate on link is actually a range.
               boundary.compute(!Field.IsLink);
               if (!Field.Locate.checkRange(Field.ValueFromCurrentRecord, Field.IsNullFromCurrentRecord))
                  result.ErrorCode = UnitComputeErrorCode.LocateFailed;
            }
         }
         return result;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fieldView"></param>
      /// <param name="userRange"></param>
      /// <returns></returns>
      private UnitComputeResult CheckUserRange(IFieldView fieldView, UserRange userRange)
      {
         UnitComputeResult result = new UnitComputeResult();

         string fieldValue = fieldView.ValueFromCurrentRecord.Trim();

         switch (fieldView.StorageAttribute)
         {
            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               {
                  int fieldValueInt = new NUM_TYPE(fieldValue).NUM_2_LONG();

                  // check the min value
                  if (!userRange.nullMin && !userRange.discardMin)
                  {
                     int min = new NUM_TYPE(userRange.min).NUM_2_LONG();
                     if (fieldValueInt < min)
                        result.ErrorCode = UnitComputeErrorCode.RangeFailed;
                  }

                  // check the max value
                  if (result.Success && !userRange.nullMax && !userRange.discardMax)
                  {
                     int max = new NUM_TYPE(userRange.max).NUM_2_LONG();
                     if (fieldValueInt > max)
                        result.ErrorCode = UnitComputeErrorCode.RangeFailed;
                  }
               }
               break;

            default:
               {
                  // check the min value
                  if (!userRange.nullMin && !userRange.discardMin)
                     if (String.Compare(fieldValue, (string)userRange.min, StringComparison.Ordinal) < 0)
                        result.ErrorCode = UnitComputeErrorCode.RangeFailed;

                  // check the max value
                  if (result.Success && !userRange.nullMax && !userRange.discardMax)
                     if (String.Compare(fieldValue, (string)userRange.max, StringComparison.Ordinal) > 0)
                        result.ErrorCode = UnitComputeErrorCode.RangeFailed;
               }
               break;
         }
         return result;
      }
   }
}
