using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.fields;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// build the record computer for incremental locate
   /// </summary>
   class IncrementalLocateRecordComputerBuilder : RecordComputerBuilder
   {
      IFieldView fieldView;
      string incrementalLocateString;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="fieldView"></param>
      /// <param name="value"></param>
      public IncrementalLocateRecordComputerBuilder(IFieldView fieldView, string value)
      {
         this.fieldView = fieldView;
         this.incrementalLocateString = value;
      }

      /// <summary>
      /// builds record computer
      /// </summary>
      /// <returns></returns>
      internal override RecordComputer Build()
      {
         RecordComputer recordComputer = base.Build();

         recordComputer.Add(new IncrementalLocateComputeStrategy(fieldView, incrementalLocateString));

         return recordComputer;
      }
   }
}
