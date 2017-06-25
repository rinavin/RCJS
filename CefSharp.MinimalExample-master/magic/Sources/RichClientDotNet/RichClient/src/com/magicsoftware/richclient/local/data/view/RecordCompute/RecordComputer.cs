using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.tasks;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{

   /// <summary>
   /// this class is responsible for computing of the record
   /// </summary>
   internal class RecordComputer : IComputeUnitStrategyContainer
   {
      public DataviewSynchronizer DataviewSynchronizer
      {
         get;
         set;
      }

      /// <summary>
      /// List of all compute units in the order they were added. When computing a record, this
      /// list is iterated and the units are computed in the order they appear in it.
      /// </summary>
      List<ComputeUnitStrategyBase> orderedComputeUnitStrategies = new List<ComputeUnitStrategyBase>();

      /// <summary>
      /// A collection of identifiable strategies. This allows accessing any compute unit that has an identifier.
      /// It is used by the 'ComputeUnit' method, to compute a single unit in the record.
      /// </summary>
      Dictionary<RecomputeId, ComputeUnitStrategyBase> identifiableStrategies = new Dictionary<RecomputeId, ComputeUnitStrategyBase>();


      /// <summary>
      /// Adds a compute unit to the record computer.
      /// </summary>
      /// <param name="computeFieldStrategy"></param>
      public void Add(ComputeUnitStrategyBase computeUnitStrategy)
      {
         orderedComputeUnitStrategies.Add((ComputeUnitStrategyBase)computeUnitStrategy);
         computeUnitStrategy.DataviewSynchronizer = DataviewSynchronizer;
      }
      
      public void Add(RecomputeId unitId, ComputeUnitStrategyBase computeUnitStrategy)
      {
         // First, any added compute unit is added to the ordered compute units list, because it should
         // participate in the overall record compute process.
         Add(computeUnitStrategy);

         // Secondly, add the unit to the identifiable units collection.
         Debug.Assert(unitId != null);
         identifiableStrategies[unitId] = computeUnitStrategy;
      }

      /// <summary>
      /// compute record
      /// </summary>
      /// <param name="useFirstRecord"></param>
      /// <returns></returns>
      internal UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions)
      {
         UnitComputeResult result = new UnitComputeResult();

         foreach (var item in orderedComputeUnitStrategies)
         {
            result = item.Compute(record, checkRange, recompute, computeInitExpressions);
            if (!result.Success)
               break;

         }
         DataviewSynchronizer.SetComputed(record);
         if (!result.Success)
            DataviewSynchronizer.RemoveRecord(record);

         return result;
      }

      /// <summary>
      /// Recompute a single record unit, whose id is unitId.
      /// </summary>
      /// <param name="unitId"></param>
      /// <param name="record"></param>
      internal void RecomputeUnit(RecomputeId unitId, IRecord record)
      {
         identifiableStrategies[unitId].Compute(record, false, true, true);
      }

   }




}
