using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.exp;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// strategy for computing the task range expression on each record
   /// </summary>
   class ComputeBoundaryExpressionStrategy : ComputeUnitStrategyBase
   {
      internal UnitComputeErrorCode FailureCode { private get; set; }
      Expression expression;

      public ComputeBoundaryExpressionStrategy(Expression expression)
      {
         Debug.Assert(expression != null);
         this.expression = expression;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="record"></param>
      /// <param name="checkRange"></param>
      /// <returns></returns>
      internal override UnitComputeResult Compute(IRecord record, bool checkRange, bool recompute, bool computeInitExpressions)
      {
         UnitComputeResult result = new UnitComputeResult();
         if (!checkRange)
            return result;

         if (!expression.DiscardCndRangeResult())
         {
            GuiExpressionEvaluator.ExpVal expVal = expression.evaluate(StorageAttribute.BOOLEAN);
            if (!expVal.BoolVal)
               result.ErrorCode = UnitComputeErrorCode.RangeFailed;
         }
         return result;
      }
   }
}
