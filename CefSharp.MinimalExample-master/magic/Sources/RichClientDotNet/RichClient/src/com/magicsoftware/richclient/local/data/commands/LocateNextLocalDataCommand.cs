using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using RichClient.src.com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// Find Next start position to be located.
   /// </summary>
   class LocateNextLocalDataCommand : LocalDataViewCommandLocateBase
   {
      RecordComputer recordComputer;
      IClientCommand Command { get; set; }

      /// <summary>
      /// RecordComputer For Locate Next command.
      /// </summary>
      protected override RecordComputer RecordComputer
      {
         get
         {
            return recordComputer;
         }
      }

      /// <summary>
      /// Start Position
      /// </summary>
      protected override DbPos StartPosition
      {
         get
         {
            if (startPosition == null)
            {
               startPosition = base.StartPosition;
            }

            return startPosition;
         }
      }

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="command"></param>
      public LocateNextLocalDataCommand(IClientCommand command)
         : base(command)
      {
         Command = command;
      }

      /// <summary>
      /// Execute
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new ReturnResult();

         // initialize the start position from which next record to be located.
         initStartPosition();

         //Build Record computer
         RecordComputerBuilder recordComputerBuilder = new LocateNextRecordComputerBuilder(StartPosition)
         {
            LocalDataviewManager = LocalDataviewManager,
            TaskViews = TaskViews
         };

         recordComputer = recordComputerBuilder.Build();

         //Locate Next record from current start position.
         base.Execute();

         return result;
      }

      /// <summary>
      /// initialize the start position with current position from DataView. 
      /// </summary>
      private void initStartPosition()
      {
         IRecord record = DataviewSynchronizer.GetCurrentRecord();
         DbPos outPos;
         DataviewSynchronizer.DataviewManager.PositionCache.TryGetValue(new PositionId(record.getId()), out outPos);
         startPosition = outPos;
      }
   }
}
