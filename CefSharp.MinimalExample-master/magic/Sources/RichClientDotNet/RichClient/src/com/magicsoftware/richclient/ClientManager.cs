using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Net;
using com.magicsoftware.richclient.env;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.httpclient;
using com.magicsoftware.httpclient.utils;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.security;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Console = System.Console;
using Environment = com.magicsoftware.richclient.env.Environment;
using Process = com.magicsoftware.richclient.util.Process;
using Task = com.magicsoftware.richclient.tasks.Task;
using RunTimeEvent = com.magicsoftware.richclient.events.RunTimeEvent;
using Manager = com.magicsoftware.unipaas.Manager;
using Field = com.magicsoftware.richclient.data.Field;
using DataView = com.magicsoftware.richclient.data.DataView;
using FieldsTable = com.magicsoftware.richclient.data.FieldsTable;
using DbhRealIdxInfo = com.magicsoftware.richclient.data.DbhRealIdxInfo;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.dotnet;
using RuntimeContext = com.magicsoftware.unipaas.management.RuntimeContextBase;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.local.application;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.richclient.http;
using com.magicsoftware.richclient.communications;
using com.magicsoftware.util.fsm;
using com.magicsoftware.richclient.gui;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.sources;
using com.magicsoftware.richclient.local.application.datasources.converter;
using com.magicsoftware.richclient.data;
using com.magicsoftware.gatewaytypes.data;
using static com.magicsoftware.unipaas.Events;
using System.Web.Script.Serialization;

#if !PocketPC
using System.Windows.Forms;
using System.Deployment.Application;
#else
using com.magicsoftware.richclient.mobile.util;
using Expression = com.magicsoftware.richclient.exp.Expression;
using Monitor = com.magicsoftware.richclient.mobile.util.Monitor;
#endif

namespace com.magicsoftware.richclient
{
   public static class ClientManagerProxy
   {
      static public void Start()
      {
         ClientManager.Main(new string[] { });
      }

      static public string GetTaskId(string parentId, string subformName)
      {
         return ClientManager.Instance.getTaskId(parentId, subformName);
      }

      static public void AddEvent(string taskId, string eventName, string controlName, string line)
      {
         ClientManager.Instance.AddEvent(taskId, eventName, controlName, line);
      }

      static public void TaskFinishedInitialization(string taskId )
      {
         ClientManager.Instance.TaskFinishedAngularInitialization(taskId);
      }
   }
  
   /// <summary> The main class of the Rich Client</summary>
   internal class ClientManager : System.IServiceProvider
   {
 
      readonly GuiEventsProcessor _guiEventsProcessor;

      public void AddEvent(string taskId, string eventName, string controlName, string line)
      {
         Task task = MGDataCollection.Instance.getCurrMGData().getTask(taskId);
         int lineIdx;
         int.TryParse(line, out lineIdx);

         if (task != null)
         {
            //TODO : use dictionary and real Id to return controls
            MgControlBase control = task.getForm().GetCtrl(controlName);
            switch (eventName)
            {
               case "click":
                  if (control.isButton())
                     Events.OnSelection("", control, lineIdx, true);
                  else
                  {
                     //for text - temporary 
                     if (control.Type == MgControlType.CTRL_TYPE_TEXT ||    control.Type == MgControlType.CTRL_TYPE_LABEL ||
                         control.Type == MgControlType.CTRL_TYPE_IMAGE)
                        Events.OnFocus(control, lineIdx, true, false);

                     Events.OnMouseDown(control.getForm(), control, null, true, lineIdx, false, true);

                  }
                  break;
               case "focus":
                  Events.OnFocus(control, lineIdx, true, false);
                  break;
            }
         }
      }
      public class TaskDescription
      {
         public string TaskId { get; set; }
         public List<string> Names { get; set; }
      }

      public string getTaskId(string parentId, string subformName)
      {
         //TODO: shell we move it to the workes tread?
         //what about multiple windows ?
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         //TODO: not always 0
         MGData mgd = mgDataTab.getMGData(0);
         Task foundtask = null;
         string result = "notfound";
         if ( String.IsNullOrEmpty(parentId))
         {
            foundtask = mgd.getFirstTask();//.getTaskTag();
         }
         else
         {
            Task task = mgd.getTask(parentId);
            for (int i = 0; i < task.SubTasks.getSize(); i++)
            {
               Task subTask = task.SubTasks.getTask(i);
               MgFormBase form = subTask.getForm();
               if (form != null && form.getSubFormCtrl() != null &&
                 form.getSubFormCtrl().getName() == subformName)
               {
                  foundtask = subTask;
               }
            }
         }
         if (foundtask != null)
         {
            TaskDescription t = new TaskDescription() { TaskId = foundtask.getTaskTag() };
            t.Names = new List<string>();
            foreach (var control in foundtask.getForm().CtrlTab.GetControls(c => IsInputType(c.Type)))
            {
               t.Names.Add(control.UniqueWebId);
            }
            JavaScriptSerializer serializer = new JavaScriptSerializer();            
            result = serializer.Serialize(t);
         }
         return result;
            
      }

      bool IsInputType(MgControlType type)
      {
         switch (type)
         {
            case MgControlType.CTRL_TYPE_CHECKBOX:
            case MgControlType.CTRL_TYPE_TEXT:
            case MgControlType.CTRL_TYPE_COMBO:
            case MgControlType.CTRL_TYPE_LIST:
            case MgControlType.CTRL_TYPE_RADIO:
            case MgControlType.CTRL_TYPE_RICH_EDIT:
               return true;
            default:
               return false;
         }
      }


      public void TaskFinishedAngularInitialization(string taskId)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         //TODO: not always 0
         MGData mgd = mgDataTab.getMGData(0);
         List<Task> tasks = mgDataTab.GetTasks(t => t.getTaskTag().Equals(taskId));
         if (tasks.Count > 0)
         {
            MgForm form = (MgForm)tasks[0].getForm();
            //TODO: need to move this tho another thread with event
            form.InitializationFinished = true;
            form.RefreshUI();
            form.RefreshTableUI();
         }
      }


      // KEYBOARD EVENTS CONSTANTS
      internal readonly KeyboardItem KBI_DOWN;
      internal readonly KeyboardItem KBI_UP;

      protected internal const int MAIN_REFRESHED_NO = 0;
      protected internal const int MAIN_REFRESHED_YES = 1;
      protected internal const int MAIN_REFRESHED_RESETED = 1;

      protected internal const String CMD_LINE_INI = "/ini=";
      protected internal const String SPAWN_AHEAD = "/spawnahead";
      protected internal const String START_HIDDEN = "/hidden";
      internal const String ENCODE = "/NumericEncode=";
      internal const String DECODE = "/NumericDecode=";
      const String STARTED_FROM_STUDIO = "StartedFromStudio";

      /// <summary> Singleton members
      /// 1. DON'T change ORDER of calling the constructors in the singleton objects!
      /// 2. Must be created BEFORE the ClientManager class members
      /// </summary>
      internal RuntimeContext RuntimeCtx { get; private set; }
      internal EventsManager EventsManager { get; private set; }

      private readonly Environment _environment;
      private readonly LanguageData _languageData;
      private readonly KeyboardMappingTable _keybMapTab;
      private readonly EnvParamsTable _envParamsTable;
      private readonly MgArrayList _unframedCmds; // client commands which do not belong to any mgdata yet
      private readonly MgArrayList _CommandsExecutedAfterTaskStarted; // client commands which are to be executed after task started.
      private readonly GlobalParams _globalParams;
      private readonly UserRights _userRights;
      private readonly DbhRealIdxInfo dbhRealIdxInfoTab;

      // include all information of the local application
      internal LocalManager LocalManager { get; private set; }

      // globals for randomize function of NUMBER
      private int _cmdFrame;

      private Thread _workerThread;
      // save Current processed control for the current event processing

      private readonly TasksTable _lastFocusedTasks; //array of tasks in focus per window
      internal long LastActionTime { get; set; } // time of last user action for IDLE function
      internal MgControl ReturnToCtrl { get; set; }
      private bool _idleTimerStarted;
      private readonly Stack _modalWinsStack;

      internal bool MoveByTab { get; private set; }
      private MgControl _lastOverCtrl;
      private MgControl _lastOutCtrl;

      internal int TasksNotStartedCount { get; set; }

      //this member is used to identify the cases in which a radio button was click//we are not 
      //interested in all the scenarios in which a radio was clicked but only in those when the 
      //clicked radio is in a new record and there is control verification with verify error in the 
      //previous record that prevents use from leaving it. In such cases IE changes the value of 
      //the radio (although it should remain the same) and those we need to refresh that control 
      //display. See QCR # 6144
      private readonly ArrayList _variableChangeEvts; //for Variable Change event execution

      private readonly TableCacheManager _tableCacheManager;
      internal CreatedFormVector CreatedForms { get; private set; }
      private bool _inIncrementalLocate;
      private bool _delayInProgress; //true if delay is in progress
      internal int StartProgLevel { get; private set; } //recursive level of "startProg" invocations

      private readonly int _varChangeDnObjectCollectionKey = -1;
      //DNObjectsCollectionEntry that keeps reference to field's prev object, until received in VarChnge Hdlr
      private readonly List<int> _tempDnObjectCollectionKeys = new List<int>(); // holds list of temp keys in objecTable

      internal bool IsHidden { get; private set; }  // Is this RC instance hidden? (applicable only for RC mobile)

      /// <summary>
      /// When true, requests are scrambled and responses are unscrambled.
      /// </summary>
      private bool _shouldScrambleAndUnscrambleMessages;
      public bool ShouldScrambleAndUnscrambleMessages
      {
         get
         {
            Logger.Instance.WriteServerMessagesToLog(String.Format("ShouldScrambleAndUnscrambleMessages.Get: {0}", _shouldScrambleAndUnscrambleMessages));
            return _shouldScrambleAndUnscrambleMessages;
         }
         set
         {
            _shouldScrambleAndUnscrambleMessages = value;
            Logger.Instance.WriteServerMessagesToLog(String.Format("ShouldScrambleAndUnscrambleMessages.Set: {0}", _shouldScrambleAndUnscrambleMessages));
         }
      }

      // <summary>return true iff the current client was started from the studio (F7)</summary>
      internal static bool StartedFromStudio { get; private set; } // true iff the current client was started from the studio (F7)

      // <summary>current executing assembly version</summary>
      internal String ClientVersion { get; private set; } // current executing assembly version

      // services containing functionality that is different between connected and offline states.
      private ServicesManager _servicesManager = new ServicesManager();

      //Errors to be Written in server log file.
      public string ErrorToBeWrittenInServerLog { get; set; }

      /// <summary> 
      /// Should the control, set on the event, be ignored. "true" when the forms are disabled due to the 
      /// worker thread being busy.
      /// </summary>
      internal bool IgnoreControl { get { return !GUIMain.getInstance().FormsEnabled; } }

#if !PocketPC
      internal static Boolean IsMobile = false;
#else
      internal static Boolean IsMobile = true;

      // Object used to block execution if launched with /hidden
      private Object _waitHidden = null;

      internal Object getWaitHiddenObject()
      {
         if (_waitHidden == null)
            _waitHidden = new Object();

         return _waitHidden;
      }
#endif

      /// <summary> Indicates if the startup program is Offline or not </summary>
      internal bool HostsOfflineApplication
      {
         get
         {
            MGData startupMgData = MGDataCollection.Instance.StartupMgData;
            return ((LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork) ||
                     (startupMgData != null &&
                     startupMgData.getFirstTask() != null &&
                     startupMgData.getFirstTask().IsOffline));
         }
      }

      /// <summary> Indicates the execution stage of the application. </summary>
      internal ApplicationExecutionStage ApplicationExecutionStage { get; set; }

      /// <summary> get singleton Environment object</summary>
      internal Environment getEnvironment()
      {
         return _environment;
      }

      /// <summary> Returns language data</summary>
      internal LanguageData getLanguageData()
      {
         return _languageData;
      }

      /// <summary> Returns the keyboard mapping</summary>
      internal KeyboardMappingTable getKbdMap()
      {
         return _keybMapTab;
      }

      /// <summary> get singleton Environment variables object</summary>
      internal EnvParamsTable getEnvParamsTable()
      {
         return _envParamsTable;
      }

      /// <summary> get singleton global parameters object</summary>
      internal GlobalParams getGlobalParamsTable()
      {
         return _globalParams;
      }

      /// <summary> get commands which are not in a frame</summary>
      protected internal MgArrayList getUnframedCmds()
      {
         return _unframedCmds;
      }

      /// <summary> add 1 to cmdFrame counter</summary>
      protected internal void add1toCmdFrame()
      {
         _cmdFrame++;
      }

      /// <summary> get cmdFrame counter</summary>
      protected internal int getCmdFrame()
      {
         return _cmdFrame;
      }

      /// <summary> adds a command to the list of commands which are not attached to any window yet</summary>
      protected internal void addUnframedCmd(IClientCommand cmd)
      {
         _unframedCmds.Add(cmd);
      }

      /// <summary> adds a command to the list of commands which are to be executed after task started.</summary>
      protected internal void addCommandsExecutedAfterTaskStarted(IClientCommand cmd)
      {
         _CommandsExecutedAfterTaskStarted.Add(cmd);
      }

      /// <summary> get commands which are to be executed after task started</summary>
      protected internal MgArrayList getCommandsExecutedAfterTaskStarted()
      {
         return _CommandsExecutedAfterTaskStarted;
      }

      /// <summary> remove a command from the list</summary>
      protected internal void removeCommandsExecutedAfterTaskStarted(IClientCommand cmd)
      {
         _CommandsExecutedAfterTaskStarted.Remove(cmd);
      }

      /// <summary> check if the return-to-control points to an existing task.</summary>
      internal bool validReturnToCtrl()
      {
         if (ReturnToCtrl != null && ((Task)ReturnToCtrl.getForm().getTask()).isAborting())
            return false;

         return true;
      }

      /// <summary>parse the log level from a string value</summary>
      /// <param name="strLogLevel">string value of the log level</param>
      /// <returns>log level</returns>
      internal Logger.LogLevels parseLogLevel(string strLogLevel)
      {
         Logger.LogLevels logLevel = Logger.LogLevels.None;

         if (!string.IsNullOrEmpty(strLogLevel))
         {
            if (strLogLevel.StartsWith("SERVER", StringComparison.CurrentCultureIgnoreCase))
               logLevel = (strLogLevel.EndsWith("#") ? Logger.LogLevels.ServerMessages : Logger.LogLevels.Server); // only requests to the server
            else if (strLogLevel.StartsWith("S", StringComparison.CurrentCultureIgnoreCase) ||
                     strLogLevel.StartsWith("Q", StringComparison.CurrentCultureIgnoreCase))
               logLevel = Logger.LogLevels.Support; // Support/QC
            else if (strLogLevel.StartsWith("G", StringComparison.CurrentCultureIgnoreCase))
               logLevel = Logger.LogLevels.Gui;
            else if (strLogLevel.StartsWith("D", StringComparison.CurrentCultureIgnoreCase))
               logLevel = Logger.LogLevels.Development;
            else if (strLogLevel.StartsWith("B", StringComparison.CurrentCultureIgnoreCase))
               logLevel = Logger.LogLevels.Basic;
         }

         return logLevel;
      }

      /// <summary>parse a response (from the Server) and execute commands if returned within the response.</summary>
      /// <param name="response">(XML formatted) response.</param>
      /// <param name="currMgdID">number of window in global array</param>
      /// <param name="openingTaskDetails">additional information of opening task</param>
      /// <param name="res">result to be read from command returned witin the response.</param>
      protected internal void ProcessResponse(String response, int currMgdID, OpeningTaskDetails openingTaskDetails, IResultValue res)
      {
         Logger.Instance.WriteDevToLog("<-- ProcessResponse started -->");

         long startTime = Misc.getSystemMilliseconds();
         if (response == null || response.Trim().Length == 0)
            return;

         MGDataCollection mgDataTab = MGDataCollection.Instance;
         mgDataTab.currMgdID = currMgdID;

         RuntimeCtx.Parser.push(); // allow recursive parsing
         RuntimeCtx.Parser.setXMLdata(response);
         RuntimeCtx.Parser.setCurrIndex(0);

         MGData mgd = mgDataTab.getCurrMGData();
         mgd.fillData(openingTaskDetails);

         // free memory
         RuntimeCtx.Parser.setXMLdata(null);
         RuntimeCtx.Parser.pop();

         Logger.Instance.WriteDevToLog("<-- ProcessResponse finished --> (" + (Misc.getSystemMilliseconds() - startTime) + ")");

         // execute the commands that were returned in the response
         mgd.CmdsToClient.Execute(res);

         ProcessRecovery();
      }

      /// <summary>
      /// processRecovery
      /// </summary>
      /// <param name="mgDataTab"></param>
      public void ProcessRecovery()
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         // QCR #771012 - process data error recovery only after the parsing process is over.
         EventsManager.pushNewExecStacks();
         mgDataTab.processRecovery();
         EventsManager.popNewExecStacks();
      }


      /// <summary>
      /// execute component's main program 
      /// </summary>
      /// <param name="callByDestSubForm"></param>
      /// <param name="moveToFirstControl"></param>
      /// <param name="argList"></param>
      /// <param name="returnValField"></param>
      /// <returns>the non interactive task if any that executed</returns>
      public Task ExecuteComponentMainPrograms(bool callByDestSubForm, bool moveToFirstControl, ArgumentsList argList, Field returnValField)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;         
         Task currentNonInteractiveTask = null;
         Task nonInteractiveTaskAlreadyExecuted = null;

         int maxCtlIdx = mgDataTab.getMGData(0).getMaxCtlIdx();

         for (int ctlIdx = 1; ctlIdx <= maxCtlIdx; ctlIdx++)
         {
            Task componentMainProgram = mgDataTab.GetMainProgByCtlIdx(ctlIdx);
            if (componentMainProgram != null)
            {
               ExecuteMainProgram(ctlIdx, callByDestSubForm, moveToFirstControl, argList, returnValField, ref currentNonInteractiveTask, ref nonInteractiveTaskAlreadyExecuted);
            }
         }

         return nonInteractiveTaskAlreadyExecuted;
      }

     /// <summary>
     /// execute main program of CtlIndex 
     /// </summary>
     /// <param name="CtlIndex"></param>
     /// <param name="callByDestSubForm"></param>
     /// <param name="moveToFirstControl"></param>
     /// <param name="argList"></param>
     /// <param name="returnValField"></param>
     /// <param name="currentNonInteractiveTask"></param>
     /// <param name="nonInteractiveTaskAlreadyExecuted"></param>
      private static void ExecuteMainProgram(int CtlIndex, bool callByDestSubForm, bool moveToFirstControl, ArgumentsList argList, Field returnValField, ref Task currentNonInteractiveTask, ref Task nonInteractiveTaskAlreadyExecuted)
      {
         Task mainProgramTask = MGDataCollection.Instance.GetMainProgByCtlIdx(CtlIndex);

         Logger.Instance.WriteDevToLog(String.Format("  ClientManager.startProg: ctlIdx={0}, task={1}", CtlIndex, mainProgramTask));
         currentNonInteractiveTask = (Task)mainProgramTask.Start(moveToFirstControl, argList, returnValField, callByDestSubForm);
         if (nonInteractiveTaskAlreadyExecuted == null)
            nonInteractiveTaskAlreadyExecuted = currentNonInteractiveTask;
         else if (currentNonInteractiveTask != null)
            Debug.Assert(false, "more than 1 non interactive tasks in startProg");
      }

      private void InitializeTasks(bool callByDestSubForm, bool moveToFirstControl, ArgumentsList argList, Field returnValField, ref Task nonInteractiveTaskAlreadyExecuted)
      {
        
         
         Task currentNonInteractiveTask = null;
         
         EventsManager.setAllowEvents(EventsManager.EventsAllowedType.ALL);
         // just to allow any action to happen in the start prog (changed for 'open subform').

         Logger.Instance.WriteDevToLog("Start \"startProg\"");

         StartProgLevel++;
         EventsManager.setStopExecution(false); // QCR #769734 - nothing can stop a task from starting up.

         // Fixed bug #283647 execute components main programs -  in the server we execute the same see ini.cpp method execute_main_progs()
         nonInteractiveTaskAlreadyExecuted = ExecuteComponentMainPrograms(callByDestSubForm, moveToFirstControl, argList, returnValField);

         MGDataCollection mgDataTab = MGDataCollection.Instance;
         // execute internal main programs
         ClientManager.ExecuteMainProgram(0, callByDestSubForm, moveToFirstControl, argList, returnValField, ref currentNonInteractiveTask, ref nonInteractiveTaskAlreadyExecuted);

         if (!_idleTimerStarted)
         {
            GUIManager.Instance.startTimer(mgDataTab.getMGData(0), _environment.getIdleTime(0), true);
            _idleTimerStarted = true;
         }

#if PocketPC
	         // Topic #13 (MAGIC version 1.8\SP1 for WIN) RC mobile - improve performance: 
	         if (IsHidden)
	         {
	            RemoteCommandsProcessor.GetInstance().Hibernate();
	            Monitor.Enter(getWaitHiddenObject());
	            RemoteCommandsProcessor.GetInstance().Resume();
	         }
#endif

         OpenForms(callByDestSubForm);
      }

      /// <summary>
      /// start the execution of the program and set focus to the first control.
      /// </summary>
      protected internal Task StartProgram(bool callByDestSubForm, bool moveToFirstControl, ArgumentsList argList, Field returnValField, AutoResetEvent initializationMonitor)
      {
         Task nonInteractiveTaskAlreadyExecuted = null;
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         bool orgStopExecution = EventsManager.GetStopExecutionFlag();
         EventsManager.EventsAllowedType savedAllowEvents = EventsManager.getAllowEvents();
         InitializeTasks(callByDestSubForm, moveToFirstControl, argList, returnValField, ref nonInteractiveTaskAlreadyExecuted);
         initializationMonitor.Set();
         //TODO: will we have enough time to register from second thread ?

         DoFirstRecordCycle();
         MoveToFirstControls(callByDestSubForm);

         // Handle the events only when all the tasks and their forms are opened.
         if (StartProgLevel == 1)
         {
            bool resetAllowEvents = false;
            bool canExecuteEvents = true;

            // set the within non interactive since the handleEvents might try to execute an event 
            // that was raised in the 1st rec pref (added in moveToFirstcontrols).
            if (nonInteractiveTaskAlreadyExecuted != null && nonInteractiveTaskAlreadyExecuted.isStarted())
            {
               resetAllowEvents = true;
               EventsManager.setNonInteractiveAllowEvents(nonInteractiveTaskAlreadyExecuted.isAllowEvents(), nonInteractiveTaskAlreadyExecuted);

               // non interactive task with allow events = NO is about to run. 
               // don't try to execute the events before it starts because they will be lost. (#138493)
               canExecuteEvents = nonInteractiveTaskAlreadyExecuted.isAllowEvents();
             }

            // if the it is a subform with visibility expression evaluated to TRUE, 
            // so after return from the server do not execute events from the queue.
            // These events will be executed from the 'startProg' of the parent task.
            if (getLastFocusedTask() != null && canExecuteEvents)
            {
               EventsManager.pushNewExecStacks();
               EventsManager.handleEvents(mgDataTab.getCurrMGData(), 0);
               EventsManager.popNewExecStacks();
            }

            // restore. just to be on the safe side. it will be set again once in the correct eventsLoop.
            if (resetAllowEvents)
               EventsManager.setAllowEvents(savedAllowEvents);
         }

         StartProgLevel--;

         Logger.Instance.WriteDevToLog("End \"startProg\"");

         // If originally we were instructed to stop events execution, then restore this
         if (orgStopExecution && !EventsManager.GetStopExecutionFlag())
            EventsManager.setStopExecution(orgStopExecution);

         return nonInteractiveTaskAlreadyExecuted;
      }

      /// <summary> Open all the form that are pending in CreatedForms vector. </summary>
      internal void OpenForms(bool callByDestSubForm)
      {
         CreatedFormVector createdForms = ClientManager.Instance.CreatedForms;
         MgFormBase outMostForm = null;
         bool canOpen = false;

         if (createdForms.Count() > 0)
         {
            for (int i = 0; i < createdForms.Count(); )
            {
               MgForm mgForm = createdForms.get(i);
               canOpen = false;

               // Open a subform only if its container form is Opened.
               // isCurrentStartProgLevel() doesn't go well for subforms (QCR #919119).
               if (mgForm.isSubForm())
               {
                  if (mgForm.getSubFormCtrl().getForm().Opened)
                     canOpen = true;
               }
               else
               {
                  if (((Task)mgForm.getTask()).isCurrentStartProgLevel())
                     canOpen = true;
               }

               if (canOpen)
               {
                  Manager.OpenForm(mgForm);
                  Manager.DoFirstRefreshTable(mgForm);
               }

               if (mgForm.Opened)
               {
                  if (callByDestSubForm && mgForm.isSubForm())
                     outMostForm = mgForm.getTopMostForm();

                  createdForms.remove(mgForm);
               }
               else
                  i++;
            }

            // non interactive cannot be subform anyways, don't ask about it.
            if (callByDestSubForm && outMostForm != null)
               outMostForm.executeLayout();
         }
      }

      /// <summary>
      /// For every mgdata perform move to first control to the form 
      /// that we marked before with MoveToFirstControl flag
      /// </summary>
      private void MoveToFirstControls(bool callByDestSubForm)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         for (int j = 0; j < mgDataTab.getSize(); j++)
         {
            MGData mgd = mgDataTab.getMGData(j);
            if (mgd == null || mgd.IsAborting)
               continue;

            int length = mgd.getTasksCount();
            for (int i = 0; i < length; i++)
            {
               Task task = mgd.getTask(i);
               MgForm form = (MgForm)task.getForm();
               if (task.isCurrentStartProgLevel() && form != null && form.MovedToFirstControl)
               {
                  form.MovedToFirstControl = false;
                  if (form.IsMDIFrame)
                     EventsManager.HandleNonParkableControls(task);
                  else
                  {
                     EventsManager.pushNewExecStacks();
                     form.moveToFirstCtrl(false, !callByDestSubForm);
                     EventsManager.popNewExecStacks();
                     break;
                  }
               }
            }
         }
      }

      /// <summary> build the XML string of the data that should be sent to the server</summary>
      /// <param name="serializeTasks">if true, tasks in the current execution will also be serialized.</param>
      protected internal String PrepareRequest(Boolean serializeTasks)
      {
         StringBuilder xmlBuf = new StringBuilder();
         xmlBuf.Append(XMLConstants.MG_TAG_OPEN);

         // the client no longer sends (and empties) the event queue to the server
         // since the server has nothing useful to do with the client pending events
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         if (mgDataTab.getCurrMGData() != null)
         {
            String xmlUnscrambledString = null;
            StringBuilder xmlUnscrambled = new StringBuilder();
            mgDataTab.buildXML(xmlUnscrambled, serializeTasks);

            if (ShouldScrambleAndUnscrambleMessages)
            {
               xmlUnscrambledString = xmlUnscrambled.ToString();
               xmlBuf.Append(Scrambler.Scramble(xmlUnscrambledString));
            }
            else
            {
               xmlBuf.Append(xmlUnscrambled);
               if (Logger.Instance.LogLevel >= Logger.LogLevels.ServerMessages)
                  xmlUnscrambledString = xmlUnscrambled.ToString();
            }
         }

         xmlBuf.Append("</" + XMLConstants.MG_TAG_XML + XMLConstants.TAG_CLOSE);

         return xmlBuf.ToString();
      }

      /// <summary>exit the client gracefully</summary>
      internal void Exit()
      {
         AbortWorkThread();
         Manager.Exit();
      }

      /// <summary>
      /// start the work thread
      /// </summary>
      private void StartWorkThread()
      {
         _workerThread = new Thread(WorkThreadExecution);
         _workerThread.Start();
      }

      /// <summary>
      /// abort the work thread
      /// </summary>
      private void AbortWorkThread()
      {
         _workerThread.Abort();
      }

      /// <summary>
      /// the work thread's entry method
      /// </summary>
      internal void WorkThreadExecution()
      {
         Misc.MarkWorkThread();

         Debug.Assert(_executionProps.Count > 0);

         if (   Logger.Instance.ShouldLog()
             || ClientManager.Instance.getDisplayStatisticInfo())
         {
            Logger.Instance.WriteToLog("", true);
            Logger.Instance.WriteToLog("Started on " + DateTimeUtils.ToString(DateTime.Now, XMLConstants.ERROR_LOG_DATE_FORMAT), true);
            ClientManager.Instance.WriteHeaderToLog();
            WriteExecutionPropertiesToLog();
         }

         StateMachine csm = ConnectionStateMachineBuilder.Build();
         ClientManager.Instance.SetService(typeof(IConnectionStateManager), csm);

         try
         {
            // in addition to the tasks' forms, the forms user state controls authentication dialogs, therefore it must be loaded first.
           LoadInitialFormsUserState();

            if (CommandsProcessorManager.StartSession())
            {
               CommandsProcessorBase server = CommandsProcessorManager.GetCommandsProcessor();

#if !PocketPC
               // Should allow processing keyboard messages when the worker thread is busy? Initialize the value from magic.ini after the response, which includes 
               // the environment, was processed
               if (String.Equals("B", _envParamsTable.get("[MAGIC_SPECIALS]SpecialKeyboardBuffering"), StringComparison.CurrentCultureIgnoreCase))
                  GUIMain.getInstance().AllowkeyboardBuffering();
               else if (String.Equals("E", _envParamsTable.get("[MAGIC_SPECIALS]SpecialKeyboardBuffering"), StringComparison.CurrentCultureIgnoreCase))
                  GUIMain.getInstance().AllowExtendedKeyboardBuffering();
               if (_environment.GetSpecialSwipeFlickeringRemoval())
                  Commands.addAsync(CommandType.SET_ENV_SPECIAL_SWIPE_FLICKERING_REMOVAL, null, 0, true);
#endif

               /// write and re-read the forms user state file - this time from the project's folder 
               ///  (for backwards compatibility - prior to the RSA authentication dialog changes, for which the forms user state is loaded above).
             //  LoadProjectFormsUserState();

               MGDataCollection mgDataTab = MGDataCollection.Instance;
               MGData startupMgData = mgDataTab.StartupMgData;
               mgDataTab.currMgdID = startupMgData.GetId();

               ApplicationExecutionStage = ApplicationExecutionStage.Executing;

               do
               {
                  Task nonInteractiveTask = null;
                  if (!startupMgData.IsAborting)
                     nonInteractiveTask = StartProgram(false, false, server.BuildArgList(), null , InitializationMonitor);

                  if (nonInteractiveTask == null)
                  {
                     // the recent network statistics are displayed in the status bar of the topmost form.
                     // the method 'UpdateRecentNetworkActivitiesTooltip' depends on the topmost form being already opened and focused.
                     // therefore, it's called initially here (as well as later after each subsequent request).
                     if (server is RemoteCommandsProcessor)
                        ((RemoteCommandsProcessor)server).UpdateRecentNetworkActivitiesTooltip();

                     

                     EventsManager.EventsLoop(startupMgData);
                  }
                  else
                     EventsManager.NonInteractiveEventsLoop(startupMgData, nonInteractiveTask);
               }
               // in 'regular' exit, not from menu, the startup will be aborting.
               while (!startupMgData.IsAborting);

               // set the "end of work" flag only for the main window
               EventsManager.setEndOfWork(true);
            }
         }
         catch (ApplicationException e)
         {
            bool isNoResultError = false;
            if (e is ServerError && ((ServerError)e).GetCode() == ServerError.INF_NO_RESULT)
               isNoResultError = true;
#if PocketPC
            // If a context hibernated and can't resume, just exit this process and start a new one
            if (e is RemoteCommandsProcessor.InternalError)
            {
               Logger.Instance.WriteWarningToLog(getMessageString(MsgInterface.STR_WARN_UNLOAD_TIMEOUT) + " (" +
                                                 (double)_environment.getContextUnloadTimeout() / (10 * 60 * 60) + " " +
                                                 getMessageString(MsgInterface.STR_HOURS) + ")");
            }
            else
#endif
            if (!(e.InnerException is HttpClient.CancelledAuthenticationException || isNoResultError))
               ProcessAbortingError(e);
            EventsManager.setEndOfWork(true);
         }
         finally
         {
            // write the forms user state file
            if (!FormUserState.GetInstance().IsDisabled)
               FormUserState.GetInstance().Write();

            // exit the gui thread.
            Manager.Exit();
         }
      }

      /// <summary>
      /// </summary>
      private void LoadInitialFormsUserState()
      {
         //if (StartedFromStudio)
            // set isDisabled if started app started from studio (why??)
            FormUserState.GetInstance().IsDisabled = true;
         //else
            // read the forms user state file
         //   FormUserState.GetInstance().Read(ConstInterface.FORMS_USER_STATE_FILE_NAME);
      }

      /// <summary>
      /// write and re-read the forms user state file - this time from the project's folder 
      ///  (for backwards compatibility - prior to the RSA authentication dialog changes, for which the forms user state is loaded above).
      /// </summary>
      private void LoadProjectFormsUserState()
      {
#if !PocketPC
         if (!FormUserState.GetInstance().IsDisabled)
         {
            FormUserState.GetInstance().Write();
            FormUserState.DeleteInstance();
            FormUserState.GetInstance().Read(ConstInterface.FORMS_USER_STATE_FILE_NAME);
         }
#endif
      }

      /// <summary>
      /// Checks if restart session is allowed. Session restart is not allowed for InvalidSourcesException, 
      /// DataSourceConversionFailedException and few instances of ServerError.
      /// </summary>
      /// <param name="ex"></param>
      /// <returns></returns>
      private bool AllowRestartSession(Exception ex)
      {
         bool allowRestart = true;

         // don't allow restart:
         // 1. if Server error is RQMRI_ERR_LIMITED_LICENSE_CS, as this is a license restriction aimed to switch to Studio Mode.
         // 2. if Server error is RQMRI_ERR_INCOMPATIBLE_RIACLIENT, as there is no point in restart.
         // 3. if application sources are invalid or failed to convert data sources
         var serverError = ex as ServerError;
         if ((serverError != null && (serverError.GetCode() == ServerError.ERR_LIMITED_LICENSE_CS || serverError.GetCode() == ServerError.ERR_INCOMPATIBLE_RIACLIENT)) ||
             (ex is InvalidSourcesException || ex is DataSourceConversionFailedException))
            allowRestart = false;

         return allowRestart;
      }

      /// <summary>Execute the TS of the MPs and close the context in the Server.
      /// If the client ever connected to the server (i.e. the context was created on the server) and 
      /// the network is currently available, execute the TS and close the context on the server. Otherwise, 
      /// execute the TS on client (terminating the context on client is irrelevant).
      /// </summary>
      internal void CloseMainProgramsForOfflineApplication()
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Debug.Assert(ApplicationExecutionStage == ApplicationExecutionStage.Terminating);

         if (HostsOfflineApplication)
         {
            bool executedOnServer = false;
            IClientCommand unloadCommand = CommandFactory.CreateUnloadCommand();

            //If the context was created on the server, try to execute the MPs on the server.
            if (RuntimeCtx.ContextID != RemoteCommandsProcessor.RC_NO_CONTEXT_ID)
            {
               ICommunicationsFailureHandler communicationsFailureHandler = HttpManager.GetInstance().GetCommunicationsFailureHandler();

               try
               {
                  //While terminating an offline application, do not show the "want to retry" message on network error.
                  HttpManager.GetInstance().SetCommunicationsFailureHandler((ICommunicationsFailureHandler)(new SilentCommunicationsFailureHandler()));

                  mgDataTab.StartupMgData.CmdsToServer.Add(unloadCommand);
                  RemoteCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.ONLY_COMMANDS);
                  executedOnServer = true;
               }
               catch (ServerError)
               {
               }
               finally
               {
                  //reset the communication failure handler.
                  HttpManager.GetInstance().SetCommunicationsFailureHandler(communicationsFailureHandler);
               }
            }

            //If the server could not be connected, execute the MPs on the client.
            //When the TS of MP is executed on client, "want to retry" message on network error should appear if there is a server operation.
            //So, TS should be executed only after the GlobalCommunicationsFailureHandler is set. Hence it cannot be done from the catch block.
            if (!executedOnServer)
            {
               //Set the ApplicationStage to Executing till it executes the MP TS.
               ApplicationExecutionStage = ApplicationExecutionStage.Executing;

               mgDataTab.StartupMgData.CmdsToServer.Add(unloadCommand);
               LocalCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.ONLY_COMMANDS);

               ApplicationExecutionStage = ApplicationExecutionStage.Terminating;
            }
         }
      }

      /// <summary>
      /// If a server error occurred, display a generic error message and instead of the message from the server.
      /// True by default.
      /// </summary>
      /// <returns></returns>
      internal bool ShouldDisplayGenericError()
      {
         bool shouldDisplayGenericError = true;

         String displayGenericError = _executionProps.getProperty(ConstInterface.DISPLAY_GENERIC_ERROR);

         if (displayGenericError != null && displayGenericError.Equals("N", StringComparison.OrdinalIgnoreCase))
            shouldDisplayGenericError = false;

         return shouldDisplayGenericError;
      }

      /// <summary>handle fatal error situation</summary>
      /// <param name = "ex">the Error object to use</param>
      internal void ProcessAbortingError(ApplicationException ex)
      {
         String eMsg;

         if (ex is ServerError)
            eMsg = ((ServerError)ex).GetMessage();
         else
         {
            eMsg = (ex.Message != null
                        ? ex.Message
                        : ex.GetType().FullName);
         }

         if (ex.InnerException != null)
            Logger.Instance.WriteExceptionToLog(ex.InnerException, ex.Message);         
         else
            // don't write server error to log when the log level is basic
            if (!(ex is ServerError) || Logger.Instance.LogLevel != Logger.LogLevels.Basic)
               Logger.Instance.WriteExceptionToLog(eMsg);

#if PocketPC
         eMsg = eMsg.Substring(0, Math.Min(eMsg.Length, 250));
#endif

         int rcMbox = Styles.MSGBOX_RESULT_NO;
         bool sendUnloadToServer = (RuntimeCtx.ContextID != RemoteCommandsProcessor.RC_NO_CONTEXT_ID); // unload the context in the server only if a context was created during the current session

         if (!Instance.IsHidden)
            Manager.Beep();
         MGDataCollection.Instance.currMgdID = 0;

         String title = Instance.getMessageString(MsgInterface.BRKTAB_STR_ERROR);
         int style = Styles.MSGBOX_ICON_ERROR;

#if PocketPC
         if (IsHidden)
         {
            // If we are hidden, don't display the message box and don't instruct the new spawned client to spawn a new instance upon its exit. 
            Logger.Instance.WriteErrorToLog(eMsg);
            ClientManager.Instance.setRespawnOnExit(false);

            // in case the client fails during reactivation (e.g. I-am-alive messages were not sent to the server) the application will be silently re-launched with the UI (as if the client wasn't started hidden).
            if (ex is ServerError && ((ServerError)ex).GetCode() == ServerError.ERR_CTX_NOT_FOUND)
               rcMbox = Styles.MSGBOX_RESULT_YES;
         }
         else
#endif
         {
            if (AllowRestartSession(ex))
            {
               eMsg += (OSEnvironment.EolSeq + OSEnvironment.EolSeq +
                        ClientManager.Instance.getMessageString(MsgInterface.STR_RESTART_SESSION));
               style |= Styles.MSGBOX_BUTTON_YES_NO;
            }

            if (ex.Message.StartsWith("<HTML", StringComparison.CurrentCultureIgnoreCase))
               processHTMLContent(ex.Message);
            else
               rcMbox = Commands.messageBox(null, title, eMsg, style);
         }

         if (ex.InnerException != null && ex.InnerException.GetType().FullName == "System.Net.WebException")
         {
            // In case if we are able to connect to the server i.e. status is ConnectionFailure
            // then there is no point in sending unload command, because it will never succeed and
            // we will in infinite loop. 
            var webEx = (WebException)ex.InnerException;
            if (webEx.Status == WebExceptionStatus.ConnectFailure ||
                webEx.Status == WebExceptionStatus.ProtocolError)
               sendUnloadToServer = false;
         }

         if (ex is ServerError)
         {
            // If the context was not found, no point in trying to unload it
            if (((ServerError)ex).GetCode() == ServerError.ERR_CTX_NOT_FOUND)
               sendUnloadToServer = false;
         }
         
         if (sendUnloadToServer)
         {
            // tell the server to close the runtime.
            // If we terminate RTE and then close RC window then also we will be 
            // Processing Abort error in that case server is already closed so catch the exception.
            try
            {
               IClientCommand cmd = CommandFactory.CreateUnloadCommand();
               MGDataCollection.Instance.getMGData(0).CmdsToServer.Add(cmd);
               CommandsProcessorManager.GetCommandsProcessor().Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
            }
            catch (ApplicationException unloadException)
            {
               Logger.Instance.WriteExceptionToLog(unloadException);
            }
         }

         // close the gui thread.
         GUIManager.Instance.abort();

         //---------------------------------------------------------
         // re-spawn the client
         //---------------------------------------------------------
         if (rcMbox == Styles.MSGBOX_RESULT_YES)
         {
            // clean the context group before re-launching. Keep it if it was received from parent process.
            if (CommandsProcessorManager.SessionStatus == CommandsProcessorManager.SessionStatusEnum.Remote)
               if (_executionProps[ConstInterface.CTX_GROUP] == ClientManager.Instance.RuntimeCtx.ContextID.ToString())
                  _executionProps.Remove(ConstInterface.CTX_GROUP);

            // If run from studio by F7, we don't want the session statistics displayed by the spawned process
            if (StartedFromStudio)
               _executionProps[ConstInterface.DISPLAY_STATISTIC_INFORMATION] = "N";

            StringBuilder executionPropertiesXML = new StringBuilder();
            _executionProps.storeToXML(executionPropertiesXML);
            String executionPropertiesEncoded = HttpUtility.UrlEncode(executionPropertiesXML.ToString(), Encoding.UTF8);
            Process.StartCurrentExecutable(executionPropertiesEncoded);
         }
      }

      /// <summary>Shows HTML content inside a browser window with dimensions same as that of current task form</summary>
      /// <param name = "htmlContent"></param>
      internal void processHTMLContent(String htmlContent)
      {
         Task currentTask = ClientManager.Instance.EventsManager.getCurrTask();
         if (currentTask != null)
         {
            var currentFormRect = new MgRectangle(0, 0, 0, 0);
            Commands.getBounds(currentTask.getForm(), currentFormRect);
            // set the size of browserControl form, same as current active form
            GUIManager.Instance.BrowserWindow.FormRect = currentFormRect;
         }
         GUIManager.Instance.showContent(htmlContent);
      }

      /// <summary>
      /// Write Execution properties to log.
      /// </summary>
      internal void WriteExecutionPropertiesToLog()
      {
         if (Logger.Instance.ShouldLogServerRelatedMessages()
             || ClientManager.Instance.getDisplayStatisticInfo())
         {
            Logger.Instance.WriteToLog("-----------------------------------------------------------------------------", true);
#if !PocketPC
            if (IsClickOnceDeployment())
            {
               Logger.Instance.WriteToLog("Publish HTML containing the execution properties:", true);
               Logger.Instance.WriteToLog(GetPublishHtmlUrl(), true);
               Logger.Instance.WriteToLog("-----------------------------------------------------------------------------", true);
            }
#endif
            //Write heading
            Logger.Instance.WriteToLog("Execution Properties:", true);

            //Write property and its value.
            foreach (String key in _executionProps.AllKeys)
            {
               string line = "";
               if (key.Equals(ConstInterface.MG_TAG_PASSWORD, StringComparison.CurrentCultureIgnoreCase))
                  line = string.Format("{0,-35} : ***", key);
               else
               {

                  String value = _executionProps[key];
                  if (key.Equals(ConstInterface.REQ_PRG_NAME, StringComparison.CurrentCultureIgnoreCase))
                  {
                     //If Public name is not given to the RC program then in log file write it's description as well as it's fictive internal name.
                     //If Public name is given in log write it's internal name.
                     string prgDescription = getPrgDescription();
                     if (!String.IsNullOrEmpty(prgDescription) && value != prgDescription)
                        value = string.Format("\"{0}\" (\"{1}\")", prgDescription, value);
                  }
                  else if (key.Equals(HttpClientConsts.HTTP_COMPRESSION_LEVEL, StringComparison.CurrentCultureIgnoreCase))
                  {
                     // Compression level is decided depending on some parameter (ref setCompressionLevel)
                     // so compression level in execution.properties and compression level used can be different.
                     value = string.Format("{0} (actual: {1})", value, HttpManager.GetInstance().GetCompressionLvl());
                  }
                  line = string.Format("{0,-35} : {1}", key, value);
               }
               Logger.Instance.WriteToLog(line, true);
            }
            Logger.Instance.WriteToLog("-----------------------------------------------------------------------------", true);
         }

      }

      /// <summary> return the cursor to the given control, beeps and ignore the events raised by returning
      /// the focus (used when leaving a control is not allowed)
      /// </summary>
      /// <param name="ctrl">the control to return to</param>
      protected internal void returnToCtrl(MgControl ctrl)
      {
         Manager.SetFocus(ctrl, -1);
         Logger.Instance.WriteWarningToLog("ClientManager.returnToCtrl");
      }

      /// <summary>
      /// compare tasks by depth
      /// </summary>
      /// <param name="task1"></param>
      /// <param name="task2"></param>
      /// <returns></returns>
      internal static int CompareByDepth(Task task1, Task task2)
      {
         int depth1 = task1.getTaskDepth(false);
         int depth2 = task2.getTaskDepth(false);
         return depth2.CompareTo(depth1);
      }

      /// <summary>
      /// get list of tasks that contain reference to the object defined in objectTableKey
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal List<Task> getTasksByObject(Object obj)
      {
         var tasks = new List<Task>();

         if (obj != null)
         {
            MGDataCollection mgDataTab = MGDataCollection.Instance;
            mgDataTab.startTasksIteration();
            Task task;
            while ((task = mgDataTab.getNextTask()) != null)
            {
               var fldTab = (FieldsTable)task.DataView.GetFieldsTab();
               for (int i = 0; i < fldTab.getSize(); i++)
               {
                  var fld = (Field)fldTab.getField(i);
                  if (fld.hasDotNetObject(obj))
                  {
                     tasks.Add(task);
                     break;
                  }
               }
            }
         }
         else
            Debug.Assert(false);
         return tasks;
      }

      /// <summary></summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMgCtrl"></param>
      internal void checkAndCloseWide(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl)
      {
         // time of last user action for IDLE function
         // DOWN or UP invoked on a SELECT/RADIO control
         MgForm form = null;
         MgControl mgControl = null;
         //    else if ((currKbdItem.equals(KBI_DOWN) || currKbdItem.equals(KBI_UP)) && (ctrl != null && ctrl.isSelectionCtrl()))
         //       return;                

         // set the action to the kbd item. 
         if (guiMgCtrl is MgControl)
            mgControl = (MgControl)guiMgCtrl;

         if (guiMgForm is MgForm)
            form = (MgForm)guiMgForm;

         MgForm topmostForm = (form != null
                               ? (MgForm)form.getTopMostForm()
                               : (mgControl != null ? (MgForm)mgControl.getTopMostForm() : null));
         if (topmostForm != null)
         {
            //when wide is open (form.wideIsOpen() ), and clicking on the none wide control , close the wide
            if (topmostForm.wideIsOpen() && (mgControl != null && !mgControl.isWideControl()))
               _guiEventsProcessor.processWide(((MgForm)topmostForm.getTopMostForm()).getWideControl());
         }
      }

      /// <summary> get last task that is in focus</summary>
      /// <returns></returns>
      internal Task getLastFocusedTask()
      {
         return getLastFocusedTask(MGDataCollection.Instance.currMgdID);
      }

      /// <summary> get last task that is in focus</summary>
      /// <returns></returns>
      internal Task getLastFocusedTask(int mgdID)
      {
         return _lastFocusedTasks.getTask(mgdID);
      }

      /// <summary> 
      /// get Current processing control
      /// </summary>
      internal MgControl getCurrCtrl()
      {
         RunTimeEvent rtEvnt = EventsManager.getLastRtEvent();
         return (rtEvnt != null && rtEvnt.getTask() != null && rtEvnt.getTask().GetContextTask() != null
                     ? GUIManager.getLastFocusedControl(((Task)rtEvnt.getTask().GetContextTask()).getMgdID())
                     : GUIManager.getLastFocusedControl());
      }

      /// <summary>
      /// get Current processing task</summary>
      /// </summary>
      internal Task getCurrTask()
      {
         RunTimeEvent rtEvnt = EventsManager.getLastRtEvent();
         return (rtEvnt != null && rtEvnt.getTask() != null && rtEvnt.getTask().GetContextTask() != null
                     ? getLastFocusedTask(((Task)rtEvnt.getTask().GetContextTask()).getMgdID())
                     : getLastFocusedTask());
      }

      /// <summary> set last focused control for current window</summary>
      /// <param name="last">focused control for current window</param>
      internal void setLastFocusedTask(ITask iTask)
      {
         Task currFocusedTask = ClientManager.Instance.getLastFocusedTask();
         if (currFocusedTask != null && currFocusedTask != iTask)
         {
            MgFormBase oldForm = currFocusedTask.getForm();
            if (oldForm != null)
               oldForm.SetActiveHighlightRowState(false);
         }
         Task task = (Task)iTask;
         int currMgdID = task.getMgdID();
         _lastFocusedTasks.setTaskAt(task, currMgdID);
         if (task.getForm() != null)
            task.getForm().SetActiveHighlightRowState(true);
      }

      /// <summary> set last focused control for current window</summary>
      /// <param name="ctrl">focused control for current window</param>
      internal void setLastFocusedCtrl(MgControl ctrl)
      {
         GUIManager.setLastFocusedControl((Task)ctrl.getForm().getTask(), ctrl);
      }

      /// <summary> push a modal window number to the stack</summary>
      /// <param name="winNum">the number of the modal window</param>
      protected internal void modalWinsStackPush(int winNum)
      {
         _modalWinsStack.Push(winNum);
      }

      /// <summary> pop a modal window number from the stack</summary>
      protected internal int modalWinsStackPop()
      {
         Object obj = null;
         int result = -1;

         if (!(_modalWinsStack.Count == 0))
            obj = _modalWinsStack.Pop();
         if (obj != null)
            result = ((Int32)obj);
         return result;
      }

      /// <summary> peek a modal window number from the stack</summary>
      protected internal int modalWinsStackPeek()
      {
         Object obj = null;
         int result = -1;

         if (!(_modalWinsStack.Count == 0))
         {
            obj = _modalWinsStack.Peek();
         }
         if (obj != null)
            result = ((Int32)obj);
         return result;
      }

      /// <summary>  return message string form Language XML according to index</summary>
      /// <param name="msgId">message index</param>
      internal String getMessageString(string msgId)
      {
         return getLanguageData().getConstMessage(msgId);
      }

      /// <summary> memory leak fix: clean dangaling refrences to closed mgData's</summary>
      /// <param name="index">the mgData index/// </param>
      protected internal void clean(int index)
      {
         GUIManager.deleteLastFocusedControlAt(index);

         if (_lastOutCtrl != null && ((Task)_lastOutCtrl.getForm().getTask()).getMGData().GetId() == index)
            _lastOutCtrl = null;

         if (_lastOverCtrl != null && ((Task)_lastOverCtrl.getForm().getTask()).getMGData().GetId() == index)
            _lastOverCtrl = null;

         if (_lastFocusedTasks != null)
            _lastFocusedTasks.setTaskAt(null, index);
      }

      /// <summary></summary>
      /// <returns> the empty choice list object</returns>
      internal TableCacheManager getTableCacheManager()
      {
         return _tableCacheManager;
      }

      /// <summary>
      ///   set MoveByTab value
      /// </summary>
      internal bool setMoveByTab(bool val)
      {
         bool currVal = MoveByTab;
         MoveByTab = val;
         return currVal;
      }

      /// <summary> extract the vector which contains the names of the months, as specified by the 
      /// language CAB
      /// </summary>
      // if it's the first time then access the language CAB and take the values
      //Check if hebrew values exist. If they do - then use them. Otherwise, use the international values
      //TODO: till date_heb.cpp is not converted, hebrew is disabled
      //if (true || names != null && names.Length == 0)
      //cut the string into separate values
      /// </returns>
      internal ArrayList getPendingVarChangeEvents()
      {
         return _variableChangeEvts;
      }

      private static ClientManager _instance;
      internal static ClientManager Instance
      {
         get
         {
            if (_instance == null)
            {
               lock (typeof(ClientManager))
               {
                  if (_instance == null)
                     _instance = new ClientManager();
               }
            }
            return _instance;
         }

         private set
         {
            _instance = value;
         }
      }

      private Uri _serverUrl;
      private String _server;
      private String _protocol;
      private String _httpReq;
      private String _appName;
      private String _prgName;        // the public name of the program for which the current session is started.
      private String _prgDescription; // the name of the program (passed from studio and is used to identify the executed program in case it doesn't have a public name).
      private String _prgArgs;
      private String _localId;
      private String _debugClient;
      private String _clientCachePath; //absolute path of the cache folder from execution properties.
      private readonly MgProperties _executionProps;
      private readonly string _globalUniqueSessionId;
      internal string LoadedExecutionPropertiesFileName { get; private set; } // the full name (including the path) of the execution properties file, only if the client was started with /INI=, otherwise null.

      /// <summary> Private CTOR as part of making this class a singleton</summary>
      private ClientManager()
      {
         ApplicationExecutionStage = ApplicationExecutionStage.Initializing;

         _servicesManager = new ServicesManager();

         KBI_DOWN = new KeyboardItem(GuiConstants.KEY_DOWN, Modifiers.MODIFIER_NONE);
         KBI_UP = new KeyboardItem(GuiConstants.KEY_UP, Modifiers.MODIFIER_NONE);

         RuntimeCtx = new RuntimeContext(RemoteCommandsProcessor.RC_NO_CONTEXT_ID) { Parser = new XmlParser() };

         EventsManager = new EventsManager();
         _environment = new Environment();

         _languageData = new LanguageData();
         _keybMapTab = new KeyboardMappingTable();
         _envParamsTable = new EnvParamsTable();
         _globalParams = new GlobalParams();

         LocalManager = new LocalManager();

         _unframedCmds = new MgArrayList(); // client commands which do not belong to any mgdata yet
         _CommandsExecutedAfterTaskStarted = new MgArrayList();

         _cmdFrame = 0;

         _lastFocusedTasks = new TasksTable();
         LastActionTime = Misc.getSystemMilliseconds(); // time of last user action for IDLE function
         ReturnToCtrl = null;

         _idleTimerStarted = false;

         _modalWinsStack = new Stack();

         ShouldScrambleAndUnscrambleMessages = true;

         TasksNotStartedCount = 0;

         _variableChangeEvts = new ArrayList();

         _tableCacheManager = new TableCacheManager();

         CreatedForms = new CreatedFormVector();

         _executionProps = new MgProperties();

         _inIncrementalLocate = false;

         _userRights = new UserRights();
         
         dbhRealIdxInfoTab = new DbhRealIdxInfo();

         _globalUniqueSessionId = UniqueIDUtils.GetUniqueMachineID() + "_" + System.Diagnostics.Process.GetCurrentProcess().Id;

         _varChangeDnObjectCollectionKey = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);

         ClientVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(4);

         Instance = this; // required by GuiEventsProcessor.
         _guiEventsProcessor = new GuiEventsProcessor();

         RegisterDelegates();
      }

      /// <summary> Returns the single instance of the ClientManager class.
      /// In case this instance does not exist it creates that instance.
      /// </summary>
      /// <returns> ClientManager instance</returns>
      private void InitGuiManager()
      {
         // init statics
         Manager.Environment   = _environment;
         Manager.EventsManager = this.EventsManager;
         Manager.MGDataTable   = MGDataCollection.Instance;

         // init members
         Manager.UseWindowsXPThemes           = getUseWindowsXPThemes();
         Manager.CanOpenInternalConsoleWindow = string.IsNullOrEmpty(getInternalLogFile()); // show the console debug window only if file logging is disabled.
         Manager.DefaultServerName            = getServer();
         Manager.DefaultProtocol              = getProtocol();

         SetService(typeof(TaskServiceBase), new RemoteTaskService());
      }

      AutoResetEvent InitializationMonitor  = new AutoResetEvent(false);


      /// <summary> Entry point to the Web Client </summary>
      /// <param name="args">array of command line arguments</param>
#if PocketPC
        [MTAThread]
#else
      [STAThread]
#endif
      internal static void Main(String[] args)
      {
         Misc.MarkGuiThread();
         

#if !PocketPC
         // In case there is proxy authentication and application is accessed through ClickOnce
         // then we create authentication dialog box before guiStartup. Hence Compatible text
         // rendering must be set before that
         //Application.SetCompatibleTextRenderingDefault(false);
#endif

         if (args.Length > 0 && args[0].StartsWith(ENCODE, StringComparison.OrdinalIgnoreCase))
         {
            ClientManager.Instance.InitGuiManager();
            NumUtil.EncodeDecode(args, NumUtilOperation.ENCODE);
         }
         else if (args.Length > 0 && args[0].StartsWith(DECODE, StringComparison.OrdinalIgnoreCase))
         {
            ClientManager.Instance.InitGuiManager();
            NumUtil.EncodeDecode(args, NumUtilOperation.DECODE);
         }
#if !PocketPC
         // If the first argument is "/spawnahead" then the request has come from F7
         // So call RCLauncher that will wait till it get execution.prperties and will
         // come back here for actual execution.
         else if (args.Length > 0 && args[0].StartsWith(SPAWN_AHEAD))
         {
            StartedFromStudio = true;
            StudioLauncher.Init(args);
         }
#endif
         else
            StartExecution(args);

#if PocketPC
         if (Instance.getRespawnOnExit())
            Instance.spawnHidden();
#endif
         Logger.Instance.Flush();
         Instance.InitializationMonitor.WaitOne();
      }

      /// <summary>Start the execution - load execution properties, initialize the session, execute the requested program, ..</summary>
      /// <param name="args">array of command line argument</param>
      internal static void StartExecution(String[] args)
      {
         // (as soon as possible, to cover the entire session)
         Statistics.RegisterDelegates();

#if PocketPC
         UseExecutionPropertiesFromDefaultFolder(ref args);
#else
         if (IsClickOnceDeployment())
         {
            // If it is ClickOnce deployment then read the argument from publish.html
            // where execution properties are stored as xml island.
            String clickOnceExecutionProperties = GetClickOnceExecutionProperties();
            if (clickOnceExecutionProperties != null)
               args = new[] { clickOnceExecutionProperties };
         }
         else
         {
            UseExecutionPropertiesFromDefaultFolder(ref args);

#if DEBUG
            if (args.Length == 0)
            {
               // use an execution properties file from the last studio invoked (F7) session
               Microsoft.Win32.RegistryKey richClientInformation =
                  Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\MSE\Magic xpa\3.3\Rich Client Information");
               if (richClientInformation != null)
               {
                  String executionPropertiesFile = (String)richClientInformation.GetValue(ConstInterface.EXECUTION_PROPERTIES_FILE_NAME);
                  if (executionPropertiesFile != null)
                     args = new[] { CMD_LINE_INI + executionPropertiesFile };
               }
            }
#endif // DEBUG
         } // !IsClickOnceDeployment()

#if DEBUG
         Trace.Listeners.Clear();
         Trace.Listeners.Add(new DefaultTraceListener());
#endif // DEBUG
#endif // !PocketPC
         // search for an execution.properties file in the current folder

         if (args.Length > 0)
            ClientManager.Instance.LoadExecutionProps(args);

         ClientManager.Instance.InitGuiManager();

         // the gui layer must be start up after LoadExecutionProps, since it relies on 'UseWindowsXPThemes' execution property
         guiStartup();

         if (ClientManager.Instance._executionProps.Count == 0)
         {
            string executionPropertiesFileName = ClientManager.Instance.LoadedExecutionPropertiesFileName;
            if (executionPropertiesFileName != null  && executionPropertiesFileName.Length > 0)
            {
               string assemblyName = Assembly.GetExecutingAssembly().ManifestModule.Name;
               Commands.messageBox(null, assemblyName,
                                   string.Format("{0} not found or invalid file", executionPropertiesFileName),
                                   Styles.MSGBOX_BUTTON_OK);
            }

         }
         else
         {
#if !PocketPC
            // When client manager is started in parallel execution, the caller will collect error stream till get gets client manager is ok
            Console.Error.WriteLine("<CLIENTMANAGER>OK</CLIENTMANGER>");
#endif
            HttpManager.GetInstance().HttpCommunicationTimeoutMS = ClientManager.Instance.GetFirstHttpRequestTimeout();

            ClientManager.Instance.EventsManager.Init();

            Logger.Instance.Initialize(ClientManager.Instance.getInternalLogFile(), ClientManager.Instance.parseLogLevel(ClientManager.Instance.getInternalLogLevel()), ClientManager.Instance.getInternalLogSync(), ClientManager.StartedFromStudio);

#if PocketPC
            string[] saWM5Files = Directory.GetFiles(@"\Windows\", "System.SR*.gac");
            string[] saWM6Files = Directory.GetFiles(@"\Windows\", "NETCFv35.Messages*.gac");
                
            if (saWM5Files.Length == 0 && saWM6Files.Length == 0)
            {
               Logger.Instance.WriteWarningToLog("*****************************************************************************************************************************************************************************************");
               Logger.Instance.WriteWarningToLog("System_SR_<culture>_[wm].cab is not installed.");
               Logger.Instance.WriteWarningToLog("In order to be able to read system-level messages and error descriptions, please install the above cabinet corresponding to the device culture.");
               Logger.Instance.WriteWarningToLog("Otherwise 'An error message cannot be displayed because an optional resource assembly containing it cannot be found' string will be presented at all times.");
               Logger.Instance.WriteWarningToLog(@"System_SR_<culture>.cab (for WM5) or System_SR_<culture>_wm.cab (for WM6) can be found in c:\Program Files\Microsoft.NET\SDK\CompactFramework\v<version#>\WindowsCE\Diagnostics\ folder");
               Logger.Instance.WriteWarningToLog("NOTE: if you are working with a locale other than English, installing System_SR cab for that locale will also install assemblies for the ENU (English, US) locale.");
               Logger.Instance.WriteWarningToLog("* NOTE2: cabinets with suffix 'wm' in their name should be installed on WM6.x, those without it are for WM5.x.");
               Logger.Instance.WriteWarningToLog("*****************************************************************************************************************************************************************************************");
            }
#endif

        
            MGData mgd = new MGData(0, null, false);
            MGDataCollection.Instance.addMGData(mgd, 0, true);

            ClientManager.Instance.StartWorkThread();

            GUIManager.Instance.messageLoop();
                  
            if (Logger.Instance.ShouldLog())
                Logger.Instance.WriteToLog(OSEnvironment.EolSeq + "Ended on " + DateTimeUtils.ToString(DateTime.Now, XMLConstants.ERROR_LOG_DATE_FORMAT) +
                            OSEnvironment.EolSeq + OSEnvironment.EolSeq, false);

#if !PocketPC
            unipaas.gui.low.Console.unhookStandards();
#endif
            
         }//args.Length > 0 && args[0] != null
      }

      /// <summary>
      ///  search for an execution.properties file in the current folder
      /// </summary>
      /// <param name="args">in/out - in case there're no arguments, and there's an execution properties in the current folder</param>
      private static void UseExecutionPropertiesFromDefaultFolder(ref String[] args)
      {
         if (args.Length == 0)
         {
            string executionProperties = getExecutionPropertiesFileName();
            if (HandleFiles.isExists(executionProperties))
               args = new[] { CMD_LINE_INI + executionProperties };
         }
      }

      /// <summary>
      ///   write header details to the log
      /// <param name="cm"></param>
      internal void WriteHeaderToLog()
      {
         Logger.Instance.WriteToLog(string.Format("Client version : {0}", ClientVersion), true);
#if !PocketPC
         Logger.Instance.WriteToLog(string.Format("Client computer: {0}", OSEnvironment.get("COMPUTERNAME")), true);
#endif
      }

      /// <summary>startup the GUI layer</summary>
      internal static void guiStartup()
      {
         if (!Manager.IsInitialized())
         {
            String startupImageFileName = null;

#if PocketPC
            startupImageFileName = ClientManager.Instance.getMobileStartupImage();

            // The startup image in execution.properties will be name of the file
            // the installer will copy the file to the same place where the .exe is copied
            // so append executing assembly's path to the startup image read from execution.properties.
            if (!String.IsNullOrEmpty(startupImageFileName))
               startupImageFileName = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + "/" + startupImageFileName;
#else
            if (!ClientManager.Instance.WasSpawnedForParallelProgram())
               startupImageFileName = ConstInterface.STARTUP_IMAGE_FILENAME;
#endif
            if (ShouldDPIAware())
               Manager.SetProcessDPIAwareness();
            Manager.Init(startupImageFileName);
         }
      }

      // execute a fictive HTTP request in order to automatically detect settings (Internet Options)
      internal static bool PreSpawnHTTP(string serverUrl)
      {
         return RemoteCommandsProcessor.EchoWebRequester(serverUrl, true);
      }

      /// <summary> returns the URL needed to connect to the server</summary>
      /// <returns> server URL</returns>
      internal Uri getServerURL()
      {
         if (_serverUrl == null)
         {
            // construct the URL string
            string s = getProtocol() + "://" + getServer() + "/" + getHttpReq();

            // create the URL object
            try
            {
               _serverUrl = new Uri(s);

               // Unlike java.net.URL, System.Uri doesn't throw Exception if the protocol is 
               // not one of the expected values i.e. http, https, file. So assert.
               Debug.Assert(_serverUrl.Scheme.Equals("http")
                            || _serverUrl.Scheme.Equals("https")
                            || _serverUrl.Scheme.Equals("file"));
            }
            catch (UriFormatException ex)
            {
               Logger.Instance.WriteExceptionToLog(ex);
            }
         }
         return _serverUrl;
      }

      /// <summary> Loads the execution properties file that contains information about the server,
      /// application and program to execute
      /// </summary>
      /// <param name="fileName">the name of the file that contains the execution properties</param>
      internal void LoadExecutionProps(String[] args)
      {
         Encoding utf8Encoding = Encoding.UTF8;

         for (int i = 0; i < args.Length; i++)
         {
            if ((args[i].StartsWith("%3c") || args[i].StartsWith("<")))
            // '<'
            {
               try
               {
                  String xml = HttpUtility.UrlDecode(args[0], utf8Encoding);
                  _executionProps.loadFromXML(xml, utf8Encoding);
               }
               catch (Exception ex)
               {
                  Logger.Instance.WriteExceptionToLog(ex);
               }
            }
            // load execution properties - could be more than one file which means that
            // if a property appeared in more than one file then the value of its latest occurence
            // will be the final value
#if !PocketPC
            // QCR # 781654. args[i].ToLowerInvariant() used here because It returns a copy of string converted to lowercase using the 
            // casing rules of the invariant culture. Otherwise when regional settings is other than English, args[i].ToLower may 
            // fail for some languages e.g Turkish.

            else if (args[i].ToLowerInvariant().StartsWith(CMD_LINE_INI))
#else
            else if (args[i].ToLower().StartsWith(CMD_LINE_INI))
#endif
            {
               String fileName = args[i].Substring(CMD_LINE_INI.Length);

#if !PocketPC
               // "/ini=<..>execution.properties|StartedFromStudio"
               if (fileName.Contains("|"))
               {
                  string[] fileNamePairs = fileName.Split('|');
                  Debug.Assert(fileNamePairs.Length == 2 && fileNamePairs[1].StartsWith(STARTED_FROM_STUDIO));

                  fileName = fileNamePairs[0];
                  StartedFromStudio = true;
               }

               LoadedExecutionPropertiesFileName = fileName;
#endif

               try
               {
                  string XMLdata = HandleFiles.readToString(fileName, utf8Encoding);
                  _executionProps.loadFromXML(XMLdata, utf8Encoding);
               }
               catch (Exception ex)
               {
                  Logger.Instance.WriteExceptionToLog(ex);
               }
            }
#if PocketPC
                else if (args[i].Equals(START_HIDDEN, StringComparison.OrdinalIgnoreCase))
                {
                    setRespawnOnExit(true);
                }
#endif
            else if (args[i].Equals(STARTED_FROM_STUDIO))
               StartedFromStudio = true;
            else
            {
               const string Delim = "=";
               // split command line argument to property (name, value)
               String[] keyValuePair = args[i].Split(Delim.ToCharArray());
               if (keyValuePair.Length == 2)
                  _executionProps[keyValuePair[0]] = keyValuePair[1];
               //else
               //   Logger.Instance.WriteErrorToLog(String.Format("Invalid command line argument: {0}", args[i]));
            }
         }
#if PocketPC
            // if respawnOnExit was passed in the execution props, we run as hidden
            if (getRespawnOnExit())
                IsHidden = true;

            if (_executionProps[ConstInterface.AUTO_START] != null &&
                _executionProps[ConstInterface.AUTO_START].Equals("Y"))
            {
                // Create the link file for startup
                makeAutoStartLinkFile();
                // set the execution property to launch a new instance when this one exits
                setRespawnOnExit(true);
                // turn off the property so it won't be passed to next instance
                _executionProps[ConstInterface.AUTO_START] = "N";
            }
#endif
      }

      /// <summary>
      /// return a copy of the current execution properties
      /// </summary>
      /// <returns>a copy of the current execution properties</returns>
      internal MgProperties copyExecutionProps()
      {
         return _executionProps.Clone();
      }

      /// <param name="username"></param>
      internal void setUsername(String username)
      {
         _executionProps[ConstInterface.MG_TAG_USERNAME] = username;
      }

      /// <param name="password"></param>
      internal void setPassword(String password)
      {
         _executionProps[ConstInterface.MG_TAG_PASSWORD] = password;
      }

      /// <returns></returns>
      internal void setSkipAuthenticationDialog(bool value)
      {
         _executionProps[ConstInterface.REQ_SKIP_AUTHENTICATION] = value.ToString();
      }

      /// <param name="string"></param>
      internal void setGlobalParams(String val)
      {
         if (val != null)
            _executionProps[ConstInterface.MG_TAG_GLOBALPARAMS] = val;
         else
            _executionProps.Remove(ConstInterface.MG_TAG_GLOBALPARAMS);
      }

      internal bool getLogClientSequenceForActivityMonitor()
      {
         bool bRet = false;
         String logSeqProp = _executionProps.getProperty(ConstInterface.LOG_CLIENTSEQUENCE_FOR_ACTIVITY_MONITOR);

         if (logSeqProp != null && String.Compare(logSeqProp, "Y", true) == 0)
            bRet = true;

         return bRet;
      }

      internal String getGlobalParams()
      {
         return _executionProps.getProperty(ConstInterface.MG_TAG_GLOBALPARAMS);
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getProtocol()
      {
         if (_protocol == null)
            _protocol = _executionProps.getProperty(ConstInterface.MG_TAG_PROTOCOL);
         return _protocol;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getServer()
      {
         if (_server == null)
            _server = _executionProps.getProperty(ConstInterface.SERVER);
         return _server;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getHttpReq()
      {
         if (_httpReq == null)
         {
            _httpReq = _executionProps.getProperty(ConstInterface.REQUESTER);

            // remove leading slashes
            if (_httpReq != null)
            {
               while (_httpReq.StartsWith("/"))
                  _httpReq = _httpReq.Substring(1);

               // remove trailing slashes
               while (_httpReq.EndsWith("/"))
                  _httpReq = _httpReq.Substring(0, _httpReq.Length - 1);
            }
         }
         return _httpReq;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getAppName()
      {
         if (_appName == null)
            _appName = _executionProps.getProperty(ConstInterface.REQ_APP_NAME);
         return _appName;
      }

      /// <summary></summary>
      /// <returns>the public name of the program for which the current session is started</returns>
      internal String getPrgName()
      {
         if (_prgName == null)
            _prgName = _executionProps.getProperty(ConstInterface.REQ_PRG_NAME);
         return _prgName;
      }

      /// <summary></summary>
      /// <returns>the name of the program (passed from studio and is used to identify the executed program in case it doesn't have a public name)</returns>
      internal String getPrgDescription()
      {
         if (_prgDescription == null)
            _prgDescription = _executionProps.getProperty(ConstInterface.REQ_PRG_DESCRIPTION);
         return _prgDescription;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getPrgArgs()
      {
         if (_prgArgs == null)
            _prgArgs = _executionProps.getProperty(ConstInterface.REQ_ARGS);
         return _prgArgs;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getLocalID()
      {
         if (_localId == null)
            _localId = _executionProps.getProperty(ConstInterface.LOCALID);
         return _localId;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getDebugClient()
      {
         if (_debugClient == null)
         {
            string value = _executionProps.getProperty(ConstInterface.DEBUGCLIENT);
            if (value != null && value.Equals("true"))
               _debugClient = "DEBUG_CLIENT=1";
         }
         return _debugClient;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal bool getDisplayStatisticInfo()
      {
         bool bRet = false;
         String logSeqProp = _executionProps.getProperty(ConstInterface.DISPLAY_STATISTIC_INFORMATION);

         if (logSeqProp != null && String.Compare(logSeqProp, "Y", true) == 0)
            bRet = true;

         return bRet;
      }

      /// <summary>
      /// Returns value of execution property 'ConnectOnStartup'
      /// </summary>
      /// <returns></returns>
      internal bool GetConnectOnStartup()
      {
         String connectOnStartup = _executionProps.getProperty(ConstInterface.CONNECT_ON_STARTUP);
         if (connectOnStartup != null && String.Compare(connectOnStartup, "N", true) == 0)
            return false;

         return true;
      }

      /// <summary>Returns absolute client cache path specified in execution properties</summary>
      /// <returns></returns>
      internal String GetClientCachePath()
      {
         if (_clientCachePath == null)
         {
            String clientCacheFolder = _executionProps.getProperty(ConstInterface.CLIENT_CACHE_PATH);

            if (!string.IsNullOrEmpty(clientCacheFolder))
            {
               try
               {
                  // Return absolute path
                  if (!Path.IsPathRooted(clientCacheFolder))
                     clientCacheFolder = Path.GetFullPath(clientCacheFolder);

                  if (!Directory.Exists(clientCacheFolder))
                  {
                     Directory.CreateDirectory(clientCacheFolder);
                  }

                  _clientCachePath = clientCacheFolder;
               }
               catch (Exception ex)
               {
                  _clientCachePath = "";
                  Misc.WriteStackTrace(ex, Console.Error);

               }
            }
         }

         return (_clientCachePath);
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getInternalLogFile()
      {
         string internalLogFileName = _executionProps[ConstInterface.INTERNAL_LOG_FILE];
#if !PocketPC
         //Logger.Instance.ShouldLog() cannot be used  instead of 
         //ClientManager.Instance.parseLogLevel(ClientManager.Instance.getInternalLogLevel()) != Logger.LogLevels.None.
         //Because ShouldLog() function uses Logger.LogLevel and which is initialized in Logger.Initialize() function. 
         //Before calling getInternalLogFile(), Logger.LogLevel will not be initialized. 
         if (String.IsNullOrEmpty(internalLogFileName) && ClientManager.Instance.parseLogLevel(ClientManager.Instance.getInternalLogLevel()) != Logger.LogLevels.None
             && !Debugger.IsAttached)
         {
            // MgxpaRIA.exe --> MgxpaRIA_YYYY_MM_DD.log, on the desktop
            string MgxpaRIA = Assembly.GetEntryAssembly().ManifestModule.Name;
            internalLogFileName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory).ToString() +
                                  string.Format("\\{0}_{1:D2}_{2:D2}_{3:D2}.log", MgxpaRIA.Remove(MgxpaRIA.Length - ".exe".Length), DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
         }
#else
         if (!String.IsNullOrEmpty(internalLogFileName) && !internalLogFileName.StartsWith("\\"))
            // rc.log --> <assembly path>\rc.log
            internalLogFileName = OSEnvironment.getAssemblyFolder() + internalLogFileName;
#endif
         return internalLogFileName;
      }

      /// <summary></summary>
      internal void setInternalLogFile(string value)
      {
         _executionProps[ConstInterface.INTERNAL_LOG_FILE] = value;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getInternalLogLevel()
      {
         return _executionProps[ConstInterface.INTERNAL_LOG_LEVEL];
      }

      internal void setInternalLogLevel(string value)
      {
         _executionProps[ConstInterface.INTERNAL_LOG_LEVEL] = value;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal String getInternalLogSync()
      {
         return _executionProps[ConstInterface.INTERNAL_LOG_SYNC];
      }
      
      /// <summary></summary>
      /// <returns></returns>
      internal static bool getUseWindowsXPThemes()
      {
         bool UseWindowsXPThemesBOOL = false;

         string UseWindowsXPThemes = Instance._executionProps.getProperty(ConstInterface.XP_THEMES);
         if (UseWindowsXPThemes != null)
            UseWindowsXPThemesBOOL = UseWindowsXPThemes.Equals("Y");

         return UseWindowsXPThemesBOOL;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal static bool ShouldDPIAware()
      {
         bool shouldDPIAware = true;

         string dpiAware = Instance._executionProps.getProperty(ConstInterface.DPI_AWARE);
         if (!string.IsNullOrEmpty(dpiAware))
            shouldDPIAware = !dpiAware.Equals("N");

         return shouldDPIAware;
      }

      internal String getUsername()
      {
         return _executionProps.getProperty(ConstInterface.MG_TAG_USERNAME);
      }

      internal String getPassword()
      {
         return _executionProps.getProperty(ConstInterface.MG_TAG_PASSWORD);
      }

      internal String getCtxGroup()
      {
         return _executionProps.getProperty(ConstInterface.CTX_GROUP);
      }

      internal void setCtxGroup(String CtxGroup)
      {
         _executionProps[ConstInterface.CTX_GROUP] = CtxGroup;
      }

      /// <summary>
      /// GET URL value of the execution property
      /// </summary>
      /// <returns></returns>
      internal Uri getURLFromExecutionProperties(String executionPropName)
      {
         Uri url = null;
         String urlStr = _executionProps[executionPropName];
         if (urlStr != null)
         {
            // if relative, prefix with the 'protocol://server/' from which the rich-client was activated
            if (urlStr.StartsWith("/"))
               urlStr = getProtocol() + "://" + getServer() + urlStr;
            try
            {
               url = new Uri(urlStr);
               // Unlike java.net.URL, System.Uri doesn't throw Exception if the
               // protocol is not one of the expected values i.e. http, https, file. 
               Debug.Assert(url.Scheme.Equals("http")
                            || url.Scheme.Equals("https"));
            }
            catch (UriFormatException ex)
            {
               Logger.Instance.WriteExceptionToLog(ex);
            }
         }
         return url;
      }

      internal bool IsLogonRTL()
      {
         string logonRTL = _executionProps[GuiConstants.STR_LOGON_RTL];

         return (logonRTL != null && logonRTL.CompareTo("Y") == 0) ? true : false;
      }

      internal Uri getLogonWindowIconURL()
      {
         return getURLFromExecutionProperties(GuiConstants.STR_LOGO_WIN_ICON_URL);
      }

      internal Uri getLogonWindowImageURL()
      {
         return getURLFromExecutionProperties(GuiConstants.STR_LOGON_IMAGE_URL);
      }

      internal String getLogonWindowTitle()
      {
         return _executionProps[GuiConstants.STR_LOGON_WIN_TITLE];
      }

      internal String getLogonGroupTitle()
      {
         return _executionProps[GuiConstants.STR_LOGON_GROUP_TITLE];
      }

      internal String getLogonMsgCaption()
      {
         return _executionProps[GuiConstants.STR_LOGON_MSG_CAPTION];
      }

      internal String getLogonUserIdCaption()
      {
         return _executionProps[GuiConstants.STR_LOGON_USER_ID_CAPTION];
      }

      internal String getLogonPasswordCaption()
      {
         return _executionProps[GuiConstants.STR_LOGON_PASS_CAPTION];
      }

      internal String getLogonOKCaption()
      {
         return _executionProps[GuiConstants.STR_LOGON_OK_CAPTION];
      }

      internal String getLogonCancelCaption()
      {
         return _executionProps[GuiConstants.STR_LOGON_CANCEL_CAPTION];
      }

      internal String getLogonChangePasswordCaption()
      {
         return _executionProps[ConstInterface.MG_TAG_CHNG_PASS_CAPTION];
      }

      internal String getChangePasswordWindowTitle()
      {
         return _executionProps[ConstInterface.MG_TAG_CHNG_PASS_WIN_TITLE];
      }

      internal String getChangePasswordOldPasswordCaption()
      {
         return _executionProps[ConstInterface.MG_TAG_CHNG_PASS_OLD_PASS_CAPTION];
      }

      internal String getChangePasswordNewPasswordCaption()
      {
         return _executionProps[ConstInterface.MG_TAG_CHNG_PASS_NEW_PASS_CAPTION];
      }

      internal String getChangePasswordConfirmPasswordCaption()
      {
         return _executionProps[ConstInterface.MG_TAG_CHNG_PASS_CONFIRM_PASS_CAPTION];
      }

      internal String getChangePasswordMismatchMsg()
      {
         return _executionProps[ConstInterface.MG_TAG_CHNG_PASS_MISMATCH_MSG];
      }

      /// <returns></returns>
      internal bool getSkipAuthenticationDialog()
      {
         String prop = _executionProps.getProperty(ConstInterface.REQ_SKIP_AUTHENTICATION);
         return (!String.IsNullOrEmpty(prop) && (prop == true.ToString()));
      }

      /// <returns></returns>
      internal String getEnvVars()
      {
         return _executionProps.getProperty(ConstInterface.ENVVARS);
      }

      /// <summary>
      /// </summary>
      /// <returns>http timeout, in ms</returns>
      internal uint GetFirstHttpRequestTimeout()
      {
         int firstHttpTimeout = Convert.ToInt32(_executionProps.getProperty(ConstInterface.FIRST_HTTP_REQUEST_TIMEOUT));
         return firstHttpTimeout > 0 ? (uint)firstHttpTimeout * 1000 : (uint)HttpManager.DEFAULT_OFFLINE_HTTP_COMMUNICATION_TIMEOUT;
      }

      /// <summary>Current process was spawned for parallel execution</summary>
      internal bool WasSpawnedForParallelProgram()
      {
         String executionPropertyValue = _executionProps[ConstInterface.PARALLEL_EXECUTION];
         return (executionPropertyValue != null && executionPropertyValue.CompareTo("Y") == 0);
      }

      /// <summary> Goes over forms is createdFormVec and performs move to first record cycle control</summary>
      internal void DoFirstRecordCycle()
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         for (int j = 0; j < mgDataTab.getSize(); j++)
         {
            MGData mgd = mgDataTab.getMGData(j);
            if (mgd == null || mgd.IsAborting)
               continue;

            for (int i = 0; i < mgd.getTasksCount(); i++)
            {
               Task task = mgd.getTask(i);
               if (task != null)
               {
                  MgForm form = (MgForm)task.getForm();

                  if (form != null && !task.IsSubForm)
                     form.RecomputeTabbingOrder(true);

                  if (form != null && task.isCurrentStartProgLevel() && !form.IgnoreFirstRecordCycle)
                  {
                     form.IgnoreFirstRecordCycle = true;
                     task.doFirstRecordCycle();
                  }
               }
            }
         }
      }

      /// <summary> Clean flag for Subform record cycle </summary>
      internal void cleanDoSubformPrefixSuffix()
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         for (int j = 0; j < mgDataTab.getSize(); j++)
         {
            MGData mgd = mgDataTab.getMGData(j);
            if (mgd == null || mgd.IsAborting)
               continue;

            int length = mgd.getTasksCount();
            for (int i = 0; i < length; i++)
            {
               Task task = mgd.getTask(i);
               task.DoSubformPrefixSuffix = false;
            }
         }
      }

      /// <summary> Execute Record cycle for all subforms that their flag is on </summary>
      internal void execAllSubformRecordCycle()
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         for (int j = 0; j < mgDataTab.getSize(); j++)
         {
            MGData mgd = mgDataTab.getMGData(j);
            if (mgd == null || mgd.IsAborting)
               continue;

            int length = mgd.getTasksCount();
            for (int i = 0; i < length; i++)
            {
               Task task = mgd.getTask(i);
               if (task.IsSubForm && task.DoSubformPrefixSuffix)
               {
                  // In the record prefix of the current task (commonHandlerBefore) we execute record prefix of all fathers.
                  // But when we return from the server we do not need it. So we prevent father record prefix execution.
                  task.PerformParentRecordPrefix = false;
                  EventsManager.pushNewExecStacks();
                  EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX, true);
                  if (!EventsManager.GetStopExecutionFlag())
                     EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                  EventsManager.popNewExecStacks();
                  task.PerformParentRecordPrefix = true;
               }
            }
         }
      }

      /// <summary>
      /// Add command, execute request and do record cycle for subforms that the server sent their new dataview
      /// </summary>
      /// <param name="cmdsToServer"></param>
      /// <param name="cmdToServer">additional command to the Server</param>
      internal void execRequestWithSubformRecordCycle(CommandsTable cmdsToServer, IClientCommand cmdToServer, Expression exp, Task task)
      {
         cleanDoSubformPrefixSuffix();
         cmdsToServer.Add(cmdToServer);

         CommandsProcessorBase commandsProcessor;
         if (cmdToServer is ExecOperCommand && ((ExecOperCommand)cmdToServer).Operation != null)
            commandsProcessor = CommandsProcessorManager.GetCommandsProcessor(((ExecOperCommand)cmdToServer).Operation);
         else
            commandsProcessor = CommandsProcessorManager.GetCommandsProcessor(task);

         commandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS, CommandsProcessorBase.SessionStage.NORMAL, exp);
         execAllSubformRecordCycle();
      }

      /// <summary> When something in the user's rights has changed either the user has changed, or he 
      /// gained \ lost some rights he had, We need to refresh the form's pulldown menu and context menu, 
      /// and the context menu of all of the controls.
      /// </summary>
      private void UserRights_RightsChanged()
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         Task internalMainProgram = mgDataTab.GetMainProgByCtlIdx(0);
         if (internalMainProgram != null)
            internalMainProgram.resetMenusContent();

         // we need to refresh all the menus, since the rights hash key has changed
         Manager.MenuManager.destroyAndRebuild();
      }

      internal String getRightsHashKey()
      {
         return _userRights.RightsHashKey;
      }

      internal UserRights getUserRights()
      {
         return _userRights;
      }

      internal DataSourceId GetDataSourceId(int taskCtlIdx, int realIdx)
      {
         return dbhRealIdxInfoTab.GetDataSourceId(taskCtlIdx, realIdx);
      }

      /// <summary> true if delay is in progress</summary>
      /// <returns></returns>
      internal bool isDelayInProgress()
      {
         return _delayInProgress;
      }

      internal void setDelayInProgress(bool delayInProgress)
      {
         _delayInProgress = delayInProgress;
      }

      /// <summary></summary>
      internal void fillUserRights()
      {
         int endContext = ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, ClientManager.Instance.RuntimeCtx.Parser.getCurrIndex());

         if (endContext != -1 && endContext < ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().Length)
         {
            // Get the substring of interest
            String tmp = ClientManager.Instance.RuntimeCtx.Parser.getXMLsubstring(endContext);
            // skip to after the "val =" part, and pass it on to user rights function
            tmp = tmp.Substring(tmp.IndexOf(XMLConstants.MG_ATTR_VALUE) + 5);
            tmp = tmp.Remove(tmp.Length - 1, 1);
            _userRights.fillUserRights(tmp);
            ClientManager.Instance.RuntimeCtx.Parser.setCurrIndex2EndOfTag();
         }
      }

      /// <summary></summary>
      internal void fillDbhRealIdxs()
      {
         int endContext = ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, ClientManager.Instance.RuntimeCtx.Parser.getCurrIndex());

         if (endContext != -1 && endContext < ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().Length)
         {
            // Get the substring of interest
            String tmp = ClientManager.Instance.RuntimeCtx.Parser.getXMLsubstring(endContext);
            // skip to after the "val =" part, and pass it on to user rights function
            tmp = tmp.Substring(tmp.IndexOf(XMLConstants.MG_ATTR_VALUE) + 5);
            tmp = tmp.Remove(tmp.Length - 1, 1);
            dbhRealIdxInfoTab.fillDbhRealIdxs(tmp);
            ClientManager.Instance.RuntimeCtx.Parser.setCurrIndex2EndOfTag();
         }
      }

      /// <summary> save the global params (one base64 encoded value) that was passed from the runtime-engine to the rich-client</summary>
      internal void fillGlobalParams()
      {
         String XMLdata = ClientManager.Instance.RuntimeCtx.Parser.getXMLdata();
         int endContext = ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, ClientManager.Instance.RuntimeCtx.Parser.getCurrIndex());

         if (endContext != -1 && endContext < XMLdata.Length)
         {
            // find last position of its tag
            String tag = ClientManager.Instance.RuntimeCtx.Parser.getXMLsubstring(endContext);
            ClientManager.Instance.RuntimeCtx.Parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_GLOBALPARAMS) +
                                        ConstInterface.MG_TAG_GLOBALPARAMS.Length);

            List<String> tokensVector = XmlParser.getTokens(ClientManager.Instance.RuntimeCtx.Parser.getXMLsubstring(endContext),
                                                            XMLConstants.XML_ATTR_DELIM);
            Debug.Assert((tokensVector[0]).Equals(XMLConstants.MG_ATTR_VALUE));
            setGlobalParams(tokensVector[1]);

            endContext = XMLdata.IndexOf(XMLConstants.TAG_OPEN, endContext);
            if (endContext == -1)
               endContext = XMLdata.Length;
            ClientManager.Instance.RuntimeCtx.Parser.setCurrIndex(endContext);
         }
      }

      /// <summary> Save each 'server file path' and its corresponding 'cache retrieval URL', that were passed from the xpa server to the client, into a dictionary:
      ///    key   = server file path
      ///    value = cache retrieval URL
      /// e.g. for:
      ///    server file path = "C:\MyImages\Image1.bmp"
      ///    cache retrieval URL = "/MagicScripts/MGRQISPI.DLL?CTX=&CACHE=C:\MyImages\Image1.bmp|09/05/2007%2012:23:00&ENCRYPTED=N".
      /// <summary>
      internal void fillCacheFilesMap()
      {
         String XMLdata = ClientManager.Instance.RuntimeCtx.Parser.getXMLdata();
         int endContext = XMLdata.IndexOf(XMLConstants.TAG_TERM, ClientManager.Instance.RuntimeCtx.Parser.getCurrIndex());

         if (endContext != -1 && endContext < ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().Length)
         {
            // find last position of its tag
            String tag = ClientManager.Instance.RuntimeCtx.Parser.getXMLsubstring(endContext);
            ClientManager.Instance.RuntimeCtx.Parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_CACHED_FILE) + ConstInterface.MG_TAG_CACHED_FILE.Length);

            List<String> tokensVector = XmlParser.getTokens(ClientManager.Instance.RuntimeCtx.Parser.getXMLsubstring(endContext),
                                                            XMLConstants.XML_ATTR_DELIM);
            // verify token names
            Debug.Assert((tokensVector[0]).Equals(ConstInterface.MG_ATTR_FILE_PATH));
            Debug.Assert((tokensVector[2]).Equals(ConstInterface.MG_ATTR_CACHE_URL));

            // get token values
            String fileName = tokensVector[1];
            String fileUrl = tokensVector[3];

            if (!String.IsNullOrEmpty((fileUrl).Trim())) // fill cachedFilesMap only if cache-Url is present
            {
               RemoteCommandsProcessor server = RemoteCommandsProcessor.GetInstance();

               // save each 'server file path' and its corresponding 'cache retrieval URL', that were passed from the xpa server to the client, into a dictionary:
               //    key   = server file path
               //    value = cache retrieval URL
               // e.g. for:
               //    server file path = "C:\MyImages\Image1.bmp"
               //    cache retrieval URL = "/MagicScripts/MGRQISPI.DLL?CTX=&CACHE=C:\MyImages\Image1.bmp|09/05/2007%2012:23:00&ENCRYPTED=N".
               server.ServerFilesToClientFiles[fileName] = fileUrl;
               
               // In case of multiple files, client needs the list of filenames to prepare the request to get contents.
               // So add filename to ServerFileNames list.
               if (server.ServerFileToClientHelper != null && server.ServerFileToClientHelper.RequestedForFolderOrWildcard)
                  server.ServerFileToClientHelper.ServerFileNames.Add(fileName);
            }

            endContext = XMLdata.IndexOf(XMLConstants.TAG_OPEN, endContext);
            if (endContext == -1)
               endContext = XMLdata.Length;
            ClientManager.Instance.RuntimeCtx.Parser.setCurrIndex(endContext);
         }
      }

      /// <summary></summary>
      internal static bool IsClickOnceDeployment()
      {
#if PocketPC
         return false;
#else
         return (ApplicationDeployment.IsNetworkDeployed);
#endif
      }

#if !PocketPC
      /// <summary>
      ///   For ClickOnce deployment, we can not pass the augments directly, 
      ///   so the execution properties will be embedded in the publish.html 
      ///   as XML data island. This function will read these execution properties and 
      ///   return as single argument.
      /// </summary>
      /// <returns>String containing execution properties of current application</returns>
      private static string GetClickOnceExecutionProperties()
      {
         //Must be called only for ClickOnce deployment.
         Debug.Assert(IsClickOnceDeployment());

         string executionProps = null;
         string publishHtmlURL = GetPublishHtmlUrl();

         HttpManager.GetInstance(); // Initialize the http manager
         string publishHtmlError = null;
         String publishHtmlContent = null;

         try
         {
            // Get the contents of publish.html which contains the execution.properties as XML island
            PersistentOnlyCacheManager.CreateInstance();
            bool isError;
            byte[] publishHtmlBuf = HttpManager.GetInstance().GetContent(RemoteCommandsProcessor.ValidateURL(publishHtmlURL, 0), null, null, false, true, out isError);
            publishHtmlContent = Encoding.UTF8.GetString(publishHtmlBuf);
         }
         catch (Exception publishHtmlHttpException)
         {
            publishHtmlError = publishHtmlHttpException.Message;

            //try to get the content of the publish.html file from the current folder
            string publishHtmlPath = GetPublishHtmlPath();

            if (HandleFiles.isExists(publishHtmlPath))
               publishHtmlContent = HandleFiles.readToString(publishHtmlPath);
            else
               publishHtmlError += " Also: the file " + publishHtmlPath + " does not exist in the current folder";
         }
         finally
         {
            PersistentOnlyCacheManager.DeleteInstance();
         }

         if (publishHtmlContent != null)
         {
            // Search for <properties> tag and extract between <Properties> </Properties>
            int startIdxProps = publishHtmlContent.LastIndexOf(ConstInterface.PROPS_START_TAG);
            if (startIdxProps < 0)
               MessageBox.Show("Error: Execution properties XML island (<properties>...</properties>) is missing from the publish.html file.");
            else
            {
               int endIdxProps = publishHtmlContent.LastIndexOf(ConstInterface.PROPS_END_TAG) + ConstInterface.PROPS_END_TAG.Length;
               int length = endIdxProps - startIdxProps;
               executionProps = HttpUtility.UrlEncode(publishHtmlContent.Substring(startIdxProps, length), Encoding.UTF8);
            }
         }
         else
         {
            string errorMsg = "Unable to access " + HttpUtility.UrlDecode(publishHtmlURL, Encoding.UTF8) +
                               OSEnvironment.EolSeq + OSEnvironment.EolSeq + publishHtmlError;
            Logger.Instance.WriteExceptionToLog(errorMsg);
            MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

         return executionProps;
      }

      /// <summary>Prepare URL for publish.html which contains execution props.</summary>
      /// <returns></returns>
      internal static string GetPublishHtmlUrl()
      {
         //Must be called only for ClickOnce deployment.
         Debug.Assert(IsClickOnceDeployment());

         string updateLocation = ApplicationDeployment.CurrentDeployment.UpdateLocation.ToString();
         int lastSlash = updateLocation.LastIndexOf('/'); // location of last '/'
         int wantedDot = updateLocation.IndexOf('.', lastSlash); // location of 1st '.' after last '/'
         int trailStart =
            updateLocation.IndexOf(ConstInterface.DEPLOYMENT_MANIFEST, wantedDot, StringComparison.OrdinalIgnoreCase) +
            ConstInterface.DEPLOYMENT_MANIFEST.Length; // location of trailer, after the 'application'

         // Prepare URL for publish.html which contains execution props.
         // The string is the original string, with "publish.html" replacing the "application"
         // We search the string for the last '.' after the last '/' - that is where the "application" should 
         // be. Anything after the "application" is added at the end of the "publish.html"
         string publishHtmlURL = updateLocation.Substring(0, wantedDot + 1) +
                                 ConstInterface.PUBLISH_HTML +
                                 updateLocation.Substring(trailStart);
         return publishHtmlURL;
      }

      /// <summary>Prepare path for publish.html file which is supposed to exist in the current folder.</summary>
      /// This function gets the update location URL string that looks like this:
      /// http://{server-name}/{folder-name}/{app-name}.application
      /// Takes the "app-name" string, and builds the path of publish.html file to look like this:
      /// .\{app-name}.publish.html
      /// <returns></returns>
      internal static string GetPublishHtmlPath()
      {
         //Must be called only for ClickOnce deployment
         Debug.Assert(IsClickOnceDeployment());

         string updateLocationUrl = ApplicationDeployment.CurrentDeployment.UpdateLocation.ToString();

         int lastSlashIdx = updateLocationUrl.LastIndexOf('/'); // location of last '/'
         int wantedDotIdx = updateLocationUrl.IndexOf('.', lastSlashIdx); // location of 1st '.' after last '/'
         string applicationName = updateLocationUrl.Substring(lastSlashIdx + 1, wantedDotIdx - lastSlashIdx);
         string publishHtmlPath = @".\" + applicationName + ConstInterface.PUBLISH_HTML;

         return publishHtmlPath;
      }

#endif //!PocketPC

      /// <summary>global unique ID of the session = 'machine unique ID' + 'process ID'</summary>
      /// <returns></returns>
      internal string GetGlobalUniqueSessionID()
      {
         return _globalUniqueSessionId;
      }

      /// <summary>return the absolute file name of the execution properties file being used by the client</summary>
      /// <returns></returns>
      private static string getExecutionPropertiesFileName()
      {
         string fullyQualifiedName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
         int lastPathSeparator = fullyQualifiedName.LastIndexOf('\\');
         return (fullyQualifiedName.Remove(lastPathSeparator + 1, fullyQualifiedName.Length - lastPathSeparator - 1) +
                 ConstInterface.EXECUTION_PROPERTIES_FILE_NAME);
      }

      /// <summary>get the objectTable</summary>
      /// <returns></returns>
      internal int getVarChangeDNObjectCollectionKey()
      {
         return _varChangeDnObjectCollectionKey;
      }

 
      /// <summary> get the list of temporary keys of 'objectTable'</summary>
      /// <returns></returns>
      internal List<int> getTempDNObjectCollectionKeys()
      {
         return _tempDnObjectCollectionKeys;
      }

#if PocketPC
      internal bool getRespawnOnExit()
      {
         string respawnOnExit = _executionProps[ConstInterface.RESPAWN_ON_EXIT];
         return (respawnOnExit != null && respawnOnExit.CompareTo("Y") == 0);
      }

      internal void setRespawnOnExit(Boolean value)
      {
         _executionProps[ConstInterface.RESPAWN_ON_EXIT] = value ? "Y" : "N";
      }

      /// <summary> Spawn a new RC Mobile instance</summary>
      private void spawnHidden()
      {
         // Create command line argument from the execution properties
         StringBuilder executionPropertiesXML = new StringBuilder();
         _executionProps.storeToXML(executionPropertiesXML);
         String executionPropertiesEncoded = HttpUtility.UrlEncode(executionPropertiesXML.ToString(), Encoding.UTF8);

         // Call the process spawner
         Process.StartCurrentExecutable(executionPropertiesEncoded);
      }

      /// <summary> Create the link file that will start RC Mobile whenever the device resets</summary>
      private void makeAutoStartLinkFile()
      {
         String execFileName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
         int lastPathSeparator = execFileName.LastIndexOf('\\');
         execFileName = execFileName.Remove(0, lastPathSeparator + 1);

         // Name of the link file
         String linkFileName = execFileName.Replace(".exe", ".lnk");

         // Name of the execution.properties file
         String execPropFileName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
         execPropFileName = execPropFileName.Replace(execFileName, ConstInterface.EXECUTION_PROPERTIES_FILE_NAME);

         // Data in the link file: this application's full name + execution.properties file name + hidden flag
         String linkFileData = "\"" + Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName +
                                 "\" \"/ini=" + execPropFileName + "\"" + START_HIDDEN;

         // Create the link file
         String dirName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup) + "\\";
         StreamWriter linkFile = new StreamWriter(dirName + linkFileName, false);
         linkFile.Write(linkFileData.Length + "#" + linkFileData); // file header - data length
         linkFile.Close();
      }

      /// <summary>Returns startup image for mobile</summary>
      /// <returns></returns>
      internal string getMobileStartupImage()
      {
         return _executionProps[ConstInterface.STARTUP_IMAGE];
      }

      /// <summary>Returns whether to show progrss bar on startup screen on mobile.</summary>
      /// <returns></returns>
      internal bool getMobileShowProgressBar()
      {
         String showProgrssBar = _executionProps[ConstInterface.SHOW_PROGRSSBAR];
         return (showProgrssBar != null && showProgrssBar.CompareTo("Y") == 0);
      }
#endif // PocketPC

      /// <summary> Invoke the request URL & return the response.</summary>
      /// <param name="requestedURL">URL to be accessed.</param>
      /// <param name="decryptResponse">if true, fresh responses from the server will be decrypted using the 'encryptionKey' passed to 'HttpManager.SetProperties'.</param>
      /// <returns>response (from the server).</returns>
      internal byte[] GetRemoteContent(String requestedURL, bool decryptResponse)
      {
         return RemoteCommandsProcessor.GetInstance().GetContent(requestedURL, decryptResponse);
      }

      /// <summary></summary>
      /// <param name="inIncrementalLocate"></param>
      internal void SetInIncrementalLocate(bool inIncrementalLocate)
      {
         _inIncrementalLocate = inIncrementalLocate;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal bool InIncrementalLocate()
      {
         return _inIncrementalLocate;
      }

      /// <summary> parses the taskURL tag
      /// every taskURL tag is replaced by the data of the xml island it refers to</summary>
      internal void ProcessTaskURL()
      {
         List<String> tokensVector = null;
         String taskCacheURL;
         String refListStr;
         String taskContentOriginal;
         StringBuilder taskContentFinal;
         String xmlData = RuntimeCtx.Parser.getXMLdata();

         int endTaskUrlIdx = xmlData.IndexOf(XMLConstants.TAG_CLOSE, RuntimeCtx.Parser.getCurrIndex());
         Debug.Assert(endTaskUrlIdx != -1 && endTaskUrlIdx < xmlData.Length);

         // extract the task url and the ref list
         String tag = RuntimeCtx.Parser.getXMLsubstring(endTaskUrlIdx);
         RuntimeCtx.Parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_TASKURL) + ConstInterface.MG_TAG_TASKURL.Length);
         tokensVector = XmlParser.getTokens(RuntimeCtx.Parser.getXMLsubstring(endTaskUrlIdx), XMLConstants.XML_ATTR_DELIM);
         taskCacheURL = tokensVector[1];
         refListStr = tokensVector[3];

         //bring the data from the xml island - in fact we are reading the logic file xml
         byte[] byteArr = ApplicationSourcesManager.GetInstance().ReadSource(taskCacheURL, true);
         try
         {
            taskContentOriginal = Encoding.UTF8.GetString(byteArr, 0, byteArr.Length);
         }
         catch (Exception)
         {
            throw new ApplicationException("Unable to parse task info, due to unsupported encoding.");
         }

         //prefix the original data till the start of the current <taskURL>
         taskContentFinal = new StringBuilder(xmlData.Substring(0, RuntimeCtx.Parser.getCurrIndex() - (ConstInterface.MG_TAG_TASKURL.Length + 1)), taskContentOriginal.Length);

         //the number of tokens in refList and the number of location in the xml to be replaced (i.e. the places with [:) should be equal
         IEnumerator refList = getReflist(refListStr, ';', true, -1);
         int currIdx = taskContentOriginal.IndexOf("[:");
         int prevIdx = 0;
         while (currIdx != -1)
         {
            refList.MoveNext();
            taskContentFinal.Append(taskContentOriginal.Substring(prevIdx, currIdx - prevIdx));
            taskContentFinal.Append(refList.Current);
            currIdx += 2;
            prevIdx = currIdx;
            currIdx = taskContentOriginal.IndexOf("[:", prevIdx);
         }
         taskContentFinal.Append(taskContentOriginal.Substring(prevIdx));

         //suffix the rest of the original xml from the end of the current </taskURL>
         taskContentFinal.Append(xmlData.Substring(xmlData.IndexOf(XMLConstants.TAG_CLOSE, endTaskUrlIdx + 1) + 1));
         xmlData = taskContentFinal.ToString();

         Logger.Instance.WriteDevToLog(String.Format("{0} -------> {1}", taskCacheURL, xmlData));

         RuntimeCtx.Parser.setXMLdata(xmlData);
         RuntimeCtx.Parser.setCurrIndex(RuntimeCtx.Parser.getCurrIndex() - (ConstInterface.MG_TAG_TASKURL.Length + 1));
      }

      /// <summary> parses the data acording to the delimeter and the reflist delimeters logic</summary>
      /// <param name="delim"></param>
      /// <param name="trunc">if true trucate first '\\' (in use for island data parsing</param>
      /// <param name="maxTokens">if -1 all tokens avialable else the number of tokens to search</param>
      /// <param name="data"></param>
      private IEnumerator getReflist(String data, char delim, bool trunc, int maxTokens)
      {
         StringBuilder currStr = new StringBuilder();
         List<String> list = new List<String>();
         int tokensCnt = 0;

         //since the first character is always ';' we start from 1
         for (int i = 1; i < data.Length; i++)
         {
            char curr = data[i];
            if (curr != '\\' && curr != delim)
               currStr.Append(curr);
            else if (curr == delim)
            {
               list.Add(currStr.ToString());
               currStr = new StringBuilder();
               tokensCnt++;

               if (tokensCnt == maxTokens)
                  break;
            }
            else if (curr == '\\')
            {
               //the format '\\delimiter' is always truncated
               if (!trunc && data[i + 1] != delim)
                  currStr.Append(data[i]);

               i++;
               currStr.Append(data[i]);
            }
         }
         //append the last element in the ref list
         list.Add(currStr.ToString());

         return list.GetEnumerator();
      }

      /// <summary>
      /// Subscribe for events defined at HttpClientEvents and 
      /// attach appropriate event handlers
      /// </summary>
      /// <returns></returns>
      internal void RegisterDelegates()
      {
         //register delegates
         com.magicsoftware.httpclient.HttpClientEvents.GetExecutionProperty_Event += GetExecutionProperty;
         com.magicsoftware.httpclient.HttpClientEvents.GetRuntimeCtxID_Event += GetRuntimeCtxID;
         com.magicsoftware.httpclient.HttpClientEvents.GetGlobalUniqueSessionID_Event += GetGlobalUniqueSessionID;
         com.magicsoftware.httpclient.HttpClientEvents.ShouldDisplayGenericError_Event += ShouldDisplayGenericError;
         com.magicsoftware.httpclient.HttpClientEvents.GetMessageString_Event += getMessageString;
         com.magicsoftware.httpclient.HttpClientEvents.IsFirstRequest_Event += IsFirstRequest;

         UserRights.RightsChanged += UserRights_RightsChanged;
      }

      /// <summary>
      /// Retrieves execution property value by it's name
      /// </summary>
      /// <param name="propertyName"></param>
      /// <returns>property value as string</returns>
      public string GetExecutionProperty(string propName)
      {
          return _executionProps.getProperty(propName);
      }

      /// <summary>
      /// Retrieves runtime context ID
      /// </summary>
      /// <returns></returns>
      public string GetRuntimeCtxID()
      {
          return RuntimeCtx.ContextID.ToString();
      }

      /// <summary>
      /// Used to determine is Client received its first HTTP request
      /// </summary>
      /// <returns></returns>
      public bool IsFirstRequest()
      {
         return (RuntimeCtx.ContextID == RemoteCommandsProcessor.RC_NO_CONTEXT_ID);
      }

      /// <summary>
      /// Get max time for retrying to write cache file 
      /// </summary>
      /// <returns></returns>
      public int GetWriteClientCacheMaxRetryTime()
      {
         String writeClientCacheMaxRetryTime = _executionProps.getProperty(ConstInterface.WRITE_CLIENT_CACHE_MAX_RETRY_TIME);
         if (writeClientCacheMaxRetryTime == null)
            writeClientCacheMaxRetryTime = "10"; // default retry timeout in seconds

         return Convert.ToInt32(writeClientCacheMaxRetryTime);
      }

      #region IServiceProvider
      /// <summary>
      /// get service
      /// </summary>
      /// <param name="serviceType"></param>
      /// <returns></returns>
      public object GetService(Type serviceType)
      {
         return _servicesManager.GetService(serviceType);
      }

      public T GetService<T>()
      {
         return (T)GetService(typeof(T));
      }

      #endregion

      /// <summary>
      /// set service
      /// </summary>
      /// <param name="serviceType"></param>
      /// <param name="value"></param>
      public void SetService(Type serviceType, object value)
      {
         _servicesManager.SetService(serviceType, value);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public bool GetShouldBeep()
      {
         return StartedFromStudio;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public string GetFormHiddenControlsXML()
      {
         Task task = getCurrTask();
         if (task != null)
         {
            MgForm form = (MgForm)task.getForm();
            if(form != null)
               return form.GetHiddenControlListXML();
         }

         return null;
      }
   }
}
