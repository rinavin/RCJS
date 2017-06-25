using System;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.view.Boundaries
{
   /// <summary>
   /// class representing field boundary - range/locate
   /// </summary>
   internal class FieldBoundaries
   {
      /// <summary>
      /// index of field in current view
      /// </summary>
      internal int IndexInView { get; set; }

      /// <summary>
      /// Range of the field
      /// </summary>
      internal Boundary Range { get; set; }

      /// <summary>
      /// Locate of the field
      /// </summary>
      internal Boundary Locate { get; set; }

      internal bool IsLink { get; set; }

      /// <summary>
      /// Storage attribute of the field
      /// </summary>
      internal StorageAttribute StorageAttribute { get; set; }

      internal DBField DBField { get; set; }


      /// <summary>
      /// compute range  values of the fields
      /// </summary>
      /// <returns></returns>
      internal RangeData ComputeRangeData()
      {
         //tsk_vee_rng
         RangeData rangeData = new RangeData() { FieldIndex = IndexInView };
         Range.compute(false);

         //check this :
         char maxChar = UtilStrByteMode.isLocaleDefLangJPN() ? (char)0xFFEE : (char)0x7FFF;

         rangeData.Min = BuildBoundaryValue(Range.hasMinExp(), Range.MaxEqualsMin, Range.MinExpVal, Char.MinValue, Range.DiscardMin);
         rangeData.Max = BuildBoundaryValue(Range.hasMaxExp(), Range.MaxEqualsMin, Range.MaxExpVal, maxChar, Range.DiscardMax);
         return rangeData;
      }

      /// <summary>
      /// compute locate values of the fields
      /// </summary>
      /// <returns></returns>
      internal RangeData ComputeLocateData()
      {
         RangeData locateData = new RangeData() { FieldIndex = IndexInView };

         // Compute of locate should search for the range of data only if it done on the main datasource and not for link.
         // Locate on link is actually a range.
         Locate.compute(!IsLink);

         //check this :
         char maxChar = UtilStrByteMode.isLocaleDefLangJPN() ? (char)0xFFEE : (char)0x7FFF;

         locateData.Min = BuildBoundaryValue(Locate.hasMinExp(), Locate.MaxEqualsMin, Locate.MinExpVal, Char.MinValue, Locate.DiscardMin);
         locateData.Max = BuildBoundaryValue(Locate.hasMaxExp(), Locate.MaxEqualsMin, Locate.MaxExpVal, maxChar, Locate.DiscardMax);
         return locateData;
      }

      /// <summary>
      /// compute range type
      /// </summary>
      /// <param name="hasBoundary"></param>
      /// <param name="isMaxEqualsMin"></param>
      /// <returns></returns>
      RangeType ComputeRangeType(bool hasBoundary, bool isMaxEqualsMin)
      {
         RangeType rangeType = RangeType.RangeNoVal;
         if (hasBoundary)
            rangeType = isMaxEqualsMin ? RangeType.RangeMinMax : RangeType.RangeParam;
         return rangeType;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasBoundary"></param>
      /// <param name="IsMaxEqualsMin"></param>
      /// <param name="expVal"></param>
      /// <returns></returns>
      BoundaryValue BuildBoundaryValue(bool hasBoundary, bool IsMaxEqualsMin, GuiExpressionEvaluator.ExpVal expVal, char wildCharFiller, bool shouldDiscard)
      {
         BoundaryValue boundaryValue = new BoundaryValue();
         boundaryValue.Type = ComputeRangeType(hasBoundary, IsMaxEqualsMin);
         if (hasBoundary)
         {
            boundaryValue.Value = GetFieldValue(expVal, wildCharFiller);

            if (shouldDiscard)
               boundaryValue.Discard = true;
         }

         return boundaryValue;
      }

      /// <summary>
      /// exp value to field value
      /// </summary>
      /// <param name="expVal"></param>
      /// <returns></returns>
      FieldValue GetFieldValue(GuiExpressionEvaluator.ExpVal expVal, char wildCharFiller)
      {
         FieldValue fieldValue = new FieldValue() { IsNull = expVal.IsNull };
         if (!expVal.IsNull)
         {
            switch (expVal.Attr)
            {
               case StorageAttribute.ALPHA:
               case StorageAttribute.UNICODE:
                  fieldValue.Value = expVal.StrVal;
                  break;
               case StorageAttribute.NUMERIC:
               case StorageAttribute.DATE:
               case StorageAttribute.TIME:
                  fieldValue.Value = expVal.MgNumVal;
                  break;
               case StorageAttribute.BOOLEAN:
                  fieldValue.Value = (expVal.BoolVal == true) ? 1 : 0;
                  break;
               default:
                  //TODO: Error
                  break;
            }
         }

         return fieldValue;
      }

      /// <summary>
      /// returns db isn of the time pair for date variable 
      /// </summary>
      internal long IsnOfTimePair
      {
         get
         {
            return StorageAttribute == StorageAttribute.DATE && DBField != null ? DBField.PartOfDateTime : 0;
         }
      }

      /// <summary>
      /// returns isn of the time field
      /// </summary>
      internal int TimeFieldDbIsn
      {
         get
         {
            return StorageAttribute == StorageAttribute.TIME && DBField != null ? DBField.Isn : 0;
         }
      }
   }

}
