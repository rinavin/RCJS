using System;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// define MG_ACT_CACHE_PREV
   /// </summary>

   internal class LocalDataViewCommandFetchPrevChunk : LocalDataViewFetchViewCommandBase
   {
      internal LocalDataViewCommandFetchPrevChunk(EventCommand command)
         : base(command)
      {
         clientRecId = command.ClientRecId;
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
            if (startPosition == null)
            {
               if (!PositionCache.TryGetValue(new PositionId(clientRecId), out startPosition))
                  startPosition = new DbPos(true);
            }
            return startPosition;
        }
      }

      protected override StartingPositionType StartingPositionType
      {
         get { return StartingPositionType.AfterStartingRecord; }
      }

      internal override ReturnResultBase Execute()
      {
         // need to remove all records fro the gui thread, since all record indexes are updated and they are used as key in Map data
         DataviewSynchronizer.InvalidateView();
         return base.Execute();
      }
   }
}
