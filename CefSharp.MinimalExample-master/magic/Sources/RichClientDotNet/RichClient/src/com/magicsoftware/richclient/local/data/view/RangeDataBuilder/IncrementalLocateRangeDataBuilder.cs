using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view.Boundaries;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using System.Diagnostics;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data.view.RangeDataBuilder
{
   /// <summary>
   /// Range data builder for incremental locate - uses the view boundaries ranges and the range data from the incremental locate string
   /// </summary>
   class IncrementalLocateRangeDataBuilder : IRangeDataBuilder
   {
      string minValue;
      ViewBoundaries viewBoundaries;
      IFieldView fieldView;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="viewBoundaries"></param>
      /// <param name="rangeData"></param>
      public IncrementalLocateRangeDataBuilder(ViewBoundaries viewBoundaries, string minValue, IFieldView fieldView)
      {
         this.viewBoundaries = viewBoundaries;
         this.minValue = minValue;
         this.fieldView = fieldView;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public List<RangeData> Build(BoudariesFlags boundariesFlag)
      {
         List<RangeData> rangesData = viewBoundaries.ComputeBoundariesData(BoudariesFlags.Range);

         rangesData.Add(BuildRangeData(minValue));

         return rangesData;
      }

      /// <summary>
      /// build the RangeData object from the min and max strings
      /// </summary>
      /// <param name="minVal"></param>
      /// <param name="maxVal"></param>
      /// <returns></returns>
      private RangeData BuildRangeData(string minVal)
      {
         int indexInView = ((RuntimeReadOnlyView)viewBoundaries.RuntimeViewBase).GetFieldIndexInViewByIndexInRecord(fieldView.Id);
         RangeData rangeData = new RangeData() { FieldIndex = indexInView };

         object mgMinVal = StringToMgVal(fieldView.StorageAttribute, minVal, fieldView.Length, fieldView.Picture);
         object minValue;

         if (fieldView.StorageAttribute == StorageAttribute.ALPHA || fieldView.StorageAttribute == StorageAttribute.UNICODE)
            minValue = StrUtil.SearchAndReplaceWildChars((String)mgMinVal, fieldView.Length, Char.MinValue);
         else
            minValue = mgMinVal;

         rangeData.Min.Value = new FieldValue() { Value = minValue, IsNull = false };
         rangeData.Min.Type = RangeType.RangeParam;

         object maxValue;
         //if looking for a string, we need to padd it with max chars, to allow locating strings which begin with the requested value
         if (fieldView.StorageAttribute == StorageAttribute.ALPHA || fieldView.StorageAttribute == StorageAttribute.UNICODE)
         {
            maxValue = ((String)mgMinVal).Replace('\0', Char.MaxValue);
            maxValue = StrUtil.SearchAndReplaceWildChars((String)maxValue, fieldView.Length, Char.MaxValue);
         }
         else
            maxValue = mgMinVal;
         rangeData.Max.Value = new FieldValue() { Value = maxValue, IsNull = false };
         rangeData.Max.Type = RangeType.RangeParam;
        
         return rangeData;
      }

      /// <summary>
      /// convert the string typed by the user to the type the gateway converter expects
      /// </summary>
      /// <param name="type"></param>
      /// <param name="value"></param>
      /// <param name="length"></param>
      /// <returns></returns>
      private object StringToMgVal(StorageAttribute type, string value, int length, string picture)
      {
         switch (type)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               {
                  String newVal = value;
                  newVal = newVal.PadRight(length, '\0');
                  return newVal;
               }

            case StorageAttribute.NUMERIC:
               NUM_TYPE numType = new NUM_TYPE(value, new PIC(picture, StorageAttribute.NUMERIC, 0), 0);
               return numType;

            default:
               Debug.Assert(false, "Unhandled case in incremental locate");
               return value;
         }
      }
   }
}
