using System.Collections.Generic;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.commands;
using com.magicsoftware.richclient.local.data.recording;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.local.ErrorHandling;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.richclient.local.data.gateways;

namespace com.magicsoftware.richclient.local.data
{
   /// <summary>
   /// local dataview manager 
   /// will execute data related commands on the local database 
   /// will use factory method to create specific LocalDataCommand: FecthCommand,ViewRefreshCommand and execute it
   /// </summary>
   class LocalDataviewManager : DataviewManagerBase
   {     
      internal LocalErrorHandlerManager LocalErrorHandlerManager { get; set; }

      private LocalDataViewCommandFactory _localDataViewCommandFactory;
      internal TaskViewsCollection TaskViews { get; private set; }
      internal TaskViewsCollectionBuilder TaskViewsBuilder { get; private set; }
      internal LocalManager LocalManager { get; set; }
      internal PositionCache PositionCache { get; set; }
      internal DataviewSynchronizer DataviewSynchronizer { get; set; }
      internal IdGenerator ClientIdGenerator { get; set; }
      /// <summary>
      /// manager for recording data 
      /// </summary>
      internal RecordingManager RecordingManager { get; set; }
      /// <summary>
      /// chunk size
      /// </summary>
      internal int MaxRecordsInView { get; set; }

      /// <summary>
      /// ranges added by the RangeAdd function for real fields
      /// </summary>
      internal List<RangeData> UserGatewayRanges { get; private set; }

      /// <summary>
      /// ranges added by the RangeAdd function for virtuals or links
      /// </summary>
      internal List<UserRange> UserRanges { get; private set; }



      /// <summary>
      /// ranges added by the LocateAdd function for real fields
      /// </summary>
      internal List<RangeData> UserGatewayLocates { get; private set; }

      /// <summary>
      /// ranges added by the LocateAdd function for virtuals or links
      /// </summary>
      internal List<UserRange> UserLocates { get; private set; }

      /// <summary>
      /// Check whether DataViewManager has user locates.
      /// </summary>
      internal bool HasUserLocates { get { return (UserGatewayLocates.Count > 0 || UserLocates.Count > 0); } }

      /// <summary>
      /// Sorts added by SortAdd function.
      /// </summary>
      internal SortCollection UserSorts { get; set; }

      //bool Preloadview = false;
      //int ChunkSize;

      /// <summary>
      /// default numeric picture
      /// </summary>
      readonly public static PIC NUMERIC_PIC = new PIC("N6", StorageAttribute.NUMERIC, 0);

      internal Dictionary<DataControlRangeDataCollection, int> rangeToDcValuesMap = new Dictionary<DataControlRangeDataCollection, int>();

      internal string IncrementalLocateString { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="task"></param>
      internal LocalDataviewManager(Task task)
         : base(task)
      {
         _localDataViewCommandFactory = new LocalDataViewCommandFactory();
         _localDataViewCommandFactory.LocalDataviewManager = this;
         PositionCache = new PositionCache();
         TaskViewsBuilder = new TaskViewsCollectionBuilder();
         ClientIdGenerator = new IdGenerator();
         DataviewSynchronizer = new DataviewSynchronizer() { DataviewManager = this };
         LocalErrorHandlerManager = new LocalErrorHandlerManager() { DataviewManager = this };
         UserGatewayRanges = new List<RangeData>();
         UserRanges = new List<UserRange>();
         UserGatewayLocates = new List<RangeData>();
         UserLocates = new List<UserRange>();
      }

      internal void Reset()
      {
         PositionCache.Reset();
         rangeToDcValuesMap.Clear();
      }


      /// <summary>
      /// !!
      /// </summary>
      /// <param name="command"></param>
      internal override ReturnResult Execute(IClientCommand command)
      {
         LocalDataViewCommandBase localDataViewCommandBase = _localDataViewCommandFactory.CreateLocalDataViewCommand(command);
         DataviewSynchronizer.InLocalDataviewCommand = true;
         ReturnResultBase returnResultBase = localDataViewCommandBase.Execute();
         DataviewSynchronizer.InLocalDataviewCommand = false;
         DataviewSynchronizer.UpdateDBViewSize();

         ReturnResult result = new ReturnResult(returnResultBase);

         if (!result.Success)
         {
             ErrorHandlingInfo errorHandlingInfo = LocalErrorHandlerManager.HandleResult (result);
             if (errorHandlingInfo!= null && errorHandlingInfo.Quit)
                 Task.EndTaskOnError(result, false);

         }
         return result;
      }



      internal override GatewayResult CreateDataControlViews(List<IDataSourceViewDefinition> dataControlViewsDefinitions)
      {
         GatewayResult result = new GatewayResult();

         foreach (var viewDefinition in dataControlViewsDefinitions)
         {
            RuntimeReadOnlyView dcView = TaskViewsBuilder.CreateDataControlSourceView(viewDefinition);

            //[MH 9-APR] This is the point where we should determine whether the linked variable is vector or not.

            result = this.TaskViews.AddDataControlView(dcView, new LocallyComputedDcValuesBuilder(dcView));

            if (!result.Success)
               break;
         }

         return result;
      }

      internal void BuildViewsCollection(Task task)
      {
         TaskViews = TaskViewsBuilder.BuildViewsCollection(task);
         TaskViews.ApplySort(task.RuntimeSorts);

      }

      internal void BuildRecorder()
      {
         RecordingManagerBuilder recordingManagerBuilder = new RecordingManagerBuilder();
         RecordingManager = recordingManagerBuilder.Build();
      }

      /// <summary>
      /// convert a UserRange to a RangeData and add it to UserRangeData
      /// </summary>
      /// <param name="rangeData"></param>
      /// <param name="fieldView"></param>
      internal void AddUserRange(UserRange userRange)
      {
         UserRanges.Add(userRange);
      }

      internal void AddUserRangeData(RangeData rangeData)
      {
         UserGatewayRanges.Add(rangeData);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void ResetUserRanges()
      {
         UserGatewayRanges.Clear();
         UserRanges.Clear();
      }

      /// <summary>
      /// Reset User locates.
      /// </summary>
      internal void ResetUserLocates()
      {
         UserGatewayLocates.Clear();
         UserLocates.Clear();
      }

      internal void MapDcValues(DataControlRangeDataCollection dcValuesRange, int dcValuesId)
      {
         rangeToDcValuesMap.Add(dcValuesRange, dcValuesId);
      }

      internal bool TryGetDcValuesIsByRange(DataControlRangeDataCollection dcValuesRange, out int dcValuesId)
      {
         if (rangeToDcValuesMap.TryGetValue(dcValuesRange, out dcValuesId))
         {
            Logger.Instance.WriteDevToLog("Reusing DC values");
            return true;
         }

         Logger.Instance.WriteDevToLog("Did not find existing DC values");
         return false;
      }

      /// <summary>
      /// Get DbRowIdx of current record.
      /// </summary>
      internal override int GetDbViewRowIdx()
      {
         //Get current Position
         DbPos currPos = TaskViews.ViewMain.GetCurrentPosition();
         int idx = DataviewSynchronizer.DataviewManager.PositionCache.IdxOf(currPos) + 1;
         return idx;
      }

   }
}
