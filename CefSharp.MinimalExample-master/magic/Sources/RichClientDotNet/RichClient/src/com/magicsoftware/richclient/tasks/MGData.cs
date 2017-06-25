using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.local.application.datasources;
using com.magicsoftware.richclient.security;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.http;

#if PocketPC
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
#endif

namespace com.magicsoftware.richclient.tasks
{
   /// <summary>
   ///   Represents all details relevant to a specific task - related tasks, commands, handlers, etc..
   /// </summary>
   internal class MGData
   {
      internal CommandsTable CmdsToClient { get; private set; } // queue of commands to client
      internal CommandsTable CmdsToServer { get; private set; } // queue of commands to server
      private readonly HandlersTable _expHandlers; // all the expression event
      private readonly int _id;
      private readonly MGData _parent; // MGdata of my direct ancestor
      private readonly TasksTable _mprgTab; // main programs
      private readonly TasksTable _tasksTab; // table of all tasks
      private readonly HandlersTable _timerHandlers; // all the timer event handlers
      private bool _timersStarted; // TRUE if the timers have already been started

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "id">the id of this mgdata object</param>
      /// <param name = "parent">the parent MGData</param>
      /// <param name = "isModal">true for modal windows</param>
      internal MGData(int id, MGData parent, bool isModal)
      {
         CmdsToServer = new CommandsTable();
         CmdsToClient = new CommandsTable();
         _timerHandlers = new HandlersTable();
         _expHandlers = new HandlersTable();
         _mprgTab = new TasksTable();
         _tasksTab = new TasksTable();

         _id = id;
         _parent = parent;

         if (ClientManager.Instance.EventsManager.getCompMainPrgTab() == null)
         {
            var compMainPrgTab = new CompMainPrgTable();
            ClientManager.Instance.EventsManager.setCompMainPrgTab(compMainPrgTab);
         }

         IsModal = isModal;
      }

      /// <summary>
      /// CTOR to be used when running offline tasks - hold the command to enable right setting of modality
      /// </summary>
      /// <param name="id"></param>
      /// <param name="parent"></param>
      /// <param name="isModal"></param>
      /// <param name="forceModal"></param>
      public MGData(int id, MGData parent, bool isModal, bool forceModal)
         : this(id, parent, isModal)
      {
         ForceModal = forceModal;
      }

      internal bool IsModal { get; set; }
      internal bool IsAborting { get; set; }
      internal bool ForceModal{ get; private set; }

      /// <summary>
      ///   returns the id of this MGData object
      /// </summary>
      internal int GetId()
      {
         return _id;
      }

      /// <summary>
      ///   Returns true if already moved to the first control on one of the MgData
      /// </summary>
      internal bool AlreadyMovedToFirstControl()
      {
         bool alreadyMoved = false;

         int length = getTasksCount();
         for (int i = 0; i < length; i++)
         {
            var form = (MgForm) getTask(i).getForm();
            if (form != null && form.MovedToFirstControl && !form.IsMDIFrame)
            {
               alreadyMoved = true;
               break;
            }
         }

         return alreadyMoved;
      }

      /// <summary>
      ///   start the timers
      /// </summary>
      internal void StartTimers()
      {
         if (_timerHandlers != null && !_timersStarted)
         {
            _timerHandlers.startTimers(this);
            _timersStarted = true;
         }
      }

      /// <summary>
      ///   Function for filling own fields, allocate memory for inner objects. Parsing the input String.
      /// </summary>
      internal void fillData(OpeningTaskDetails openingTaskDetails)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;

         // To avoid the need to send data that sets default values, we'll assume we want the default values
         // unless different info was sent. i.e. if the relevant tags are not in the XML, set the default values.
         while (true)
         {
            string nextTag = parser.getNextTag();

            if (!initInnerObjects(parser, nextTag, openingTaskDetails))
               break;
         }

         Logger.Instance.WriteDevToLog("MGData.FillData(): THE END of the parsing " +
                                                   parser.getCurrIndex() +
                                                   " characters");
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">possible tag name, name of object, which need be allocated</param>
      /// <param name="openingTaskDetails">additional information of opening task</param>
      /// <returns> xmlParser.getCurrIndex(), the found object tag and ALL its subtags finish</returns>
      private bool initInnerObjects(XmlParser parser, String foundTagName, OpeningTaskDetails openingTaskDetails)
      {
         if (foundTagName == null)
            return false;

         switch (foundTagName)
         {
            case XMLConstants.MG_TAG_XML:
               parser.setCurrIndex(
                  parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1); // do
               // nothing
               break;

            case ConstInterface.MG_TAG_DATAVIEW:
               if (!insertDataView(parser))
               {
                  // the task of insert data view not found -> set parsers counter to the
                  // end of the data view
                  // the data view, got from server and there is no task to the data view , yet.
                  int endContext = parser.getXMLdata().IndexOf('/' + ConstInterface.MG_TAG_DATAVIEW,
                                                                  parser.getCurrIndex());
                  parser.setCurrIndex(endContext);
                  parser.setCurrIndex2EndOfTag();
               }
               break;
            // read DataViewContent that received as a response of GetDataViewContent Command.
            case ConstInterface.MG_TAG_COMPLETE_DV:
               {
                  Task task = null;
                  int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
                  int index = parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_COMPLETE_DV, parser.getCurrIndex()) +
                                                                              ConstInterface.MG_TAG_COMPLETE_DV.Length;

                  List<String> tokensVector = XmlParser.getTokens(parser.getXMLdata().Substring(index, endContext - index),
                                                                  "\"");

                  // get task id
                  string attribute = (tokensVector[0]);
                  String valueStr;
                  if (attribute.Equals(XMLConstants.MG_ATTR_TASKID))
                  {
                     valueStr = (tokensVector[1]);
                     String taskId = valueStr;
                     task = getTask(taskId) ?? (Task)MGDataCollection.Instance.GetTaskByID(taskId);
                  }

                  int start = endContext + 1;
                  endContext = parser.getXMLdata().IndexOf(XMLConstants.END_TAG + ConstInterface.MG_TAG_COMPLETE_DV,
                                                                  parser.getCurrIndex());

                  String dataViewContent = parser.getXMLdata().Substring(start, endContext - start);
                  task.dataViewContent = dataViewContent;
                  parser.setCurrIndex(endContext);
                  parser.setCurrIndex2EndOfTag();
               }

               break;
            case XMLConstants.MG_TAG_TREE:
               {
                  int start = parser.getCurrIndex();
                  int endContext = parser.getXMLdata().IndexOf('/' + XMLConstants.MG_TAG_TREE,
                                                                  parser.getCurrIndex());
                  parser.setCurrIndex(endContext);
                  parser.setCurrIndex2EndOfTag();
                  String treeData = parser.getXMLdata().Substring(start, parser.getCurrIndex() - start);
                  //read tree data using sax parser
                  try
                  {
                     var mgSAXParser = new MgSAXParser(new TreeSaxHandler());
                     mgSAXParser.parse(Encoding.UTF8.GetBytes(treeData));
                  }
                  catch (Exception e)
                  {
                     Logger.Instance.WriteExceptionToLog(e);
                     Misc.WriteStackTrace(e, Console.Error);
                  }
                  break;
               }

            case ConstInterface.MG_TAG_CONTEXT:
               {
                  int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
                  if (endContext != -1 && endContext < parser.getXMLdata().Length)
                  {
                     // last position of its tag
                     String tag = parser.getXMLsubstring(endContext);
                     parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_CONTEXT) +
                                          ConstInterface.MG_TAG_CONTEXT.Length);
                     parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete "/>" too
                  }
                  break;
               }

            case XMLConstants.MG_TAG_RECOMPUTE:
               var recompTab = new RecomputeTable();
               recompTab.fillData();
               break;

            case ConstInterface.MG_TAG_COMMAND:
               Logger.Instance.WriteDevToLog("goes to command");
               CmdsToClient.fillData();
               break;

            case ConstInterface.MG_TAG_LANGUAGE:
               Logger.Instance.WriteDevToLog("goes to language data");
               ClientManager.Instance.getLanguageData().fillData();
               break;

            case ConstInterface.MG_TAG_KBDMAP_URL:
               Logger.Instance.WriteDevToLog("goes to keyBoard");
               ClientManager.Instance.getEnvironment().fillFromUrl(foundTagName);
               break;

            case ConstInterface.MG_TAG_KBDMAP:
               Logger.Instance.WriteDevToLog("goes to keyBoard");

               int kbdStartIdx = parser.getCurrIndex();
               int kbdEndIdx = parser.getXMLdata().IndexOf('/' + ConstInterface.MG_TAG_KBDMAP,
                                                              parser.getCurrIndex());
               parser.setCurrIndex(kbdEndIdx);
               parser.setCurrIndex2EndOfTag();
               String kbdData = parser.getXMLdata().Substring(kbdStartIdx, parser.getCurrIndex() - kbdStartIdx);
               ClientManager.Instance.getKbdMap().fillKbdMapTable(Encoding.UTF8.GetBytes(kbdData));

               break;

            case ConstInterface.MG_TAG_COLORTABLE_URL:
               Logger.Instance.WriteDevToLog("goes to color");
               ClientManager.Instance.getEnvironment().fillFromUrl(foundTagName);
               break;

            case XMLConstants.MG_TAG_COLORTABLE:
               {
                  Logger.Instance.WriteDevToLog("goes to color");
                  int colorStartIdx = parser.getCurrIndex();
                  int colorEndIdx = parser.getXMLdata().IndexOf('/' + XMLConstants.MG_TAG_COLORTABLE,
                                                                   parser.getCurrIndex());
                  parser.setCurrIndex(colorEndIdx);
                  parser.setCurrIndex2EndOfTag();
                  String colorData = parser.getXMLdata().Substring(colorStartIdx,
                                                                      parser.getCurrIndex() - colorStartIdx);

                  Manager.GetColorsTable().FillFrom(Encoding.UTF8.GetBytes(colorData));
                  break;
               }

            case ConstInterface.MG_TAG_FONTTABLE_URL:
               Logger.Instance.WriteDevToLog("goes to font");
               ClientManager.Instance.getEnvironment().fillFromUrl(foundTagName);
               break;

            case ConstInterface.MG_TAG_FONTTABLE:
               Logger.Instance.WriteDevToLog("goes to font");

               int startIdx = parser.getCurrIndex();
               int endIdx = parser.getXMLdata().IndexOf('/' + ConstInterface.MG_TAG_FONTTABLE,
                                                           parser.getCurrIndex());
               parser.setCurrIndex(endIdx);
               parser.setCurrIndex2EndOfTag();
               String fontData = parser.getXMLdata().Substring(startIdx, parser.getCurrIndex() - startIdx);
               Manager.GetFontsTable().FillFrom(Encoding.UTF8.GetBytes(fontData));
               break;

            case ConstInterface.MG_TAG_COMPMAINPRG:
               Logger.Instance.WriteDevToLog("goes to compmainprg");
               ClientManager.Instance.EventsManager.getCompMainPrgTab().fillData();
               break;

            case ConstInterface.MG_TAG_EVENTS_QUEUE:
               Logger.Instance.WriteDevToLog("goes to eventsqueue");
               fillEventsQueue(parser);
               break;

            case ConstInterface.MG_TAG_TASKURL:
               ClientManager.Instance.ProcessTaskURL();
               break;

            case XMLConstants.MG_TAG_TASK:
               Logger.Instance.WriteDevToLog("goes to task");
               int taskCountBefore = _mprgTab.getSize();
               
               _mprgTab.fillData(this, openingTaskDetails);

               // QCR #759911: mprg must belong to the main application's MGData.
               // This ensures it will not be discarded until the end of execution.
               // There could be more then 1 mprgs that are added (Qcr #168549)
               if (_id != 0)
               {
                   int taskCountAfter = _mprgTab.getSize();
                   MGData mgd0 = MGDataCollection.Instance.getMGData(0);

                   // check all the new main prgs.
                   for (int taskIndex = taskCountBefore; taskIndex < taskCountAfter; taskIndex++)
                   {
                       Task newTask = _mprgTab.getTask(taskIndex);


                       if (newTask.isMainProg() && mgd0._mprgTab.getTask(newTask.getTaskTag()) == null)
                       {
                           mgd0._mprgTab.addTask(newTask);
                           mgd0.addTask(newTask);
                       }
                   }
                   
               }
               break;

            case ConstInterface.MG_TAG_ENV:
               Logger.Instance.WriteDevToLog("goes to environment");
               ClientManager.Instance.getEnvironment().fillData();
               break;

            case ConstInterface.MG_TAG_FLWMTR_CONFIG:
               FlowMonitorQueue.Instance.fillData();
               break;

            case XMLConstants.MG_TAG_XML_END:
               parser.setCurrIndex2EndOfTag();
               return false;

            case ConstInterface.MG_TAG_USER_RIGHTS:
               ClientManager.Instance.fillUserRights();
               break;

            case ConstInterface.MG_TAG_DBH_REAL_IDXS:
               ClientManager.Instance.fillDbhRealIdxs();
               break;

            case ConstInterface.MG_TAG_GLOBALPARAMSCHANGES:
               Logger.Instance.WriteDevToLog("applying global params changes from the server (Set/GetParams)");
               ClientManager.Instance.getGlobalParamsTable().fillData();
               break;

            case ConstInterface.MG_TAG_GLOBALPARAMS:
               Logger.Instance.WriteDevToLog("processing base64 encoded image of all global params from the server");
               ClientManager.Instance.fillGlobalParams();
               break;

            case ConstInterface.MG_TAG_CACHED_FILES:
               RemoteCommandsProcessor.GetInstance().ServerFileToClientHelper.RequestedForFolderOrWildcard = true;
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               break;

            case ConstInterface.MG_TAG_CACHED_FILES_END:
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               break;
   
            case ConstInterface.MG_TAG_CACHED_FILE:
               ClientManager.Instance.fillCacheFilesMap();
               break;

            case ConstInterface.MG_TAG_ENV_PARAM_URL:
               Logger.Instance.WriteDevToLog("goes to env params name ");
               ClientManager.Instance.getEnvironment().fillFromUrl(foundTagName);
               break;

            case ConstInterface.MG_TAG_ENV_PARAM:
               Logger.Instance.WriteDevToLog("goes to env params name ");
               ClientManager.Instance.getEnvParamsTable().fillData();
               break;

            case ConstInterface.MG_TAG_USER_DETAILS:
               UserDetails.Instance.fillData();
               break;

            case ConstInterface.MG_TAG_DBHS:
               ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.FillData();
               break;

            case ConstInterface.MG_TAG_DATABASE_URL:
               // If data includes only file url
               ClientManager.Instance.LocalManager.ApplicationDefinitions.DatabaseDefinitionsManager.FillUrl();
               break;

            case ConstInterface.MG_TAG_DATABASES_HEADER:
               ClientManager.Instance.LocalManager.ApplicationDefinitions.DatabaseDefinitionsManager.FillData();
               break;

            case ConstInterface.MG_TAG_TASKDEFINITION_IDS_URL:
               Logger.Instance.WriteDevToLog("goes to task definition ids");
               ClientManager.Instance.LocalManager.ApplicationDefinitions.TaskDefinitionIdsManager.FillFromUrl(foundTagName);
               break;

            case ConstInterface.MG_TAG_OFFLINE_SNIPPETS_URL:
               ClientManager.Instance.LocalManager.ApplicationDefinitions.OfflineSnippetsManager.FillFromUrl(foundTagName);
               break;

            case ConstInterface.MG_TAG_STARTUP_PROGRAM:
               CommandsProcessorManager.GetCommandsProcessor().ParseStartupProgram(parser);
               break;

            case ConstInterface.MG_TAG_CONTEXT_ID:
               {
                  String ctxId;
                  int ctxEndIdx;

                  parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
                  ctxEndIdx = parser.getXMLdata().IndexOf(XMLConstants.TAG_OPEN, parser.getCurrIndex());
                  ctxId = parser.getXMLsubstring(ctxEndIdx).Trim();
                  parser.setCurrIndex(ctxEndIdx);
                  parser.setCurrIndex2EndOfTag();
                  
                  ClientManager.Instance.RuntimeCtx.ContextID = Int64.Parse(ctxId);
               }
               break;

            case ConstInterface.MG_TAG_HTTP_COMMUNICATION_TIMEOUT:
               {
                  uint httpCommunicationTimeout; // communication-level timeout (i.e. the access to the web server, NOT the entire request/response round-trip), in seconds.
                  int httpCommunicationTimeoutIdx;

                  parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
                  httpCommunicationTimeoutIdx = parser.getXMLdata().IndexOf(XMLConstants.TAG_OPEN, parser.getCurrIndex());
                  httpCommunicationTimeout = uint.Parse(parser.getXMLsubstring(httpCommunicationTimeoutIdx));
                  parser.setCurrIndex(httpCommunicationTimeoutIdx);
                  parser.setCurrIndex2EndOfTag();

                  HttpManager.GetInstance().HttpCommunicationTimeoutMS = httpCommunicationTimeout * 1000;
               }
               break;

            default:
               Logger.Instance.WriteExceptionToLog(
                  "There is no such tag name in MGData, add case to MGData.initInnerObjects and case to while of MGData.FillData  " +
                  foundTagName);
               return false;
         }

         return true;
      }

      /// <summary>
      ///   Insert Records Table to DataView of the task
      /// </summary>
      /// <returns> if the task of dataview found</returns>
      private bool insertDataView(XmlParser parser)
      {
         Task task = null;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         int index = parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_DATAVIEW, parser.getCurrIndex()) +
                     ConstInterface.MG_TAG_DATAVIEW.Length;
         List<String> tokensVector = XmlParser.getTokens(parser.getXMLdata().Substring(index, endContext - index),
                                                         "\"");

         bool invalidate = false;

         // look for the task id
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            string attribute = (tokensVector[j]);
            String valueStr;
            if (attribute.Equals(XMLConstants.MG_ATTR_TASKID))
            {
               valueStr = (tokensVector[j + 1]);
               String taskId = valueStr;
               task = getTask(taskId) ?? (Task) MGDataCollection.Instance.GetTaskByID(taskId);
            }
            if (attribute.Equals(ConstInterface.MG_ATTR_INVALIDATE))
            {
               valueStr = (tokensVector[j + 1]);
               invalidate = XmlParser.getBoolean(valueStr);
               break;
            }
         }
         if (task != null)
         {
            // add the records and update the current record in the dataview
            task.insertRecordTable(invalidate);
            return true;
         }
         return false; // the task not found
      }

      /// <summary>
      ///   add task to taskTab
      /// </summary>
      /// <param name = "newTask">to be added</param>
      internal void addTask(Task newTask)
      {
         _tasksTab.addTask(newTask);
      }

      /// <summary>
      ///   remove a task from the tasks table
      /// </summary>
      /// <param name = "task"></param>
      internal void removeTask(Task task)
      {
         _tasksTab.removeTask(task);
         _mprgTab.removeTask(task);
      }

      /// <summary>
      ///   returns the number of tasks in the tasks table
      /// </summary>
      /// <returns></returns>
      internal int getTasksCount()
      {
         if (_tasksTab != null)
            return _tasksTab.getSize();
         return 0;
      }

      /// <summary>
      ///   returns a task by its id
      /// </summary>
      /// <param name = "taskId">task ID</param>
      internal Task getTask(String taskId)
      {
         if (_tasksTab != null)
            return _tasksTab.getTask(taskId);
         return null;
      }

      /// <summary>
      ///   returns a task by its index in the tasks table
      /// </summary>
      /// <param name = "idx">index of the task in the tasks table</param>
      internal Task getTask(int idx)
      {
         return _tasksTab.getTask(idx);
      }

      /// <summary>
      ///   build XML string of the MGData object
      /// </summary>
      /// <param name="message">a message being prepared.</param>
      /// <param name="serializeTasks">if true, tasks in the current execution will also be serialized.</param>
      internal void buildXML(StringBuilder message, Boolean serializeTasks)
      {
         if (CmdsToServer != null)
            CmdsToServer.buildXML(message);
         if (serializeTasks && _tasksTab != null)
            _tasksTab.buildXML(message);
      }

      /// <summary>
      /// should save offline startuo info only while :
      /// 1. first task is define as offline enable 
      /// or 
      /// 2. Main Program
      /// </summary>
      /// <returns></returns>
      internal bool ShouldSaveOfflineStartupInfo
      {
         get
         {
            Task task = null;

            for (int i = 0; i < _tasksTab.getSize(); i++)
            {
               task = _tasksTab.getTask(i);
               if (!task.isMainProg())
                  return task.IsOffline;
            }

            return true;
         }
      }

      /// <summary>
      ///   get the first task which is not a main program in the task table
      /// </summary>
      internal Task getFirstTask()
      {
         Task task = null;
         Task MDIFrameTask = null;

         for (int i = 0; i < _tasksTab.getSize(); i++)
         {
            task = _tasksTab.getTask(i);
            if (task.isMainProg())
            {
               if (task.HasMDIFrame)
                  MDIFrameTask = task;
            }
            else
               break;
            task = null;
         }
         if (task == null && _id == 0)
            task = MDIFrameTask; //return MDI frame program
         return task;
      }

      /// <summary>
      ///   get a main program of a component by its ctl idx
      /// </summary>
      /// <param name = "ctlIdx">the ctl idx</param>
      internal Task getMainProg(int ctlIdx)
      {
         for (int i = 0; i < _mprgTab.getSize(); i++)
         {
            Task task = _mprgTab.getTask(i);
            if (task.getCtlIdx() == ctlIdx && task.isMainProg())
               return task;
         }
         return null;
      }

      /// <summary>
      ///   returns the next main program in the search order AFTER the ctlIdx
      /// </summary>
      /// <param name = "ctlIdx">the current ctl idx</param>
      internal Task getNextMainProg(int ctlIdx)
      {
         CompMainPrgTable compMainPrgTab = ClientManager.Instance.EventsManager.getCompMainPrgTab();
         int currSearchIndex = compMainPrgTab.getIndexOf(ctlIdx);
         int nextCtlIdx = compMainPrgTab.getCtlIdx(currSearchIndex + 1);

         if (nextCtlIdx == -1)
            return null;
         return getMainProg(nextCtlIdx);
      }

      /// <summary>
      ///   get the timer event handlers
      /// </summary>
      internal HandlersTable getTimerHandlers()
      {
         return _timerHandlers;
      }

      /// <summary>
      ///   add a timer event handler to the timer event handlers table
      /// </summary>
      /// <param name = "handler">a reference to a timer event handler</param>
      internal void addTimerHandler(com.magicsoftware.richclient.events.EventHandler handler)
      {
         _timerHandlers.add(handler);
      }

      /// <summary>
      ///   removes timer handlers for the specific task
      ///   it is needed for destination call for exit from the previous subform
      /// </summary>
      /// <param name = "task"></param>
      internal void removeTimerHandler(Task task)
      {
         for (int i = 0; i < _timerHandlers.getSize();)
         {
            Task timerTask = _timerHandlers.getHandler(i).getTask();
            if (timerTask.isDescendentOf(task) && timerTask.getMGData() == this)
               _timerHandlers.remove(i);
            else
               i++;
         }
      }

      /// <summary>
      ///   removes expression handlers for the specific task
      ///   it is needed for destination call for exit from the previous subform
      /// </summary>
      /// <param name = "task"></param>
      internal void removeExpressionHandler(Task task)
      {
         for (int i = 0; i < _expHandlers.getSize();)
         {
            Task timerTask = _expHandlers.getHandler(i).getTask();
            if (timerTask.isDescendentOf(task) && timerTask.getMGData() == this)
               _expHandlers.remove(i);
            else
               i++;
         }
      }

      /// <summary>
      ///   get the expression event handlers
      /// </summary>
      internal HandlersTable getExpHandlers()
      {
         return _expHandlers;
      }

      /// <summary>
      ///   add a expression event handler to the expression event handlers table
      /// </summary>
      /// <param name = "handler">a reference to an expression event handler</param>
      internal void addExpHandler(com.magicsoftware.richclient.events.EventHandler handler, int idx)
      {
         _expHandlers.insertAfter(handler, idx);
      }

      /// <summary>
      ///   stops timers that already no needed and start new timers
      /// </summary>
      /// <param name = "oldTimers"></param>
      /// <param name = "newTimers"></param>
      internal void changeTimers(List<Int32> oldTimers, List<Int32> newTimers)
      {
         int i = 0, j = 0;
         GUIManager guiManager = GUIManager.Instance;

         oldTimers.Sort();
         newTimers.Sort();

         while ((i < oldTimers.Count) && (j < newTimers.Count))
         {
            if (oldTimers[i] > newTimers[j])
            {
               guiManager.startTimer(this, newTimers[j], false);
               j++;
            }
            else if (oldTimers[i] < newTimers[j])
            {
               guiManager.stopTimer(this, oldTimers[i], false);
               i++;
            }
            else
            {
               i++;
               j++;
            }
         }

         for (; i < oldTimers.Count; i++)
            guiManager.stopTimer(this, oldTimers[i], false);

         for (; j < newTimers.Count; j++)
            guiManager.startTimer(this, newTimers[j], false);
      }

      /// <summary>
      ///   add data from <eventsqueue> tag to the events queue
      /// </summary>
      private void fillEventsQueue(XmlParser parser)
      {
         while (initEvents(parser, parser.getNextTag()))
         {
         }
      }

      /// <summary>
      ///   add Run Time Events to the events queue
      /// </summary>
      /// <param name = "foundTagName">possible tag name, name of object, which need be allocated</param>
      private bool initEvents(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_EVENT))
         {
            var evt = new RunTimeEvent((MgControl) null);
            evt.fillData(parser, null);
            evt.convertParamsToArgs();
            evt.setTask(null);
            evt.setFromServer();
            ClientManager.Instance.EventsManager.addToTail(evt);
         }
         else if (foundTagName.Equals(ConstInterface.MG_TAG_EVENTS_QUEUE))
         {
            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) +
                                   1); // end of outer tag and its ">"
         }
         else if (foundTagName.Equals('/' + ConstInterface.MG_TAG_EVENTS_QUEUE))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog("There is no such tag in EventsQueue: " + foundTagName);
            return false;
         }
         return true;
      }

      /// <summary>
      ///   returns true for the main windows MGData
      /// </summary>
      internal bool isMainWindow()
      {
         return _parent == null;
      }

      /// <summary>
      ///   returns the parent MGDdata of this MGData (might be null)
      /// </summary>
      internal MGData getParentMGdata()
      {
         return _parent;
      }

      /// <summary>
      ///   set the aborting flag
      /// </summary>
      internal void abort()
      {
         IsAborting = true;
      }

      /// <summary>
      ///   add all commands which are not attached to any window into the current window
      /// </summary>
      internal void copyUnframedCmds()
      {
         MgArrayList unframedCmds = ClientManager.Instance.getUnframedCmds();

         for (int i = 0; i < unframedCmds.Count; i++)
            CmdsToClient.Add((IClientCommand) unframedCmds[i]);

         unframedCmds.SetSize(0);
      }

      /// <summary>
      ///   returns an ancestor of this MGData object which is not pending unload
      /// </summary>
      internal MGData getValidAncestor()
      {
         return (_parent);
      }

      /// <summary> Returns if the first non-MP task in this MgData is a non-Offline task.</summary>
      /// <returns></returns>
      internal bool ContainsNonOfflineProgram()
      {
         bool containsNonOfflineProgram = false;
         Task firstTask = getFirstTask();

         if (firstTask != null && !firstTask.isMainProg())
            containsNonOfflineProgram = !firstTask.IsOffline;

         return containsNonOfflineProgram;
      }

      /// <summary>
      /// do something for eahc task in _tasksTab, and send the extra data
      /// </summary>
      /// <param name="closedTask"></param>
      internal void ForEachTask(TaskDelegate taskDelegate, object extraData)
      {
         for (int i = 0; i < _tasksTab.getSize(); i++)
         {
            Task task = getTask(i);
            taskDelegate(task, extraData);
         }
      }

      public int getMaxCtlIdx()
      {
         int rc = 0;
         for (int i = 0; i < _tasksTab.getSize(); i++)
         {
            Task tsk = _tasksTab.getTask(i);
            if (tsk != null)
               rc = System.Math.Max(rc, tsk.getCtlIdx());
         }
         return rc;
      }
   }
}
