using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// base class for units that compute record
   /// </summary>
   internal abstract class ComputeUnitStrategyBase
   {
      internal DataviewSynchronizer DataviewSynchronizer { get; set; }

      /// <summary>
      /// computes the unit
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>  
      internal abstract UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions);
   }


}
