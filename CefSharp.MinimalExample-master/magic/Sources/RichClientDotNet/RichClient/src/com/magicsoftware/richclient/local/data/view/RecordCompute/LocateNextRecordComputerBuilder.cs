using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.gatewaytypes;

namespace RichClient.src.com.magicsoftware.richclient.local.data.view.RecordCompute
{
   /// <summary>
   /// RecordComputer builder for Locate next command.
   /// </summary>
   class LocateNextRecordComputerBuilder : RecordComputerBuilder
   {
      /// <summary>
      /// Start position from which next record to be located.
      /// </summary>
      DbPos StartPosition { get; set; }

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="startPosition"></param>
      public LocateNextRecordComputerBuilder(DbPos startPosition)
      {
         StartPosition = startPosition;
      }

      /// <summary>
      /// Build
      /// </summary>
      /// <returns></returns>
      internal override RecordComputer Build()
      {
         RecordComputer recordComputer = base.Build();

         recordComputer.Add(new LocateNextComputeStrategy(StartPosition));

         return recordComputer;
      }
   }
}
