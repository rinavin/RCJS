using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   internal abstract class CompositeComputeUnitStrategyBase : ComputeUnitStrategyBase, IComputeUnitStrategyContainer
   {
      public abstract void Add(ComputeUnitStrategyBase childComputeUnit);
   }
}
