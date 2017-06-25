using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.Boundaries;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view.fields;

namespace com.magicsoftware.richclient.local.data.view.RangeDataBuilder
{
   /// <summary>
   /// build the RangeData from the view boundaries according to the range/locate flag. This is the common builder for records fetching
   /// </summary>
   class ViewRangeDataBuilder : IRangeDataBuilder
   {
      ViewBoundaries viewBoundaries;

      internal List<RangeData> userRanges;
      internal List<RangeData> userLocates;

      LocalDataviewManager dataviewManager;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="viewBoundaries"></param>
      /// <param name="cursorPositionFlag"></param>
      public ViewRangeDataBuilder(ViewBoundaries viewBoundaries, LocalDataviewManager dataviewManager)
      {
         this.viewBoundaries = viewBoundaries;
         this.dataviewManager = dataviewManager;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public List<RangeData> Build(BoudariesFlags boundariesFlag)
      {
         List<RangeData> rangesData = new List<RangeData>();

         // If toolkit locates and runtime locates are present, then use user locates and not use toolkit locates.
         if (userLocates == null || userLocates.Count == 0)
         {
            rangesData = viewBoundaries.ComputeBoundariesData(boundariesFlag);
         }
         else
         {
            rangesData = viewBoundaries.ComputeBoundariesData(BoudariesFlags.Range);
         }

         if((boundariesFlag & BoudariesFlags.Range) != 0 && userRanges != null)
         {
            rangesData.AddRange(userRanges);
         }

         if((boundariesFlag & BoudariesFlags.Locate) != 0 && userLocates != null)
         {
            rangesData.AddRange(userLocates);
         }
         
         return rangesData;
      }
   }

}
