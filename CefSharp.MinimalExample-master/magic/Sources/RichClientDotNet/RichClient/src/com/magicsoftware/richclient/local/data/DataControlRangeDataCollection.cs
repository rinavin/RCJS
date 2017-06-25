using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data
{
   class DataControlRangeDataCollection
   {
      DataSourceId dataSourceIdentifier;
      int controlId;
      List<RangeData> rangeDataList;
      ListComparer<List<RangeData>> rangeComparer;

      public DataControlRangeDataCollection(DataSourceId dataSourceIdentifier, IRangeDataBuilder rangeDataBuilder, int controlId)
      {
         this.dataSourceIdentifier = dataSourceIdentifier;
         rangeDataList = rangeDataBuilder.Build(BoudariesFlags.Range);
         rangeComparer = new ListComparer<List<RangeData>>();
         this.controlId = controlId;
      }

      public override bool Equals(object obj)
      {
         DataControlRangeDataCollection other = obj as DataControlRangeDataCollection;
         if (other == null)
            return false;

         return (dataSourceIdentifier == other.dataSourceIdentifier) && 
            (controlId == other.controlId) &&
            rangeComparer.Equals(rangeDataList, other.rangeDataList);
      }

      public override int GetHashCode()
      {
         HashCodeBuilder hashBuilder = new HashCodeBuilder();
         hashBuilder.Append(dataSourceIdentifier)
            .Append(controlId)
            .Append(rangeComparer.GetHashCode(rangeDataList));
         return hashBuilder.HashCode;
      }
   }
}
