using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.commands;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   class LocalDataViewFetcherCommand : LocalDataViewCommandFetchTopChunk
   {
      OnRecordFetchDelegate onRecordFetch;

      internal LocalDataViewFetcherCommand (IClientCommand command)
         : base(command)
      {
         this.onRecordFetch = ((FetchAllDataViewCommand)command).onRecordFetch;
      }

      internal override ReturnResultBase Execute()
      {
         bool isEmptyDataView = ((DataView)(Task.DataView)).isEmptyDataview();

         DataviewSynchronizer.SetupDataview(Reverse);

         IRecord origRecord = DataviewSynchronizer.GetCurrentRecord();

         TaskViews.ViewMain.IgnorePositionCache = true;
         
         GatewayResult result = TaskViews.OpenCursors(Reverse, new DbPos(true));
         if (result.Success)
         {
            IRecord record;
            while ((record = GetBasicRecord()) != null)
            {
               if (RecordComputer.Compute(record, true, false, false).Success)
               {
                  //execute event OnRecordFetchEvent
                  onRecordFetch((Record)record);
                  DataviewSynchronizer.RemoveRecord(record);
               }
            }
         }
         TaskViews.CloseMainCursor();
         
         TaskViews.ViewMain.IgnorePositionCache = false;
         
         if (origRecord != null)
         {
            DataviewSynchronizer.SetCurrentRecord(origRecord != null ? origRecord.getId() : Int32.MinValue);
            ((DataView)(Task.DataView)).takeFldValsFromCurrRec();
         }

         ((DataView)(Task.DataView)).setEmptyDataview(isEmptyDataView);

         return result;
      }

   }
}
