using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// define MG_ACT_DATAVIEW_BOTTOM
   /// </summary>

   internal class LocalDataViewCommandFetchBottomChunk : LocalDataViewFetchViewCommandBase
   {
      internal LocalDataViewCommandFetchBottomChunk(IClientCommand command)
         : base(command)
      {
      }

      protected override bool Reverse
      {
         get { return true; }
      }

      /// <summary>
      /// Start Position
      /// </summary>
      protected override DbPos StartPosition
      {
         get
         {

            DbPos pos = new DbPos(true);
            return pos;

         }
      }

      /// <summary>
      /// update top index
      /// </summary>
      protected override void UpdateTopIndex()
      {
          DataviewSynchronizer.UpdateTopIndex(0);
      }

      /// <summary>
      /// should fetch start from current position or from next
      /// </summary>
      protected override StartingPositionType StartingPositionType
      {
         get { return StartingPositionType.OnStartingRecord; }
      }

      internal override ReturnResultBase Execute()
      {
         DataviewSynchronizer.Invalidate();
         return base.Execute();
      }
   }
}
