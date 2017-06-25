using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// define _ACT_CACHE_NEXT
   /// </summary>

   internal class LocalDataViewCommandFetchNextChunk : LocalDataViewFetchViewCommandBase
   {
      internal LocalDataViewCommandFetchNextChunk(EventCommand command)
         : base(command)
      {
         clientRecId = command.ClientRecId;

         //bool PreloadView = false; //TODO : handle
         //if (!PreloadView && Task.getMode() != Constants.TASK_MODE_CREATE)
         //   browser_mod_fillscr(1, TRUE, FALSE, FALSE);

         //if (tsk_rt->mode == 'C')
         //         {
         //            cre_create_ini(FALSE);
         //           // if (tsk_rt->RtMainDb.Object() != 0)
         //               PosCache->AscEnd(TRUE);
         //            PosCache->DescEnd(TRUE);
         //         }



      }



      protected override bool Reverse
      {
         get { return false; }
      }


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
   }
}
