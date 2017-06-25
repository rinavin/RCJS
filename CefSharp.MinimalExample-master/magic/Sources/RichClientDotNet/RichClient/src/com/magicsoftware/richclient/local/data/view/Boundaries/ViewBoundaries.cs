using System.Collections.Generic;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view.fields;

namespace com.magicsoftware.richclient.local.data.view.Boundaries
{
   /// <summary>
   /// collection of fields boundaries of the view
   /// </summary>
   internal class ViewBoundaries
   {

      internal RuntimeViewBase RuntimeViewBase { get; set; }

      /// <summary>
      /// list of view boundaries
      /// </summary>
      List<FieldBoundaries> runtimeBoundaries = new List<FieldBoundaries>();

      internal bool HasLocate { get; private set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="Field"></param>
      /// <param name="indexInView"></param>
      /// <returns></returns>
      private FieldBoundaries GetFieldBoundary(IFieldView Field, int indexInView)
      {
         FieldBoundaries boundary = new FieldBoundaries();
         RuntimeReadOnlyView runtimeRealView = RuntimeViewBase as RuntimeReadOnlyView;

         boundary.IndexInView = indexInView;
         boundary.IsLink = Field.IsLink;
         boundary.StorageAttribute = Field.StorageAttribute;
         
         if (runtimeRealView != null)
            boundary.DBField = runtimeRealView.GetDbField(boundary.IndexInView);
         
         return boundary;
      }

      /// <summary>
      /// Get the "range" boundary definition of the field
      /// </summary>
      /// <param name="Field"></param>
      /// <param name="indexInView"></param>
      /// <returns></returns>
      internal FieldBoundaries GetFieldRange(IFieldView Field, int indexInView)
      {
         FieldBoundaries boundary = null;
         if (Field.Range != null)
         {
            boundary = GetFieldBoundary(Field, indexInView);
            boundary.Range = Field.Range;
         }
         return boundary;
      }

      /// <summary>
      /// Get the "locate" boundary definition of the field
      /// </summary>
      /// <param name="Field"></param>
      /// <param name="indexInView"></param>
      /// <returns></returns>
      internal FieldBoundaries GetFieldLocate(IFieldView Field, int indexInView)
      {
         FieldBoundaries boundary = null;
         if (Field.Locate != null)
         {
            boundary = GetFieldBoundary(Field, indexInView);
            boundary.Locate = Field.Locate;
         }
         return boundary;
      }

      /// <summary>
      /// add field's ranges to the collection
      /// </summary>
      /// <param name="field"></param>
      /// <param name="indexInView"></param>
      internal void AddFieldBoundaries(IFieldView field, int indexInView)
      {
         FieldBoundaries fieldBoundaries = GetFieldRange(field, indexInView);
         if (fieldBoundaries != null)
            runtimeBoundaries.Add(fieldBoundaries);

         fieldBoundaries = GetFieldLocate(field, indexInView);
         if (fieldBoundaries != null)
         {
            runtimeBoundaries.Add(fieldBoundaries);
            HasLocate = true;
         }
      }

      /// <summary>
      /// compute range and locate data
      /// </summary>
      /// <returns></returns>
      internal List<RangeData> ComputeBoundariesData(BoudariesFlags cursorPositionFlag)
      {
         List<RangeData> rangesList = new List<RangeData>();
         foreach (var item in runtimeBoundaries)
         {            
            if ((cursorPositionFlag & BoudariesFlags.Range) != 0)
            {
               if (item.Range != null)
                  rangesList.Add(item.ComputeRangeData());
            }

            if ((cursorPositionFlag & BoudariesFlags.Locate) != 0)
            {
               if (item.Locate != null)
                  rangesList.Add(item.ComputeLocateData());
            }
         }
         SetPartDatetimeMatch(rangesList);
         return rangesList;
      }

      /// <summary>
      /// Refactored from SetDatetimeInSlctTbl
      /// Sets pairs of date time fields in ranges
      /// </summary>
      /// <param name="rangesList">list of computed ranges</param>
      void SetPartDatetimeMatch(List<RangeData> rangesList)
      {
         for (int i = 0; i < runtimeBoundaries.Count; i++)
         {
            long isnOfTimePair = runtimeBoundaries[i].IsnOfTimePair;
            if (isnOfTimePair != 0)
            {
               int? timeIndex = FindTimePairIndexInRanges(isnOfTimePair);
               if (timeIndex != null)
               {
                  //TODO : check after date is implemented in ranges if we need the "+1" to time index
                  rangesList[i].DatetimeRangeIdx = (int)timeIndex + 1;
                  rangesList[(int)timeIndex].DatetimeRangeIdx = -1;
                  break;
               }
            }

         }
      }

      /// <summary>
      /// find time pair for date
      /// </summary>
      /// <param name="dateIsn">  isn of date field in table</param>
      /// <returns> index of time pare of given date. If not found returns null</returns>
      int? FindTimePairIndexInRanges(long dateIsn)
      {
         for (int i = 0; i < runtimeBoundaries.Count; i++)
         {
            FieldBoundaries boundary = runtimeBoundaries[i];
            if (boundary.TimeFieldDbIsn == dateIsn)
               return i;
         }
         return null;
      }
   }
}
