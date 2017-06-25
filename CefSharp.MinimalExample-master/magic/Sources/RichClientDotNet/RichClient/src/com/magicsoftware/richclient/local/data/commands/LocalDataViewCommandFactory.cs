using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.local.data.commands;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// factory for creating local dataview commands
   /// </summary>
   internal class LocalDataViewCommandFactory
   {

      internal LocalDataviewManager LocalDataviewManager { get; set; }
      /// <summary>
      /// a factory method that creates an data view command from the send command
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      internal LocalDataViewCommandBase CreateLocalDataViewCommand(IClientCommand command)
      {
         LocalDataViewCommandBase localDataViewCommandBase = null;

         if (command is TransactionCommand)
            localDataViewCommandBase = new CommitTransactionLocalDataViewCommand((TransactionCommand)command);
         else if (command is DataviewCommand)
            localDataViewCommandBase = CreateDataViewCommand((DataviewCommand)command);
         else if (command is EventCommand)
            localDataViewCommandBase = CreateEventCommand((EventCommand)command);
         else if (command is ExecOperCommand)
         {
            ExecOperCommand execOperCommand = (ExecOperCommand)command;
            if (execOperCommand.Operation != null && execOperCommand.Operation.getType() == ConstInterface.MG_OPER_UPDATE)
               localDataViewCommandBase = new LocalDataViewCommandUpdateNonModifiable(execOperCommand);
         }

         if (localDataViewCommandBase != null)
         {
            localDataViewCommandBase.DataviewManager = LocalDataviewManager;
            localDataViewCommandBase.LocalManager = ClientManager.Instance.LocalManager;
         }

         return localDataViewCommandBase;
      }

      /// <summary>
      /// helper function for command type == event
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      private LocalDataViewCommandBase CreateEventCommand(EventCommand eventCommand)
      {
         LocalDataViewCommandBase localDataViewCommandBase = null;

         switch (eventCommand.MagicEvent)
         {
            case InternalInterface.MG_ACT_DATAVIEW_TOP:
               localDataViewCommandBase = new LocalDataViewCommandFetchTopChunk(eventCommand);
               break;
            case InternalInterface.MG_ACT_DATAVIEW_BOTTOM:
               localDataViewCommandBase = new LocalDataViewCommandFetchBottomChunk(eventCommand);
               break;
            case InternalInterface.MG_ACT_CACHE_PREV:
               localDataViewCommandBase = new LocalDataViewCommandFetchPrevChunk(eventCommand);
               break;
            case InternalInterface.MG_ACT_CACHE_NEXT:
               localDataViewCommandBase = new LocalDataViewCommandFetchNextChunk(eventCommand);
               break;
            case InternalInterface.MG_ACT_RT_REFRESH_VIEW:
               localDataViewCommandBase = new LocalDataViewCommandViewRefresh((RefreshEventCommand)eventCommand);
               break;
            case InternalInterface.MG_ACT_SUBFORM_REFRESH:
               localDataViewCommandBase = new LocalDataViewCommandSubformRefresh((SubformRefreshEventCommand)eventCommand);
               break;
            case InternalInterface.MG_ACT_RTO_CREATE:
               localDataViewCommandBase = new LocalDataViewCommandGoToCreateMode(eventCommand);
               break;
            case InternalInterface.MG_ACT_COMPUTE:
               localDataViewCommandBase = new LocalDataViewCommandCompute((ComputeEventCommand)eventCommand);
               ((LocalDataViewCommandCompute)localDataViewCommandBase).Record = ((DataView)LocalDataviewManager.Task.DataView).GetRecordById(eventCommand.ClientRecId);
               break;
            case InternalInterface.MG_ACT_RTO_LOCATE:
               localDataViewCommandBase = new LocalDataViewCommandLocateInQuery((LocateQueryEventCommand)eventCommand);
               break;
            case InternalInterface.MG_ACT_COL_SORT:
               localDataViewCommandBase = new LocalDataViewCommandSort((ColumnSortEventCommand)eventCommand);
               break;
            case InternalInterface.MG_ACT_RT_REFRESH_RECORD:
               localDataViewCommandBase = new GetCurrentRecordLocalDataViewCommand(eventCommand);
               break;
            case InternalInterface.MG_ACT_ROLLBACK:
               localDataViewCommandBase = new RollbackTransactionLocalDataViewCommand((RollbackEventCommand)eventCommand);
               break;

            /////NOT Implemented commands
            case InternalInterface.MG_ACT_RT_REFRESH_SCREEN:
               localDataViewCommandBase = new LocalDataViewCommandViewRefreshScreen(eventCommand);
               break;
            case InternalInterface.MG_ACT_RTO_SEARCH:
               localDataViewCommandBase = new LocalDataviewCommandLocateNext(eventCommand);
               break;
            case InternalInterface.MG_ACT_UPDATE_DATAVIEW_TO_DATASOURCE:
               localDataViewCommandBase = new LocalUpdateRemoteDataViewToDataSourceCommand((UpdateDataViewToDataSourceEventCommand)eventCommand);
               break;
            default:
               Logger.Instance.WriteExceptionToLog("in DataView.fetchChunkFromServer() unknown magicEvent: " + eventCommand.MagicEvent);
               break;
         }

         return localDataViewCommandBase;
      }

      /// <summary>
      /// helper function for command type == dataview
      /// </summary>
      /// <param name="command"></param>
      /// <param name="localDataViewCommandBase"></param>
      /// <returns></returns>
      private LocalDataViewCommandBase CreateDataViewCommand(DataviewCommand dataviewCommand)
      {
         LocalDataViewCommandBase localDataViewCommandBase = null;

         switch (dataviewCommand.CommandType)
         {
            case DataViewCommandType.Init:
               localDataViewCommandBase = new LocalInitDataViewCommand(dataviewCommand);
               break;
            case DataViewCommandType.Clear:
               localDataViewCommandBase = new LocalClearDataViewCommand(dataviewCommand);
               break;
            case DataViewCommandType.Prepare:
               localDataViewCommandBase = new LocalDataViewCommandPrepare(dataviewCommand);
               break;
            case DataViewCommandType.FirstChunk:
               localDataViewCommandBase = new LocalDataViewCommandFetchFirstChunk(dataviewCommand);
               break;
             case DataViewCommandType.RecomputeUnit:
               localDataViewCommandBase = new RecomputeUnitLocalDataViewCommand((RecomputeUnitDataviewCommand)dataviewCommand);
               ((RecomputeUnitLocalDataViewCommand)localDataViewCommandBase).Record = ((DataView)LocalDataviewManager.Task.DataView).GetRecordById(
                   ((RecomputeUnitDataviewCommand)dataviewCommand).ClientRecId);
               break;
            case DataViewCommandType.ExecuteLocalUpdates:
               localDataViewCommandBase = new LocalDataViewCommandExecuteLocalUpdates(dataviewCommand);
               break;
            case DataViewCommandType.InitDataControlViews:
               localDataViewCommandBase = new InitDataControlLocalViewsCommand(dataviewCommand);
               break;
            case DataViewCommandType.OpenTransaction:
               localDataViewCommandBase = new OpenTransactionLocalDataCommand(dataviewCommand);
              break;
            case DataViewCommandType.CloseTransaction:
              localDataViewCommandBase = new CloseTransactionLocalDataViewCommandBase(dataviewCommand);
              break;
            case DataViewCommandType.AddUserRange:
              localDataViewCommandBase = new AddUserRangeLocalDataCommand((AddUserRangeDataviewCommand)dataviewCommand);
               break;
            case DataViewCommandType.ResetUserRange:
               localDataViewCommandBase = new ResetUserRangeLocalDataviewCommand(dataviewCommand);
               break;
            case DataViewCommandType.DbDisconnect:
               localDataViewCommandBase = new LocalClientDbDisconnectCommand((ClientDbDisconnectCommand)dataviewCommand);
               break;
            case DataViewCommandType.AddUserLocate:
               localDataViewCommandBase = new AddUserLocateLocalDataCommand((AddUserLocateDataViewCommand)dataviewCommand);
               break;
            case DataViewCommandType.AddUserSort:
               localDataViewCommandBase = new AddUserSortLocalDataCommand((AddUserSortDataViewCommand)dataviewCommand);
               break;
            case DataViewCommandType.ResetUserLocate:
               localDataViewCommandBase = new ResetUserLocateDataviewCommand(dataviewCommand);
               break;
            case DataViewCommandType.ResetUserSort:
               localDataViewCommandBase = new ResetUserSortLocalDataviewCommand(dataviewCommand);
               break;
            case DataViewCommandType.DbDelete:
               localDataViewCommandBase = new LocalClientDbDeleteCommand((ClientDbDeleteCommand)dataviewCommand);
               break;
            case DataViewCommandType.DataViewToDataSource:
               localDataViewCommandBase = new LocalDataViewToDataSourceCommand((DataViewOutputCommand)dataviewCommand);
               break;
            case DataViewCommandType.FetchAll:
               localDataViewCommandBase = new LocalDataViewFetcherCommand(dataviewCommand);
               break;
            case DataViewCommandType.ControlItemsRefresh:
               localDataViewCommandBase = new LocalDataViewCommandControlItemsRefersh((ControlItemsRefreshCommand)dataviewCommand);
               break;
            case DataViewCommandType.SQLExecute:
               localDataViewCommandBase = new SQLExecuteLocalDataViewCommand((SQLExecuteCommand)dataviewCommand);
               break;
         }

         return localDataViewCommandBase;
      }
   }
}
