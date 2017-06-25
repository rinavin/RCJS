using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.rt;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// calculation of link condition
   /// </summary>
   class LinkConditionCalculator
   {
      /// <summary>
      /// level of link eveluation : Task or record
      /// </summary>
      internal LnkEval_Cond LinkEvaluationConditionLevel { get { return LinkHeader.LinkEvaluateCondition; } }

      /// <summary>
      /// Result of condition on TaskLevel
      /// </summary>
      bool LinkConditionResultOnTaskLevel { get; set; }

      /// <summary>
      /// dataviewHeader of the link
      /// </summary>
      internal IDataviewHeader LinkHeader { get; set; }


      /// <summary>
      /// calculates task condition
      /// </summary>
      internal void CalculateTaskCondition()
      {
         LinkConditionResultOnTaskLevel = GetLinkconditionValue(LnkEval_Cond.Task);

      }

      /// <summary>
      /// retruns result of link condition according to the level
      /// </summary>
      /// <param name="level"></param>
      /// <returns></returns>
      internal bool GetLinkconditionValue(LnkEval_Cond level)
      {
         if (LinkEvaluationConditionLevel == level)
            return LinkHeader.EvaluateLinkCondition();
         return true;
      }

      /// <summary>
      /// returns true if link should pe perfromed
      /// </summary>
      internal bool ShouldPerformLink
      {
         get
         {
            if (LinkEvaluationConditionLevel == LnkEval_Cond.Record)
               return GetLinkconditionValue(LnkEval_Cond.Record);
            return LinkConditionResultOnTaskLevel;

         }
      }
   }
}
