using System.Collections.Generic;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;

namespace com.magicsoftware.richclient.local.data.commands
{
   enum StartingPositionType { AfterStartingRecord, OnStartingRecord }

   /// <summary>
   /// base command for the fetch commands
   /// </summary>
   internal abstract class LocalDataViewFetchViewCommandBase : LocalDataViewCommandBase
   {
      protected int clientRecId; 
      protected DbPos startPosition;
      protected virtual RecordComputer RecordComputer { get { return TaskViews.RecordComputer; } }
      protected bool PerformUpdateAfterFetch { get; set; }

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="command"></param>
      internal LocalDataViewFetchViewCommandBase(IClientCommand command)
         : base(command)
      {
         clientRecId = 0;
         PerformUpdateAfterFetch = true;
         ClientIdsToCompute = new List<int>();
      }

      /// <summary>
      /// returns true if end of chunk is reached
      /// </summary>
      /// <param name="currentRecordNum"></param>
      /// <returns></returns>
      protected bool EndOfChunk(int currentRecordNum)
      {
         return currentRecordNum >= LocalDataviewManager.MaxRecordsInView || IsEndOfView;
      }

      /// <summary>
      /// use first record of the dataview
      /// </summary>
      virtual protected internal bool UseFirstRecord { get; set; }

      /// <summary>
      /// ids of records that are fetched in current fetch  and need to be computed 
      /// </summary>
      protected List<int> ClientIdsToCompute { get; set; }

      /// <summary>
      /// starting position type determing if we should start on/after given position
      /// </summary>
      protected abstract StartingPositionType StartingPositionType { get; }

      /// <summary>
      /// condition for stopping execution of fetch next record
      /// </summary>
      protected virtual bool StopFetches(int recordNum)
      {
         return EndOfChunk(recordNum);
      }

      /// <summary>
      /// execute command
      /// </summary>
      internal override ReturnResultBase Execute()
      {
         DataviewSynchronizer.SetupDataview(Reverse);
         GatewayResult gatewayResult = SetupMainCursor();
         ReturnResultBase result;
         int fetchedRecordNumber = 0;

         if (gatewayResult.Success)
         {
            for (fetchedRecordNumber = 0; !StopFetches(fetchedRecordNumber); )
            {
               IRecord record = GetBasicRecord();
               if (record != null && RecordComputer.Compute(record, true, false, false).Success)
                  fetchedRecordNumber++;
            }
         }
         else
            UpdateViewEnd(gatewayResult);

         TaskViews.CloseMainCursor();

         //If PerformUpdateAfterFetch is false , the UpdateAterFetch () should be called only if recs are fetched.
         if (gatewayResult.Success && (PerformUpdateAfterFetch || fetchedRecordNumber > 0))
             result = UpdateAfterFetch();
         else
             result = gatewayResult;
          return result;
      }

      /// <summary>
      /// update dataview after fetch
      /// </summary>
      protected virtual ReturnResultBase UpdateAfterFetch()
      {
         ReturnResultBase result = DataviewSynchronizer.UpdateAfterFetch(clientRecId);
         if (result.Success)
             UpdateTopIndex();
         return result;

      }

      /// <summary>
      /// fetch record
      /// </summary>
      /// <returns></returns>
      protected virtual IRecord GetBasicRecord()
      {
         IRecord record = DataviewSynchronizer.GetRecord(UseFirstRecord);
         UseFirstRecord = false;
         GatewayResult result = TaskViews.GetPrimaryView().Fetch(record);
         UpdateViewEnd(result);
         if (!result.Success)
         {
            DataviewSynchronizer.RemoveRecord(record);
            record = null;
         }
         return record;
      }

      /// <summary>
      /// calculate Top Index
      /// </summary>
      protected virtual void UpdateTopIndex()
      {
      }


      /// <summary>
      /// setup main cursor for fetch
      /// executed for all types of fetches
      /// </summary>
      /// <returns></returns>
      private GatewayResult SetupMainCursor()
      {
         GatewayResult gatewayResult;


         UpdateViewBoundaries(StartPosition);
         gatewayResult = TaskViews.OpenCursors(Reverse, StartPosition);

         //move to strategy ????
         if (gatewayResult.Success && StartingPositionType == StartingPositionType.AfterStartingRecord 
            && PositionCache.ContainsValue(startPosition))
         {
            IRecord dummyRecord = DataviewSynchronizer.CreateRecord();
            gatewayResult = TaskViews.FetchMain(dummyRecord);
         }

         return gatewayResult;
      }

      abstract protected bool Reverse { get; }

      abstract protected DbPos StartPosition { get; }

      /// <summary>
      /// updates inscludesFirst/includeslast
      /// </summary>
      /// <param name="startPosition"></param>
      void UpdateViewBoundaries(DbPos startPosition)
      {
         if (startPosition.IsZero)
         {
            if (Reverse)
            {
               PositionCache.IncludesLast = true;
               PositionCache.IncludesFirst = false;
            }
            else
            {
               PositionCache.IncludesFirst = true;
               PositionCache.IncludesLast = false;
            }
         }

      }

      /// <summary>
      /// update end of view
      /// </summary>
      /// <param name="result"></param>
      void UpdateViewEnd(GatewayResult result)
      {
         if (result.ErrorCode == GatewayErrorCode.NoRecord && !TaskViews.ViewMain.IgnorePositionCache)
            PositionCache.UpdateEnd(Reverse);
      }

      /// <summary>
      /// true if we reached end of view
      /// </summary>
      protected bool IsEndOfView
      {
         get
         {
            return Reverse ? PositionCache.IncludesFirst : PositionCache.IncludesLast;
         }
      }

      bool ReverseDelegate()
      {
         return Reverse;
      }

   }





}




