using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.local.data;
using Field = com.magicsoftware.richclient.data.Field;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands.ServerToClient;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using RichClient.src.com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// factory class for creating commands
   /// </summary>
   class CommandFactory
   {
      /// <summary>
      /// create the abort command for local command processing
      /// </summary>
      /// <param name="taskTag"></param>
      /// <returns></returns>
      internal static IClientCommand CreateAbortCommand(string taskTag)
      {
         return new AbortCommand(taskTag);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newTaskXML"></param>
      /// <param name="isModal"></param>
      /// <returns></returns>
      internal static IClientCommand CreateOpenTaskCommand(string newTaskXML, ArgumentsList argList, Field retrnValueField,
                                                    bool forceModal, string callingTaskTag, string pathParentTaskTag,
                                                    int ditIdx, string subformCtrlName)
      {
         var cmd = new OpenURLCommand(
            "", // key not used
            newTaskXML, 
            argList, 
            retrnValueField,
            forceModal, 
            callingTaskTag, 
            pathParentTaskTag,
            ditIdx, 
            subformCtrlName,
            "0"); // new id

         return cmd;
      }

      /// <summary>
      ///   a factory method that creates an Event command
      /// </summary>
      /// <param name = "taskTag">the task id</param>
      /// <param name = "magicEvent">the code of the internal event</param>
      /// <returns>newly created command.</returns>
      internal static EventCommand CreateEventCommand(String taskTag, int magicEvent)
      {
         return new EventCommand(magicEvent) { TaskTag = taskTag };
      }

      /// <summary>
      ///   a factory method that creates an Event command
      /// </summary>
      /// <param name = "taskTag">the task id</param>
      /// <param name = "cHandlerId">the id of the handler where execution should start</param>
      /// <param name = "cObj">the DIT index of the control that had the focus when the event occurred</param>
      /// <param name = "magicEvent">the code of the internal event</param>
      /// <returns>newly created command.</returns>
      internal static RollbackEventCommand CreateRollbackEventCommand(String taskTag, RollbackEventCommand.RollbackType rollbackType)
      {
         var cmd = new RollbackEventCommand
         {
            TaskTag = taskTag,
            Rollback = rollbackType
         };
         return cmd;
      }

      /// <summary>
      ///   a factory method that creates an dataview command
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="unitId"></param>
      /// <param name="clientRecId"></param>
      /// <returns></returns>
      internal static RecomputeUnitDataviewCommand CreateRecomputeUnitDataViewCommand(String taskId, RecomputeId unitId, int clientRecId)
      {
         return new RecomputeUnitDataviewCommand { TaskTag = taskId, UnitId = unitId, ClientRecId = clientRecId };
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="onRecordFetchDelegate"></param>
      /// <returns></returns>
      internal static FetchAllDataViewCommand CreateFetchAllDataViewCommand(String taskId, OnRecordFetchDelegate onRecordFetchDelegate)
      {
         return new FetchAllDataViewCommand { TaskTag = taskId, onRecordFetch = onRecordFetchDelegate };
      }

      /// <summary>
      /// create a general dataview command
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="commandType"></param>
      /// <returns></returns>
      internal static DataviewCommand CreateDataViewCommand(String taskId, DataViewCommandType commandType)
      {
         return new DataviewCommand { CommandType = commandType, TaskTag = taskId };
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="userRange"></param>
      /// <returns></returns>
      internal static AddUserRangeDataviewCommand CreateAddUserRangeDataviewCommand(String taskId, UserRange userRange)
      {
         return new AddUserRangeDataviewCommand { TaskTag = taskId, Range = userRange };
      }

      /// <summary>
      /// CreateAddUserSortDataviewCommand
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="userRange"></param>
      /// <returns></returns>
      internal static AddUserSortDataViewCommand CreateAddUserSortDataviewCommand(String taskId, Sort sort)
      {
         return new AddUserSortDataViewCommand { TaskTag = taskId, Sort = sort };
      }


      /// <summary>
      /// CreateAddUserLocateDataviewCommand 
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="userRange"></param>
      /// <returns></returns>
      internal static AddUserLocateDataViewCommand CreateAddUserLocateDataviewCommand(String taskId, UserRange userRange)
      {
         return new AddUserLocateDataViewCommand { TaskTag = taskId, Range = userRange };
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="databaseName"></param>
      /// <returns></returns>
      internal static ClientDbDisconnectCommand CreateClientDbDisconnectCommand(string databaseName)
      {
         return new ClientDbDisconnectCommand(databaseName);
      }

     /// <summary>
     /// 
     /// </summary>
     /// <param name="dataSourceNumber"></param>
      /// <param name="dataSourceName"></param>
     /// <returns></returns>
      internal static ClientDbDeleteCommand CreateClientDbDeleteCommand(int dataSourceNumber, string dataSourceName)
      {
         return new ClientDbDeleteCommand { DataSourceNumber = dataSourceNumber, DataSourceName = dataSourceName };
      }

      /// <summary>
      /// Create an SQL execute command
      /// </summary>
      /// <param name="dataSourceNumber"></param>
      /// <param name="dataSourceName"></param>
      /// <returns></returns>
      internal static SQLExecuteCommand CreateSQLExecuteCommand(string dataSourceName, string sqlStatement, StorageAttribute[] storageAttributes, DBField[] dbFields)
      {
         return new SQLExecuteCommand { DataSourceName = dataSourceName, SQLStatement = sqlStatement, StorageAttributes = storageAttributes, DbFields = dbFields };
      }

      /// <summary>
      /// create a set transaction state command
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="transactionIsOpened"></param>
      /// <returns></returns>
      internal static SetTransactionStateDataviewCommand CreateSetTransactionStateDataviewCommand(String taskId, bool transactionIsOpened)
      {
         return new SetTransactionStateDataviewCommand { TaskTag = taskId, TransactionIsOpen = transactionIsOpened };
      }

      /// <summary>
      /// Create ControlItemsRefreshCommand to refersh the data control.
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="controlNameParam"></param>
      /// <param name="generationParam"></param>
      /// <returns></returns>
      internal static ControlItemsRefreshCommand CreateControlItemsRefreshCommand(String taskId, MgControlBase controlParam)
      {
         return new ControlItemsRefreshCommand() 
         { 
            TaskTag = taskId, 
            CommandType = DataViewCommandType.ControlItemsRefresh,
            Control = controlParam
         };
      }

      /// <summary>
      /// creates a refresh command which was initiated internally, i.e. not by a user function call
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="magicEvent"></param>
      /// <param name="currentRecId"></param>
      /// <returns></returns>
      internal static RefreshEventCommand CreateInternalRefreshCommand(String taskId, int magicEvent, int currentRecId, int currentRow)
      {
         return new RefreshEventCommand(magicEvent)
         {
            TaskTag = taskId,
            RefreshMode = ViewRefreshMode.CurrentLocation,
            ClientRecId = currentRecId,
            IsInternalRefresh = true,
            CurrentRecordRow = currentRow
         };
      }
      
      /// <summary>
      ///   creates only a real refresh event command
      /// </summary>
      /// <returns>newly created command.</returns>
      internal static RefreshEventCommand CreateRealRefreshCommand(String taskId, int magicEvent, int currentRow, ArgumentsList argList, int currentRecId)
      {
         RefreshEventCommand cmd = new RefreshEventCommand(magicEvent)
         {
            TaskTag = taskId,
            RefreshMode = ViewRefreshMode.CurrentLocation,
            KeepUserSort = false,
            ClientRecId = currentRecId,
            CurrentRecordRow = currentRow
         };

         if (argList != null && argList.getSize() != 0)
         {
            try
            {
               var refreshMode = new NUM_TYPE(argList.getArgValue(0, StorageAttribute.NUMERIC, 0));
               cmd.RefreshMode = (ViewRefreshMode)refreshMode.NUM_2_LONG() + 1;
            }
            catch (Exception)
            {
               cmd.RefreshMode = ViewRefreshMode.CurrentLocation;
            }

            if (argList.getSize() > 1)
            {
               try
               {
                  cmd.KeepUserSort = (argList.getArgValue(1, StorageAttribute.BOOLEAN, 0) == "1");
               }
               catch (Exception)
               {
                  cmd.KeepUserSort = false;
               }
            }
         }

         return cmd;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="taskId"></param>
      /// <param name="taskVarNames"></param>
      /// <param name="destinationDSNumber"></param>
      /// <param name="destinationDSName"></param>
      /// <param name="destinationColumns"></param>
      /// <returns></returns>
      internal static DataViewOutputCommand CreateDataViewToDataSourceCommand(string taskId, int generation, string taskVarNames, int destinationDSNumber, string destinationDSName, string destinationColumns)
      {
         return new DataViewOutputCommand(DataViewCommandType.DataViewToDataSource)
         {
            TaskTag = taskId,
            Generation = generation,
            TaskVarNames = taskVarNames,
            DestinationDataSourceNumber = destinationDSNumber,
            DestinationDataSourceName = destinationDSName,
            DestinationColumns = destinationColumns
         };
      }

      /// <summary>
      ///   creates UpdateDataViewToDataSource Command
      /// </summary>
      /// <param name = "taskTag">the task id</param>
      /// <param name = "generation">identifies parent task</param>
      /// <param name = "taskVarList">task variable list</param>
      /// <param name = "destDataSource">destination data source</param>
      /// <param name = "destDataSource">destination data source Name</param>
      /// <param name = "destColumnList">destination column list</param>
      /// <param name = "dataViewContent">dataview content to be updated to the remote DS</param>
      /// <returns>newly created command.</returns>
      internal static UpdateDataViewToDataSourceEventCommand CreateUpdateDataViewToDataSourceCommand(String taskTag,
                                 String taskVarList, int destDataSource, String destDataSourceName, String destColumnList, String dataViewContent, List<FieldDef> sourceVarList, List<DBField> destinationDataSourceFieldsList)
      {
         return new UpdateDataViewToDataSourceEventCommand
         {
            TaskTag = taskTag,
            TaskVarList = taskVarList,
            DestDataSource = destDataSource,
            DestDataSourceName = destDataSourceName,
            DestColumnList = destColumnList,
            DataViewContent = dataViewContent,
            SourceVarList = sourceVarList,
            DestinationDataSourceFieldsList = destinationDataSourceFieldsList
         };
      }

      /// <summary>
      /// Create FetchDataControlValuesEvent command which will fetch the data control values from server. This is used while executing ControlItemsRefresh() function.
      /// </summary>
      /// <param name="taskTag"></param>
      /// <param name="controlName"></param>
      /// <param name="generationParam"></param>
      /// <returns></returns>
      internal static FetchDataControlValuesEventCommand CreatecFetchDataControlValuesCommand(String taskTag, String controlName)
      {
         return new FetchDataControlValuesEventCommand
         {
            TaskTag = taskTag,
            ControlName = controlName,
         };
      }

      /// <summary>
      ///   creates GetDataViewContent Command
      /// </summary>
      /// <param name = "taskTag">the task id</param>
      /// <param name = "generation">identifies parent task</param>
      /// <param name = "magicEvent">the code of the internal event</param>
      /// <param name = "varList">task's variable list</param>
      /// <param name = "outputType">output type for dataview content</param> 
      /// <returns>newly created command.</returns>
      internal static GetDataViewContentEventCommand CreateGetDataViewContentCommand(String taskTag, String generation, String varList, DataViewOutputType outputType, int destinationDataSourceNumber, string listIndexesOfDestinationSelectedFields)
      {
         return new GetDataViewContentEventCommand
         {
            TaskTag = taskTag,
            Generation = generation,
            TaskVarList = varList,
            OutputType = outputType,
            DestinationDataSourceNumber = destinationDataSourceNumber,
            ListOfIndexesOfSelectedDestinationFields = listIndexesOfDestinationSelectedFields
         };
      }

      /// <summary>
      /// Create WriteMessagesToServerLog command
      /// </summary>
      /// <param name="taskTag"></param>
      /// <param name="errorMessage"></param>
      /// <returns></returns>
      internal static WriteMessageToServerLogCommand CreateWriteMessageToServerLogCommand(String taskTag, string errorMessage)
      {
         return new WriteMessageToServerLogCommand
         {
            TaskTag = taskTag,
            ErrorMessage = errorMessage
         };
      }

      /// <summary>
      ///   creates command for tree expand
      /// </summary>
      /// <param name = "taskTag"></param>
      /// <param name = "path"> - values of parents of node</param>
      /// <param name = "values">-  rec ids of parents of node</param>
      /// <param name = "treeIsNulls">TODO</param>
      /// <returns>newly created command.</returns>
      internal static ExpandCommand CreateExpandCommand(String taskTag, String path,
                                                  String values,
                                                  String treeIsNulls)
      {
         return new ExpandCommand
         {
            TaskTag = taskTag,
            TreePath = path,
            TreeValues = values,
            TreeIsNulls = treeIsNulls
         };
      }

      /// <summary>
      ///   creates a subform refresh event command
      /// </summary>
      /// <returns>newly created command.</returns>
      internal static SubformRefreshEventCommand CreateSubformRefreshCommand(String taskTag, String subformTaskTag, bool explicitSubformRefresh)
      {
         return new SubformRefreshEventCommand
         {
            TaskTag = taskTag,
            SubformTaskTag = subformTaskTag,
            ExplicitSubformRefresh = explicitSubformRefresh,
            RefreshMode = ViewRefreshMode.UseTaskLocate
         };
      }

      /// <summary>
      ///   creates incremental search command
      /// </summary>
      /// <returns>newly created command.</returns>
      internal static LocateQueryEventCommand CreateLocateQueryCommand(String taskTag, int currentDit, string pressedString, bool reset, int fieldId)
      {
         return new LocateQueryEventCommand
         {
            TaskTag = taskTag,
            DitIdx = currentDit,
            IncrmentalSearchString = pressedString,
            ResetIncrementalSearch = reset,
            FldId = fieldId
         };
      }

      /// <summary>
      ///   creates a subform open event command
      /// </summary>
      /// <returns>newly created command.</returns>
      internal static SubformOpenEventCommand CreateSubformOpenCommand(String taskTag, int subformDitIdx)
      {
         return new SubformOpenEventCommand { TaskTag = taskTag, DitIdx = subformDitIdx };
      }

      /// <summary>
      ///   creat screen refresh command
      /// </summary>
      /// <returns>newly created command.</returns>
      internal static RefreshScreenEventCommand CreateScreenRefreshCommand(String taskTag, int topRecIdx, int clientRecId)
      {
         return new RefreshScreenEventCommand
         {
            TaskTag = taskTag,
            TopRecIdx = topRecIdx,
            RefreshMode = ViewRefreshMode.CurrentLocation,
            ClientRecId = clientRecId
         };
      }

      /// <summary>
      ///   creates a column sort event command
      /// </summary>
      /// <returns>newly created command.</returns>
      internal static ColumnSortEventCommand CreateColumnSortCommand(string taskTag, int direction, int ditIdx, int fieldId, int recId)
      {
         return new ColumnSortEventCommand
         {
            TaskTag = taskTag,
            DitIdx = ditIdx,
            FldId = fieldId + 1,
            ClientRecId = recId,
            Direction = direction
         };
      }

      ///   creates an Index Change event command
      /// </summary>
      /// <returns>newly created command.</returns>
      internal static IndexChangeEventCommand CreateIndexChangeCommand(string taskTag, int recId, ArgumentsList argList)
      {
         IndexChangeEventCommand cmd = new IndexChangeEventCommand
         {
            TaskTag = taskTag,
            ClientRecId = recId
         };

         // 1 parameter : The new Key Index
         if (argList != null && argList.getSize() != 0)
         {
            try
            {
               var keyIndex = new NUM_TYPE(argList.getArgValue(0, StorageAttribute.NUMERIC, 0));
               cmd.KeyIndex = keyIndex.NUM_2_LONG();
            }
            catch (Exception)
            {
               cmd.KeyIndex = 0;
            }
         }

         return cmd;
      }

/// <summary>
///   a factory method that creates an Event command
/// </summary>
/// <param name="taskTag"></param>
/// <param name="obj"></param>
/// <param name="exitByMenu"></param>
/// <param name="closeSubformOnly"></param>
/// <returns></returns>
internal static BrowserEscEventCommand CreateBrowserEscEventCommand(string taskTag, bool exitByMenu, bool closeSubformOnly)
      {
         return new BrowserEscEventCommand { TaskTag = taskTag, ExitByMenu = exitByMenu, CloseSubformOnly = closeSubformOnly };
      }

      /// <summary>
      ///   a factory method that creates an Event command
      /// </summary>
      /// <param name="taskTag"></param>
      /// <param name="obj"></param>
      /// <param name="clientRecId"></param>
      /// <returns></returns>
      internal static ComputeEventCommand CreateComputeEventCommand(string taskTag, bool subforms, int clientRecId)
      {
         return new ComputeEventCommand { TaskTag = taskTag, Subforms = subforms, ClientRecId = clientRecId };
      }

      /// <summary>
      ///   a factory method that creates a nonreversible exit command
      /// </summary>
      /// <param name = "taskTag">the task id</param>
      /// <returns>newly created command.</returns>
      internal static NonReversibleExitEventCommand CreateNonReversibleExitCommand(String taskTag, bool closeSubformOnly)
      {
         return new NonReversibleExitEventCommand { TaskTag = taskTag, CloseSubformOnly = closeSubformOnly };
      }

      /// <summary>
      ///   a factory method that creates a Recompute command
      /// </summary>
      /// <param name = "taskTag">the id of the task</param>
      /// <param name = "fieldId">the id of the field</param>
      /// <returns>newly created command.</returns>
      internal static RecomputeCommand CreateRecomputeCommand(String taskTag, int fieldId, bool ignoreSubformRecompute)
      {
         return new RecomputeCommand
         {
            TaskTag = taskTag,
            FldId = fieldId,
            IgnoreSubformRecompute = ignoreSubformRecompute
         };
      }

      /// <summary>
      ///   a factory method that creates a Transaction command
      /// </summary>
      /// <param name = "oper">the type of operation: Begin, Commit</param>
      /// <param name = "task">the task of the transaction</param>
      /// <param name = "cReversibleExit">true if the task exit is reversible</param>
      /// <param name = "level"></param>
      /// <returns>newly created command.</returns>
      internal static TransactionCommand CreateTransactionCommand(char oper, string taskTag, bool cReversibleExit, char level)
      {
         return new TransactionCommand
         {
            TaskTag = taskTag,
            Oper = oper,
            ReversibleExit = cReversibleExit,
            Level = level
         };
      }

      /// <summary>
      ///   a factory method that creates an Unload command
      /// </summary>
      /// <returns>newly created command.</returns>
      protected internal static UnloadCommand CreateUnloadCommand()
      {
         return new UnloadCommand();
      }

      /// <summary>
      ///   a factory method that creates an Hibernate command
      /// </summary>
      /// <returns>newly created command.</returns>
      protected internal static HibernateCommand CreateHibernateCommand()
      {
         return new HibernateCommand();
      }

      /// <summary>
      ///   a factory method that creates an Resume command
      /// </summary>
      /// <returns>newly created command.</returns>
      protected internal static ResumeCommand CreateResumeCommand()
      {
         return new ResumeCommand();
      }

      /// <summary>
      ///   a factory method that creates an ExecOper command
      /// </summary>
      /// <param name = "taskTag">the id of the task</param>
      /// <param name = "handlerId">the id of the handler where execution should start</param>
      /// <param name = "operIdx">the operation index is the server index indicated by the operation</param>
      /// <param name = "ditIdx">the control id</param>
      /// <param name = "value">the value of the control</param>
      /// <returns>newly created command.</returns>
      internal static ExecOperCommand CreateExecOperCommand(String taskTag, String handlerId, int operIdx, int ditIdx, String value)
      {
         var cmd = new ExecOperCommand
         {
            OperIdx = operIdx,
            TaskTag = taskTag,
            HandlerId = handlerId,
            DitIdx = ditIdx
         };

         if (value != null && value.Length == 0)
            cmd.Val = " ";
         else
            cmd.Val = value;
         return cmd;
      }

      /// <summary>
      /// A factory method that creates an verifyCache command
      /// </summary>
      /// <param name="collectedOfflineRequiredMetadata">list of cached files accessed in the current session.</param>
      /// <param name="isAccessingServerUsingHTTPS">true if web requests from the client are made using the HTTPS protocol.</param>
      /// <returns>the newly created command</returns>
      internal static VerifyCacheCommand CreateVerifyCacheCommand(Dictionary<string, string> collectedOfflineRequiredMetadata, bool isAccessingServerUsingHTTPS)
      {
         return new VerifyCacheCommand { CollectedOfflineRequiredMetadata = collectedOfflineRequiredMetadata,
                                         IsAccessingServerUsingHTTPS = isAccessingServerUsingHTTPS };
      }

      /// <summary> A factory method that creates an abortNonOfflineTasks command. </summary>
      /// <returns></returns>
      internal static AbortNonOfflineTasksCommand CreateAbortNonOfflineTasksCommand()
      {
         return new AbortNonOfflineTasksCommand();
      }

      /// <summary>
      ///   This method creates a command matching the passed menuUid and ctlIdx
      /// </summary>
      /// <param name = "currentTask">- the task from which the menu was selected</param>
      /// <param name = "menuUid">- the uid of the selected menu</param>
      /// <param name = "ctlIdx">- the ctlIdx of the selected menu</param>
      /// <returns>newly created command.</returns>
      internal static MenuCommand CreateMenuCommand(string taskTag, int menuUid, int ctlIdx, String menuPath)
      {
         return new MenuCommand
         {
            MenuUid = menuUid,
            MenuComp = ctlIdx,
            TaskTag = taskTag,
            MenuPath = menuPath
         };
      }

      /// <summary>
      ///   a factory method that creates an Evaluate command
      /// </summary>
      /// <param name = "taskTag">the id of the task</param>
      /// <param name = "expType">The expected type is the data type (attribute) of the result</param>
      /// <param name = "expIdx">expression id attributes are a unique identifier of the expression to evaluate</param>
      /// <param name = "expValLen"> represents the maximum length of an alpha result or the maximum digits to the right of the decimal point, for numeric result</param>
      /// <param name = "mprgCreator"></param>
      /// <returns>newly created command.</returns>
      internal static EvaluateCommand CreateEvaluateCommand(String taskTag, StorageAttribute expType, int expIdx,
                                                    int expValLen, Task mprgCreator)
      {
         var cmd = new EvaluateCommand
         {
            TaskTag = taskTag,
            ExpIdx = expIdx,
            ExpType = expType,
            MprgCreator = mprgCreator
         };

         
         if (expValLen > 0)
            cmd.LengthExpVal = expValLen;
         return cmd;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal static GlobalParamsQueryCommand CreateQueryGlobalParamsCommand()
      {
         return new GlobalParamsQueryCommand();
      }

      /// <summary>
      ///   Query Server for the Url of Cached file
      /// </summary>
      /// <param name = "cachedFileName">Points to the path of file on Server whose Url(cached file Url) is to be queried</param>
      /// <returns>newly created command.</returns>
      internal static CachedFileQueryCommand CreateQueryCachedFileCommand(string cachedFileName)
      {
         return new CachedFileQueryCommand { Text = cachedFileName };
      }

      /// <summary>
      ///   force ini file update by INIPut()
      /// </summary>
      /// <param name = "param">env param changed by INIPut</param>
      /// <returns>newly created command.</returns>
      internal static IniputForceWriteCommand CreateIniputForceWriteCommand(string param)
      {
         return new IniputForceWriteCommand { Text = param };
      }
   }
}
