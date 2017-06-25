using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.Boundaries;

namespace com.magicsoftware.richclient.local.data.view.RangeDataBuilder
{
   class DataControlRangeDataBuilder : IRangeDataBuilder
   {
      IEnumerable<FieldBoundaries> fieldBoundaries;

      public DataControlRangeDataBuilder(IEnumerable<FieldBoundaries> fieldBoundaries)
      {
         this.fieldBoundaries = fieldBoundaries;
      }

      public List<gatewaytypes.RangeData> Build(BoudariesFlags boundariesFlag)
      {
         var rangeData = new List<gatewaytypes.RangeData>();

         foreach (var fb in fieldBoundaries)
         {
            rangeData.Add(fb.ComputeRangeData());
         }
         return rangeData;
      }
   }
}
