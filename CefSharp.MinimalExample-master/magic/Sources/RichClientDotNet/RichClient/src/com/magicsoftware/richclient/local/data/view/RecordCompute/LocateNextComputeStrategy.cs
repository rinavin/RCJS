using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;

namespace RichClient.src.com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// Statergy for Locate next Operation.
   /// If Start position from which next record to be located is equal to the given record, then locate fails.
   /// If it is not equal then it indicates successful locate.
   /// </summary>
   class LocateNextComputeStrategy : ComputeUnitStrategyBase
   {
      /// <summary>
      /// Start position from which next record to be located.
      /// </summary>
      DbPos StartPosition { get; set; }

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="startPosition"></param>
      public LocateNextComputeStrategy(DbPos startPosition)
      {
         StartPosition = startPosition;
      }

      /// <summary>
      /// Compute
      /// </summary>
      /// <param name="record"></param>
      /// <param name="checkRange"></param>
      /// <param name="recompute"></param>
      /// <returns></returns>
      internal override UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions)
      {
         DbPos outPos;
         UnitComputeResult result = new UnitComputeResult();

         //Get the position of the given record from the position cache.
         DataviewSynchronizer.DataviewManager.PositionCache.TryGetValue(new PositionId(record.getId()), out outPos);

         //If Start position from which next record to be located is equal to the given record, then locate fails.
         //If it is not equal then it indicates successful locate.
         if (StartPosition.Equals(outPos))
         {
            result.ErrorCode = UnitComputeErrorCode.LocateFailed;
         }
         else
         {
            result.ErrorCode = UnitComputeErrorCode.NoError;
         }
         
         return result;
      }
   }
}
