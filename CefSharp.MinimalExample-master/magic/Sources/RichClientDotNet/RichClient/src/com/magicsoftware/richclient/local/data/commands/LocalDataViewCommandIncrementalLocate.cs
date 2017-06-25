using System;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// special command to calculate the start position based on incremental locate
   /// </summary>
   class LocalDataViewCommandIncrementalLocate : LocalDataViewCommandLocateBase
   {
      int fieldId;
      string minValue;
      RecordComputer recordComputer;

      protected override RecordComputer RecordComputer
      {
         get
         {
            return recordComputer;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      protected override DbPos StartPosition
      {
         get
         {
            if (startPosition == null)
            {
               startPosition = TaskViews.ViewMain.SetCursorOnPosition(Reverse, new DbPos(true), BoudariesFlags.Range);
            }

            return startPosition;
         }
      }


      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewCommandIncrementalLocate(LocateQueryEventCommand command, string minValue)
         : base(command)
      {
         this.fieldId = command.FldId;
         this.minValue = minValue;
         InvalidateView = false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         IRangeDataBuilder origRangeDataBuilder = null;
         IFieldView fieldView = TaskViews.Fields[fieldId];

         // Initialize the stuff needed to locate the right record
         if (!fieldView.IsVirtual && !fieldView.IsLink)
         {
            // set the range builder, which will pass the data to the gateway
            origRangeDataBuilder = TaskViews.ViewMain.RangeBuilder;
            TaskViews.ViewMain.RangeBuilder = new IncrementalLocateRangeDataBuilder(TaskViews.ViewMain.ViewBoundaries, minValue, fieldView);
            // use the base record computer - no need for extra computing after the fetch
            recordComputer = base.RecordComputer;
         }
         else
         {
            // the gateway will not compute it for us - need to use a special record computer
            RecordComputerBuilder recordComputerBuilder = new IncrementalLocateRecordComputerBuilder(fieldView, minValue)
            {
               LocalDataviewManager = LocalDataviewManager,
               TaskViews = TaskViews
            };
            recordComputer = recordComputerBuilder.Build();
         }

         TaskViews.ViewMain.CurrentCursor = TaskViews.ViewMain.LocateCursor;

         // try and get a matching record
         base.Execute();

         TaskViews.ViewMain.CurrentCursor = TaskViews.ViewMain.defaultCursor;

         // restore ViewMain state
         if (origRangeDataBuilder != null)
            TaskViews.ViewMain.RangeBuilder = origRangeDataBuilder;

         return new GatewayResult();
      }
   }
}
