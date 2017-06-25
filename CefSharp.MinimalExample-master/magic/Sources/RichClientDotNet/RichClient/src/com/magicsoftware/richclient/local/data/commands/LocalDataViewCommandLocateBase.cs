using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// Base class for locate commands - includes basic functionality for finding the one record for the start position without
   /// changing too much in the data view. Derived classes are responsible for setting the locate conditions.
   /// </summary>
   class LocalDataViewCommandLocateBase : LocalDataViewCommandFetchTopChunk
   {
      /// <summary>
      /// start position value to return
      /// </summary>
      public DbPos ResultStartPosition { get; protected set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewCommandLocateBase(IClientCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// find the start record and get its position
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         IRecord origRecord = DataviewSynchronizer.GetCurrentRecord();

         bool recordFound = FindMatchingRecord();

         if (recordFound)
         {
            // found a record - keep its position
            IRecord currentRecord = DataviewSynchronizer.GetCurrentRecord();
            DbPos outPos;
            PositionCache.TryGetValue(new PositionId(currentRecord.getId()), out outPos);
            ResultStartPosition = outPos;
            LocalDataviewManager.Reset();
            DataviewSynchronizer.RemoveRecord(currentRecord);
         }
         else
            DataviewSynchronizer.SetCurrentRecord(origRecord != null ? origRecord.getId() : Int32.MinValue);

         return new GatewayResult();
      }

      /// <summary>
      /// fetch-and-compute loop
      /// </summary>
      /// <returns></returns>
      private bool FindMatchingRecord()
      {
         bool recordFound = false;

         if (!StartPosition.IsZero)
         {
            GatewayResult result = TaskViews.OpenCursors(Reverse, StartPosition);

            if (result.Success)
            {
               IRecord record;
               // Looking for one record only
               do
               {
                  record = GetBasicRecord();
                  if (record != null && RecordComputer.Compute(record, true, false, false).Success)
                  {
                     recordFound = true;
                     break;
                  }
               } while (record != null);
            }

            TaskViews.CloseMainCursor();
         }
         return recordFound;
      }


      /// <summary>
      /// override - always get a new record, and no need to refresh the position cache
      /// </summary>
      /// <returns></returns>
      protected override IRecord GetBasicRecord()
      {
         IRecord record = DataviewSynchronizer.GetRecord(false);
         GatewayResult result = TaskViews.GetPrimaryView().Fetch(record);

         if (!result.Success)
         {
            DataviewSynchronizer.RemoveRecord(record);
            record = null;
         }
         return record;
      }

      /// <summary>
      /// override - stop fetching once the 1st record was found
      /// </summary>
      /// <param name="recordNum"></param>
      /// <returns></returns>
      protected override bool StopFetches(int recordNum)
      {
         if (recordNum > 0)
            return true;
         else
            return base.StopFetches(recordNum);
      }
   }
}
