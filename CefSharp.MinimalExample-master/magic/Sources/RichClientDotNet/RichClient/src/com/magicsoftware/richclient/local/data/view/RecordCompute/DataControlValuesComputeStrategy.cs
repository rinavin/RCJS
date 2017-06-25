using System.Collections.Generic;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// Strategy for computing a data control's display values and field values.
   /// Unlike other view dependent elements, a data control is not dependent on
   /// the task's view but actually on designated view.<para/>
   /// However, the compute strategy's Compute method is called for each record
   /// of the _Task's_ view and in each invocation we need to build the data
   /// control's values. The building of the values set is done by the 
   /// dcValuesBuilder passed to the strategy when instantiated.<para/>
   /// The strategy also reduces the computations by mapping the DC values lists 
   /// according to the range set that was used to create them. This way, when a 
   /// similar range set is used for subsequent computations, the DC values need
   /// not be rebuilt, but only fetched from the existing values list.
   /// </summary>
   class DataControlValuesComputeStrategy : ComputeUnitStrategyBase
   {
      private DcValuesBuilderBase dcValuesBuilder;
      private int boundControlId;
      private IRangeDataBuilder dcValuesRangeBuilder;
      private DataSourceId rangeDataSourceId;

      /// <summary>
      /// Instantiates a DC values compute strategy for a specific data control. The compute
      /// strategy will use the given DC values builder to build the list. 
      /// </summary>
      /// <param name="boundControlId"></param>
      /// <param name="dcValuesBuilder"></param>
      /// <param name="dcValuesRangeBuilder"></param>
      /// <param name="rangeDataSourceId"></param>
      public DataControlValuesComputeStrategy(int boundControlId, DcValuesBuilderBase dcValuesBuilder, IRangeDataBuilder dcValuesRangeBuilder, DataSourceId rangeDataSourceId)
      {
         this.dcValuesBuilder = dcValuesBuilder;
         this.boundControlId = boundControlId;
         this.dcValuesRangeBuilder = dcValuesRangeBuilder;
         this.rangeDataSourceId = rangeDataSourceId;
      }

      internal override UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions)
      {
         var rangeData = new DataControlRangeDataCollection(rangeDataSourceId, dcValuesRangeBuilder, boundControlId);
         var dcv = (DataviewSynchronizer.GetDcValues(rangeData));
         if (dcv == null)
         {
            // The range does not exist in the map - need to create a new DcValues object.
            dcv = dcValuesBuilder.Build();
            DataviewSynchronizer.AddDcValues(dcv, rangeData);
         }

         DataviewSynchronizer.ApplyDcValues(dcv, record, boundControlId);
         return new UnitComputeResult();
      }

   }

}
