using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// define MG_ACT_DATAVIEW_TOP
   /// </summary>
   internal class LocalDataViewCommandFetchTopChunk : LocalDataViewFetchViewCommandBase
   {
      internal LocalDataViewCommandFetchTopChunk(IClientCommand command)
         : base(command)
      {
         InvalidateView = true;
      }

      /// <summary>
      /// Should the view be invalidate before the fetch
      /// </summary>
      protected bool InvalidateView { get; set; }

      protected override bool Reverse
      {
         get { return false; }
      }

      /// <summary>
      /// start position
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
      /// set top index
      /// </summary>
      protected override void UpdateTopIndex()
      {
         DataviewSynchronizer.UpdateTopIndex(0);
      }

      protected override StartingPositionType StartingPositionType
      {
         get { return StartingPositionType.OnStartingRecord; }
      }

      internal override ReturnResultBase Execute()
      {
         if (InvalidateView && !UseFirstRecord)
            DataviewSynchronizer.Invalidate();
         return base.Execute();
      }
   }
}
