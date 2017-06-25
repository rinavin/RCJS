using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// local dataview command for view-refresh command
   /// </summary>
   class LocalDataViewCommandViewRefresh : LocalDataViewCommandFetchFirstChunk
   {


      internal ViewRefreshMode RefreshMode { get; set; }
      internal int CurrentRecordRow { get; set; }
      internal bool KeepUserSort { get; set; }

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewCommandViewRefresh(RefreshEventCommand command)
         : base(command)
      {
         clientRecId = command.ClientRecId;
         UseFirstRecord = false;
         RefreshMode = command.RefreshMode;
         CurrentRecordRow = command.CurrentRecordRow;
         KeepUserSort = command.KeepUserSort;
      }

      /// <summary>
      /// start position of the current record
      /// </summary>
      protected override DbPos StartPosition
      {
         get
         {
            switch (RefreshMode)
            {
               case ViewRefreshMode.UseTaskLocate:
                  // use the base class start position, which uses the "locate" info
                  return base.StartPosition;

               case ViewRefreshMode.FirstRecord:
                  // QCR #424836. If the user defined locates, use these locates.
                  if (LocalDataviewManager.HasUserLocates)
                     // use the base class start position, which uses the "locate" info
                     return base.StartPosition;
                  else
                     return new DbPos(true);

               case ViewRefreshMode.CurrentLocation:
                  return (startPosition == null ? new DbPos(true) : startPosition.Clone());

               
               default:
                  return new DbPos(true);
            }
         }
      }

      private bool reverse = false;

      /// <summary>
      /// 
      /// </summary>
      protected override bool Reverse
      {
         get
         {
            return reverse;
         }
      }

      private StartingPositionType startingPositionType = StartingPositionType.OnStartingRecord;

      /// <summary>
      /// 
      /// </summary>
      protected override StartingPositionType StartingPositionType
      {
         get { return startingPositionType; }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="recordNum"></param>
      /// <returns></returns>
      protected override bool StopFetches(int recordNum)
      {
         if (RefreshMode == ViewRefreshMode.CurrentLocation && reverse)
            // fetch until we got the number of records we have till the start of the table on the form
            return recordNum == CurrentRecordRow || IsEndOfView;
         else
            return base.StopFetches(recordNum);
      }

      /// <summary>
      /// 
      /// </summary>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = null;

         if (LocalDataviewManager.UserSorts != null)
         {
            TaskViews.ApplySort(LocalDataviewManager.UserSorts);
         }
         else
         {
            if (!KeepUserSort)
               TaskViews.ApplySort(Task.RuntimeSorts);
         }

         TaskViews.ApplyUserRangesAndLocates();

         SetTaskMode();

         if (RefreshMode == ViewRefreshMode.CurrentLocation)
         {
            InitCurrentPosition();
            if (startPosition != null)
            {
               result = ExecuteRefreshAndKeepCurrentLocation();
               if (result.Success)
               {
                  LocalDataviewManager.UserGatewayLocates.Clear();
                  LocalDataviewManager.UserLocates.Clear();
                  LocalDataviewManager.UserSorts = null;
               }

               return result;
            }
         }

         // clean the position cache
         LocalDataviewManager.Reset();

         // Release the ViewMain cursor and re-prepare it.
         if (TaskViews.ViewMain != null)
         {
            TaskViews.ViewMain.ReleaseCursor();
            TaskViews.ViewMain.Prepare();
         }

         result = base.Execute();

         if (result.Success)
         {
            DataviewSynchronizer.SetCurrentRecordByIdx(0);
            LocalDataviewManager.UserGatewayLocates.Clear();
            LocalDataviewManager.UserLocates.Clear();
            LocalDataviewManager.UserSorts = null;
         }

         return result;
      }

      /// <summary>
      /// If the task is in create mode, change it to modify before the refresh
      /// </summary>
      protected virtual void SetTaskMode()
      {
         if (Task.getMode() == Constants.TASK_MODE_CREATE)
            Task.setMode(Constants.TASK_MODE_MODIFY);
      }

      /// <summary>
      /// execute the view refresh command while keeping the current location.
      /// </summary>
      /// <param name="result"></param>
      /// <returns></returns>
      ReturnResultBase ExecuteRefreshAndKeepCurrentLocation()
      {
         ReturnResultBase result = new GatewayResult();

         LocalDataviewManager.Reset();

         //In order to build startposition using user locates reset the startposition.
         if (LocalDataviewManager.HasUserLocates)
         {
            startPosition = null;
         }
         // Release the ViewMain cursor and re-prepare it.
         if (TaskViews.ViewMain != null)
         {
            TaskViews.ViewMain.ReleaseCursor();
            TaskViews.ViewMain.Prepare();
         }

         //Performing UpdateAfterFetch(),  while fetching recs with startpos, may cause the task to enter in create/EmptyDataView mode, if 
         //no recs are fetched. This should be avoided because if no recs are fetched we should try fetching recs with no startpos.
         PerformUpdateAfterFetch = false;

         //if the location in not on 1st line, fetch from the current location backward
         if (this.RefreshMode == ViewRefreshMode.CurrentLocation )
            result = FetchBackward();

         // fetch from current location forward
         if (result.Success)
            result = base.Execute();

         PerformUpdateAfterFetch = true;

         //look for the right record to set the dataview on
         int currentRecordId = FindCurrentRecordRow();

         // set the dataview on the right record - either the same record, if found, or the same line in te table,
         // if the previously current record was deleted
         if (currentRecordId != 0)
            DataviewSynchronizer.SetCurrentRecord(currentRecordId);
         else
         {
            //If no record found with CurrentLocation. Then set the RefreshMode as FirstRecord, Re-Execute ViewRefresh command.
            if (((DataView)Task.DataView).getSize() == 0)
            {
               RefreshMode = ViewRefreshMode.FirstRecord;
               result = Execute();
            }
            else
            {
               //If CurrentRecordRow is out of boundary of total records fetched, then set CurrentRecordRow on last record fetched.
               CurrentRecordRow = CurrentRecordRow > ((DataView)Task.DataView).getSize() ? ((DataView)Task.DataView).getSize() : CurrentRecordRow;
               DataviewSynchronizer.SetCurrentRecordByIdx(CurrentRecordRow - 1);
            }
         }

         Task.RefreshDisplay();

         return result;
      }

      /// <summary>
      /// find currect record in the main view
      /// </summary>
      /// <returns></returns>
      int FindCurrentRecordRow()
      {         
          return PositionCache.GetKeyOfValue(startPosition, RuntimeViewBase.MAIN_VIEW_ID).ClientRecordId;
      }

      /// <summary>
      /// fetch records from the current location backward
      /// </summary>
      /// <returns></returns>
      private ReturnResultBase FetchBackward()
      {
         ReturnResultBase result;

         //get backward
         reverse = true;
         result = base.Execute();

         //prepare the state for the fetch forward
         InvalidateView = false;
         reverse = false;

         // If the current record is found, we need to skip it in the next fetch
         if (PositionCache.ContainsValue(startPosition))
            startingPositionType = StartingPositionType.AfterStartingRecord;

         return result;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      void InitCurrentPosition()
      {
         PositionCache.TryGetValue(new PositionId(clientRecId), out startPosition);
      }

   }
}
