using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.http;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using DataView = com.magicsoftware.richclient.data.DataView;
using EventHandler = com.magicsoftware.richclient.events.EventHandler;
using Field = com.magicsoftware.richclient.data.Field;
using FieldsTable = com.magicsoftware.richclient.data.FieldsTable;
using Record = com.magicsoftware.richclient.data.Record;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.tasks.CommandsProcessing;
using com.magicsoftware.richclient.local.application;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.exp;
using util.com.magicsoftware.util; 
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.tasks
{
   internal enum Flow
   {
      NONE = 0,
      STEP,
      FAST
   }

   internal enum Direction
   {
      BACK = -1,
      NONE,
      FORE
   }

   internal enum SubformExecModeEnum
   {
      NO_SUBFORM = -1,
      SET_FOCUS,
      FIRST_TIME,
      REFRESH
   }

   /// <summary>
   ///   represents a Task entity
   /// </summary>
   internal class Task : TaskBase
   {
      private readonly DvCache _dvCache;
      private MGData _mgData;
      internal String PreviouslyActiveTaskId { get; set; } // ID of the task that was the active context task, when this task was created

      private bool _aborting; // true if an abort command was executed for this task or one of its ancestors
      private bool _bExecEndTask;
      internal bool InEndTask { get; private set; } // am I checking whether the task should be closed?
      private bool _isStarted;
      internal bool IsTryingToStop { get; private set; } // if true then the task is trying to stop but not necessarily stopped yet
      internal bool IsAfterRetryBeforeBuildXML { get; private set; } // indicates whether flag AfterRetry is set to TRUE before building the XML
      private bool _evalOldValues; // indicates for the get value to get the old value of the field
      private bool _inRecordSuffix; // indicate we are in record suffix (handle before)
      private bool _cancelWasRaised; // was cancel/quit event raised ?
      private int _counter; // increases only in non interval tasks.
      private int _currStartProgLevel; //the level of startProg request in which we task is started
      private bool _destinationSubform; //if TRUE, the task is a Destination Subform task
      private bool _enableZoomHandler; //if TRUE, there is a zoom handler for this field
      private bool _exitingByMenu;
      private bool _firstRecordCycle; // indicates whether we are in the first record cycle
      private Direction _direction;
      private Flow _flowMode;
      private char _originalTaskMode = Constants.TASK_MODE_NONE;
      internal int MenuUid { get; private set; }
      private bool _inCreateLine; // is the task into processing CREATE LINE
      internal bool InSelect { get; set; } // true when the task is executing a SELECT internal event
      internal bool InStartProcess { get; private set; }
      private bool _isDestinationCall; // if TRUE, the task is a Destination Subform task by call operation
      private bool _knownToServer = true; // False if the server has
      internal LocateQuery locateQuery = new LocateQuery();
      private Stack _loopStack; // stack of iteration count for each block loop those are in execution i.e nested loops.
      private bool? _hasLocallyBoundDataControls = null;
      ///<summary>
      /// This is the task which invoked the handler containing the Call operation. 
      /// This member is used for searching the variables like VarIndex(), etc
      ///</summary>
      private Task _parentTask;
      internal Task ParentTask { get { return _parentTask; } }

      internal Task ContextTask { get; private set; } // under context of which task this task is currently running

      /// <summary>
      /// This is same as _parentTask except for Modal tasks. In case, the current task is modal, _triggeringTask refers to 
      ///  the task which invoked the handler of the _parentTask. This member is used for all generation-based functions and Prog(). 
      /// </summary>
      private Task _triggeringTask; // reference to the task that triggered the handler which created me

      /// <summary>
      ///   This is the task which contained the Call operation. If the task was executed without call 
      ///   operation eg. the MP, the startup program, etc., it refers to itself. This member used to search for the event handlers
      /// </summary>
      internal Task PathParentTask { get; set; }

      private bool _preventControlChange; // prevent change of control
      private bool _preventRecordSuffix; // True if we already performed record suffix
      private Direction _revertDirection = Direction.FORE; // for Verify operation's new Revert mode
      private int _revertFrom = -1; // for Verify operation's new Revert mode
      private List<Task> _taskPath; // the path from main program to this task
      private bool _transactionFailed;

      // returns true if the task is currently trying to commit a dynamicDef.transaction
      private bool _tryingToCommit;
      internal bool TryingToCommit
      {
         get { return _tryingToCommit; }
         set { _tryingToCommit = value; }
      }

      private bool _useLoopStack; // indicates whether the expression (LOOPCOUNTER()) is in the scope of Block Loop.

      private bool _inProcessingTopMostEndTaskSaved;

      internal bool InHandleActCancel { get;set;} 

      internal List<UserRange> UserLocs { get; set; }
      internal List<UserRange> UserRngs { get; set; }
      internal List<Sort> UserSorts { get; set; }
      internal SortCollection RuntimeSorts { get; set; }
      internal bool ResetLocate { get; set; }
      internal bool ResetRange { get; set; }
      internal bool ResetSort { get; set; }
      internal int VewFirst { get; set; }
      internal string dataViewContent { get; set; } // dataViewContent retrieved by GetDataViewContent command.
      internal bool PerformParentRecordPrefix { get; set; } // In record prefix (commonHandlerBefore) we execute record prefix of all fathers.
      // But we do not need it in all cases.
      // When we return from the server we do not need it.
      // When we click on the nested subform we need it.
      // if true, do the parent subform record prefix
      internal SubformExecModeEnum SubformExecMode { get; set; } // for SubformExecMode function
      // -1 – The task is not executed as a subform
      //  0 – The task is executed by setting the focus on it
      //  1 – The subtask is executed for the first time
      //  2 – The task is executed because the Automatic Refresh property 
      //      or the Subform Refresh event has been triggered
      internal bool ModeAsParent { get; set; }   //if the task mode is 'As Parent' we need to care out it specifically

      // the data view manager of the task
      internal DataviewManager DataviewManager { get; private set; }

      internal TaskTransactionManager TaskTransactionManager { get; set; }


      //off-line execution - task argument, to enable the update of field values
      internal ArgumentsList ArgumentsList { get; private set; }

      private Field _returnValueField; // off-line execution - the field of the calling task which will hold the task's return value

      /// <summary>
      /// is set true after initial data from remote and local database is fetched
      /// </summary>
      internal bool AfterFirstRecordPrefix { get; set; }

      /// <summary>
      /// Indicates that the subform must be refreshed on the client. 
      /// Relevant only for non-offline with local data.
      /// </summary>
      internal bool ExecuteClientSubformRefresh { get; set; }

      /// <summary>
      /// Indicates that the task executes CommonHandlerBefore method.
      /// For subforms we do not execute it's RP if the parent didn't finished yet
      /// it's CommandHandlerBefore method.
      /// </summary>
      internal bool InCommonHandlerBeforeRP { get; set; }

      /// <summary>
      /// retrieve and return a service object containing functionality that is different in the task level between connected and offline states.
      /// </summary>
      private ITaskServiceStrategy _taskServiceStrategy;
      public TaskServiceBase TaskService
      {
         get { return _taskServiceStrategy.GetTaskService(); }
      }

      /// <summary>
      /// retrieve and return a commands processor depending on task type and state.
      /// </summary>
      private ICommandsProcessorStrategy _commandsProcessorStrategy;
      internal CommandsProcessorBase CommandsProcessor
      {
         get { return _commandsProcessorStrategy.GetCommandProcessor(); }
      }

      /// <summary>
      /// list of tables of the task 
      /// </summary>
      internal List<DataSourceReference> DataSourceReferences { get; private set; }

      internal bool IsPreloadView { get { return ((DataView)DataView).HasMainTable && getProp(PropInterface.PROP_TYPE_PRELOAD_VIEW).getValueBoolean(); } }

      /// <summary>
      /// parent task of the subtask by toolkit tree include the main program.
      /// </summary>
      public Task LogicalStudioParentTask
      {
         get
         {
            if (StudioParentTask == null && !isMainProg())
            {
               Task mainProg = (Task)Manager.MGDataTable.GetMainProgByCtlIdx(ContextID, _ctlIdx);
               return mainProg;
            }
            return (Task)StudioParentTask;
         }

      }

      /// <summary>
      /// true if task has any fields
      /// </summary>
      internal bool HasFields
      {
         get
         {
            return DataView.GetFieldsTab().getSize() > 0;
         }
      }
      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal Task()
      {
         ActionManager = new ActionManager(this);
         DataView = new DataView(this);
         _flowMonitor = FlowMonitorQueue.Instance;

         ContextTask = this;
         _dvCache = new DvCache(this);
         MenuUid = Manager.GetCurrentRuntimeContext().LastClickedMenuUid;
         Manager.GetCurrentRuntimeContext().LastClickedMenuUid = 0;
         PerformParentRecordPrefix = true;
         DataviewManager = new DataviewManager(this);
         DataSourceReferences = new List<DataSourceReference>();
         TaskTransactionManager = new TaskTransactionManager(this);
      }

      /// <summary>
      ///   CTOR for subTasks
      /// </summary>
      /// <param name = "parent">a reference to the parent task</param>
      private Task(Task parent)
         : this()
      {
         _parentTask = PathParentTask = parent;
         _triggeringTask = null;
         buildTaskPath();
      }

      private String _menusFileName;

      internal bool KnownToServer
      {
         get { return _knownToServer; }
      }

      internal bool DoSubformPrefixSuffix { get; set; } // if true, do the subform record prefix & suffix
      internal bool ConfirmUpdateNo { get; set; } //only for confirms update
      internal bool RetainFocus { get; set; } //only for Call operation with destination
      internal bool InCtrlPrefix { get; set; }
      internal bool DataSynced { get; set; }
      internal UniqueTskSort UniqueSort { get; set; }

      internal bool HasMDIFrame
      {
         // Only main prog can be MDI frame. if not, don't bother because IsMDIFrame may have 
         // an expression and we don't want a compute.
         get { return (isMainProg() && (Form != null && Form.IsMDIFrame)); }
      }

      public Transaction TransactionErrorHandlingsRetry;
      /// <summary>
      /// the transaction need to be on the data view manager
      /// </summary>
      internal Transaction Transaction
      {
         get
         {
            return DataviewManager.CurrentDataviewManager.Transaction;
         }

         set
         {
            DataviewManager.CurrentDataviewManager.Transaction = value;
         }
      }

      internal int[] RefreshOnVars { get; set; }
      internal List<String[]> DvPosDescriptor { get; set; }
      internal TasksTable SubTasks { get; set; }
      internal ExpTable ExpTab { get; set; }
      internal UserEventsTable UserEvtTab { get; set; }
      internal DataviewHeaders DataviewHeadersTable { get; set; }
      internal HandlersTable HandlersTab { get; set; }

      public FormsTable _forms { get; set; }

      internal String Name { get; set; }
      private bool hasLocate;
      internal String PublicName { get; set; }
      public bool IsOffline { get; set; }

      /// <summary>
      /// indicates whether the task suffix was performed for this task
      /// </summary>
      internal bool TaskSuffixExecuted { get; set; }

      TaskDefinitionId taskDefinitionId;
      public TaskDefinitionId TaskDefinitionId
      {
         get
         {
            if (taskDefinitionId == null)
               taskDefinitionId = new TaskDefinitionId(_ctlIdx, ProgramIsn, TaskIsn, _isPrg);
            return taskDefinitionId;
         }
      }
      internal int RetrunValueExp { get; set; }
      internal MgControl SubformControl { get; set; }

      List<IDataviewHeader> LocalDataViewHeaders
      {
         get
         {
            return DataviewHeadersTable.FindAll(l => l is LocalDataviewHeader);
         }
      }

      /// <summary>
      /// should the dataview use the direction of the task locate - use it if and only if the range direction is "Ascending" and 
      /// the locate direction is "Descending"
      /// </summary>
      internal bool UseTaskLocateDirection
      {
         get
         {
            // check of range and locate directions differ
            Order locateDirection = Order.Ascending;
            Order rangeDirection = Order.Ascending;

            // get the locate order value
            if (checkIfExistProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE_ORDER))
               locateDirection = (Order)getProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE_ORDER).getValue()[0];

            // get the range order value
            if (checkIfExistProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_RANGE_ORDER))
               rangeDirection = (Order)getProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_RANGE_ORDER).getValue()[0];

            // QCR #283428 - if range direction is "descending", the locate direction is not used unless other locate definitions
            // exist
            return locateDirection == Order.Descending && rangeDirection == Order.Ascending;
         }
      }

      #region ITask Members

      /// <summary>
      ///   tells whether the task is aborting
      /// </summary>
      public override bool isAborting()
      {
         return _aborting;
      }

      #endregion

      /// <summary>
      /// 
      /// </summary>
      private void buildTaskPath()
      {
         _taskPath = new List<Task>();
         if (((Task)_parentTask)._taskPath != null)
            _taskPath.AddRange(((Task)_parentTask)._taskPath);
         _taskPath.Add(this);
      }

      /// <summary>
      ///   get the task data by parsing the xml string
      /// </summary>
      /// <param name = "mgd">reference to MGData</param>
      /// <param name="openingTaskDetails">additional information of opening task</param>
      protected internal void fillData(MGData mgd, OpeningTaskDetails openingTaskDetails)
      {
         // If this task was opened by an "open window" command - connect him to its parent
         if (!isMainProg())
         {
            if (_parentTask == null && openingTaskDetails.CallingTask != null)
            {
               _parentTask = openingTaskDetails.CallingTask;
               buildTaskPath();
               openingTaskDetails.CallingTask.addSubTask(this);
            }

            if (PathParentTask == null && openingTaskDetails.PathParentTask != null)
               PathParentTask = openingTaskDetails.PathParentTask;
         }

         // for MG_ACT_ZOOM action enable
         if ((_parentTask != null) && ((Task)_parentTask).getEnableZoomHandler())
            setEnableZoomHandler();

         _mgData = mgd;
         fillAttributes();

         TaskService.SetToolkitParentTask(this);

         _mgData.addTask(this); // add current Task to the global table

         MgFormBase parentForm = (_parentTask != null ? _parentTask.getForm() : null);

         while (initInnerObjects(ClientManager.Instance.RuntimeCtx.Parser.getNextTag(), parentForm))
         {
         }

         if (TaskService is RemoteTaskService)
         {
            initCtrlVerifyHandlerBits();
            TaskTransactionManager.PrepareTransactionProperties(DataviewManager.RemoteDataviewManager.Transaction, false);
         }

         if (TaskService is RemoteTaskService)
         {
            HandleTriggerTask();
            HandlePreviouslyActiveTaskId();
         }

         if (Form != null)
            Form.FormToBoActivatedOnClosingCurrentForm = openingTaskDetails.FormToBeActivatedOnClosingCurrentForm;

         //Set isSubform flag
         CheckAndSetSubForm();
         if (IsSubForm)
         {
            // get the subform control from the parent task
            var subFormCtrl = ((MgForm)getParent().getForm()).getSubFormCtrl(_taskTag);
            if (subFormCtrl != null)
               subFormCtrl.setSubformTaskId(_taskTag);

            // update the form with the subform control from the parent task
            if (Form != null)
               ((MgForm)Form).setSubFormCtrl(subFormCtrl);
         }

         DataviewManager.HasRemoteData = getDataviewHeaders().HasRemoteData;
         DataviewManager.HasLocalData = HasLocalData();
         DataviewManager.HasLocalLinks = getDataviewHeaders().HasLocalLinks;
      }

      /// <summary>
      /// true if task has local data
      /// </summary>
      /// <returns></returns>
      bool HasLocalData()
      {
         if (HasLocallyBoundDataControls)
            return true;
         return DataSourceReferences.Exists(d => d.IsLocal);

      }

      /// <summary>
      /// Gets whether the task has at least one data control that is bound to a local
      /// data source.
      /// </summary>
      bool HasLocallyBoundDataControls
      {
         get
         {
            if (_hasLocallyBoundDataControls == null)
            {
               // If the task does not have a form - do not change the value of _hasLocallyBoundDataControls.
               // The form may be set at a later stage.
               if (Form == null)
                  return false;

               // We do have a form --> find locally bound data controls.
               _hasLocallyBoundDataControls = false;

               IList<MgControlBase> dataControls = Form.CtrlTab.GetControls(ControlTable.SelectDataControlPredicate);
               foreach (var dc in dataControls)
               {
                  // If the SourceTableReference is set to null, the data control is bound to a server-wise data source.
                  // If the value is not null, the data control is bound to a local data source.
                  // See 'IPropertySerializerStrategy.h' and usage in 'AppSerializer.cpp'.
                  if (dc.SourceTableReference != null)
                  {
                     _hasLocallyBoundDataControls = true;
                     break;
                  }
               }
            }
            return (bool)_hasLocallyBoundDataControls;
         }
      }

      public bool ForceLocalCompute
      {
         get { return HasLocallyBoundDataControls || DataviewManager.HasLocalLinks; }
      }


      /// handle the PreviouslyActiveTaskId
      /// </summary>
      internal void HandlePreviouslyActiveTaskId()
      {
         if (_parentTask != null && _parentTask.GetContextTask() != null && _mgData != ((Task)_parentTask).getMGData())
            PreviouslyActiveTaskId = _parentTask.GetContextTask().getTaskTag();
         else if (_parentTask != null)
            PreviouslyActiveTaskId = _parentTask.getTaskTag();
      }

      /// <summary>
      /// handle the trigger task
      /// </summary>
      internal void HandleTriggerTask()
      {
         _triggeringTask = _parentTask;
      }

      /// <summary>
      ///  Fills the task tables data 
      /// </summary>
      /// <param name="taskTablesData"></param>
      /// <returns></returns>
      internal void FillTaskTables(byte[] taskTablesData)
      {
         TaskTablesSaxHandler handler = new TaskTablesSaxHandler(DataSourceReferences);
         MgSAXHandler mgSAXHandler = new MgSAXHandler(handler);
         mgSAXHandler.parse(taskTablesData);
      }

      /// <summary>
      ///   get the task attributes
      /// </summary>
      protected override void fillAttributes()
      {
         int endContext = ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, ClientManager.Instance.RuntimeCtx.Parser.getCurrIndex());
         if (endContext != -1 && endContext < ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().Length)
         {
            base.fillAttributes();

            // QCR #759911: mprg must belong to the main application's MGData.
            // This ensures it will not be discarded until the end of execution.
            if (isMainProg())
            {
               _mgData = MGDataCollection.Instance.getMGData(0);
               _taskServiceStrategy = new MainProgTaskServiceStrategy();
               _commandsProcessorStrategy = new MainProgCommandProcessorStrategy();
            }
            else
            {
               // task is not main program - initialize strategies according to task's "IsOffline" state
               _commandsProcessorStrategy = new CommonCommandProcessorStrategy(IsOffline);
               _taskServiceStrategy = new CommonTaskServiceStrategy(IsOffline);
               // for offline tasks, we need to change the task tag, so it won't be confused with server-originated tags 
               setTaskId(TaskService.GetTaskTag(getTaskTag()));
            }

            return;
         }
         Logger.Instance.WriteExceptionToLog("in Task.fillAttributes() out of string bounds");
      }

      /// <summary>
      ///  get the Isn of PosElement of PosCache.
      /// </summary>
      public override int GetDVControlPosition()
      {
         return 0;
      }

      /// <summary>
      ///   set the task attributes
      /// </summary>
      protected override bool setAttribute(string attribute, string valueStr)
      {
         //----------------------------------------------------------------------

         bool isTagProcessed;
         isTagProcessed = base.setAttribute(attribute, valueStr);

         if (!isTagProcessed)
         {
            isTagProcessed = true;
            switch (attribute)
            {
               case ConstInterface.MG_ATTR_REFRESHON:
                  if (!valueStr.Trim().Equals(""))
                     SetRefreshOnVars(valueStr);
                  break;

               case XMLConstants.MG_ATTR_NAME:
                  Name = valueStr;
                  break;

               case ConstInterface.MG_ATTR_DVPOS_DEC:
                  if (!valueStr.Trim().Equals(""))
                     setDescriptor(valueStr);
                  break;

               case ConstInterface.MG_ATTR_HAS_LOCATE:
                  hasLocate = true;
                  break;

               case ConstInterface.MG_ATTR_AS_PARENT:
                  ModeAsParent = true;
                  break;

               case ConstInterface.MG_ATTR_TASK_UNIQUE_SORT:
                  UniqueSort = (UniqueTskSort)valueStr[0];
                  break;

               case ConstInterface.MG_ATTR_TRANS_ID:
                  setRemoteTransaction(valueStr);
                  break;

               case ConstInterface.MG_ATTR_PUBLIC:
                  PublicName = XmlParser.unescape(valueStr);
                  break;             

               case XMLConstants.MG_ATTR_IS_OFFLINE:
                  IsOffline = XmlParser.getBoolean(valueStr);
                  break;

               case XMLConstants.MG_ATTR_RETURN_VALUE_EXP:
                  RetrunValueExp = XmlParser.getInt(valueStr);
                  break;

               case XMLConstants.MG_ATTR_MENUS_FILE_NAME:
                  _menusFileName = valueStr;
                  break;

               default:
                  isTagProcessed = false;
                  break;
            }
         }

         if (!isTagProcessed)
            Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));

         return true;
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">possible tag name , name of object, which need be allocated</param>
      protected override bool initInnerObjects(String foundTagName, MgFormBase parentForm)
      {
         if (foundTagName == null)
            return false;

         bool isTagProcessed = base.initInnerObjects(foundTagName, parentForm);

         if (!isTagProcessed)
         {
            isTagProcessed = true;
            switch (foundTagName)
            {
               case XMLConstants.MG_TAG_FORMS:
                  _forms = new FormsTable(this, parentForm);
                  Logger.Instance.WriteDevToLog(string.Format("{0} ...", foundTagName));

                  _forms.fillData();
                  break;

               case XMLConstants.MG_TAG_TASK:
                  // Subtask found
                  if (SubTasks == null)
                     SubTasks = new TasksTable();
                  var subtask = new Task(this); // new subTask
                  ClientManager.Instance.TasksNotStartedCount += 1;
                  SubTasks.addTask(subtask); // add subtask to table
                  MGData mgd = _mgData;
                  if (isMainProg())
                     mgd = MGDataCollection.Instance.GetMGDataForStartupProgram();
                  subtask.fillData(mgd, new OpeningTaskDetails()); // init of the subtask
                  break;

               case ConstInterface.MG_TAG_TASKURL:
                  ClientManager.Instance.ProcessTaskURL();
                  break;

               case ConstInterface.MG_TAG_EXPTABLE:
                  if (ExpTab == null)
                     ExpTab = new ExpTable();
                  Logger.Instance.WriteDevToLog("goes to exp");
                  ExpTab.fillData(this);
                  break;

               case ConstInterface.MG_TAG_USER_EVENTS:
                  if (UserEvtTab == null)
                     UserEvtTab = new UserEventsTable();
                  Logger.Instance.WriteDevToLog("goes to user event tab");
                  UserEvtTab.fillData(this);
                  break;

               case ConstInterface.MG_TAG_LINKS:
                  DataviewHeadersTable = new DataviewHeaders(this);
                  DataviewHeadersTable.fillData();
                  break;
               case ConstInterface.MG_TAG_SORTS:
                  RuntimeSorts = new SortCollection();
                  RuntimeSorts.fillData(this);
                  break;
               case ConstInterface.MG_TAG_TASK_TABLES:
                  FillTaskTables();
                  break;
               case XMLConstants.MG_TAG_RECOMPUTE:
                  Logger.Instance.WriteDevToLog("goes to recompute");
                  RecomputeFillData();
                  break;

               case ConstInterface.MG_TAG_EVENTHANDLERS:
                  Logger.Instance.WriteDevToLog("goes to eventhandlers");
                  if (HandlersTab == null)
                     HandlersTab = new HandlersTable();
                  HandlersTab.fillData(this);
                  addExpHandlersToMGData();
                  break;

               default:
                  if (foundTagName.Equals('/' + XMLConstants.MG_TAG_TASK))
                  {
                     ClientManager.Instance.RuntimeCtx.Parser.setCurrIndex2EndOfTag();
                     return false;
                  }
                  else
                     isTagProcessed = false;

                  break;
            }
         }

         if (!isTagProcessed)
         {
            Logger.Instance.WriteDevToLog("There is no such tag in Task.initInnerObjects . Enter case for " +
                                        foundTagName);
            return false;
         }

         return true;
      }

      internal void RecomputeFillData()
      {
         var recompTab = new RecomputeTable();
         recompTab.fillData((DataView)DataView, this); //TODO: disconnect from the dataview
      }

      /// <summary>
      /// 
      /// </summary>
      private void FillTaskTables()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         // Get the end of the task table element
         int endContext = parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_TASK_TABLES_END, parser.getCurrIndex());
         endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, endContext) + XMLConstants.TAG_CLOSE.Length;
         // get the task table xml string
         String taskTablesData = parser.getXMLsubstring(endContext);

         FillTaskTables(Encoding.UTF8.GetBytes(taskTablesData));

         parser.setCurrIndex(endContext);
      }

      /// <summary>
      ///   fill the control verification handler bits
      /// </summary>
      protected internal void initCtrlVerifyHandlerBits()
      {
         int j;
         EventHandler aHandler;
         MgControl hndCtrl;

         Debug.Assert(HandlersTab != null);

         // go over the task handlers
         for (j = 0; j < HandlersTab.getSize(); j++)
         {
            aHandler = HandlersTab.getHandler(j);
            // for each handler which is a ctrlVerify handler
            if (InternalInterface.MG_ACT_CTRL_VERIFICATION == aHandler.getEvent().getInternalCode())
            {
               // locate the control and update the relevant bit
               hndCtrl = aHandler.getStaticCtrl();
               if (hndCtrl != null)
                  hndCtrl.HasVerifyHandler = true;
            }
         }
      }

      /// <summary>
      ///   add handlers triggered by expressions to mgdata
      /// </summary>
      private void addExpHandlersToMGData()
      {
         int idx = 0;
         for (int i = HandlersTab.getSize() - 1; i >= 0; i--)
         {
            EventHandler handler = HandlersTab.getHandler(i);
            Event evt = handler.getEvent();
            if (evt.getType() == ConstInterface.EVENT_TYPE_EXPRESSION ||
                (evt.getType() == ConstInterface.EVENT_TYPE_USER &&
                 evt.getUserEventType() == ConstInterface.EVENT_TYPE_EXPRESSION))
            {
               // add the handlers to the top of the vector
               _mgData.addExpHandler(handler, idx);
               idx++;
            }
         }
      }

      /// <summary>
      ///   get the refresh on variable list
      /// </summary>
      internal void SetRefreshOnVars(String variables)
      {
         String[] strTok = StrUtil.tokenize(variables, ",");
         Int32 fldNum;
         var intVals = new List<Int32>();
         int i;

         for (i = 0; i < strTok.Length; i++)
         {
            fldNum = Int32.Parse(strTok[i]);
            intVals.Add(fldNum);
         }
         RefreshOnVars = new int[intVals.Count];
         for (i = 0; i < intVals.Count; i++)
         {
            fldNum = intVals[i];
            RefreshOnVars[i] = fldNum;
         }
      }

      /// <summary>
      ///   set the DataSynced property of task
      /// </summary>
      public override void setDataSynced(bool synced)
      {
         DataSynced = synced;
      }


      /// <summary>
      /// sets the current task mode
      /// </summary>
      /// <param name="val">new task mode</param>
      public override void setMode(char val)
      {
         base.setMode(val);

         MgForm form = (MgForm)getTopMostForm();
         if (form != null)
            form.UpdateStatusBar(GuiConstants.SB_TASKMODE_PANE_LAYER, getModeString(), false);
      }

      /// <summary>
      ///   get the task mode string
      /// </summary>
      public String getModeString()
      {
         char taskMode = getMode();
         String mode = null;
         switch (taskMode)
         {
            case Constants.TASK_MODE_MODIFY:
               mode = ConstInterface.TASK_MODE_MODIFY_STR;
               break;
            case Constants.TASK_MODE_CREATE:
               mode = ConstInterface.TASK_MODE_CREATE_STR;
               break;
            case Constants.TASK_MODE_DELETE:
               mode = ConstInterface.TASK_MODE_DELETE_STR;
               break;
            case Constants.TASK_MODE_QUERY:
               mode = ConstInterface.TASK_MODE_QUERY_STR;
               break;
            default:
               mode = null;
               break;
         }
         return mode;
      }


      /// <summary>
      ///   get a field by its name
      /// </summary>
      /// <param name = "fldName">of the field</param>
      /// <returns> the field by its name</returns>
      internal Field getField(String fldName)
      {
         return ((DataView)DataView).getField(fldName);
      }

      /// <summary>
      ///   return an expression by its id
      /// </summary>
      /// <param name = "id">id of the expression</param>
      internal Expression getExpById(int id)
      {
         if (ExpTab != null)
            return ExpTab.getExpById(id);
         Logger.Instance.WriteExceptionToLog("in Task.GetExpById(): no expression table");
         return null;
      }


      /// <summary>
      /// Calculate expresion
      /// </summary>
      /// <param name="expId">The expression to evaluate</param>
      /// <param name="resType">The expected result type.</param>
      /// <param name="length">The expected length of the result.</param>
      /// <param name="contentTypeUnicode">Denotes whether the result is expected to be a unicode string.</param>
      /// <param name="resCellType">Not used.</param>
      /// <returns></returns>
      public override string CalculateExpression(int expId, StorageAttribute resType, int length, bool contentTypeUnicode, StorageAttribute resCellType)
      {
         String result = null;
         if (expId > 0)
         {
            Expression exp = getExpById(expId);
            Debug.Assert(exp != null);
            result = exp.evaluate(resType, length);
         }
         return result;
      }

      /// <summary>
      /// Evaluate the expression whose id is 'expressionId', expecting the result to be
      /// a unicode value.
      /// </summary>
      /// <param name="expressionId">The expression number to evaluate</param>
      /// <param name="result">The evaluation result</param>
      /// <returns>
      /// The method returns 'true' if it evaluated the expression. Otherwise
      /// it returns 'false', in which case the value of result should not be regarded.
      /// </returns>
      internal bool EvaluateExpressionAsUnicode(int expressionId, out string value)
      {

         bool wasEvaluated;
         value = EvaluateExpression(expressionId, StorageAttribute.UNICODE, 0, false, StorageAttribute.NONE, true, out wasEvaluated);
         return wasEvaluated;
      }

      /// <summary>
      /// Evaluate the expression whose id is 'expressionId', expecting the result to be
      /// a long numeric value.
      /// </summary>
      /// <param name="expressionId">The expression number to evaluate</param>
      /// <param name="result">The evaluation result</param>
      /// <returns>
      /// The method returns 'true' if it evaluated the expression. Otherwise
      /// it returns 'false', in which case the value of result should not be regarded.
      /// </returns>
      internal bool EvaluateExpressionAsLong(int expressionId, out long result)
      {
         result = 0;
         GuiExpressionEvaluator.ExpVal evalutationResult = null;
         bool wasEvaluated = false;
         if (expressionId > 0)
         {
            Expression exp = getExpById(expressionId);
            Debug.Assert(exp != null);

            evalutationResult = exp.evaluate(StorageAttribute.NUMERIC);
            result = evalutationResult.MgNumVal.NUM_2_LONG();
            wasEvaluated = true;
         }

         return wasEvaluated;
      }


      /// <summary>
      ///   Insert Record Table to dataview of the task if it came after closing task tag
      /// </summary>
      internal void insertRecordTable(bool invalidate)
      {
         // check cache logic only for sub-forms
         if (isCached())
         {
            bool firstTime = ((DataView)DataView).getFirstDv();
            // if it is the first time the task get dv or the old dv is not invalidated or
            // the old dv does not include the first record do not put in cache
            if (!firstTime && invalidate)
            {
               if (!hasLocate)
               {
                  // the old d.v was not updated put it in cache (its replicate)
                  if (((DataView)DataView).IncludesFirst())
                  {
                     if (!((DataView)DataView).getChanged())
                        _dvCache.putInCache(((DataView)DataView).replicate());
                     else
                        _dvCache.removeDvFromCache(((DataView)DataView).getDvPosValue(), true);
                  }
               }
               else
                  locatePutInCache();
            }
         }

         // parse the new dataview
         ((DataView)DataView).fillData();

         var currRec = ((DataView)DataView).getCurrRec();
         if (currRec != null)
            currRec.SetDotNetsIntoRec();

         if (invalidate)
            ((DataView)DataView).setChanged(false);
      }

      /// <summary>
      ///   get the handlers table
      /// </summary>
      internal HandlersTable getHandlersTab()
      {
         return HandlersTab;
      }

      /// <summary>
      ///   build the XML string for the dataview
      /// </summary>
      protected internal void buildXML(StringBuilder message)
      {
         IsAfterRetryBeforeBuildXML = getAfterRetry();

         if (KnownToServer && !IsOffline)
         {
            message.Append(XMLConstants.START_TAG + XMLConstants.MG_TAG_TASK);
            message.Append(" " + XMLConstants.MG_ATTR_TASKID + "=\"" + getTaskTag() + "\"");
            message.Append(" " + ConstInterface.MG_ATTR_TASK_MAINLEVEL + "=\"" + getMainLevel() + "\"");
            message.Append(" " + ConstInterface.MG_ATTR_TASK_FLOW_DIRECTION + "=\"" + (char)getFlowModeDir() + "\"");
            message.Append(XMLConstants.TAG_CLOSE);

            ((DataView)DataView).buildXML(message);

            if (Form != null && Form.Opened)
               message.Append(((MgForm)Form).GetHiddenControlListXML());

            message.Append("\n   </" + XMLConstants.MG_TAG_TASK + XMLConstants.TAG_CLOSE);
         }
      }

      /// <summary>
      ///   build the XML string for the ranges
      /// </summary>
      internal void buildXMLForRngs(StringBuilder message, List<UserRange> UserRanges, bool locate)
      {
         int i;
         Field fld;
         const StorageAttribute cellAttr = StorageAttribute.SKIP;
         bool toBase64 = (ClientManager.Instance.getEnvironment().GetDebugLevel() <= 1);

         message.Append(XMLConstants.TAG_CLOSE);
         for (i = 0; i < UserRanges.Count; i++)
         {
            message.Append(XMLConstants.START_TAG + ConstInterface.USER_RNG);

            UserRange rng = UserRanges[i];

            message.Append(" " + XMLConstants.MG_TAG_FLD + "=\"" + Convert.ToString(rng.veeIdx) + "\"");

            fld = (Field)DataView.getField((int)rng.veeIdx - 1);
            StorageAttribute fldAttr = fld.getType();

            if (rng.nullMin)
               message.Append(" " + ConstInterface.NULL_MIN_RNG + "=\"1\"");

            if (!rng.discardMin && rng.min != null)
            {
               string val = Record.itemValToXML(rng.min, fldAttr, cellAttr, toBase64);
               message.Append(" " + ConstInterface.MIN_RNG + "=\"" + val + "\"");
            }

            if (rng.nullMax)
               message.Append(" " + ConstInterface.NULL_MAX_RNG + "=\"1\"");

            if (!rng.discardMax && rng.max != null)
            {
               string val = Record.itemValToXML(rng.max, fldAttr, cellAttr, toBase64);
               message.Append(" " + ConstInterface.MAX_RNG + "=\"" + val + "\"");
            }

            message.Append(XMLConstants.TAG_TERM);
         }
         if (locate)
            message.Append(XMLConstants.END_TAG + ConstInterface.USER_LOCATES + XMLConstants.TAG_CLOSE);
         else
            message.Append(XMLConstants.END_TAG + ConstInterface.USER_RANGES + XMLConstants.TAG_CLOSE);
      }

      /// <summary>
      ///   build the XML string for the user added sorts
      /// </summary>
      protected internal void buildXMLForSorts(StringBuilder message)
      {
         if (UserSorts != null && UserSorts.Count > 0)
         {
            int i;
            for (i = 0; i < UserSorts.Count; i++)
            {
               message.Append(XMLConstants.TAG_CLOSE);
               Sort srt = UserSorts[i];

               message.Append(XMLConstants.START_TAG + ConstInterface.SORT);
               message.Append(" " + XMLConstants.MG_TAG_FLD + "=\"" + Convert.ToString(srt.fldIdx) + "\"");
               if (srt.dir)
                  message.Append(" " + ConstInterface.MG_ATTR_DIR + "=\"1\"");

               message.Append(XMLConstants.TAG_TERM);
            }
            UserSorts.Clear();
            UserSorts = null;
            message.Append(XMLConstants.END_TAG + ConstInterface.MG_TAG_SORTS + XMLConstants.TAG_CLOSE);
         }
      }

      /// <summary>
      ///   returns a user event by its index
      /// </summary>
      /// <param name = "idx">the index of the requested event</param>
      internal Event getUserEvent(int idx)
      {
         Debug.Assert(UserEvtTab != null);
         Event retEvent = null;
         if (UserEvtTab != null)
            retEvent = UserEvtTab.getEvent(idx);
         return retEvent;
      }

      /// <summary> Set the MoveToFirstControl flag on the form. </summary>
      /// <param name="moveToFirstControl"></param>
      private void SetMoveToFirstControl(bool moveToFirstControl)
      {
         if (moveToFirstControl || !IsSubForm)
         {
            if (Form != null)
            {
               //this is a call destination/ subform call
               //We perform move to first control only to one control form per request - the first one that we receive
               bool alreadyMoved = ((MgForm)Form).alreadyMovedToFirstControl();
               if (!alreadyMoved)
                  ((MgForm)Form).MovedToFirstControl = true;
            }
         }
      }

      /// <summary>
      ///   start the execution of the task and its subTasks
      /// </summary>
      internal ITask Start(bool moveToFirstControl, ArgumentsList argList, Field returnValField, bool callByDestSubForm)
      {
         Task nonInteractiveTask = null;

         if (!_isStarted)
         {
            _isStarted = true;
            ClientManager.Instance.TasksNotStartedCount -= 1;

            InitializeExecution(argList, returnValField);

            InStartProcess = true;
            ReturnResult result = Setup(argList);

            // task initialization failed - close the task 
            if (!result.Success)
               return null;

            _inProcessingTopMostEndTaskSaved = ClientManager.Instance.EventsManager.getProcessingTopMostEndTask();
            ClientManager.Instance.EventsManager.setProcessingTopMostEndTask(false);

            EnableActions();

            if (_isMainPrg || Form == null)
               Manager.MenuManager.getApplicationMenus(this);

            nonInteractiveTask = InitializeForm(moveToFirstControl, nonInteractiveTask);

            TaskService.OpenForm(this, callByDestSubForm);

            DataViewWasRetrieved = true;

            // QCR # 167332 :
            // If we start porgram P1, for which MP is started. MP is calling P2 from it's TP. So, when it calls P2 
            // again it starts MP, but from MP it again starts P1. as P1 is in it's subtask and it is not yet started. 
            // So, it's wrong to start P1 while executing TP of MP. It should execute only P2.
            // So, in order to avoid this, set MP's subtasks to null before entering TP. and again restore it back, in order to
            // start P1 properly.

            TasksTable subTasks = SubTasks;
            if (isMainProg())
               SubTasks = null;
            result = TaskService.ExecuteTaskPrefix(this);
            if (isMainProg())
               SubTasks = subTasks;
            
            // task initialization failed - close the task 
            if (!result.Success)
               return null;

            // If task prefix failed (e.g. with verify error), exit. Don't call EndTaskOnError because endTask was already called
            if (_aborting)
               return null;

            IClientCommand dataViewCommand = CommandFactory.CreateDataViewCommand(getTaskTag(), DataViewCommandType.FirstChunk);
            result = DataviewManager.Execute(dataViewCommand);


            if (!result.Success)
               return null;
           
            ResumeSubformLayout(false);            

            TaskService.DoFirstRefreshTable(this);

            TaskService.EnterFirstRecord(this);
         }

         nonInteractiveTask = StartSubTasks(moveToFirstControl, argList, returnValField, nonInteractiveTask, callByDestSubForm);

         InStartProcess = false;

         return nonInteractiveTask;
      }


      protected override bool CanResumeSubformLayout()
      {
         // wait till we retrive dataview (relevant for local) before resuming layout
         // number of records affect visibility of scrollbar and initial table pacement
         return DataViewWasRetrieved;
      }
      /// <summary>
      /// Initialize execution-related stuff.
      /// This method should be called ONLY from Task.Start
      /// </summary>
      /// <param name="argList"></param>
      /// <param name="returnValField"></param>
      private void InitializeExecution(ArgumentsList argList, Field returnValField)
      {
         // currently the is no control executing there for the
         // current executing control index is set to -1
         setBrkLevel(ConstInterface.BRK_LEVEL_REC_MAIN, -1);
         _firstRecordCycle = true; // While starting a Task, set firstRecordCycle = TRUE

         ArgumentsList = argList;

         // QCR #299153. Add recomputes only for subforms (not for subtasks).
         if (IsSubForm)
            TaskService.AddSubformRecomputes(this);

         _returnValueField = returnValField;

         _currStartProgLevel = (IsSubForm && ((MgForm)getParent().getForm()).alreadyMovedToFirstControl()
                                  ? ((Task)getParent())._currStartProgLevel
                                  : ClientManager.Instance.StartProgLevel);
      }

      /// <summary>
      /// initialize form-related stuff
      /// </summary>
      /// <param name="moveToFirstControl"></param>
      /// <param name="nonInteractiveTask"></param>
      /// <returns></returns>
      private Task InitializeForm(bool moveToFirstControl, Task nonInteractiveTask)
      {
         if (Form != null)
         {
            if (!_isMainPrg)
               resetRcmpTabOrder();

            // set MoveToFirstControl flag on the form.
            SetMoveToFirstControl(moveToFirstControl);

            // the InitForm might cause execution of code (variable change) so we should do it with a new server execution stack
            ClientManager.Instance.EventsManager.pushNewExecStacks();

            // initialize the current task's form.
            nonInteractiveTask = (Task)InitForm();

            ClientManager.Instance.EventsManager.popNewExecStacks();

            getMGData().StartTimers();
            Commands.beginInvoke();

            //Update the panes with task info.
            if (!_isMainPrg)
               ((MgForm)Form).UpdateStatusBar(GuiConstants.SB_TASKMODE_PANE_LAYER, getModeString(), false);
         }
         return nonInteractiveTask;
      }

      /// <summary>
      /// Sets subform control for the new opened subform task and
      /// sets subform task tag on the parent subform control
      /// </summary>
      /// <param name="subformControl"></param>
      internal void PrepareForSubform(MgControl subformControl)
      {
         TaskService.PrepareForSubform(this, subformControl);
      }

      /// <summary>
      /// start sub tasks of this task
      /// </summary>
      /// <param name="moveToFirstControl"></param>
      /// <param name="argList"></param>
      /// <param name="returnValField"></param>
      /// <param name="nonInteractiveTask"></param>
      /// <returns></returns>
      private Task StartSubTasks(bool moveToFirstControl, ArgumentsList argList, Field returnValField, Task nonInteractiveTask, bool callByDestSubForm)
      {
         if (hasSubTasks())
         {
            for (int i = 0; i < SubTasks.getSize(); i++)
            {
               Task task = SubTasks.getTask(i);
               var tmpNonInteractiveTask = (Task)task.Start(moveToFirstControl, argList, returnValField, callByDestSubForm);
               if (nonInteractiveTask == null)
                  nonInteractiveTask = tmpNonInteractiveTask;
               else if (tmpNonInteractiveTask != null)
                  Debug.Assert(false, "more than 1 non interactive in task.start");
            }
         }

         return nonInteractiveTask;
      }

      /// <summary>
      /// 
      /// </summary>
      private void EnableActions()
      {
         int[] actList = HasMDIFrame
                   ? events.ActionManager.actMDIFrameEnabled
                   : events.ActionManager.actEnabled;
         ActionManager.enableList(actList, true, false);

         if (IsSubForm)
            ActionManager.enable(InternalInterface.MG_ACT_POST_REFRESH_BY_PARENT, true);

         if (!HasMDIFrame)
         {
            //when we have default button then enable the action
            if (Form != null && ((MgForm)Form).getDefaultButton(false) != null)
               ActionManager.enable(InternalInterface.MG_ACT_DEFAULT_BUTTON, true);

            // enable actions depending on task properties
            enableModes();
            if (checkProp(PropInterface.PROP_TYPE_SELECTION, false))
            {
               if ((!Form.getProp(PropInterface.PROP_TYPE_DEFAULT_BUTTON).getValue().Equals("")))
               {
                  String defaultButtonName = Form.getProp(PropInterface.PROP_TYPE_DEFAULT_BUTTON).getValue();
                  MgControl ctrl = ((MgForm)Form).getCtrl(defaultButtonName);
                  var aRtEvt = new RunTimeEvent(ctrl);
                  if (aRtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                      aRtEvt.getInternalCode() == InternalInterface.MG_ACT_SELECT)
                     ActionManager.enable(InternalInterface.MG_ACT_SELECT, true);
               }
               else
                  ActionManager.enable(InternalInterface.MG_ACT_SELECT, true);
            }

            bool enablePrintdata = checkProp(PropInterface.PROP_TYPE_PRINT_DATA, false);
            ActionManager.enable(InternalInterface.MG_ACT_PRINT_DATA, enablePrintdata);
         }
      }

      /// <summary>
      /// show error on task's form
      /// </summary>
      /// <param name="text"></param>
      internal void ShowError(string text)
      {
         //Fixed bug #311878, while the task is in abort and we try to display message box, 
         //                   the form is dispose and then we get the message box command in GuiInteractive,
         //                   so if the task in abort we will send null and form (so the command will create a dummy form).
         MgForm currForm = InStartProcess || isAborting() ? null : (MgForm)getTopMostForm();
         String mlsTransText = ClientManager.Instance.getLanguageData().translate(text);
         String mlsTransTitle = ClientManager.Instance.getLanguageData().translate(ConstInterface.ERROR_STRING);
         //mls ???
         Commands.messageBox(currForm, mlsTransTitle, mlsTransText,
                                       Styles.MSGBOX_ICON_ERROR | Styles.MSGBOX_BUTTON_OK);
      }
      /// <summary>
      /// check the result. If failed - display the error messagebox and end the task
      /// </summary>
      /// <param name="result"></param>
      /// <returns></returns>
      internal bool EndTaskOnError(ReturnResult result, bool displayError)
      {
         if (!result.Success && !isAborting())
         {
             if (InStartProcess)
             {
                 endTask(false, false, false);
                 if (displayError)
                     ShowError(result.ErrorDescription);
                 InStartProcess = false;
                 return true;
             }
             else
             {
                 if (displayError)
                     ShowError(result.ErrorDescription);
                 ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_EXIT);
             }               
         }

         return false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="argList"></param>
      /// <returns></returns>
      private ReturnResult Setup(ArgumentsList argList)
      {
         ReturnResult result;
         ReturnResult resultInitMode;
         TaskService.InitTaskPrefixExecutedFlag(this);
         TaskSuffixExecuted = false;

        
         TaskService.CreateFirstRecord(this);
         TaskService.CopyArguments(this, argList);

         // set the task mode of the task
         resultInitMode = TaskService.SetTaskMode(this);
         

      
         // init data view
         // Note: Even if the resultInitMode is false we MUST init the data view
         IClientCommand dataViewCommand = CommandFactory.CreateDataViewCommand(getTaskTag(), DataViewCommandType.Init);
         result = DataviewManager.Execute(dataViewCommand);

         // Note: need to check resultInitMode and if not success then return this error
         if (!resultInitMode.Success)
         {
            EndTaskOnError(resultInitMode, true);
            return resultInitMode;
         }


         // need to check result(of the data view init and if not success then return this error
         if (!result.Success)
            return result;



         if (result.Success)
         {
            //Execute the command, whose execution was delayed becuase task was not started. So Excute those commands after task started.
            MgArrayList commands = ClientManager.Instance.getCommandsExecutedAfterTaskStarted();

            for(int cmdIdx = 0; cmdIdx < commands.Count; cmdIdx ++)
            {
               IClientTargetedCommand command = commands[cmdIdx] as IClientTargetedCommand;
               if (command.TaskTag == this.getTaskTag())
               {
                  command.Execute(null);
                  ClientManager.Instance.removeCommandsExecutedAfterTaskStarted(command);
               }
            }

            dataViewCommand = CommandFactory.CreateDataViewCommand(getTaskTag(), DataViewCommandType.Prepare);
            result = DataviewManager.Execute(dataViewCommand);

         }

         if (result.Success)
         {
            result = TaskService.PrepareTask(this);
            if (!result.Success)
               EndTaskOnError(result, true);
         }


         if (result.Success)
         {
            dataViewCommand = CommandFactory.CreateDataViewCommand(getTaskTag(), DataViewCommandType.InitDataControlViews);
            result = DataviewManager.Execute(dataViewCommand);
         }

         return result;
      }

      /// <summary>
      /// set the arguments on the task parameters
      /// </summary>
      /// <param name="task"></param>
      /// <param name="args"></param>
      internal void CopyArguments(ArgumentsList args)
      {
         FieldsTable fieldsTable = DataView.GetFieldsTab() as FieldsTable;

         int numberOfArguments = (args != null) ? args.getSize() : 0;
         int currentArgIndex = 0; // index of currently handled argument
         bool taskHasParameters = ((DataView)DataView).ParametersExist();

         if (numberOfArguments > 0)
         {
            int dvPos = ((DataView)DataView).GetRecordMainIdx();
            int dvSize = ((DataView)DataView).GetRecordMainSize();

            // Go over the fields, find the fields to set the arguments to
            for (int i = dvPos; (i < dvSize) && (currentArgIndex < numberOfArguments); i++)
            {
               Field field = fieldsTable.getField(i) as Field;

               if (!field.IsForArgument(taskHasParameters))
                  continue;

               // get the argument
               Argument arg = args.getArg(currentArgIndex);
               if (arg.skipArg())
               {
                  field.invalidate(true, Field.CLEAR_FLAGS);
                  field.compute(false);
               }
               else
                  arg.setValueToField(field);

               currentArgIndex++;
            }
         }

      }

      /// <summary>
      ///   do first record prefix & suffix
      ///   This function is executed only for connected task.
      ///   For offline task we run EnterFirstRecord function. There we execute RP and open subforms.
      /// </summary>
      internal void doFirstRecordCycle()
      {
         if (!_isMainPrg)
         {
            ClientManager.Instance.EventsManager.pushNewExecStacks();
            SubformExecMode = SubformExecModeEnum.FIRST_TIME;
            ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_REC_PREFIX, IsSubForm);
            if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag())
            {
               if (IsSubForm)
               {
                  if (hasSubTasks())
                     for (int i = 0; i < SubTasks.getSize(); i++)
                     {
                        Task subTask = SubTasks.getTask(i);
                        if (subTask.IsSubForm)
                        {
                           ((MgForm)subTask.getForm()).IgnoreFirstRecordCycle = true;
                           subTask.doFirstRecordCycle();
                        }
                     }
                  ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_REC_SUFFIX);
               }
            }
            SubformExecMode = SubformExecModeEnum.SET_FOCUS;
            ClientManager.Instance.EventsManager.popNewExecStacks();
         }
      }

      /// <summary>
      ///   returns true if this task is a sub form by its mgd definition (used only while initiating task for setting isSubform)
      /// </summary>
      private void CheckAndSetSubForm()
      {
         IsSubForm = (!isMainProg() && (this != _mgData.getFirstTask()));
      }

      /// <summary>
      ///   returns true if this task is a sub form under frame set (not pure sub form)
      /// </summary>
      internal bool isSubFormUnderFrameSet()
      {
         bool result = IsSubForm && _parentTask.getForm().IsFrameSet;
         return result;
      }

      /// <summary>
      ///   moves the cursor to the first parkable control in the task
      /// </summary>
      /// <param name = "formFocus">if true and if no control found - focus on form</param>
      internal void moveToFirstCtrl(bool formFocus)
      {
         ((MgForm)Form).moveToFirstCtrl(false, formFocus);
      }

      /// <summary>
      ///   stop the execution of a program
      /// </summary>
      internal void stop()
      {
         Task task;
         MgFormBase form = getForm();

         if (_aborting)
            return;

         if (hasSubTasks())
         {
            while ((task = SubTasks.getTask(0)) != null)
            {
               task.setDestinationSubform(_destinationSubform);
               task.stop();
            }
         }

         if (!_isMainPrg)
            _aborting = true;

         if (form != null)
            form.SaveUserState();

         // if this task and its parent are from different MGDATA objects then disconnect
         // them by removing the reference to this task from its parent
         if (_parentTask != null)
         {
            ((Task)_parentTask).removeSubTask(this);
            if (IsSubForm)
            {
               List<Int32> oldTimers = new List<Int32>(), newTimers = new List<Int32>();
               var mgd = getMGData();

               if (mgd.getTimerHandlers() != null)
                  oldTimers = mgd.getTimerHandlers().getTimersVector();
               mgd.removeTimerHandler(this);
               mgd.removeExpressionHandler(this);
               if (mgd.getTimerHandlers() != null)
                  newTimers = mgd.getTimerHandlers().getTimersVector();
               mgd.changeTimers(oldTimers, newTimers);

               mgd.removeTask(this);
               GUIManager.Instance.abort((MgForm)getForm());
            }
         }

         AbortTransaction();
         locateQuery.FreeTimer();

         // If the task got closed due to a click on same task, then set CurrentClickedCtrl to null
         if (getClickedControl() != null)
            ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl = null;
         if (Form != null)
            ((MgForm)Form).removeRefsToCtrls();

         if (IsSubForm)
         {
            var subForm = (MgControl)Form.getSubFormCtrl();

            subForm.setSubformTaskId(null);
            subForm.initSubformTask();
            ((MgForm)Form).removeFromParentsTabbingOrder();
            if (ClientManager.Instance.getLastFocusedTask() == this)
               ClientManager.Instance.setLastFocusedTask(_parentTask);

            // QCR #310113. If the subform task for was not loaded yet, do not refresh it.
            if (subForm.SubformLoaded)
               subForm.getProp(PropInterface.PROP_TYPE_VISIBLE).RefreshDisplay(true);
         }

         TaskService.UpdateArguments(this, ArgumentsList);
         TaskService.UpdateReturnValue(this, _returnValueField);

         // remove dotnet objects
         removeDotNetObjects();
          
         IClientCommand clearDataViewCommand = CommandFactory.CreateDataViewCommand(getTaskTag(), DataViewCommandType.Clear);
         DataviewManager.Execute(clearDataViewCommand);

      }

      /// <summary>
      /// abort the MgData to which the task belongs
      /// </summary>
      internal void abort()
      {
         getMGData().abort();
      }

      /// <summary>
      ///   returns true if the task has sub-tasks
      /// </summary>
      internal bool hasSubTasks()
      {
         if (SubTasks != null && SubTasks.getSize() > 0)
            return true;
         return false;
      }

      /// <summary>
      ///   A number, 1-24, the current task's depth in the task execution hierarchy.
      /// </summary>
      /// <param name = "byParentOrder">if true, we travel the parents, otherwise we travel the triggering tasks</param>
      /// <returns> depth of the task, 1 if called from the root task</returns>
      internal int getTaskDepth(bool byParentOrder)
      {
         if (byParentOrder)
            return getTaskDepth_(byParentOrder);
         int depth = ((Task)GetContextTask()).getTaskDepth_(byParentOrder);

         // QCR 366748 if we are in a component handler add it to the path
         if (isMainProg() && getCtlIdx() != ConstInterface.TOP_LEVEL_CONTAINER)
            depth++;

         return depth;
      }

      /// <summary>
      ///   helper function which implements the getTaskDepth recursion.
      /// </summary>
      /// <param name = "byParentOrder">if true, we travel the parents, otherwise we travel the triggering tasks</param>
      private int getTaskDepth_(bool byParentOrder)
      {
         Task tmpTask = getTriggeringTask();
         if (tmpTask == null || byParentOrder)
            if (_parentTask == null)
               return 1;
            else
            {
               /* if the ctlIdx of the current task and the parent are different,   */
               /* that is the current task is from a component, add the component's */
               /* Main Prg as well. */
               if (byParentOrder && getCtlIdx() != _parentTask.getCtlIdx())
                  return 2 + ((Task)_parentTask).getTaskDepth_(byParentOrder);
               return 1 + ((Task)_parentTask).getTaskDepth_(byParentOrder);
            }
         return 1 + (tmpTask).getTaskDepth_(byParentOrder);
      }

      /// <summary>
      ///   get the ancestor of the Task
      /// </summary>
      /// <param name = "generation">of the ancestor task</param>
      public override ITask GetTaskAncestor(int generation)
      {
         return getTaskAncestor_(generation);
      }

      /// <summary>
      ///   
      /// </summary>
      private ITask getTaskAncestor_(int generation)
      {
         ITask retTask = null;

         if (generation == 0)
            retTask = this;
         else
         {
            Task tmpTask = getTriggeringTask();
            if (tmpTask == null)
               tmpTask = (Task)_parentTask;
            if (tmpTask != null)
               retTask = tmpTask.getTaskAncestor_(--generation);
         }

         return retTask;
      }

      /// <summary>
      /// Returns task depth
      /// </summary>
      /// <returns></returns>
      public override int GetTaskDepth()
      {
         return (getTaskDepth(false));
      }

      /// <param name="task"></param>
      /// <returns>true iff the path of the current task contains the task passed as parameter</returns>
      internal bool pathContains(Task task)
      {
         return (_taskPath.Contains(task));
      }

      /// <summary>
      ///   sets the context task of this task
      /// </summary>
      /// <param name = "context">the new context task of the current task</param>
      internal void SetContextTask(Task context)
      {
         ContextTask = context;
      }

      /// <summary>
      ///   returns the task which is the context of the currently executing handler.
      /// </summary>
      public override ITask GetContextTask()
      {
         var currRec = ((DataView)DataView).getCurrRec();

         if (InStartProcess
             || currRec != null && (currRec.InCompute || currRec.InRecompute)
             || Form != null && Form.inRefreshDisplay())
            return this;

         return ContextTask;
      }

      /// <summary>
      ///   get a reference to the task that triggered the handler which created me
      /// </summary>
      internal Task getTriggeringTask()
      {
         // This is a semi patch which fixes problems such as the ones in QCR #296428
         // When a modal task gets opened, it might set its triggering task to a task
         // whose 'abort' command arrived and is pending for execution. Under these
         // circumstances, we must check every access to triggeringTask and make sure
         // it points to a valid task.
         if (_triggeringTask != null && _triggeringTask.getMGData().IsAborting)
            _triggeringTask = (Task)_parentTask;

         return _triggeringTask;
      }

      /// <summary>
      ///   set the last parked control in this task
      /// </summary>
      /// <param name = "ctrl">last focused control</param>
      public override void setLastParkedCtrl(MgControlBase ctrl)
      {
         base.setLastParkedCtrl(ctrl);

         GUIManager.setLastFocusedControl(this, ctrl);
      }

      /// <summary>
      ///   Query Task Path Returns the task path that leads to the current task. The resulting alpha string
      ///   contains the task names, as well as the current name of the task. Names are separated by the ';'
      ///   character. For PROG() using.
      /// </summary>
      internal StringBuilder queryTaskPath()
      {
         int insertedNames = 0; // how mach names inserted to the output string
         var result = new StringBuilder(7936); /* 31 * 256 */
         var treeRoute = new Task[getTaskDepth(false)];
         pathToRoot(treeRoute, false);
         for (int tsk = treeRoute.Length - 1; tsk >= 0; tsk--)
         {
            Task currTsk = treeRoute[tsk];
            if (currTsk.isMainProg())
               continue;
            Property nameProp = currTsk.getProp(PropInterface.PROP_TYPE_NAME);
            if (nameProp == null)
               continue;
            string name = nameProp.getValue();
            if (!string.IsNullOrEmpty(name))
            {
               // if there is only 1 name -> don't insert ';'
               // after last name don't insert ';' too.
               if (insertedNames++ != 0)
                  result.Append(';');
               result.Append(name);
            }
         }
         return result;
      }

      /// <summary>
      ///   Get Field which contain item number itm
      /// </summary>
      internal Field ctl_itm_2_parent_vee(int itm)
      {
         Task currTsk;
         int currItm = 0;
         int lastItm = 0; // start of the field in fieldeTab number for checked task
         // save the route to the root in the treeRoute array
         var treeRoute = new Task[getTaskDepth(true)];
         // the route up, the insert (our task) is [0], and exit (root) is [treeRoute.length-1]
         pathToRoot(treeRoute, true);

         for (int tsk = treeRoute.Length - 1; tsk >= 0; tsk--)
         {
            currTsk = treeRoute[tsk];

            int sizeOfFieldTab = currTsk.DataView.GetFieldsTab().getSize();

            currItm += sizeOfFieldTab;
            // found task which contains the needed field
            if (itm < currItm)
            {
               // number of Field in found task
               itm -= lastItm;
               return (Field)currTsk.DataView.getField(itm);
            }
            lastItm = currItm;
         }
         // not found the field
         return null;
      }

      /// <summary>
      /// Get the control id which contain field attached to control
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      internal int GetControlIDFromVarItem(int item)
      {
         Task currTask;
         int currItem = 0;
         int lastItem = 0; // start of the field in fieldeTab number for checked task
         int noOfControlsBeforeCurrTask = 0;

         // save the route to the root in the treeRoute array
         var treeRoute = new Task[getTaskDepth(true)];
         // the route up, the insert (our task) is [0], and exit (root) is [treeRoute.length - 1]
         pathToRoot(treeRoute, true);

         for (int task = treeRoute.Length - 1; task >= 0; task--)
         {
            currTask = treeRoute[task];
            currItem += currTask.DataView.GetFieldsTab().getSize();

            // Found the task which contains the needed control & field
            if (item < currItem)
            {
               // Number of fields in found task
               item -= lastItem;

               Field field = (Field)currTask.DataView.getField(item);
               if (field.getCtrl() != null)
                  return noOfControlsBeforeCurrTask = noOfControlsBeforeCurrTask + currTask.Form.getControlIdx(field.getCtrl()) + 1;
               return 0;
            }

            if (currTask.Form != null)
               noOfControlsBeforeCurrTask += currTask.Form.getCtrlCount();

            lastItem = currItem;
         }
      
         //Control attached to the variable in same task not found 
         return 0;
      }

      /// <summary>
      /// Get the control from controlID
      /// </summary>
      /// <param name="controlID"></param>
      /// <returns></returns>
      internal MgControl GetControlFromControlID(int controlID, out int parent)
      {
         MgControl control = null;
         parent = -1;
         if (controlID >= 0)
         {
            Task currTask = null;
            Task callerTask = null;
            int  noOfControlsInCurrtask = 0;
            int  noOfControlsBeforeCurrTask = 0;
            int  controlIdx = 0;
            
            // save the route to the root in the treeRoute array
            var treeRoute = new Task[getTaskDepth(true)];
            // the route up, the insert (our task) is [0], and exit (root) is [treeRoute.length - 1]
            pathToRoot(treeRoute, true);
            
            for (int task = treeRoute.Length - 1; task >= 0; task--)
            {
               currTask = treeRoute[task];

               if (currTask.Form != null)
               {
                  noOfControlsInCurrtask = currTask.Form.getCtrlCount();

                  // Found the task which contains the needed control
                  if (controlID < (noOfControlsBeforeCurrTask + noOfControlsInCurrtask))
                  {
                     // Idx of the control in current task
                     controlIdx = controlID - noOfControlsBeforeCurrTask;
                     control = (MgControl)currTask.Form.CtrlTab.getCtrl(controlIdx);
                     callerTask = currTask;
                     break;
                  }

                  noOfControlsBeforeCurrTask += noOfControlsInCurrtask;
               }
            }

            //Get parent
            if (control != null)
            {
               parent = 0;
               int prevCtlIdx = treeRoute[0].TaskDefinitionId.CtlIndex;
               
               for (int task = 0; task < treeRoute.Length - 1; task++)
               {
                  currTask = treeRoute[task];

                  if (prevCtlIdx != currTask.TaskDefinitionId.CtlIndex)
                     parent++;

                  if (callerTask.TaskDefinitionId.CtlIndex == currTask.TaskDefinitionId.CtlIndex)
                     break;

                  prevCtlIdx = currTask.TaskDefinitionId.CtlIndex;
               }
            }
         }

         //Control corresponding to the specified ControlID
         return control;
      }


      /// <summary>
      /// </summary>
      internal int ctl_itm_4_parent_vee(int parent, int vee)
      {
         Task tsk;
         int depth = getTaskDepth(true);

         if (parent != MAIN_PRG_PARENT_ID && depth <= parent)
            return vee;

         var treeRoute = new Task[depth];

         if (vee != 0)
         {
            pathToRoot(treeRoute, true);

            // get the index of parent in the task tree
            int indOfParentInTaskTree = getIndOfParentInTaskTree(parent, treeRoute);

            int i;
            for (i = treeRoute.Length - 1; i > indOfParentInTaskTree; i--)
            {
               tsk = treeRoute[i];
               vee += tsk.DataView.GetFieldsTab().getSize();
            }
         }
         return vee;
      }

      /// <summary>
      /// </summary>
      /// <param name = "parent"></param>
      /// <param name = "taskTree"></param>
      /// <returns></returns>
      protected internal int getIndOfParentInTaskTree(int parent, Task[] taskTree)
      {
         int indOfParentInTaskTree = 0;

         if (parent == MAIN_PRG_PARENT_ID)
         {
            for (int i = 1; i < taskTree.Length; i++)
            {
               if (taskTree[i].isMainProg() && _ctlIdx == taskTree[i].getCtlIdx())
               {
                  indOfParentInTaskTree = i;
                  break;
               }
            }
         }
         else
            indOfParentInTaskTree = parent;

         return indOfParentInTaskTree;
      }

      /// <summary>
      ///   entry point to the recursion induced by getFieldByName_.
      /// </summary>
      /// <returns> a field which belongs to me or to my parents with the matching name</returns>
      internal Field getFieldByName(String fldName)
      {
         return ((Task)GetContextTask()).getFieldByName_(fldName);
      }

      /// <summary>
      ///   find field with the name in task or its ancestors
      /// </summary>
      /// <param name = "fldName">name, the name can be empty string too</param>
      /// <returns> If the variable name is not found then the function will return NULL.</returns>
      private Field getFieldByName_(String fldName)
      {
         Task tmpTask;

         if (fldName == null)
            return null;
         Field fld = getField(fldName);

         // if the field is not ours - recursive check the ancestors
         if (fld == null)
         {
            tmpTask = getTriggeringTask() ?? (Task)_parentTask;

            if (tmpTask != null)
            {
               if (!isMainProg() && getCtlIdx() != tmpTask.getCtlIdx())
               {
                  Task mainPrg = MGDataCollection.Instance.GetMainProgByCtlIdx(getCtlIdx());
                  fld = mainPrg.getFieldByName_(fldName);
               }

               if (fld == null)
                  fld = tmpTask.getFieldByName_(fldName);
            }
         }
         return fld;
      }

      /// <summary>
      ///   get index of the field in the task or its ancestors
      /// </summary>
      /// <param name = "fldName">name</param>
      /// <returns> If the variable name is not found then this function will return Zero.</returns>
      internal int getIndexOfFieldByName(String fldName)
      {
         int depth = getTaskDepth(true);
         int index = 0;
         Field fld = getFieldByName(fldName);
         FieldsTable fldTab;

         if (fld == null)
            return 0;

         var treeRoute = new Task[depth];
         pathToRoot(treeRoute, true);
         for (int curr = depth - 1; curr >= 0; curr--)
         {
            Task currTask = treeRoute[curr];
            if (currTask == fld.getTask())
            {
               curr = fld.getId();
               index += ++curr; // start index in Magic from 1 and not from 0
               break; // get out of the task tree - the index is found
            }
            fldTab = (FieldsTable)currTask.DataView.GetFieldsTab();
            index += fldTab.getSize();
         }

         return index;
      }

      /// <summary>
      ///   calculate the path from this task to the root of the tasks tree
      /// </summary>
      /// <param name = "path">array of tasks, the route up. The 0 item is start point task</param>
      /// <param name = "byParentOrder">if true, we travel the parents, otherwise we travel the triggering tasks</param>
      internal void pathToRoot(Task[] path, bool byParentOrder)
      {
         Task task = (byParentOrder
                         ? this
                         : (Task)GetContextTask());
         for (int i = 0; i < path.Length; i++)
         {
            path[i] = task;
            int ctlIdx = task.getCtlIdx();

            Task tmpTask = byParentOrder
                              ? null
                              : task.getTriggeringTask();
            if (tmpTask != null)
               task = tmpTask;
            else
            {
               task = (Task)task.getParent();

               if (task == null)
               {
                  // QCR 366748 if we are in a component global handler add it to the path
                  if (!byParentOrder && isMainProg() && getCtlIdx() != ConstInterface.TOP_LEVEL_CONTAINER)
                     path[i + 1] = this;
                  break;
               }

               if (byParentOrder && ctlIdx != task.getCtlIdx())
                  path[++i] = MGDataCollection.Instance.GetMainProgByCtlIdx(ctlIdx);
            }
         }
      }

      /// <summary>
      ///   check if this task should be refreshed - compare the "refresh on" fields of the current record to the
      ///   previous record and if different values are found returns true
      /// </summary>
      private bool shouldBeRefreshed()
      {
         var parentDv = ((DataView)_parentTask.DataView);
         bool result = false;

         //prevCurrRec is null only if it is the first time or after Cancel.
         //After Cancel in the parent task Subform refresh must be done always
         if (_parentTask == null || parentDv.isPrevCurrRecNull() ||
             (RefreshOnVars != null && !parentDv.currEqualsPrev(RefreshOnVars)))
            result = true;
         return result;
      }

      /// <summary>
      ///   returns true if the subTasks (subforms) should be refreshed
      /// </summary>
      internal bool CheckRefreshSubTasks()
      {
         bool ret = false;

         // do not refresh subforms before the first record prefix. The server sent us their dataview!
         if (_enteredRecLevel)
         {
            List<Task> subforms = GetSubformsToRefresh();
            if (subforms.Count > 0)
               ret = true;
         }
         return ret;
      }

      /// <summary>
      /// Gets a list of subtasks
      /// </summary>
      /// <returns>a list of subtasks</returns>
      private List<Task> GetSubTasks()
      {
         List<Task> subTasks = new List<Task>();

         if (hasSubTasks())
         {
            for (int i = 0; i < SubTasks.getSize(); i++)
               subTasks.Add(SubTasks.getTask(i));
         }

         return subTasks;
      }

      /// <summary>
      /// Gets a list of those subforms that must be refreshed.
      /// </summary>
      /// <returns>list of subforms to refresh</returns>
      internal List<Task> GetSubformsToRefresh()
      {
         List<Task> subTasks = GetSubTasks();
         return subTasks.FindAll(subTask => ShouldRefreshSubformTask(subTask));
      }

      /// <summary>
      /// Refresh all subforms that should be refreshed
      /// </summary>
      internal void RefreshSubforms()
      {
         CleanDoSubformPrefixSuffix();
         List<Task> subformsToRefresh = GetSubformsToRefresh();
         foreach (Task subformTask in subformsToRefresh)
            SubformRefresh(subformTask, false);
      }

      /// <summary>
      /// Check if the specific subform must be refreshed
      /// </summary>
      /// <param name="subTask"></param>
      /// <returns></returns>
      internal bool ShouldRefreshSubformTask(Task subTask)
      {
         bool refresh = false;
         MgControl subformCtrl = (MgControl)subTask.getForm().getSubFormCtrl();

         subTask.DoSubformPrefixSuffix = false;
         // Defect 76979. Subform can execute refresh only after it has TaskView.
         if (subformCtrl != null && !subTask.InSelect && subTask.AfterFirstRecordPrefix)
         {
            char parentMode = getMode();
            int modeProperty = (parentMode == Constants.TASK_MODE_QUERY ?
                                               PropInterface.PROP_TYPE_ALLOW_QUERY :
                                parentMode == Constants.TASK_MODE_MODIFY ?
                                               PropInterface.PROP_TYPE_ALLOW_MODIFY :
                                               PropInterface.PROP_TYPE_ALLOW_CREATE);
            // For a subform with 'AsParent' mode we also must refresh the subform 
            // after the parent record prefix. If the specific mode is not allowed 
            // the subform continues in the old mode.
            if (subTask.ModeAsParent && parentMode != subTask.getMode() &&
                getLevel() == Constants.TASK_LEVEL_RECORD &&
                subTask.getProp(PropInterface.PROP_TYPE_ALLOW_OPTION).getValueBoolean() &&
                subTask.checkProp(modeProperty, true))
            {
               if (!subformCtrl.isVisible() &&
                   !subformCtrl.checkProp(PropInterface.PROP_TYPE_REFRESH_WHEN_HIDDEN, false))
               {
                  subformCtrl.RefreshOnVisible = true;
                  refresh = false;
               }
               else
                  refresh = true;
               subTask.SetModeAsParent(parentMode);
            }
            else
            {
               refresh = subformCtrl.checkProp(PropInterface.PROP_TYPE_AUTO_REFRESH, true) &&
                              subTask.shouldBeRefreshed();
               if (refresh)
               {
                  if (!subformCtrl.isVisible() &&
                      !subformCtrl.checkProp(PropInterface.PROP_TYPE_REFRESH_WHEN_HIDDEN, false))
                  {
                     subformCtrl.RefreshOnVisible = true;
                     refresh = false;
                  }
               }
            }

            if (refresh)
               subTask.DoSubformPrefixSuffix = true;
         }
         return refresh;
      }
      /// <summary>
      /// Sets the task mode as parent mode
      /// </summary>
      /// <param name="parentMode"></param>
      internal void SetModeAsParent(char parentMode)
      {
         enableModes();
         if (parentMode == Constants.TASK_MODE_CREATE)
         {
            IClientCommand cmd = CommandFactory.CreateEventCommand(getTaskTag(), InternalInterface.MG_ACT_RTO_CREATE);
            DataviewManager.Execute(cmd);
         }
         else if (getMode() == Constants.TASK_MODE_CREATE)
         {
            setPreventRecordSuffix(true);
            setPreventControlChange(true);
            ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_RT_REFRESH_VIEW);
            setPreventRecordSuffix(false);
            setPreventControlChange(false);
            DataView subTaskDv = (DataView)DataView;
            if (!subTaskDv.isEmpty() && ((Record)subTaskDv.getCurrRec()).getMode() != DataModificationTypes.Insert)
               setMode(parentMode);
            else
               setMode(Constants.TASK_MODE_CREATE);
            ((MgForm)getForm()).RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
         }
         else
            setMode(parentMode);

         setOriginalTaskMode(parentMode);
         setCreateDeleteActsEnableState();
      }

      // after compute do subform task record prefix & suffix
      internal void doSubformRecPrefixSuffix()
      {
         List<Task> subTasks = GetSubTasks();
         foreach (Task subTask in subTasks)
         {
            if (subTask.DoSubformPrefixSuffix)
            {
               ClientManager.Instance.EventsManager.handleInternalEvent(subTask, InternalInterface.MG_ACT_REC_PREFIX, true);
               if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                  ClientManager.Instance.EventsManager.handleInternalEvent(subTask, InternalInterface.MG_ACT_REC_SUFFIX);
            }
         }
      }

      /// <summary> Clean flag for Subform record cycle </summary>
      internal void CleanDoSubformPrefixSuffix()
      {
         List<Task> subTasks = GetSubTasks();
         foreach (Task subTask in subTasks)
            subTask.DoSubformPrefixSuffix = false;
      }
      /// <summary>
      ///   returns true if the task must display confirmation window prior to deleting a record.
      /// </summary>
      internal bool mustConfirmInDeleteMode()
      {
         Property prop = getProp(PropInterface.PROP_TYPE_CONFIRM_UPDATE);

         // As strange as it sounds, if the property is an expression which evaluates to
         // False, then no need to display confirmation...
         return !prop.isExpression();
      }

      /// <summary>
      ///   add a sub-task
      /// </summary>
      /// <param name = "subTask">the sub task to add</param>
      protected internal void addSubTask(Task subTask)
      {
         if (SubTasks == null)
            SubTasks = new TasksTable();
         SubTasks.addTask(subTask);
      }

      /// <summary>
      ///   remove one of this tasks sub-task
      /// </summary>
      /// <param name = "subTask">a reference to the sub-task to remove</param>
      protected internal void removeSubTask(Task subTask)
      {
         if (SubTasks != null)
            SubTasks.removeTask(subTask);
      }

      /// <summary>
      ///   sets the value of the dynamicDef.transaction failed flag
      /// </summary>
      /// <param name = "val">the new value of the flag</param>
      internal void setTransactionFailed(bool val)
      {
         _transactionFailed = val;

         if (val == false)
            TransactionErrorHandlingsRetry = null;
      }

      /// <summary>
      ///   returns the value of the "dynamicDef.transaction failed" flag
      /// </summary>
      /// <param name = "level">is the dynamicDef.transaction level we are interested in checking. TRANS_NONE --> check for any
      ///   dynamicDef.transaction level failure TRANS_RECORD/TASK_PREFIX --> check for a failure in that specific
      ///   level</param>
      internal bool transactionFailed(char level)
      {
         if (level == ConstInterface.TRANS_NONE ||
             (Transaction != null && Transaction.getLevel() == level))
            return _transactionFailed;
         else
            if (level == ConstInterface.TRANS_NONE ||
                (TransactionErrorHandlingsRetry != null && TransactionErrorHandlingsRetry.getLevel() == level))
               return _transactionFailed;
         return false;
      }

      /// <summary>
      ///   set the flag of a dynamicDef.transaction retry on the task's dynamicDef.transaction
      /// </summary>
      /// <param name = "val">value of flag</param>
      internal void setAfterRetry(char val)
      {
         if (Transaction != null)
            Transaction.setAfterRetry(val);
         else if (TransactionErrorHandlingsRetry != null)
         {
            if (val == ConstInterface.RECOVERY_NONE)
               TransactionErrorHandlingsRetry = null;
            else
               TransactionErrorHandlingsRetry.setAfterRetry(val);
         }

      }

      /// <returns> true if this task is part of a dynamicDef.transaction currently being retried</returns>
      internal bool getAfterRetry()
      {
         if (Transaction != null)
            return Transaction.getAfterRetry();
         else if (TransactionErrorHandlingsRetry != null)
            return TransactionErrorHandlingsRetry.getAfterRetry();
         return false;
      }

      /// <returns> true if this task is part of a dynamicDef.transaction currently being retried with a specific recovery</returns>
      internal bool getAfterRetry(char recovery)
      {
         if (Transaction != null)
            return Transaction.getAfterRetry(recovery);
         else if (TransactionErrorHandlingsRetry != null)
            return TransactionErrorHandlingsRetry.getAfterRetry(recovery);
         return false;
      }

      /// <summary>
      ///   get the ExecEndTask flag
      /// </summary>
      /// <returns> the _bExecEndTask value</returns>
      internal bool getExecEndTask()
      {
         return _bExecEndTask;
      }

      /// <summary>
      ///   reset the "execute end task" flag
      /// </summary>
      internal void resetExecEndTask()
      {
         _bExecEndTask = false;
      }

      /// <summary>
      ///   reset the "execute end task" flag
      /// </summary>
      internal void setExecEndTask()
      {
         _bExecEndTask = true;
      }

      /// <summary>
      ///   Evaluate the end condition property
      /// </summary>
      /// <param name = "mode">the mode to compare to</param>
      /// <returns> should we evaluate the end condition?</returns>
      internal bool evalEndCond(String mode)
      {
         Property endTaskCondProp;
         Property evalEndCondProp = getProp(PropInterface.PROP_TYPE_EVAL_END_CONDITION);

         // Dont evaluate end condition if we are already closing the task
         if (InEndTask)
            return false;

         if (_bExecEndTask)
            return true;
         if (evalEndCondProp != null &&
             (evalEndCondProp.getValue().Equals(ConstInterface.END_COND_EVAL_IMMIDIATE,
                                                StringComparison.InvariantCultureIgnoreCase) ||
              evalEndCondProp.getValue().Equals(mode, StringComparison.InvariantCultureIgnoreCase)))
         {
            // check the end condition
            endTaskCondProp = getProp(PropInterface.PROP_TYPE_END_CONDITION);
            if (endTaskCondProp == null)
               return false;
            string endTask = endTaskCondProp.getValue();
            if (DisplayConvertor.toBoolean(endTask))
            // signal we need to perform an end task after we get out of the loop
            {
               _bExecEndTask = true;
               return true;
            }
            return false;
         }
         return false;
      }

      /// <summary>
      ///   set the "trying to stop" flag for the task and it's subTasks
      /// </summary>
      /// <param name = "val">the value to use for the flag</param>
      internal void setTryingToStop(bool val)
      {
         if (hasSubTasks())
         {
            for (int i = 0; i < SubTasks.getSize(); i++)
            {
               Task subTask = SubTasks.getTask(i);
               subTask.setTryingToStop(val);
            }
         }
         IsTryingToStop = val;
      }

      /// <summary>
      ///   get the bInRecordSuffix
      /// </summary>
      internal bool getInRecordSuffix()
      {
         return _inRecordSuffix;
      }

      /// <summary>
      ///   set the bInRecordSuffix
      /// </summary>
      internal bool setInRecordSuffix(bool bValue)
      {
         if (bValue != _inRecordSuffix)
         {
            _inRecordSuffix = bValue;
            return true;
         }
         return false;
      }

      /// <summary>
      ///   set the bEvalOldValues flag
      /// </summary>
      /// <param name = "bFlag">true/false value</param>
      /// <returns> true/flase - value updated</returns>
      internal bool setEvalOldValues(bool bFlag)
      {
         if (bFlag != _evalOldValues)
         {
            _evalOldValues = bFlag;
            return true;
         }
         return false;
      }

      /// <summary>
      ///   get the bEvalOldValues value
      /// </summary>
      /// <returns> true/false - look at old values when getting the field value</returns>
      internal bool getEvalOldValues()
      {
         return _evalOldValues;
      }

      /// <summary>
      ///   returns true if succeeded in ending the task and its sub tasks
      /// </summary>
      /// <param name = "reversibleExit">if true then the exit is reversible</param>
      /// <param name = "onlyDescs">if true then end only the sub tasks and handle rec suffix for this task</param>
      /// <param name = "dueToVerifyError"></param>
      internal bool endTask(bool reversibleExit, bool onlyDescs, bool dueToVerifyError)
      {
         return endTask(reversibleExit, onlyDescs, false, dueToVerifyError);
      }

      /// <summary>
      ///   returns true if succeeded in ending the task and its sub tasks
      /// </summary>
      /// <param name = "reversibleExit">if true then the exit is reversible</param>
      /// <param name = "onlyDescs">if true then end only the sub tasks and handle rec suffix for this task</param>
      /// <param name = "subformDestination">if true then end only this task</param>
      protected internal bool endTask(bool reversibleExit, bool onlyDescs, bool subformDestination, bool dueToVerifyError)
      {
         bool succeeded = true;
         bool closedSubForm = false;
         Task subTask;
         MGDataCollection mgdTab = MGDataCollection.Instance;

         if ((IsSubForm && !subformDestination) && !((Task)_parentTask).InEndTask)
         {
            // if this task is a subform and its parent is not trying to stop then
            // start stopping the tasks sub tree from the parent form
            succeeded = ((Task)_parentTask).endTask(reversibleExit, onlyDescs, dueToVerifyError);
         }
         else if (!isAborting() && !InEndTask)
         {
            try
            {
               InEndTask = true;

               if (!IsTryingToStop)
                  setTryingToStop(true);
               // exit from subTasks in a prefix manner
               int i;
               if (reversibleExit)
               {
                  // exit from the sub tasks
                  if (hasSubTasks())
                  {
                     for (i = 0; i < SubTasks.getSize() && succeeded; i++)
                     {
                        subTask = SubTasks.getTask(i);
                        closedSubForm = closedSubForm || subTask.IsSubForm;
                        ClientManager.Instance.EventsManager.handleInternalEvent(subTask, InternalInterface.MG_ACT_EXIT);
                        succeeded = (!ClientManager.Instance.EventsManager.GetStopExecutionFlag() || dueToVerifyError);
                     }
                  }

                  // exit for the first task must end all tasks
                  if (this == mgdTab.StartupMgData.getFirstTask())
                  {
                     for (i = mgdTab.getSize() - 1; i > 0 && succeeded; i--)
                     {
                        if (mgdTab.getMGData(i) != null)
                        {
                           ClientManager.Instance.EventsManager.handleInternalEvent(mgdTab.getMGData(i).getFirstTask(),
                                                                           InternalInterface.MG_ACT_EXIT);
                           succeeded = (!ClientManager.Instance.EventsManager.GetStopExecutionFlag() || dueToVerifyError);
                        }
                     }
                  }
               }

               if (succeeded)
               {
                  // if closing the sub tasks was successful then execute record suffix
                  if (reversibleExit && !dueToVerifyError && !HasMDIFrame)
                  {
                     bool forceSuffix = checkProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, false);
                     // for non interactive, turn off the force record suffix, 
                     // otherwise an unwanted update might be sent when task ends.
                     if (!IsInteractive && forceSuffix == true && getMode() == Constants.TASK_MODE_CREATE)
                        setProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, "0");

                     ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_REC_SUFFIX);

                     // restore the force suffix, for just in case.
                     if (!IsInteractive && forceSuffix == true && getMode() == Constants.TASK_MODE_CREATE)
                        setProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, "1");
                  }

                  succeeded = (!ClientManager.Instance.EventsManager.GetStopExecutionFlag() || dueToVerifyError);

                  if (succeeded && !onlyDescs)
                     HandleTransactionInfo();

                  bool preTskSuffix = succeeded;

                  // now perform task suffix
                  if (succeeded && !onlyDescs && reversibleExit && !dueToVerifyError &&
                      (!IsSubForm || subformDestination) &&
                      !HasMDIFrame && TaskPrefixExecuted && !TaskSuffixExecuted)
                     succeeded = handleTaskSuffix(true);

                  // Don't forget to exit from any subTasks opened during the last rec-suffix or task suffix
                  if (reversibleExit && hasSubTasks())
                     for (i = 0; i < SubTasks.getSize(); i++)
                     {
                        subTask = SubTasks.getTask(i);

                        if (!subTask.IsSubForm)
                           ClientManager.Instance.EventsManager.handleNonReversibleEvent(subTask, InternalInterface.MG_ACT_EXIT);
                     }

                  // dont continue with task exit if a data error occured during task suffix (e.g. rollback())
                  if (reversibleExit)
                     succeeded = (!ClientManager.Instance.EventsManager.GetStopExecutionFlag() || dueToVerifyError);

                  // task suffix only stops execution for current task, which is being closed
                  if (preTskSuffix)
                     ClientManager.Instance.EventsManager.setStopExecution(false);

                  setAfterRetry(ConstInterface.RECOVERY_NONE);

                  if (!isAborting() && succeeded && !onlyDescs)
                  {
                     // Try closing any task level dynamicDef.transaction
                     if (reversibleExit)
                     {
                        setTransactionFailed(false);
                        TaskTransactionManager.checkAndCommit(false, ConstInterface.TRANS_TASK_PREFIX, false);
                        succeeded = (!ClientManager.Instance.EventsManager.GetStopExecutionFlag() || dueToVerifyError) && !isAborting() &&
                                     !transactionFailed(ConstInterface.TRANS_TASK_PREFIX);
                     }

                     setAfterRetry(ConstInterface.RECOVERY_NONE);

                     if (succeeded)
                     {
                        MGData baseMgd = MGDataCollection.Instance.StartupMgData;
                        // If we are closing the topmost task - dont loose any events, left in the queue
                        if (reversibleExit && baseMgd.getFirstTask().getTaskTag().Equals(getTaskTag()))
                        {
                           ClientManager.Instance.EventsManager.setProcessingTopMostEndTask(true);
                           ClientManager.Instance.EventsManager.handleEvents(baseMgd, 0);
                           ClientManager.Instance.EventsManager.setProcessingTopMostEndTask(false);

                           var st = new SessionStatisticsForm();
                           st.WriteToFlowMonitor((FlowMonitorQueue)_flowMonitor);

                           if (isAborting())
                              return true;
                        }

                        // updates dotnet object back to argument field
                        updateDNArg();

                        TaskService.Exit(this, reversibleExit, subformDestination);

                        // When closing a task whose parent is a main program, check the main program's end
                        // condition
                        if (_parentTask != null && _parentTask.isMainProg())
                        {
                           MGData mgd = MGDataCollection.Instance.getMGData(0);
                           Task mainPrgTask = mgd.getMainProg(0);
                           Task rootTask = MGDataCollection.Instance.StartupMgData.getFirstTask();
                           bool endMainPrg = mainPrgTask.checkProp(PropInterface.PROP_TYPE_END_CONDITION, false);

                           // No need to end the root task if we are already doing it.
                           if (this != rootTask && endMainPrg)
                           {
                              var rtEvt = new RunTimeEvent(rootTask);
                              rtEvt.setInternal(InternalInterface.MG_ACT_CLOSE);
                              rtEvt.setCtrl(null);
                              rtEvt.setArgList(null);
                              ClientManager.Instance.EventsManager.addToTail(rtEvt);
                           }
                        }
                        ClientManager.Instance.CreatedForms.remove((MgForm)Form);
                     }
                  }
               }
            }
            finally
            {
               if (!IsSubForm || subformDestination)
                  handleFinallyEndTask(succeeded, reversibleExit);
            }
         }



         // Qcr #914645 : return false in a recursive call.
         // otherwise commonHandlerAfter(eventManager) will not be executed if the call is recursively made from handleEvent.
         // for example : task cond fulfilled. handleInternalEvent(this, MG_ACT_REC_SUFFIX) is called.
         // while in rec suf handler, endTask is called again..if we return true then the commonHandlerAfter is not executed.
         // if the rec suffix handler did not exist, there will be no call to endTask and the commonHandlerAfter will be executed.
         // The name 'succeeded' is a bit misleading..its more like 'executed'..but we decided to keep it.
         else if (InEndTask)
            succeeded = false;

         // restore the flag.
         ClientManager.Instance.EventsManager.setProcessingTopMostEndTask(_inProcessingTopMostEndTaskSaved);

         return succeeded;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="onlyDescs"></param>
      /// <param name="succeeded"></param>
      private void HandleTransactionInfo()
      {
         // while remote task without local data, has subform that has local data : Remote transaction is set on the parent task and local Transaction is set on subform.
         // The member task.Transaction is get the DataviewManager.CurrentDataviewManager.Transaction
         // for subform :  the CurrentDataviewManager is the localDataview so the transaction is the local transaction that the subform is the owner but not the owner of the remote transaction 
         // The method setTransCleared() need to be called only for remote transaction that is the owner task . (_transCleared is send to the server )

         // check the local transaction
         Transaction localTransaction = this.DataviewManager.LocalDataviewManager.Transaction;
         if (localTransaction != null && localTransaction.isOwner(this) && localTransaction.getLevel() == ConstInterface.TRANS_RECORD_PREFIX)
            this.DataviewManager.LocalDataviewManager.Transaction = null;

         // check the remote transaction
         Transaction remoteTransaction = this.DataviewManager.RemoteDataviewManager.Transaction;
         if (remoteTransaction != null && remoteTransaction.isOwner(this) && remoteTransaction.getLevel() == ConstInterface.TRANS_RECORD_PREFIX)
         {
            this.DataviewManager.RemoteDataviewManager.Transaction = null;
            ((DataView)DataView).setTransCleared();
         }

      }



      /// <summary>
      /// Creates exit command, adds it to commands for execution and execute it if it's needed
      /// </summary>
      /// <param name="reversibleExit"></param>
      /// <param name="subformDestination"></param>
      internal void Exit(bool reversibleExit, bool subformDestination)
      {
         //If we are closing the root task, set the ApplicationExecutionStage as Terminating.
         if (this == MGDataCollection.Instance.StartupMgData.getFirstTask())
            ClientManager.Instance.ApplicationExecutionStage = ApplicationExecutionStage.Terminating;

         // if record suffix was successful and this task is not a sub form
         // then end the task at the server side
         IClientCommand cmd = reversibleExit
                  ? (IClientCommand)CommandFactory.CreateBrowserEscEventCommand(getTaskTag(), getExitingByMenu(), subformDestination)
                  : (IClientCommand)CommandFactory.CreateNonReversibleExitCommand(getTaskTag(), subformDestination);

         _mgData.CmdsToServer.Add(cmd);
         if (!subformDestination)
         {
            try
            {
               CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
            }
            catch (ServerError)
            {
               //If the closing of Main Program didn't succeed on server, close it here --- ofcourse it 
               //includes the execution of TS as well.
               if (this.isMainProg())
               {
                  _mgData.CmdsToServer.Add(cmd);
                  LocalCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
               }
               else
                  throw;
            }
         }
      }

      /// <summary>
      ///   Recursive function for finally operation of endTask function
      /// </summary>
      /// <param name = "succeeded"></param>
      /// <param name = "reversibleExit">
      /// </param>
      private void handleFinallyEndTask(bool succeeded, bool reversibleExit)
      {
         Task subTask;

         if (hasSubTasks())
         {
            for (int i = 0; i < SubTasks.getSize(); i++)
            {
               subTask = SubTasks.getTask(i);
               subTask.handleFinallyEndTask(succeeded, reversibleExit);
            }
         }

         InEndTask = false;

         if (succeeded)
            _isStarted = false;

         if (!succeeded && reversibleExit && IsTryingToStop)
            setTryingToStop(false);
      }

      /// <summary>
      ///   Recursive function for Task Prefix execution for all subforms
      /// </summary>
      /// <returns></returns>
      internal bool handleTaskSuffix(bool withSubTasks)
      {
         bool succeeded = true;
         Task subTask;

         if (withSubTasks)
         {
            if (hasSubTasks())
            {
               for (int i = 0; i < SubTasks.getSize() && succeeded; i++)
               {
                  subTask = SubTasks.getTask(i);
                  succeeded = subTask.handleTaskSuffix(withSubTasks);
               }
            }
         }

         if (succeeded)
            ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_TASK_SUFFIX);
         succeeded = !ClientManager.Instance.EventsManager.GetStopExecutionFlag();
         TaskSuffixExecuted = true;
         return succeeded;
      }

      /// <summary>
      ///   set the flow mode, the function checks if it is allowed to change the mode
      /// </summary>
      /// <param name = "aFlowMode">the new flow mode</param>
      /// <returns> true if the flow mode was changed</returns>
      internal bool setFlowMode(Flow aFlowMode)
      {
         if (aFlowMode == Flow.NONE || _flowMode == Flow.NONE)
         {
            _flowMode = aFlowMode;
            return true;
         }
         return false;
      }

      /// <summary>
      ///   set the direction, the function checks if it is allowed to change the direction
      /// </summary>
      /// <param name = "aDirection">the new direction</param>
      /// <returns> true if the direction was changed successfully</returns>
      internal bool setDirection(Direction aDirection)
      {
         if (aDirection == Direction.NONE || _direction == Direction.NONE)
         {
            _direction = aDirection;
            return true;
         }
         return false;
      }

      /// <summary>
      ///   Compares the current flow attributes of the task to a given flow mode and returns true if they are
      ///   compatible.
      /// </summary>
      /// <param name = "modeStr">N=next,step; F=fast,forward; P=prev,step; R=reverse,fast; S=select exit; C=cancel exit</param>
      internal bool checkFlowMode(String modeStr)
      {
         bool bRc = false;
         int i;

         for (i = 0; i < modeStr.Length && !bRc; i++)
         {
            char aMode = modeStr[i];

            // Server had many more FlowModeDir. However, only four is supported at client
            switch (aMode)
            {
               case (char)FlowModeDir.STEP_FORWARD:
                  bRc = _direction == Direction.FORE && _flowMode == Flow.STEP;
                  break;


               case (char)FlowModeDir.FAST_FORWARD:
                  bRc = _direction == Direction.FORE && _flowMode == Flow.FAST;
                  break;


               case (char)FlowModeDir.STEP_BACKWARD:
                  bRc = _direction == Direction.BACK && _flowMode == Flow.STEP;
                  break;


               case (char)FlowModeDir.FAST_BACKWARD:
                  bRc = _direction == Direction.BACK && _flowMode == Flow.FAST;
                  break;
            }
         }
         return bRc;
      }

      /// <summary>
      ///   set if the task into create line processing or not
      /// </summary>
      internal void setInCreateLine(bool inCreateLine)
      {
         _inCreateLine = inCreateLine;
      }

      /// <summary>
      ///   get if the task is into Create line processing
      /// </summary>
      internal bool getInCreateLine()
      {
         return _inCreateLine;
      }

      /// <summary>
      ///   get tasks flow mode
      /// </summary>
      internal Flow getFlowMode()
      {
         return _flowMode;
      }

      /// <summary>
      ///   get tasks direction
      /// </summary>
      internal Direction getDirection()
      {
         return _direction;
      }

      /// <summary>
      ///   handle an internal event on this tasks slave tasks. Slave tasks are: (1) If this task is a dynamicDef.transaction
      ///   owner - all tasks participating in the transactions are its slaves. (2) If this task is not an owner -
      ///   all its subforms are considered slaves. if one of the sub tasks failed handling the event then a
      ///   reference to that task is returned otherwise the returned value is null
      /// </summary>
      /// <param name = "internalEvtCode">the code of the internal event to handle</param>
      internal Task handleEventOnSlaveTasks(int internalEvtCode)
      {
         int i;
         Task task = null;
         Task failureTask = null;
         MGDataCollection mgdTab = MGDataCollection.Instance;
         List<Task> triggeredTasks = mgdTab.getTriggeredTasks(this);
         List<Task> orphans = null;
         bool sameTrans;
         bool hasSlaves = false;
         bool isOwner = Transaction != null && Transaction.isOwner(this);

         // event on trans-owner should be propogated to all other tasks in the dynamicDef.transaction.
         if (isOwner)
         {
            ClientManager.Instance.EventsManager.setEventScopeTrans();
            orphans = new List<Task>();

            // In case a change of trans-ownership previously took place, there might be tasks
            // which belong to the current dynamicDef.transaction but their parent is gone. These are orphan tasks
            // and we need to propogate the event to them too.
            mgdTab.startTasksIteration();
            while ((task = mgdTab.getNextTask()) != null)
            {
               sameTrans = task.Transaction != null &&
                           Transaction.getTransId() == task.Transaction.getTransId();
               if (task != this && sameTrans && task.PreviouslyActiveTaskId != null &&
                   mgdTab.GetTaskByID(task.PreviouslyActiveTaskId) == null)
                  orphans.Add(task);
            }
         }

         bool scopeTrans = ClientManager.Instance.EventsManager.isEventScopeTrans();

         // Propagate the event according to the scope, on all tasks triggered by me
         for (i = 0; i < triggeredTasks.Count && !ClientManager.Instance.EventsManager.GetStopExecutionFlag(); i++)
         {
            task = triggeredTasks[i];
            sameTrans = Transaction != null && task.Transaction != null &&
                        Transaction.getTransId() == task.Transaction.getTransId();

            if (!task.IsSubForm && (scopeTrans && sameTrans) && task.isStarted())
            {
               // QCR 438530 Guarantee that before prefix compute will be executed
               if (task.getForm().FormRefreshed && internalEvtCode == InternalInterface.MG_ACT_REC_PREFIX &&
                   task.getLevel() == Constants.TASK_LEVEL_TASK)
                  ((DataView)task.DataView).currRecCompute(false);

               ClientManager.Instance.EventsManager.handleInternalEvent(task, internalEvtCode);
               hasSlaves = true;
            }
         }

         // Propagate the event on orphans too.
         for (i = 0; isOwner && i < orphans.Count && !ClientManager.Instance.EventsManager.GetStopExecutionFlag(); i++)
         {
            task = orphans[i];
            // QCR 438530 Guarantee that before prefix compute will be executed
            if (task.getForm().FormRefreshed && internalEvtCode == InternalInterface.MG_ACT_REC_PREFIX &&
                task.getLevel() == Constants.TASK_LEVEL_TASK)
               ((DataView)task.DataView).currRecCompute(false);
            ClientManager.Instance.EventsManager.handleInternalEvent(task, internalEvtCode);
            hasSlaves = true;
         }

         if (hasSlaves && ClientManager.Instance.EventsManager.GetStopExecutionFlag())
            failureTask = task;

         if (isOwner)
            ClientManager.Instance.EventsManager.clearEventScope();

         return failureTask;
      }

      /// <summary>
      ///   enable-disable actions for a record
      /// </summary>
      internal void enableRecordActions()
      {
         DataView dataview = (DataView)DataView;

         setCreateDeleteActsEnableState();
         if (dataview.IncludesFirst() || dataview.IsOneWayKey)
         {
            if (dataview.isEmptyDataview() || dataview.getCurrRecIdx() == 0)
            {
               ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVLINE, false);
               if (dataview.IsOneWayKey)
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_BEGTBL, true);
               else
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_BEGTBL, false);
               if (Form.isScreenMode())
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVPAGE, false);
            }
            else
            {
               ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVLINE, true);
               ActionManager.enable(InternalInterface.MG_ACT_TBL_BEGTBL, true);
               if (Form.isScreenMode())
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVPAGE, true);
            }

            if (!Form.isScreenMode())
               if (dataview.isEmptyDataview() || (dataview.getCurrRecIdx() == 0) ||
                   (dataview.getTopRecIdx() == 0))
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVPAGE, false);
               else
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVPAGE, true);
         }
         else
         {
            ActionManager.enable(InternalInterface.MG_ACT_TBL_BEGTBL, true);
            ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVPAGE, true);
            ActionManager.enable(InternalInterface.MG_ACT_TBL_PRVLINE, true);
         }

         if (dataview.IncludesLast())
         {
            if (((getMode() == Constants.TASK_MODE_QUERY) || dataview.isEmptyDataview() ||
                 ((getMode() == Constants.TASK_MODE_MODIFY) &&
                  (!ClientManager.Instance.getEnvironment().allowCreateInModifyMode(getCompIdx()) ||
                   !checkProp(PropInterface.PROP_TYPE_ALLOW_CREATE, true)))) &&
                (dataview.getCurrRecIdx() + 1) == dataview.getSize())
            {
               ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTLINE, false);
               ActionManager.enable(InternalInterface.MG_ACT_TBL_ENDTBL, false);
               if (Form.isScreenMode())
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTPAGE, false);
            }
            else
            {
               ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTLINE, true);
               ActionManager.enable(InternalInterface.MG_ACT_TBL_ENDTBL, true);
               if (Form.isScreenMode())
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTPAGE, true);
            }

            if (!Form.isScreenMode())
            {
               if (dataview.isEmptyDataview() ||
                   ((dataview.getSize() - dataview.getTopRecIdx()) <= Form.getRowsInPage()))
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTPAGE, false);
               else
                  ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTPAGE, true);
            }
         }
         else
         {
            ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTLINE, true);
            ActionManager.enable(InternalInterface.MG_ACT_TBL_NXTPAGE, true);
            ActionManager.enable(InternalInterface.MG_ACT_TBL_ENDTBL, true);
         }

         if (Form.getTreeCtrl() != null)
         {
            if (dataview.isEmptyDataview())
            {
               ActionManager.enableTreeActions(false);
               ActionManager.enableNavigationActions(false);
            }
            else
            {
               ActionManager.enableTreeActions(true);
               ActionManager.enableNavigationActions(true);
            }
         }

         if (dataview.isEmptyDataview())
            ActionManager.enable(InternalInterface.MG_ACT_CANCEL, false);
         else
            ActionManager.enable(InternalInterface.MG_ACT_CANCEL, true);
      }

      /// <summary>
      ///   returns true if the task is started
      /// </summary>
      public override bool isStarted()
      {
         return _isStarted;
      }

      /// <summary>
      ///   returns the sub tasks table
      /// </summary>
      internal TasksTable getSubTasks()
      {
         return SubTasks;
      }

      /// <summary>
      ///   returns true if this tasks refreshes on a change of one of its parent fields with the given id
      /// </summary>
      /// <param name = "fldId">the id of the parent field</param>
      protected internal bool refreshesOn(int fldId)
      {
         bool result = false;
         for (int i = 0; !result && RefreshOnVars != null && i < RefreshOnVars.Length; i++)
            result = (RefreshOnVars[i] == fldId);
         return result;
      }

      /// <summary>
      ///   clear this task from the active transactions account
      /// </summary>
      private void AbortTransaction()
      {
         // while task is the owner of the local transaction we need to abort the transaction
         TaskTransactionManager.CheckAndAbortLocalTransaction(this);

         // must free all references to allow the garbage collector free this MGData from memory
         //Transaction = null;
         DataviewManager.LocalDataviewManager.Transaction = null;
         DataviewManager.RemoteDataviewManager.Transaction = null;
      }

      /// <summary>
      ///   set value of preventRecordSuffix to val
      /// </summary>
      /// <param name = "val">true if we already performed record suffix</param>
      internal void setPreventRecordSuffix(bool val)
      {
         _preventRecordSuffix = val;
      }

      /// <summary>
      ///   get value of preventRecordSuffix
      /// </summary>
      internal bool getPreventRecordSuffix()
      {
         return (_preventRecordSuffix);
      }

      internal void setPreventControlChange(bool val)
      {
         _preventControlChange = val;
      }

      internal bool getPreventControlChange()
      {
         return (_preventControlChange);
      }

      /// <summary>
      ///   signal the server closed this task
      /// </summary>
      internal void resetKnownToServer()
      {
         _knownToServer = false;
      }

      /// <summary>
      ///   returns the dvPos value
      /// </summary>
      /// <param name = "-">evaluates the dercriptor and return the non-have dvPos value</param>
      protected internal long evaluateDescriptor()
      {
         String resulte = "";
         NUM_TYPE res;
         if (DvPosDescriptor == null)
            return NUM_TYPE.get_hash_code(Encoding.Default.GetBytes(resulte));

         bool first = true;

         // go over the list of the descriptor
         for (int i = 0; i < DvPosDescriptor.Count; i++)
         {
            if (!first)
               resulte += ";";

            string[] currentCell = DvPosDescriptor[i];

            // parse the current task and field number
            String currentTag = currentCell[0];
            int currentVeeIdx = Int32.Parse(currentCell[1]);
            var currentTask = (Task)MGDataCollection.Instance.GetTaskByID(currentTag);
            var currentField = (Field)currentTask.DataView.getField(currentVeeIdx);
            String dispVal = currentField.getDispValue();

            // QCR 978566
            if (currentField.isNull())
               dispVal = "";

            // QCR 743290
            PIC pic;
            switch (currentField.getType())
            {
               case StorageAttribute.BOOLEAN:
               // boolean is stored as "o" or "1" so we just add them as we
               // do in the server
               case StorageAttribute.ALPHA:
               case StorageAttribute.UNICODE:
               case StorageAttribute.BLOB:
               case StorageAttribute.BLOB_VECTOR:
               case StorageAttribute.DOTNET:
                  resulte += dispVal;
                  break;

               case StorageAttribute.DATE:
               case StorageAttribute.TIME:
                  // QCR 742966 date and time special treatment
                  if (!dispVal.Equals(""))
                  {
                     pic = new PIC(currentField.getPicture(), currentField.getType(), 0);
                     DisplayConvertor conv = DisplayConvertor.Instance;
                     resulte += conv.mg2disp(dispVal, "", pic, getCompIdx(), false);
                  }
                  break;

               case StorageAttribute.NUMERIC:
                  // QCR 543204 when there is an empty string (alpha)append nothing
                  if (!dispVal.Equals(""))
                  {
                     res = new NUM_TYPE(dispVal);
                     pic = new PIC(currentField.getPicture(), currentField.getType(), 0);
                     resulte += res.to_a(pic);
                  }
                  break;
            }

            first = false;
         }
         String tmpStr2 = StrUtil.stringToHexaDump(resulte, 4);

         return NUM_TYPE.get_hash_code(Encoding.Default.GetBytes(tmpStr2));
      }

      /// <summary>
      ///   builds the dvPos descriptor vector each of the dvPosDescriptor vector elements is a String array of size
      ///   2 in the first cell of each array we have the tag and in the second we have the vee iindex
      /// </summary>
      protected internal void setDescriptor(String newDesc)
      {
         if (!newDesc.Trim().Equals(""))
         {
            String[] desc = StrUtil.tokenize(newDesc, ";");
            DvPosDescriptor = new List<String[]>();

            for (int i = 0; i < desc.Length; i++)
            {
               var cell = new String[2];
               String[] tagVeePair = StrUtil.tokenize(desc[i], ",");
               cell[0] = tagVeePair[0];
               cell[1] = tagVeePair[1];

               // put the cell value into the vector
               DvPosDescriptor.Add(cell);
            }
         }
      }

      /// <summary>
      ///   returns the cache object of this task
      /// </summary>
      internal DvCache getTaskCache()
      {
         return _dvCache;
      }

      /// <summary>
      ///   prepare all sub-form tree's to either go to server (after deleting relevant entries) or to take d.v from
      ///   cache
      /// </summary>
      /// <returns> true if all subform can do need to go to the server because of updates</returns>
      internal bool prepareCache(bool ignoreCurr)
      {
         bool success = true;

         // checking myself if i am a subform
         if (!ignoreCurr)
         {
            if (isCached())
            {
               if (((DataView)DataView).getChanged())
               {
                  // is the current update dataview were previously taken from cache
                  _dvCache.removeDvFromCache(((DataView)DataView).getDvPosValue(), true);
                  success = false;
               }
            }
            else
            {
               // checks that i am a sub form but i am not cached
               if (IsSubForm && !isCached())
                  success = false;
            }
         }

         Task curretTask;

         // check all my sub forms
         if (hasSubTasks())
         {
            // this loop must run to its end so all updated d.v will be deleted
            for (int i = 0; i < SubTasks.getSize(); i++)
            {
               curretTask = SubTasks.getTask(i);
               // if one of my sub-forms needs to go to the server - so do i
               if (!curretTask.prepareCache(false))
                  success = false;
            }
         }

         return success;
      }

      /// <summary>
      ///   checks if we can take the d.v of the task and all his sub-forms from cache is we can - take them and
      ///   replace them with the current d.v of each task
      /// </summary>
      /// <returns> true if all d.v's were found and set</returns>
      internal bool testAndSet(bool ignoreCurr)
      {
         // check current task
         if (!ignoreCurr && isCached())
         {
            long requestedDv = evaluateDescriptor();
            // try to take from cache only if requested is not current
            if (requestedDv != ((DataView)DataView).getDvPosValue())
            {
               DataView fromCache = _dvCache.getCachedDataView(requestedDv);
               if (fromCache == null)
                  return false;
               changeDv(fromCache); // changes the current d.v with the one taken from cache do not forget to
               // check if the current needs to be inserted to the cache
            }
            // QCR #430045. Indicate that RP must be executed after get from the cache.
            DoSubformPrefixSuffix = true;
         }

         // check all sub-forms tasksd
         if (hasSubTasks())
         {
            // this loop does not need to reach its end
            // the moment one that need to go to the server is discovered all need to go also
            for (int i = 0; i < SubTasks.getSize(); i++)
            {
               // does the i-th son need to go to the server
               if (!SubTasks.getTask(i).testAndSet(false))
                  return false;
            }
         }
         return true;
      }

      /// <summary>
      ///   changes the current d.v with one taken from cache in this method we do not have to worry about updates
      ///   of the current dataview since the method is only called from testAndSet that in turn is only used after
      ///   a Successful run prapreCache which is responsible for checking for updates in the current dataview of
      ///   each related task. however to be on the safe side we check it anyway
      /// </summary>
      /// <param name = "fromCache">the new d.v</param>
      private void changeDv(DataView fromCache)
      {
         DataView dataview = (DataView)DataView;
         // the data view we receive is Certainly from the cache

         if (!hasLocate)
         {
            if (!dataview.getChanged() && dataview.IncludesFirst())
               _dvCache.putInCache(dataview.replicate());
         }
         else
            locatePutInCache();

         // set the data view from the cache as the current
         dataview.setSameAs(fromCache);
         dataview.takeFldValsFromCurrRec();
         MgTreeBase mgTree = Form.getMgTree();
         if (mgTree != null)
         {
            mgTree.clean();
            mgTree = fromCache.getMgTree();
            Form.setMgTree(mgTree);
            dataview.setMgTree(mgTree);

            ((MgForm)Form).RefreshDisplay(Constants.TASK_REFRESH_TREE_AND_FORM);
         }
         else
         {
            Form.SetTableItemsCount(0, true);
            ((MgForm)Form).SetTableItemsCount(false);

            ((MgForm)Form).RefreshDisplay(Constants.TASK_REFRESH_FORM);
         }

         // QCR 9248094
         setOriginalTaskMode(getMode());
      }

      /// <summary>
      ///   sets mode of task
      /// </summary>
      /// <param name = "originalTaskMode">mode of task</param>
      internal void setOriginalTaskMode(String originalTaskMode)
      {
         _originalTaskMode = originalTaskMode[0];
      }

      /// <summary>
      ///   sets mode of task
      /// </summary>
      /// <param name = "originalTaskMode">mode of task</param>
      internal void setOriginalTaskMode(char originalTaskMode)
      {
         _originalTaskMode = originalTaskMode;
      }

      /// <summary>
      ///   get mode of task
      /// </summary>
      internal char getOriginalTaskMode()
      {
         return _originalTaskMode;
      }

      /// <summary>
      ///   returns the is Cache property value of the sub form control this task Correlates with this method
      ///   actually combines two test first that the task is a sub form second that the sub that the sub form has
      ///   the is cached property set to yes
      /// </summary>
      internal bool isCached()
      {
         if (IsSubForm)
            return Form.getSubFormCtrl().checkProp(PropInterface.PROP_TYPE_IS_CACHED, false);
         return false;
      }

      /// <summary>
      ///   does the insert in cache logic in case the task has locate expressions
      /// </summary>
      private void locatePutInCache()
      {
         DataView cached = _dvCache.getCachedDataView(((DataView)DataView).getDvPosValue());

         // if this data view has been inserted to the cache before
         if (cached != null)
         {
            if (((DataView)DataView).checkFirst(cached.getLocateFirstRec()) && !((DataView)DataView).getChanged())
            {
               ((DataView)DataView).setLocateFirstRec(cached.getLocateFirstRec());
               _dvCache.putInCache(((DataView)DataView).replicate());
            }
            else
               _dvCache.removeDvFromCache(((DataView)DataView).getDvPosValue(), true);
         }
         // put it in the cache if it is not updated
         else
         {
            if (!((DataView)DataView).getChanged())
               _dvCache.putInCache(((DataView)DataView).replicate());
         }
      }

      /// <summary>
      ///   returns the hasLocate flag
      /// </summary>
      internal bool HasLoacte()
      {
         return hasLocate;
      }

      /// <summary>
      ///   opens a new Transaction or set reference to the existing Transaction
      /// </summary>
      private void setRemoteTransaction(string transId)
      {
         MGDataCollection mgdTab = MGDataCollection.Instance;
         Task transTask;
         bool isFind = false;

         mgdTab.startTasksIteration();
         while ((transTask = mgdTab.getNextTask()) != null)
         {
            // transaction might be null when task is aborting but not yet unloaded.
            if (transTask.DataviewManager.RemoteDataviewManager.Transaction != null && transTask.DataviewManager.RemoteDataviewManager.Transaction.getTransId().Equals(transId))
            {
               isFind = true;
               break;
            }
         }

         DataviewManager.RemoteDataviewManager.Transaction = (isFind
                          ? transTask.DataviewManager.RemoteDataviewManager.Transaction
                          : new Transaction(this, transId, false));
      }

      /// <summary>
      ///   sets the new Transaction Owner
      /// </summary>
      internal void setTransOwnerTask()
      {
         Transaction.setOwnerTask(this);
         Transaction.setTransBegin(Constants.TASK_LEVEL_TASK);
      }

      /// <summary>
      ///   returns the value of the loop counter according to the topmost value in the stack and the status of the
      ///   "useLoopStack" flag
      /// </summary>
      internal int getLoopCounter()
      {
         int result = 0;

         if (_useLoopStack && _loopStack != null && !(_loopStack.Count == 0))
         {
            result = ((Int32)_loopStack.Peek());
         }

         return result;
      }

      /// <summary>
      ///   pushes the value 0 on the top of the loop stack
      /// </summary>
      internal void enterLoop()
      {
         if (_loopStack == null)
            _loopStack = new Stack();

         _loopStack.Push(0);
      }

      /// <summary>
      ///   pops the topmost entry from the loop stack
      /// </summary>
      internal void leaveLoop()
      {
         popLoopCounter();
      }

      /// <summary>
      ///   pops the value of the loop counter from the stack and returns it
      /// </summary>
      internal int popLoopCounter()
      {
         int result = 0;

         if (_loopStack != null && !(_loopStack.Count == 0))
            result = ((Int32)_loopStack.Pop());

         return result;
      }

      /// <summary>
      ///   increases the value at the top of the loop stack
      /// </summary>
      internal void increaseLoopCounter()
      {
         _loopStack.Push((popLoopCounter() + 1));
      }

      /// <summary>
      ///   returns the number of entries in the loop stack
      /// </summary>
      internal int getLoopStackSize()
      {
         if (_loopStack != null)
            return _loopStack.Count;
         return 0;
      }

      /// <summary>
      ///   set the value of the "use loop stack" variable
      /// </summary>
      /// <param name = "val">the new value to set</param>
      internal void setUseLoopStack(bool val)
      {
         _useLoopStack = val;
      }

      /// <summary>
      ///   return the links table
      /// </summary>
      internal DataviewHeaders getDataviewHeaders()
      {
         return DataviewHeadersTable;
      }

      internal bool isFirstRecordCycle()
      {
         return (_firstRecordCycle);
      }

      internal void isFirstRecordCycle(bool Value_)
      {
         _firstRecordCycle = Value_;
      }

      ///<summary>get the internal name</summary>
      internal String getPublicName()
      {
         return PublicName;
      }

      ///<summary>get the AllowEvents</summary>
      internal bool isAllowEvents()
      {
         Property prop = getProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_EVENTS);
         return prop.getValueBoolean();
      }

      ///<summary>get the task id</summary>
      internal String getExternalTaskId()
      {
         String taskId = "";
         Property prop = getProp(PropInterface.PROP_TYPE_TASK_ID);

         if (prop != null)
            taskId = prop.getValue();
         return taskId;
      }

      /// <summary>
      ///   returns the value of the "revertFrom" flag
      /// </summary>
      internal int getRevertFrom()
      {
         return _revertFrom;
      }

      /// <summary>
      ///   sets the value of the "revertFrom" flag
      /// </summary>
      internal void setRevertFrom(int RevertFrom)
      {
         _revertFrom = RevertFrom;
      }

      /// <summary>
      ///   returns the value of the "revertDirection" flag
      /// </summary>
      internal Direction getRevertDirection()
      {
         return _revertDirection;
      }

      /// <summary>
      ///   sets the value of the "revertDirection" flag
      /// </summary>
      internal void setRevertDirection(Direction RevertDirection)
      {
         _revertDirection = RevertDirection;
      }

      /// <summary>
      ///   returns TRUE if this task owns a dynamicDef.transaction
      /// </summary>
      internal bool isTransactionOwner()
      {
         bool isTransOwner = false;
         if (Transaction != null && Transaction.isOwner(this))
            isTransOwner = true;

         return isTransOwner;
      }

      internal bool isTransactionOnLevel(char level)
      {
         bool isTransOnLevel = isTransactionOwner() && Transaction.getLevel() == level;
         return isTransOnLevel;
      }

      /// <summary>
      ///   get whether the task is the Program (i.e. not a subtask)
      /// </summary>
      internal bool isProgram()
      {
         return _isPrg;
      }

      /// <summary>
      ///   enable-disable actions for task modes
      /// </summary>
      internal void enableModes()
      {
         bool enable = checkProp(PropInterface.PROP_TYPE_ALLOW_MODIFY, true);
         ActionManager.enable(InternalInterface.MG_ACT_RTO_MODIFY, enable);

         bool hasTree = (Form != null && Form.hasTree());
         enable = !hasTree && checkProp(PropInterface.PROP_TYPE_ALLOW_CREATE, true);
         ActionManager.enable(InternalInterface.MG_ACT_RTO_CREATE, enable);

         enable = checkProp(PropInterface.PROP_TYPE_ALLOW_QUERY, true);
         ActionManager.enable(InternalInterface.MG_ACT_RTO_QUERY, enable);

         enable = checkProp(PropInterface.PROP_TYPE_ALLOW_RANGE, true);
         ActionManager.enable(InternalInterface.MG_ACT_RTO_RANGE, enable);

         enable = checkProp(PropInterface.PROP_TYPE_ALLOW_LOCATE, true);
         ActionManager.enable(InternalInterface.MG_ACT_RTO_LOCATE, enable);
         ActionManager.enable(InternalInterface.MG_ACT_RTO_SEARCH, enable);

         enable = checkProp(PropInterface.PROP_TYPE_ALLOW_SORT, true);
         ActionManager.enable(InternalInterface.MG_ACT_SORT_RECORDS, enable);

         enable = checkProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_INDEX, true);
         ActionManager.enable(InternalInterface.MG_ACT_VIEW_BY_KEY, enable);
      }

      /// <summary>
      ///   set hasZoomHandler to true
      /// </summary>
      internal void setEnableZoomHandler()
      {
         _enableZoomHandler = true;
      }

      /// <returns> the hasZoomHandler
      /// </returns>
      internal bool getEnableZoomHandler()
      {
         return _enableZoomHandler;
      }

      /// <returns> is the task is a Destination Subform task</returns>
      internal bool isDestinationSubform()
      {
         return _destinationSubform;
      }

      /// <summary>
      ///   set for Destination Subform task
      /// </summary>
      /// <param name = "destinationSubform"></param>
      internal void setDestinationSubform(bool destinationSubform)
      {
         _destinationSubform = destinationSubform;
      }

      /// <returns> is the task is a Destination Subform task</returns>
      internal bool getIsDestinationCall()
      {
         return _isDestinationCall;
      }

      /// <summary>
      ///   set for Destination Subform task
      /// </summary>
      /// <param name = "isDestinationCall"></param>
      internal void setIsDestinationCall(bool isDestinationCall)
      {
         _isDestinationCall = isDestinationCall;
      }

      /// <summary>
      /// </summary>
      /// <param name="action"></param>
      /// <param name="enable"></param>
      internal void enableActionMenu(int action, bool enable)
      {
            if (Form != null)
            {
                MgFormBase actualForm = Form;
                if (Form.isSubForm())
                {
                    MgControlBase subformCtrl = Form.getSubFormCtrl();
                    actualForm = subformCtrl.getForm().getTopMostForm();
                }
                actualForm.EnableActionMenu(action, enable);
            }
        }


      /// <summary>
      ///   for Destination Subform call it is needed to recompute tab order
      ///   for whole subform chain. If the current subform is a nested subform,
      ///   so up to the first not subform parent. From it start recompute tab order.
      /// </summary>
      internal void resetRcmpTabOrder()
      {
         if (IsSubForm)
            ((Task)_parentTask).resetRcmpTabOrder();
         else
            resetRcmpTabOrderForSubTasks();
      }

      /// <summary>
      ///   recompute tabbing order for all subforms and its subforms too.
      /// </summary>
      private void resetRcmpTabOrderForSubTasks()
      {
         ((MgForm)getForm()).resetRcmpTabOrder();
         if (hasSubTasks())
            for (int i = 0; i < getSubTasks().getSize(); i++)
            {
               Task subTask = getSubTasks().getTask(i);
               if (subTask.IsSubForm)
                  subTask.resetRcmpTabOrderForSubTasks();
            }
      }

      /// <param name = "val">
      /// </param>
      internal void setCancelWasRaised(bool val)
      {
         _cancelWasRaised = val;
      }

      /// <returns>
      /// </returns>
      internal bool cancelWasRaised()
      {
         return _cancelWasRaised;
      }

      internal void enableCreateActs(bool val)
      {
         ActionManager.enable(InternalInterface.MG_ACT_CRELINE, val);
         ActionManager.enable(InternalInterface.MG_ACT_TREE_CRE_SON, val);
      }

      /// <summary>
      ///   sets enable/disable status of create and delete actions
      /// </summary>
      internal void setCreateDeleteActsEnableState()
      {
         if (!(DataView).isEmptyDataview() && checkProp(PropInterface.PROP_TYPE_ALLOW_DELETE, true) &&
             getMode() == Constants.TASK_MODE_MODIFY && ((DataView)DataView).HasMainTable)
            ActionManager.enable(InternalInterface.MG_ACT_DELLINE, true);
         else
            ActionManager.enable(InternalInterface.MG_ACT_DELLINE, false);

         if ((checkProp(PropInterface.PROP_TYPE_ALLOW_CREATE, true))
             &&
             (((DataView)DataView).HasMainTable &&
              (getMode() == Constants.TASK_MODE_MODIFY &&
               ClientManager.Instance.getEnvironment().allowCreateInModifyMode(getCompIdx()))
              || (getMode() == Constants.TASK_MODE_CREATE)))
            enableCreateActs(true);
         else
            enableCreateActs(false);
      }

      /// <param name = "emptyDataview">the emptyDataview to set </param>
      internal void setEmptyDataview(bool emptyDataview)
      {
         (DataView).setEmptyDataview(emptyDataview);
      }

      /// <returns> </returns>
      internal String getName()
      {
         return Name;
      }

      /// <summary>
      ///   This function is executed when task starts its execution in Empty Dataview mode
      /// </summary>
      internal void emptyDataviewOpen(bool subformRefresh)
      {
         // to prevent double prefix on the same record
         if (getLevel() == Constants.TASK_LEVEL_TASK)
         {
            ActionManager.enable(InternalInterface.MG_ACT_EMPTY_DATAVIEW, true);
            // if the task is a subform and subform refresh is done, so execute the empty datatview handler,
            // if the task is not a subform, put the empty dataview event on the queue
            if (IsSubForm)
            {
               if (subformRefresh)
               {
                  RunTimeEvent rtEvt = new RunTimeEvent(this);
                  rtEvt.setInternal(InternalInterface.MG_ACT_EMPTY_DATAVIEW);
                  rtEvt.SetEventSubType(EventSubType.Normal);
                  ClientManager.Instance.EventsManager.handleEvent(rtEvt, false);
               }
            }
            else
               ClientManager.Instance.EventsManager.addInternalEvent(this, InternalInterface.MG_ACT_EMPTY_DATAVIEW);

            setLevel(Constants.TASK_LEVEL_RECORD);

            // for subform mode 'AsParent'. If the parent task that in 'Empty Dataview' state changes its mode
            // the subform also must change mode.
            // QCR #281862. If the subform task is opened in the first time do not refresh it's subforms,
            // because they've received their dataviews already.
            // We must refresh them when the subform task passes to empty DV.
            if (SubformExecMode != SubformExecModeEnum.FIRST_TIME && subformRefresh && CheckRefreshSubTasks())
            {
               ((DataView)DataView).computeSubForms();
               doSubformRecPrefixSuffix();
            }

            enableRecordActions();
            ActionManager.enable(InternalInterface.MG_ACT_RT_REFRESH_RECORD, false);
            ActionManager.enable(InternalInterface.MG_ACT_RT_REFRESH_SCREEN, false);
         }
      }

      /// <summary>
      ///   This function is executed when task ends its execution in Empty Dataview mode
      /// </summary>
      internal void emptyDataviewClose()
      {
         ActionManager.enable(InternalInterface.MG_ACT_EMPTY_DATAVIEW, false);
         setLevel(Constants.TASK_LEVEL_TASK);

         ActionManager.enable(InternalInterface.MG_ACT_RT_REFRESH_RECORD, true);
         ActionManager.enable(InternalInterface.MG_ACT_RT_REFRESH_SCREEN, true);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="subformTask"></param>
      internal void SubformRefresh(Task subformTask, bool explicitSubformRefresh)
      {
         Task currTask = ClientManager.Instance.getLastFocusedTask();
         bool isInside = subformTask.ExecuteNestedRS(currTask);

         if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
            return;

         // QCR #434467. Execute RS of the current subform if it's the current (last focused) task.
         if (subformTask.getLevel() != Constants.TASK_LEVEL_TASK && !ClientManager.Instance.EventsManager.GetStopExecutionFlag())
         {
            ClientManager.Instance.EventsManager.pushNewExecStacks();
            ClientManager.Instance.EventsManager.handleInternalEvent(subformTask, InternalInterface.MG_ACT_REC_SUFFIX, true);
            ClientManager.Instance.EventsManager.popNewExecStacks();
            isInside = true;
            // Defect 77483. Do not continue if the record suffix execution is failed.
            if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
               return;
         }

         // create command for the server
         IClientCommand cmd = CommandFactory.CreateSubformRefreshCommand(getTaskTag(), subformTask.getTaskTag(), explicitSubformRefresh);
         ((Task)subformTask).DataviewManager.Execute(cmd);
         if (subformTask.isAborting())
            return;

         if (explicitSubformRefresh)
         {
            // QCR #429112. Push execution stack before RP execution.
            ClientManager.Instance.EventsManager.pushNewExecStacks();
            // Defect #77049. Do not execute parent RP. We need it only when click on subformwas done.
            bool performParentRecordPrefixOrg = subformTask.PerformParentRecordPrefix;
            subformTask.PerformParentRecordPrefix = false;
            ClientManager.Instance.EventsManager.handleInternalEvent(subformTask, InternalInterface.MG_ACT_REC_PREFIX, true);
            subformTask.PerformParentRecordPrefix = performParentRecordPrefixOrg;
            ClientManager.Instance.EventsManager.popNewExecStacks();
            if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
               return;
         }

         // if SubformRefresh event to itself so the cursor must be on the first control of the current record
         // as for ViewRefresh event. 
         if (isInside)
         {
            // Defect 115793. currTask can't be null.
            if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag() && !subformTask.getPreventControlChange() && currTask != null)
               currTask.moveToFirstCtrl(false);
         }
         else
         {
            if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag())
            {
               if (explicitSubformRefresh)
               {
                  ClientManager.Instance.EventsManager.handleInternalEvent(subformTask, InternalInterface.MG_ACT_REC_SUFFIX);
                  if (currTask != null)
                     ((DataView)currTask.DataView).setPrevCurrRec();
                  subformTask.DoSubformPrefixSuffix = false;
               }
            }
         }
      }

      /// <summary>
      /// Executes Record Suffixes of all nested subforms from bottom to top.
      /// </summary>
      /// <param name="subformTask"></param>
      /// <param name="currTask"></param>
      /// <returns></returns>
      internal bool ExecuteNestedRS(Task lastTask)
      {
         bool isExecuteNestedRS = false;

         // QCR #168342. A new task is set as focused task after the first CP. 
         // So if in CP Update operation is executed currTask is null. But we must refresh the subform.
         if (lastTask == null)
            return isExecuteNestedRS;

         // Do not execute subform refresh when the focused task is not opened as a subform (it was opened from call operation),
         // but the focused task is a child of the subform task. See for example QCR #431850.
         if (getLevel() != Constants.TASK_LEVEL_TASK && lastTask.getMgdID() != getMgdID())
            return isExecuteNestedRS;

         for (Task task = lastTask; task != this && task.pathContains(this) && task.getLevel() != Constants.TASK_LEVEL_TASK; task = (Task)task.getParent())
         {
            ClientManager.Instance.EventsManager.pushNewExecStacks();
            ClientManager.Instance.EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
            ClientManager.Instance.EventsManager.popNewExecStacks();
            isExecuteNestedRS = true;
         }

         return isExecuteNestedRS;
      }
      /// <summary>
      ///   get the Flow mode and direction of the task
      /// </summary>
      /// <returns></returns>
      private FlowModeDir getFlowModeDir()
      {
         FlowModeDir flowModeDir = FlowModeDir.NONE;

         if (_flowMode == Flow.FAST)
            flowModeDir = _direction == Direction.BACK
                             ? FlowModeDir.FAST_BACKWARD
                             : FlowModeDir.FAST_FORWARD;
         else //Step
         {
            switch (_direction)
            {
               case Direction.BACK:
                  flowModeDir = FlowModeDir.STEP_BACKWARD;
                  break;
               case Direction.FORE:
                  flowModeDir = FlowModeDir.STEP_FORWARD;
                  break;
            }
         }

         return flowModeDir;
      }

      /// <summary>
      ///   execute Rec suffix for all nested subforms untill subform task that contains these 2 subforms
      /// </summary>
      /// <param name = "newTask"></param>
      /// <returns></returns>
      internal int execSubformRecSuffix(Task newTask)
      {
         Task subfTask;
         int i, idx = 0;

         for (i = _taskPath.Count; i > 0; i--)
         {
            subfTask = _taskPath[i - 1];
            if (!newTask._taskPath.Contains(subfTask))
            {
               ClientManager.Instance.EventsManager.handleInternalEvent(subfTask, InternalInterface.MG_ACT_REC_SUFFIX);
               if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                  break;
               // Change the Create mode to Modify if the current record is not a new record.
               // So the mode is changed only for the first record that was not changed.
               if (subfTask.getMode() == Constants.TASK_MODE_CREATE && !((DataView)subfTask.DataView).getCurrRec().isNewRec())
                  subfTask.setMode(Constants.TASK_MODE_MODIFY);
            }
            else
            {
               idx = newTask._taskPath.IndexOf(subfTask);
               break;
            }
         }

         Debug.Assert(i > 0);
         return idx;
      }

      /// <summary>
      ///   get the counter of the task
      /// </summary>
      /// <returns>counter</returns>
      internal int getCounter()
      {
         return _counter;
      }

      /// <summary>
      ///   increase the tasks counter by 1.
      /// </summary>
      /// <returns>counter</returns>
      internal void increaseCounter()
      {
         _counter++;
      }

      internal void setCounter(int cnt)
      {
         _counter = cnt;
      }

      /// <summary>
      ///   !!
      /// </summary>
      /// <returns></returns>
      internal bool isCurrentStartProgLevel()
      {
         return _currStartProgLevel == ClientManager.Instance.StartProgLevel;
      }

      /// <summary>
      ///   updates dotnet object back to argument field
      /// </summary>
      private void updateDNArg()
      {
         FieldsTable fldTab = (FieldsTable)DataView.GetFieldsTab();
         Field fld;
         for (int i = 0; i < fldTab.getSize(); i++)
         {
            fld = (Field)fldTab.getField(i);
            if (fld.getType() == StorageAttribute.DOTNET)
            {
               String blobStr = fld.getValue(false);

               if (!string.IsNullOrEmpty(blobStr))
               {
                  int key = BlobType.getKey(blobStr);
                  if (key != 0)
                     // copy the object back to original field passed as argument 
                     fld.updateDNArgFld();
               }
            }
         }
      }

      /// <summary>
      ///   garbage collection for DotNet Objects
      ///   1. clears DNObjectsCollection entries for DotNet Fields
      ///   2. clears DNObjectsCollection entries for Expressions
      /// </summary>
      internal void removeDotNetObjects()
      {
         FieldsTable fldTab = (FieldsTable)DataView.GetFieldsTab();
         Field fld;
         Expression exp;
         int key;

         // 1. clears DNObjectsCollection entries for DotNet Fields
         for (int i = 0; i < fldTab.getSize(); i++)
         {
            fld = (Field)fldTab.getField(i);

            if (fld.getType() == StorageAttribute.DOTNET)
            {
               String blobStr = fld.getValue(false);
               if (!string.IsNullOrEmpty(blobStr))
               {
                  key = BlobType.getKey(blobStr);
                  if (key != 0)
                  {
                     // Remove the event handlers only if the field doesn't refer to a control. In case of 
                     // control, this will be done while disposing the control.
                     if (!fld.refersDNControl())
                        fld.removeDNEventHandlers();

                     // finally remove the field
                     DNManager.getInstance().DNObjectsCollection.Remove(key);
                     DNManager.getInstance().DNObjectFieldCollection.remove(key);
                  }
               }
            }
         }

         // 2. clears DNObjectsCollection entries for Expressions
         if (ExpTab != null)
         {
            for (int i = 0; i < ExpTab.getSize(); i++)
            {
               exp = ExpTab.getExp(i);

               if (exp != null)
               {
                  key = exp.GetDNObjectCollectionKey();
                  if (key != 0)
                     DNManager.getInstance().DNObjectsCollection.Remove(key);
               }
            }
         }
      }

      /// <summary>
      /// Synchronize non-Gui[RTE(MgCore)] after creating a .NET control.
      /// </summary>
      /// <param name="ctrlIdx">the .net control</param>
      public override void OnDNControlCreate(int ctrlIdx)
      {
         // Do nothing for RC
      }

      /// <summary>
      /// return events-related details about a .net control on a form
      /// </summary>
      /// <param name="ctrlIdx">in: the .net control</param>
      /// <param name="dnObjKey">out: object key of the variable attached to the control</param>
      /// <param name="dnType">out: dotnet type name of the variable attached to the control</param>
      /// <param name="allTasksDNEventsNames">out: comma-delimited string of events that are handled in the current tasks or its ancestors, up to the main program</param>
      public override void GetDNControlEventsDetails(int ctrlIdx, out int dnObjKey, out List<String> allTasksDNEventsNames)
      {
         MgControl ctrl = (MgControl)getCtrl(ctrlIdx);
         Field dnObjectRefField = (Field)ctrl.DNObjectReferenceField;

         if (dnObjectRefField != null)
            dnObjKey = dnObjectRefField.GetDNKey();
         else
            dnObjKey = 0;

         allTasksDNEventsNames = GetDNEvents(dnObjectRefField, ctrl.DNType, true);
      }

      /// <summary> return .NET events of the field </summary>
      /// <param name="field"></param>
      /// <param name = "dnType"></param>
      /// <param name="fromAncestors"></param>
      /// <returns></returns>
      internal List<String> GetDNEvents(Field field, Type dnType, bool fromAncestors)
      {
         return GetDNEvents(field, dnType, new List<String>(), this, fromAncestors);
      }

      /// <summary>
      ///   collect and return .NET events that can be raised (i.e. have handlers) for a field.
      ///   collected events are added on top of the list 'dnEventsNames'.
      /// </summary>
      /// <param name = "field"></param>
      /// <param name = "dnType"></param>
      /// <param name = "dnEventsNames">list of events names on top of which events associated for the field will be added</param>
      /// <param name = "currTask"></param>
      /// <param name = "fromAncestors"> if true - gather events from parents also</param>
      /// <returns></returns>
      private List<String> GetDNEvents(Field field, Type dnType, List<String> dnEventsNames, Task currTask, bool fromAncestors)
      {
         // go over the task handlers
         for (int i = 0; i < HandlersTab.getSize(); i++)
         {
            EventHandler handler = HandlersTab.getHandler(i);
            Event evt = handler.getEvent();
            if (evt.isMatchingDNEvent(field, dnType) &&
                handler.checkTaskScope(this == currTask) &&
                !dnEventsNames.Contains(evt.DotNetEventName))
               dnEventsNames.Add(evt.DotNetEventName);
         }

         if (fromAncestors && getParent() != null)
            return ((Task)getParent()).GetDNEvents(field, dnType, dnEventsNames, currTask, fromAncestors);

         return dnEventsNames;
      }

      /// <summary>
      /// returns url for menus
      /// </summary>
      /// <returns></returns>
      public override String getMenusFileURL()
      {
         String menusFileURL = _menusFileName;

         // If menu passed in message, we need a unique name to store it
         if (menusFileURL != null)
         {
            // we need to insert the rights hash code into the menus file name
            if (ClientManager.Instance.getRightsHashKey().Length > 0)
               menusFileURL = menusFileURL.Insert(menusFileURL.IndexOf("Menus.xml"),
                                                  ClientManager.Instance.getRightsHashKey() + "_");
         }

         return menusFileURL;
      }

      /// <summary>
      /// return menus content (passed completely inside a response, i.e. not thru the cache)
      /// </summary>
      /// <returns></returns>
      public override byte[] getMenusContent()
      {
         byte[] menusContent = base.getMenusContent();
         if (menusContent == null)
         {
            Debug.Assert(getMenusFileURL() != null); // We got a file name from the cache

            menusContent = CommandsProcessor.GetContent(getMenusFileURL(), true);
            base.setMenusContent(menusContent);
         }
         return menusContent;
      }

      /// <summary>
      ///   Check if reference to main program field
      /// </summary>
      /// <param name = "fieldStr">a reference to the field</param>
      /// <returns></returns>
      internal static Boolean isMainProgramField(String fieldStr)
      {
         List<String> Field = XmlParser.getTokens(fieldStr, ", ");
         bool mainProgField = false;

         if (Field.Count == 2)
         {
            int parent = Int32.Parse(Field[0]);
            if (parent == MAIN_PRG_PARENT_ID)
               mainProgField = true;
         }

         return mainProgField;
      }

      public override string ToString()
      {
         return String.Format("(task {0}-\"{1}\"{2})", _taskTag, Name, isMainProg() ? (" ctl " + this._ctlIdx) : "");
      }

      internal void setExitingByMenu(bool val)
      {
         _exitingByMenu = val;
      }

      internal bool getExitingByMenu()
      {
         return _exitingByMenu;
      }

      /// <summary>
      /// return menus file name
      /// </summary>
      /// <returns></returns>
      internal String getMenusFileName()
      {
         return _menusFileName;
      }

      /// <summary></summary>
      /// <returns>true iff menus were attached to the program (in the server side)</returns>
      public override bool menusAttached()
      {
         return base.menusAttached() || getMenusFileName() != null;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override MgFormBase ConstructMgForm(ref bool alreadySetParentForm)
      {
         MgForm form = new MgForm();
         MgFormBase parentForm = (ParentTask != null ? ParentTask.getForm() : null);

         //ParentForm of any form should be a visible one. It might happen that the ParentTask 
         //is a non-interactive task with OpenWindow=No. In this case, the Parent of the non-interactive
         //task, should be the parent of the current form.
         while (parentForm != null && !parentForm.getTask().isOpenWin())
         {
            parentForm = parentForm.ParentForm;
         }

         form.ParentForm = parentForm;

         alreadySetParentForm = true;
         return form;
      }

      /// <summary>
      ///   returns the mgdata ID to which task belongs
      /// </summary>
      internal int getMgdID()
      {
         return _mgData.GetId();
      }

      /// <summary>
      ///   get reference to current MGData
      /// </summary>
      internal MGData getMGData()
      {
         return _mgData;
      }

      /// <summary> refresh the display, paint the controls and their properties again </summary>
      public override void RefreshDisplay()
      {
         if (Form != null && !isAborting() && isStarted())
         {
            Logger.Instance.WriteDevToLog("Start task.RefreshDisplay()");

            //TODO: MgForm.RefreshDisplay() also refreshes the table meaning it fetches the 
            //records from the DataView and refreshes the rows.
            //Now, Task.RefreshDisplay() is called from 2 places:
            //1. while initing the form
            //2. when coming back from server
            //In case #1, we do not need to refresh the table as it is done from firstTableRefresh(). 
            //Check if this can be eliminated.
            if (_refreshType != Constants.TASK_REFRESH_NONE)
            {
               MgForm form = (MgForm)Form;
               bool wasRefreshed = (_refreshType == Constants.TASK_REFRESH_TREE_AND_FORM
                                    ? form.RefreshDisplay(Constants.TASK_REFRESH_TREE_AND_FORM)
                                    : form.RefreshDisplay(Constants.TASK_REFRESH_FORM));

               if (wasRefreshed)
                  _refreshType = Constants.TASK_REFRESH_NONE;
            }

            Logger.Instance.WriteDevToLog("End task.RefreshDisplay()");
         }
      }

      /// <summary> get the value of a field. </summary>
      /// <param name="fieldDef"></param>
      /// <param name="value"></param>
      /// <param name="isNull"></param>
      public override void getFieldValue(FieldDef fieldDef, ref String value, ref bool isNull)
      {
         value = ((Field)fieldDef).getValue(false);
         isNull = ((Field)fieldDef).isNull();
      }

      /// <summary> get the display value of a field. </summary>
      /// <param name="fieldDef"></param>
      /// <param name="value"></param>
      /// <param name="isNull"></param>
      public override void getFieldDisplayValue(FieldDef fieldDef, ref String value, ref bool isNull)
      {
         value = ((Field)fieldDef).getDispValue();
         isNull = ((Field)fieldDef).isNull();
      }

      /// <summary> updates the field's value and starts recompute. </summary>
      /// <param name="fieldDef"></param>
      /// <param name="value"></param>
      /// <param name="isNull"></param>
      public override void UpdateFieldValueAndStartRecompute(FieldDef fieldDef, String value, bool isNull)
      {
         if (TaskPrefixExecuted)
            ((Field)fieldDef).setValueAndStartRecompute(value, isNull, true, false, false);
      }

      /// <summary> /// This function returns true if field in null else false.</summary>
      /// <param name="fieldDef"></param>
      /// <returns>true if field is null else false</returns>
      public override bool IsFieldNull(FieldDef fieldDef)
      {
         Field fld = (Field)fieldDef;
         return fld.isNull();
      }

      /// <summary>
      /// For a richclient tasks, Non-Interactive task will be always Modal.
      /// </summary>
      /// <returns>true</returns>
      internal bool ShouldNonInteractiveBeModal()
      {
         return true;
      }

      /// <summary>
      /// For a richclient tasks, check if Non-Interactive task child should be Modal.
      /// </summary>
      /// <returns>true</returns>
      internal bool ShouldNonInteractiveChildBeModal()
      {
         return ShouldChildBeModal();
      }

      /// <summary>
      /// Check if a tasks child should be modal
      /// 
      /// </summary>
      /// <returns></returns>
      private bool ShouldChildBeModal()
      {
         bool childShouldBehaveAsModal = false;

         // window is opened and of modal type (doesn't matter if interactive or not)
         if ((Form.ConcreteWindowType == WindowType.Modal || Form.ConcreteWindowType == WindowType.ApplicationModal) && getForm().Opened)
            childShouldBehaveAsModal = true;
         else if (!IsInteractive)
         {
            // if special flag is off
            if (!ClientManager.Instance.getEnvironment().CloseTasksOnParentActivate())
               childShouldBehaveAsModal = true;
            // when the form is opened (no matter what is type is), except from main prog.
            else if (getForm().Opened && !isMainProg())
               childShouldBehaveAsModal = true;
            // we got here when a non interactive did not open a form and the special is on.
            // in that case we need to recursively check the parent task. (for example , a no form non interactive, 
            // might have a modal interactive parent.
            // if the parent task is main program, there is no need to keep the recursive loop (we will get an exception when trying to check its form)
            else if (ParentTask != null && !ParentTask.isMainProg())
               childShouldBehaveAsModal = ParentTask.ShouldChildBeModal();
         }

         return childShouldBehaveAsModal;
      }

      /// <summary>
      ///   get the parent task of this task
      /// </summary>
      internal Task getParent()
      {
         return _parentTask;
      }

      /// <summary>
      ///   returns true if and only if this task is the checked task or a descendant of it
      /// </summary>
      internal virtual bool isDescendentOf(Task chkTask)
      {
         if (chkTask == this)
            return true;

         if (chkTask == null || _parentTask == null)
            return false;

         return _parentTask.isDescendentOf(chkTask);
      }

      /// <summary>
      /// return true if the table is local
      /// </summary>
      /// <param name="tableIdx"></param>
      /// <returns></returns>
      internal bool IsTableLocal(int tableIdx)
      {
         bool isLocal = false;
         if (tableIdx >= 0)
         {
            DataSourceReference table = DataSourceReferences[tableIdx];
            isLocal = table.IsLocal;
         }
         return isLocal;
      }

      #region Nested type: FlowModeDir

      private enum FlowModeDir
      {
         NONE = ' ',
         FAST_BACKWARD = 'R',
         FAST_FORWARD = 'F',
         STEP_BACKWARD = 'P',
         STEP_FORWARD = 'N'
      }

      #endregion

      // task members that are sent from the server differently for each called task (eye catchers)
      // each element of this vector is a string array of size 2. 
      // each array has in the first cell the task id (task tag) and in the second the corresponding vee.

      // TODO: identify additional dynamic members ([:)
      //<prop id="197" name="task mode" val="[:"/>
      //<prop id="457" name="main display" val="[:"/>
      //<prop id="465" name="tabbing cycle" val="[:"/>
      //<prop id="207" name="transaction begin" val="[:"/>
      //<dvheader rmpos="1,0" chunk_size="[:" compute_by="C" has_main_tbl="0">
      //<prop id="234" val="\levent eventtype\e\qU\q user\e\q[:,0\q/\g
      //<event eventtype="U" user="[:,0"/>

      #region Nested type: LocateQuery

      /// <summary>
      ///   members for incremental search
      /// </summary>
      internal class LocateQuery
      {
         internal LocateQuery()
         {
            InitServerReset = true;
            Buffer = new StringBuilder();
         }

         internal bool ServerReset { get; set; }
         internal bool InitServerReset { get; set; }
         internal int Offset { get; set; }
         internal MgControl Ctrl { get; set; }
         internal Timer Timer { get; set; }
         internal StringBuilder Buffer { get; set; }

         internal void FreeTimer()
         {
            if (Timer != null)
               Timer.Dispose();
         }

         internal void ClearIncLocateString()
         {
            Buffer.Remove(0, Buffer.Length);
         }

         internal void IncLocateStringAddBackspace()
         {
            var backspace = (char)0xFF;

            if (Buffer.Length != 0)
               if (Buffer[Buffer.Length - 1] == backspace)
                  Buffer.Append(backspace);
               else
                  Buffer.Remove(Buffer.Length - 1, 1);
            else
               Buffer.Append(backspace);
         }

         internal void Init(MgControl currCtrl)
         {
            if (InitServerReset)
            {
               ServerReset = true;
               Offset = 0;
            }
            ClearIncLocateString();
            Ctrl = currCtrl;
            FreeTimer();
         }
      }

      #endregion


      /// <summary>
      /// return true for remote task for expression that was computed once on server
      /// </summary>
      /// <returns></returns>
      public override bool ShouldEvaluatePropertyLocally(int propId)
      {
         return TaskService.ShouldEvaluatePropertyLocally(propId);
      }


      /// <summary>
      /// This method runs actions after the 'form' tag is handled.
      /// 
      /// </summary>
      internal virtual void EnsureValidForm()
      {
         // Default implementation of the method:
         // If this task is the main program and _openWin evaluates to 'false', then
         // prevent the display of the form, by setting it to 'null'.
         if (_isMainPrg && Form != null)
         {
            if (!_openWin || !Form.isLegalForm) //This can be mdi frame
               Form = null;
         }
      }

      /// <summary>
      /// compute the main display once 
      /// </summary>
      /// <returns></returns>
      internal void ComputeMainDisplay()
      {
         Property propMainDisplay = getProp(PropInterface.PROP_TYPE_MAIN_DISPLAY);
         int mainDisplayIndex = TaskService is RemoteTaskService ? propMainDisplay.GetComputedValueInteger() : propMainDisplay.getValueInt();

         mainDisplayIndex = GetRealMainDisplayIndexOnCurrentTask(mainDisplayIndex);

         _forms.InitFormFromXmlString(mainDisplayIndex);
         EnsureValidForm();
      }

      /// <summary>
      /// prepare task form
      /// </summary>
      internal ReturnResult PrepareTaskForm()
      {
         TaskServiceBase.PreparePropOpenTaskWindow(this);
         return TaskServiceBase.PreparePropMainDisplay(this);
      }

      /// <summary>
      /// return true if form is legal       
      /// we are not get to that method for main display that open window is false
      /// </summary>
      /// <returns></returns>
      internal bool FormIsLegal()
      {
         bool isFormIsLegal = false;

         if (isMainProg())
            isFormIsLegal = true;
         else
            // check if the form is legal for RC task
            isFormIsLegal = getForm().isLegalForm;

         return isFormIsLegal;
      }

      /// <summary>
      /// get the main display on studio depth 
      /// </summary>
      /// <param name="mainDspIdx"></param>
      /// <returns></returns>
      internal int GetRealMainDisplayIndexOnDepth(int mainDspIdx)
      {
         Task task = LogicalStudioParentTask;

         while (task != null)
         {
            mainDspIdx += ((Task)task)._forms.Count;
            task = task.LogicalStudioParentTask;
         }

         return mainDspIdx;
      }

      /// <summary>
      /// get the main display on studio depth 
      /// </summary>
      /// <param name="mainDspIdx"></param>
      /// <returns></returns>
      internal int GetRealMainDisplayIndexOnCurrentTask(int mainDspIdx)
      {
         if (mainDspIdx > 1)
         {
            Task task = LogicalStudioParentTask;

            while (task != null)
            {
               mainDspIdx -= ((Task)task)._forms.Count;
               task = task.LogicalStudioParentTask;
            }
         }

         return mainDspIdx;
      }

      /// <summary>
      /// get Ancestor task (on runtime tree) of the send task definition id
      /// </summary>
      /// <param name="findTaskDefinitionId"></param>
      /// <returns></returns>
      internal Task GetAncestorTaskByTaskDefinitionId(TaskDefinitionId findTaskDefinitionId)
      {
         Task currentTask = this;

         // Fixed bug #:172959
         // if the findTaskDefinitionId is main program (can be of component) we will not found it in the _parentTask
         // so we will take the task from the MGDataTable
         if (findTaskDefinitionId.IsMainProgram())
            return (Task)Manager.MGDataTable.GetMainProgByCtlIdx(ContextID, findTaskDefinitionId.CtlIndex);

         while (currentTask != null)
         {
            if (currentTask.TaskDefinitionId.Equals(findTaskDefinitionId))
               return currentTask;

            currentTask = (Task)currentTask._parentTask;
         }

         return null;
      }


      internal void SetDataControlValuesReference(int controlId, int dcValuesId)
      {
         var control = getForm().getCtrl(controlId);
         control.setDcValId(dcValuesId);
      }

      /// <summary>
      /// display test on status bar
      /// </summary>
      /// <param name="task"></param>
      /// <param name="text"></param>
      internal void DisplayMessageToStatusBar(string text)
      {
         //Qcr #798311. if server side had MP doing verify to pane and MP has no form
         // we will get an exception. so in case of no form, get it from last focused task.
         Task taskToVerifyOn = (Task)GetContextTask();
         if (taskToVerifyOn.getForm() == null)
            taskToVerifyOn = ClientManager.Instance.getLastFocusedTask();

         Manager.WriteToMessagePane(taskToVerifyOn, text, true);
      }

      // Fixed bug#: 441251&179217
      bool inViewRefreshAfterRollback{ get; set; }
      /// <summary>
      /// after local rollback we need to refresh the current record and to refresh the view
      /// </summary>
      /// <param name="task"></param>
      internal void ViewRefreshAfterRollback(bool executeRefreshView)
      {
         if (DataviewManager.HasLocalData && getForm() != null)
         {
            if (executeRefreshView && !inViewRefreshAfterRollback)
            {
               inViewRefreshAfterRollback = true;
               ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_RT_REFRESH_VIEW, EventSubType.RtRefreshViewUseCurrentRow);
               inViewRefreshAfterRollback = false;
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      internal void CancelAndRefreshCurrentRecordAfterRollback()
      {
         if (DataviewManager.HasLocalData && getForm() != null)
         {
            ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_CANCEL, EventSubType.CancelWithNoRollback);

            IClientCommand cmd = CommandFactory.CreateEventCommand(getTaskTag(), InternalInterface.MG_ACT_RT_REFRESH_RECORD);
            DataviewManager.LocalDataviewManager.Execute(cmd);
         }
      }


      /// <summary>
      /// abort all direct tasks
      /// </summary>
      internal void AbortDirectTasks()
      {
         if (hasSubTasks())
         {
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < SubTasks.getSize(); i++)
            {
               Task subtask = (Task)SubTasks.getTask(0);
               if (!subtask.IsSubForm)
                  tasks.Add(subtask);
            }

            // abort all the direct tasks (not subform)
            foreach (Task subtask in tasks)
               ClientManager.Instance.EventsManager.handleInternalEvent(subtask, InternalInterface.MG_ACT_EXIT);
         }
      }

      /// <summary>
      /// the same code can be found at (tsk_mode.cpp method "BOOLEAN RT::tsk_mode (Uchar mode)")
      /// </summary>
      /// <param name="task"></param>
      /// <param name="taskModeValue"></param>
      /// <returns>return true if the mode is allow</returns>
      internal bool CheckAllowTaskMode(char taskModeValue)
      {
         int initModeRelevantYNExp = 0;
         bool allowedTaskMode = false;

         //for RC         
         // #define RTSIDOW_MODE_MODIFY      0 -> M
         // #define RTSIDOW_MODE_CREATE      1 -> C
         // #define RTSIDOW_MODE_QUERY       2 -> E
         // #define RTSIDOW_MODE_LOCATE      3 -> L not relevant for RC
         // #define RTSIDOW_MODE_SEARCH      4 -> L not relevant for RC
         // #define RTSIDOW_MODE_RANGE       5 -> R not relevant for RC 
         // #define RTSIDOW_MODE_KEY         6 -> K not relevant for RC
         // #define RTSIDOW_MODE_SORT        7 -> S not relevant for RC
         // #define RTSIDOW_MODE_FILES       8 -> O not relevant for RC
         // #define RTSIDOW_MODE_PRINT_DATA  9 -> G
         // #define RTSIDOW_MODE_DELETE      9 -> D - delete for non interactive only


         // #define RTSIDOW_MODE_AS_PARENT   9 -> p

         //bool modeRelatedToMainDbOnTask = false;

         Debug.Assert(taskModeValue != 'P', "'As Parent' task mode must be handled before calling this method.");
         switch (taskModeValue)
         {
            case 'D':// delete , is relevant only for non interactive task
               if (!IsInteractive)
                  initModeRelevantYNExp = PropInterface.PROP_TYPE_ALLOW_DELETE; // CTX->ctl_tsk_.tskr->delete_ynexp;
               break;
            case 'C': //CREATE
               initModeRelevantYNExp = PropInterface.PROP_TYPE_ALLOW_CREATE; // CTX->ctl_tsk_.tskr->create_ynexp;
               break;
            case 'M':// MODIFY
               initModeRelevantYNExp = PropInterface.PROP_TYPE_ALLOW_MODIFY;// CTX->ctl_tsk_.tskr->modify_ynexp;
               break;
            case 'E': // QUERY
               initModeRelevantYNExp = PropInterface.PROP_TYPE_ALLOW_QUERY;// CTX->ctl_tsk_.tskr->query_ynexp;
               break;
            case 'G': // PRINT_DATA
               break;
            case 'R':
               //modeRelatedToMainDbOnTask = true;
               //initModeRelevantYNExp = PropInterface.PROP_TYPE_ALLOW_RANGE;//CTX->ctl_tsk_.tskr->range_ynexp;
               break;
            case 'K': // -> not relevant to RC
               //if (CTX->ctl_tsk_.tsk_rt->RtMainDb.Object() == 0 || 
               //    (!TASK_IS_SQL(CTX->ctl_tsk_.tskh) && 
               //     CTX->ctl_tsk_.tsk->db->open == DB_OPEN_REINDEX))
               //   return (false);
               //modeRelatedToMainDbOnTask = true;
               //ynexp = CTX->ctl_tsk_.tskr->key_ynexp;
               break;
            case 'S':// -> not relevant to RC
               //modeRelatedToMainDbOnTask = true;
               //ynexp = CTX->ctl_tsk_.tskr->sort_ynexp;
               break;
            case 'O':// -> not relevant to RC
               //if (CTX->ctl_tsk_.tskr->ios == 0)
               //   return (false);
               //ynexp = CTX->ctl_tsk_.tskr->file_ynexp;
               break;
            default:
               break;
         }
         //todo !!!
         //if (modeRelatedToMainDbOnTask )//&&  CTX->ctl_tsk_.tsk_rt->RtMainDb.Object() == 0)
         //   return (false);

         if (initModeRelevantYNExp != 0)
         {
            Property propTaskMode = getProp(initModeRelevantYNExp);
            allowedTaskMode = propTaskMode.getValueBoolean();
         }

         return allowedTaskMode;
      }

      public override bool IsBlockingBatch
      {
         get
         {
            return false;
         }
      }

      /// <summary>
      /// isTableWithAbsolutesScrollbar
      /// </summary>
      public bool isTableWithAbsolutesScrollbar()
      {
         bool isTableWithAbsolutesScrollbar = false;

         if (getForm() != null)
            isTableWithAbsolutesScrollbar = ((MgForm)getForm()).isTableWithAbsoluteScrollbar();

         return isTableWithAbsolutesScrollbar;
      }
   }

   /// <summary>
   /// 
   /// </summary>
   internal class UserRange
   {
      internal long veeIdx { get; set; }
      internal String min { get; set; }
      internal String max { get; set; }
      internal bool nullMax { get; set; }
      internal bool nullMin { get; set; }
      internal bool discardMax { get; set; }
      internal bool discardMin { get; set; }
   }

   /// <summary>
   /// Class holding additional information regarding a task being opened.
   /// </summary>
   internal class OpeningTaskDetails
   {
      internal Task CallingTask { get; private set; }       // the task that triggered a call to the current task (possibly by raising an event, in which case 'CallingTask' differs from 'PathParentTask').
      internal Task PathParentTask { get; private set; }    // the task that contains the Call operation to the current task.
      internal MgForm FormToBeActivatedOnClosingCurrentForm { get; private set; }    // the active form when the Call operation is executed.

      public OpeningTaskDetails()
      {
         CallingTask = PathParentTask = null;
         FormToBeActivatedOnClosingCurrentForm = null;
      }

      public OpeningTaskDetails(Task callingTask, Task pathParentTask, MgForm formToBeActivatedOnClosingCurrentForm)
      {
         CallingTask = callingTask;
         PathParentTask = pathParentTask;
         FormToBeActivatedOnClosingCurrentForm = formToBeActivatedOnClosingCurrentForm;
      }
   }
}
