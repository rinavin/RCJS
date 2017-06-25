using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.gateways;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// strategy for computing of the link unit
   /// </summary>
   internal class ComputeLinkStrategy : CompositeComputeUnitStrategyBase
   {
      internal LinkView View { get; set; }

      /// <summary>
      /// list of compute strategies for field
      /// </summary>
      List<ComputeFieldStrategy> computeUnitStrategies = new List<ComputeFieldStrategy>();


      /// <summary>
      /// compute link
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      internal override UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions)
      {
         bool linkSuccess = false;
         UnitComputeResult unitComputeResult = new UnitComputeResult();
         if (View.DataviewHeader.Mode != LnkMode.Create && View.ShouldPerformLink)
            linkSuccess = FetchLinkedRecord(record, recompute);

         if (!linkSuccess)
            DataviewSynchronizer.InitLinkFields(View.DataviewHeader, record);

         if (checkRange)
            unitComputeResult = CheckRangeLinkFields(record);

         DataviewSynchronizer.SetLinkReturnValue(View.DataviewHeader, record, linkSuccess, recompute);


         return unitComputeResult;
      }

      /// <summary>
      /// fetch record from the link
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      private bool FetchLinkedRecord(IRecord record, bool recompute)
      {
         GatewayResult result = new GatewayResult();
         UnitComputeResult unitComputeResult = new UnitComputeResult();
         result = View.OpenCursor(false, new gatewaytypes.DbPos(true), BoudariesFlags.Locate);
         while (result.Success)
         {
            result = View.Fetch(record);
            if (result.Success)
            {
               unitComputeResult = ComputeFieldsAndCheckLocate(record, recompute);
               if (unitComputeResult.Success) //match found
                  break;
            }
         }
         View.CloseCursor();
         return result.Success;
      }


      /// <summary>
      /// computes fields checking locate values
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns> 
      private UnitComputeResult ComputeFieldsAndCheckLocate(IRecord record, bool recompute)
      {
         UnitComputeResult unitComputeResult = new UnitComputeResult();
         for (int i = 0; i < computeUnitStrategies.Count && unitComputeResult.Success; i++)
            unitComputeResult = (computeUnitStrategies[i].Compute(record, false, recompute, false));

         return unitComputeResult;
      }


      /// <summary>
      /// check range on the fields
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      private UnitComputeResult CheckRangeLinkFields(IRecord record)
      {
         UnitComputeResult unitComputeResult = new UnitComputeResult();
         for (int i = 0; i < computeUnitStrategies.Count && unitComputeResult.Success; i++)
            unitComputeResult = (computeUnitStrategies[i].CheckRange());
         return unitComputeResult;
      }

      /// <summary>
      /// add field compute strategy to link compute strategy
      /// </summary>
      /// <param name="computeFieldStrategy"></param>
      public override void Add(ComputeUnitStrategyBase childComputeUnit)
      {
         computeUnitStrategies.Add((ComputeFieldStrategy)childComputeUnit);
         childComputeUnit.DataviewSynchronizer = DataviewSynchronizer;
      }
   }
}
