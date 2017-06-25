using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{

   /// <summary>
   /// this class is responsible for building a records computer which includes a computer for the locate expression
   /// </summary>
   internal class LocateExpressionRecordComputerBuilder : RecordComputerBuilder
   {
      /// <summary>
      /// builds record computer
      /// </summary>
      /// <returns></returns>
      internal override RecordComputer Build()
      {
         RecordComputer recordComputer = base.Build();

         AddBoundaryExpressionComputeStrategy(recordComputer, PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE, UnitComputeErrorCode.LocateFailed);
        
         return recordComputer;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="field"></param>
      protected override bool ShouldCheckLocateOnField(IFieldView field)
      {
         //If the field is virtual and has a locate  we need to add the locate computation in the strategy
         return field.IsVirtual && field.Locate != null;
      }
   }
}

