using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// Describes the data that corresponds to a header line in a task's data view.
   /// The information in this interface supplements the data source information provided
   /// by the IDataSourceViewDefinition interface.
   /// </summary>
   internal interface IDataviewHeader : IDataSourceViewDefinition
   {
      /// <summary>
      /// Gets the header's identifier within the task to which it belongs.
      /// </summary>
      int Id { get; }

      /// <summary>
      /// Gets a value denoting whether this header is the main task's source, as
      /// defined in the task's data view.
      /// </summary>
      bool IsMainSource { get; }

      /// <summary>
      /// Gets the view's link mode (query, create, write, inner/outer join)
      /// </summary>
      LnkMode Mode { get; }

      /// <summary>
      /// Gets the field index, after which the link starts. This property applies
      /// only when IsMainSource == false.
      /// </summary>
      int LinkStartAfterField { get; }

      /// <summary>
      /// Gets the condition that determines whether the link should be evaluated or not. 
      /// This property applies only when IsMainSource == false.
      /// </summary>
      LnkEval_Cond LinkEvaluateCondition { get; }
      
      /// <summary>
      /// Implementing classes should evaluate the Link condition.
      /// </summary>
      /// <returns></returns>
      bool EvaluateLinkCondition();

   } 
}
