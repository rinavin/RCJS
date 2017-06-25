using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using Field = com.magicsoftware.richclient.data.Field;
using MGDataTable = com.magicsoftware.richclient.tasks.MGDataTable;
using com.magicsoftware.richclient.local.data;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.data;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.commands.ServerToClient;

namespace com.magicsoftware.richclient
{
   /// <summary>
   ///   data for <command> tag
   /// </summary>
   internal class Command : IClientTargetedCommand
   {
      internal StorageAttribute _attr = StorageAttribute.NONE; // for the result command
      internal char _buttonsID; // buttons of verify opr
      internal String _callingTaskTag; // is the id of the task that called the new window
      internal String _pathParentTaskTag; // is the id of path parent task tag
      private bool _checkOnly; // whether check only the Destination Subform call or execute
      private bool _closeSubformOnly; //for close subform task with destination subform
      private int _cmdFrame = Int32.MinValue;
      internal int _defaultButton; // default button of verify opr
      internal int _direction = -1;
      internal char _display = (char)(0);
      internal int _ditIdx = Int32.MinValue;
      internal bool _encryptXML; // for openwindow only
      internal bool _errLogAppend; // append to error log for verify opr
      private ExecutionStack _executionStack; // stack of all taskid+handlerid+operid of current execution
      internal bool _exitByMenu; // on exit event. is the exit for the startup mgdata was sent from a menu when opening another prog.
      internal int _expIdx = Int32.MinValue;
      internal StorageAttribute _expType = StorageAttribute.NONE;
      internal int _fldId = -1;
      internal bool _ignoreSubformRecompute;
      internal String _handlerId;
      internal String _html;
      internal int _htmlLen = -1; // for openwindow only
      internal bool _htmlScan; // for openwindow only
      internal char _image; // image of verify opr
      internal String _incrmentalSearchString; //for incremental search
      internal bool _isModal = true; // Y|N
      internal bool _isNull;
      internal bool _keepUserSort; // for view refresh command
      internal String _key; // for openurl command
      internal int _lengthExpVal = Int32.MinValue;
      internal char _level = (char)(0); // for transaction only
      internal int MagicEvent { get; set; }
      internal RollbackEventCommand.RollbackType rollbackType = RollbackEventCommand.RollbackType.NONE;
      internal DataViewCommandType DataViewCommandType { get; set; }
      internal int MenuComp = Int32.MinValue;
      internal int _menuUid = Int32.MinValue;
      internal char _mode = (char)(0);
      private Task _mprgCreator;
      internal String _newId;
      internal String _newTaskXML; //xml of new task , for openurl command
      public String Obj { get; set; }
      internal String _oldId;
      internal char _oper = (char)(0);
      internal int _operIdx = Int32.MinValue;
      internal QueryType _queryType = QueryType.NULL; // query the context in the server, e.g. whether GlobalParams should be copied for a parallel program
      internal ViewRefreshMode RefreshMode { get; set; }
      internal bool _resetIncrementalSearch; //for incremental search
      internal Field _returnVal; // return value of verify opr
      internal String _returnValStr;
      internal bool _reversibleExit = true;
      internal bool _sendAck = true;
      internal bool _standalone; // for openwindow only. If true, the new window does not belong to the browser.
      internal String _subformCtrlName; // Subform control name for Destination Subform
      internal String SubformTaskTag { get; set; }
      internal bool ExplicitSubformRefresh { get; set; }
      public String TaskTag { get; set; }
      internal String _text; // the verify message
      internal String _title; // title of verify opr
      internal String _transOwner;
      internal String _treeIsNulls; //are values of parent nodes nulls
      internal String _treePath; //rec ids of parents of node
      internal String _treeValues; //values of parents of node
      public ClientCommandType Type { get; set; } // Event|Openwindow|Abort|Return|Transaction|Unload|eXecoper|evaLuate|reSult|query
      internal String _url;
      internal String _val; // for Return type only
      internal ArgumentsList _varList;
      internal String _generation; // generation identifies parent task in the server’s task tree.
      internal String _taskVarList; // variable list identifies view required for GetDataViewContent command.
      internal bool ForceModal = false;
      internal Dictionary<string, string> _accessedCacheFiles = null;
      internal int CurrentRecordRow { get; set; }  // for offline view refresh command
      internal bool TransactionIsOpened { get; set; }

      internal Operation Operation { get; set; }
      /// <summary>
      /// client id
      /// </summary>
      internal int ClientRecId { get; set; }

      /// <summary>
      /// id of fetched link
      /// </summary>
      internal RecomputeId UnitId { get; set; }

      /// <summary>
      /// user range, used by RangeAdd
      /// </summary>
      internal UserRange UserRange { get; set; }

      /// <summary>
      ///   default CTOR
      /// </summary>
      protected internal Command()
      {
      }

      /// <summary>
      ///   build the XML structure of the command
      /// </summary>
      public void buildXML(StringBuilder message)
      {
         if (TaskTag != null && MGDataTable.Instance.GetTaskByID(TaskTag) != null &&
             !((Task)MGDataTable.Instance.GetTaskByID(TaskTag)).KnownToServer)
            return;

         bool execStackExists = _executionStack != null && !_executionStack.empty();

         BuildXMLInternal(message, execStackExists);

         if (execStackExists)
            _executionStack.buildXML(message);
      }

      /// <summary>
      /// part of the buildXML which deals with the command only - used for command serialization unit test
      /// </summary>
      /// <param name="message"></param>
      /// <param name="execStackExists"></param>
      /// <returns></returns>
      internal void BuildXMLInternal(StringBuilder message, bool execStackExists)
      {
         bool hasChildElements = false;
         message.Append(XMLConstants.START_TAG + ConstInterface.MG_TAG_COMMAND);
         message.Append(" " + XMLConstants.MG_ATTR_TYPE + "=\"");
         switch (Type)
         {
            case ClientCommandType.VerifyCache:
               message.Append(ConstInterface.MG_ATTR_VAL_VERIFY_CACHE);
               break;

            case ClientCommandType.Event:
               message.Append(ConstInterface.MG_ATTR_VAL_EVENT);
               break;

            case ClientCommandType.Recompute:
               message.Append(ConstInterface.MG_ATTR_VAL_RECOMP);
               break;

            case ClientCommandType.Transaction:
               message.Append(ConstInterface.MG_ATTR_VAL_TRANS);
               break;

            case ClientCommandType.Unload:
               message.Append(ConstInterface.MG_ATTR_VAL_UNLOAD);
               break;

            case ClientCommandType.Hibernate:
               message.Append(ConstInterface.MG_ATTR_VAL_HIBERNATE);
               break;

            case ClientCommandType.Resume:
               message.Append(ConstInterface.MG_ATTR_VAL_RESUME);
               break;

            case ClientCommandType.ExecOper:
               message.Append(ConstInterface.MG_ATTR_VAL_EXEC_OPER);
               break;

            case ClientCommandType.Menu:
               message.Append(ConstInterface.MG_ATTR_VAL_MENU);
               break;

            case ClientCommandType.Evaluate:
               message.Append(ConstInterface.MG_ATTR_VAL_EVAL);
               break;

            case ClientCommandType.OpenURL:
               message.Append(ConstInterface.MG_ATTR_VAL_OPENURL);
               break;

            case ClientCommandType.Query:
               message.Append(ConstInterface.MG_ATTR_VAL_QUERY);
               break;

            case ClientCommandType.Expand:
               message.Append(ConstInterface.MG_ATTR_VAL_EXPAND);
               break;
            case ClientCommandType.IniputForceWrite:
               message.Append(ConstInterface.MG_ATTR_VAL_INIPUT_FORCE_WRITE);
               break;
            default:
               Logger.Instance.WriteErrorToLog("in Command.buildXML() no such type: " + Type);
               break;
         }

         message.Append("\"");

         if (TaskTag != null && !execStackExists)
         {
            // QCR #980454 - the task id may change during the task's lifetime, so taskId might
            // be holding the old one - find the task and refetch its current ID.
            String id = MGDataTable.Instance.getTaskIdById(TaskTag);

            message.Append(" " + XMLConstants.MG_ATTR_TASKID + "=\"" + id + "\"");
         }
         if (_oper != 0)
            message.Append(" " + ConstInterface.MG_ATTR_OPER + "=\"" + _oper + "\"");
         if (_fldId != -1)
            message.Append(" " + ConstInterface.MG_ATTR_FIELDID + "=\"" + _fldId + "\"");
         if (_ignoreSubformRecompute)
            message.Append(" " + ConstInterface.MG_ATTR_IGNORE_SUBFORM_RECOMPUTE + "=\"" + 1 + "\"");
         if (_handlerId != null && !execStackExists)
            message.Append(" " + ConstInterface.MG_ATTR_HANDLERID + "=\"" + _handlerId + "\"");
         if (MagicEvent != 0)
            message.Append(" " + ConstInterface.MG_ATTR_MAGICEVENT + "=\"" + MagicEvent + "\"");
         if (rollbackType != RollbackEventCommand.RollbackType.NONE)
            message.Append(" " + ConstInterface.MG_ATTR_ROLLBACK_TYPE + "=\"" + (char)rollbackType + "\"");
         if (_exitByMenu)
            message.Append(" " + ConstInterface.MG_ATTR_EXIT_BY_MENU + "=\"" + "=\"1\"");
         if (_operIdx > Int32.MinValue && !execStackExists)
            message.Append(" " + ConstInterface.MG_ATTR_OPER_IDX + "=\"" + _operIdx + "\"");
         if (_ditIdx > Int32.MinValue)
            message.Append(" " + XMLConstants.MG_ATTR_DITIDX + "=\"" + _ditIdx + "\"");
         if (_val != null)
            message.Append(" " + XMLConstants.MG_ATTR_VALUE + "=\"" + XmlParser.escape(_val) + "\"");
         if (_expIdx > Int32.MinValue)
            message.Append(" " + ConstInterface.MG_ATTR_EXP_IDX + "=\"" + _expIdx + "\"");
         if (_expType != StorageAttribute.NONE)
         {
            String maxDigits = "";
            if (_lengthExpVal > 0)
               maxDigits += _lengthExpVal;

            message.Append(" " + ConstInterface.MG_ATTR_EXP_TYPE + "=\"" + (char)_expType + maxDigits + "\"");
         }
         if (!_reversibleExit)
            message.Append(" " + ConstInterface.MG_ATTR_REVERSIBLE + "=\"0\"");

         if (_varList != null)
         {
            message.Append(" " + ConstInterface.MG_ATTR_ARGLIST + "=\"");
            _varList.buildXML(message);
            message.Append("\"");
         }

         if (_generation != null)
            message.Append(" " + ConstInterface.MG_ATTR_GENERATION + "=\"" + _generation + "\"");

         if (_taskVarList != null)
            message.Append(" " + ConstInterface.MG_ATTR_TASKVARLIST + "=\"" + _taskVarList + "\"");

         if (Obj != null)
            message.Append(" " + ConstInterface.MG_ATTR_OBJECT + "=\"" + Obj + "\"");

         if (_mprgCreator != null)
            message.Append(" " + ConstInterface.MG_ATTR_MPRG_SOURCE + "=\"" + _mprgCreator.getTaskTag() + "\"");

         if (_level != 0)
            message.Append(" " + ConstInterface.MG_ATTR_TRANS_LEVEL + "=\"" + _level + "\"");

         if (_key != null)
            message.Append(" " + ConstInterface.MG_ATTR_KEY + "=\"" + _key + "\"");

         if (RefreshMode != 0)
            message.Append(" " + ConstInterface.MG_ATTR_REALREFRESH + "=\"" + (int)RefreshMode + "\"");
         //this tag value alway when sent equals to 1

         if (_keepUserSort)
            message.Append(" " + ConstInterface.MG_ATTR_KEEP_USER_SORT + "=\"1\"");

         if (SubformTaskTag != null)
            message.Append(" " + ConstInterface.MG_ATTR_SUBFORM_TASK + "=\"" + SubformTaskTag + "\"");
         //this tag value always when sent does not equal to 0

         if (_menuUid > Int32.MinValue)
            message.Append(" " + ConstInterface.MG_ATTR_MNUUID + "=\"" + _menuUid + "\"");

         if (MenuComp > Int32.MinValue)
            message.Append(" " + ConstInterface.MG_ATTR_MNUCOMP + "=\"" + MenuComp + "\"");

         if (_closeSubformOnly)
            message.Append(" " + ConstInterface.MG_ATTR_CLOSE_SUBFORM_ONLY + "=\"1\"");

         if (_checkOnly)
            message.Append(" " + ConstInterface.MG_ATTR_CHECK_ONLY + "=\"1\"");

         if (_treePath != null)
            message.Append(" " + ConstInterface.MG_ATTR_PATH + "=\"" + _treePath + "\"");

         if (_treeValues != null)
            message.Append(" " + ConstInterface.MG_ATTR_VALUES + "=\"" + _treeValues + "\"");

         if (_treeIsNulls != null)
            message.Append(" " + ConstInterface.MG_ATTR_TREE_IS_NULLS + "=\"" + _treeIsNulls + "\"");

         if (_direction != -1)
            message.Append(" " + ConstInterface.MG_ATTR_DIRECTION + "=\"" + _direction + "\"");

         if (_incrmentalSearchString != null)
            message.Append(" " + ConstInterface.MG_ATTR_SEARCH_STR + "=\"" + _incrmentalSearchString + "\"");

         if (_resetIncrementalSearch)
            message.Append(" " + ConstInterface.MG_ATTR_RESET_SEARCH + "=\"1\"");

         if (_accessedCacheFiles != null && _accessedCacheFiles.Count > 0)
         {
            hasChildElements = true;
            message.Append(" " + ConstInterface.MG_ATTR_SHOULD_PRE_PROCESS + "=\"Y\"");
            message.Append(XMLConstants.TAG_CLOSE + "\n");

            foreach (KeyValuePair<string, string> cachedFileInfo in _accessedCacheFiles)
            {
               message.Append(XMLConstants.XML_TAB + XMLConstants.XML_TAB + XMLConstants.TAG_OPEN);
               message.Append(ConstInterface.MG_TAG_CACHE_FILE_INFO + " ");
               message.Append(ConstInterface.MG_ATTR_SERVER_PATH + "=\"" + cachedFileInfo.Key + "\" ");
               message.Append(ConstInterface.MG_ATTR_TIMESTAMP + "=\"" + cachedFileInfo.Value + "\"");
               message.Append(XMLConstants.TAG_TERM + "\n");
            }

            message.Append(XMLConstants.XML_TAB + XMLConstants.END_TAG + ConstInterface.MG_TAG_COMMAND);
         }

         switch (Type)
         {
            case ClientCommandType.Query:
               message.Append(" " + ConstInterface.MG_ATTR_VAL_QUERY_TYPE + "=\"");
               switch (_queryType)
               {
                  case QueryType.GLOBAL_PARAMS:
                     message.Append(ConstInterface.MG_ATTR_VAL_QUERY_GLOBAL_PARAMS);
                     break;
                  default:
                     message.Append(ConstInterface.MG_ATTR_VAL_QUERY_CACHED_FILE);
                     message.Append("\" ");
                     message.Append(ConstInterface.MG_ATTR_FILE_PATH);
                     message.Append("=\"");
                     message.Append(_text);
                     break;
               }

               message.Append("\"");
               break;
            case ClientCommandType.IniputForceWrite:
               _text = XmlParser.escape(_text);
               message.Append(" " + ConstInterface.MG_ATTR_VAL_INIPUT_PARAM + "=\"" + _text + "\"");
               break;
            default:
               try
               {
                  MGData currMGData = MGDataTable.Instance.getCurrMGData();
                  int length = currMGData.getTasksCount();
                  bool titleExist = false;
                  Task currFocusedTask = ClientManager.Instance.getLastFocusedTask();

                  for (int i = 0;
                       i < length;
                       i++)
                  {
                     Task task = currMGData.getTask(i);
                     var ctrl = (MgControl)task.getLastParkedCtrl();
                     if (ctrl != null && task.KnownToServer && !task.IsOffline)
                     {
                        if (!titleExist)
                        {
                           message.Append(" " + ConstInterface.MG_ATTR_FOCUSLIST + "=\"");
                           titleExist = true;
                        }
                        else
                           message.Append('$');
                        message.Append(task.getTaskTag() + ",");
                        message.Append((task.getLastParkedCtrl()).getDitIdx());
                     }
                  }

                  if (titleExist)
                     message.Append("\"");

                  if (currFocusedTask != null && !currFocusedTask.IsOffline)
                     message.Append(" " + ConstInterface.MG_ATTR_FOCUSTASK + "=\"" + currFocusedTask.getTaskTag() + "\"");
               }
               catch (Exception ex)
               {
                  Logger.Instance.WriteErrorToLog(ex);
               }
               break;
         }

         if (hasChildElements)
            message.Append(XMLConstants.TAG_CLOSE);
         else
            message.Append(XMLConstants.TAG_TERM);
      }

      /// <summary>
      ///   Get type of the command
      /// </summary>
      protected internal ClientCommandType getType()
      {
         return Type;
      }



      /// <summary>
      ///   Returns TRUE if this command orders the client to replace the id of a task
      /// </summary>
      protected internal bool isReplaceWindowCmd()
      {
         return (_oldId != null);
      }

      /// <summary>
      ///   set the frame of the command if it is not already set
      /// </summary>
      /// <param name = "newFrame">the new frame of the command</param>
      protected internal void setFrame(int newFrame)
      {
         if (_cmdFrame == Int32.MinValue)
            _cmdFrame = newFrame;
      }

      /// <summary>
      ///   For server-commands types issued from a main program only.
      ///   Set the name of the task which caused this main program to participate in the runtime
      ///   flow (i.e. when running a task from a different component, we instantiate his main program
      ///   right above him.
      /// </summary>
      /// <param name = "src">is the "responsible" task.</param>
      internal void setMainPrgCreator(Task src)
      {
         _mprgCreator = src;
      }

      /// <summary>
      /// return the creator task
      /// </summary>
      /// <returns></returns>
      internal Task GetMainPrgCreator()
      {
         return _mprgCreator;
      }

      /// <summary>
      ///   returns the frame of the command or min_value if it wasn't set yet
      /// </summary>
      protected internal int getFrame()
      {
         return _cmdFrame;
      }

      /// <summary>
      ///   returns true if the execution of the command blocks the caller.
      ///   currently the only known command to be blocking is "open modal window".
      /// </summary>
      protected internal bool isBlocking()
      {
         return ((Type == ClientCommandType.OpenURL) &&
                 _isModal && !isReplaceWindowCmd() && (_subformCtrlName == null));
      }

      /// <summary>
      ///   sets the execstack of the current command to be sent to the server
      /// </summary>
      /// <param name = "execStack">- current execution stack of raise event operations from rt </param>
      internal void setExecStack(ExecutionStack execStack)
      {
         _executionStack = new ExecutionStack();
         _executionStack.push(MGDataTable.Instance.getTaskIdById(TaskTag), _handlerId, _operIdx);
         _executionStack.pushUpSideDown(execStack);
      }

      /// <summary>
      ///   Execute the command of every type
      /// </summary>
      /// <param name="expression">to execute for CMD_TYPE_RESULT</param>
      public void Execute(Expression expression)
      {
         switch (Type)
         {
            case ClientCommandType.Abort:
               ExecuteAbortCommand();
               break;

            case ClientCommandType.Verify:
            case ClientCommandType.EnhancedVerify:
               ExecuteVerifyCommand();
               break;

            case ClientCommandType.Result:
               ExecuteResultCommand(expression);
               break;

            case ClientCommandType.OpenURL:
               ExecuteOpenUrlCommand();
               break;

            // code to be used when RangeAdd is called on the server, with a local data field
            case ClientCommandType.AddRange:
               ExecuteAddRangeCommand();
               break;

            case ClientCommandType.ClientRefresh:
               ExecuteClientRefreshCommand();
               break;
            
            default:
               Logger.Instance.WriteErrorToLog("in Command.execute() unknown type: " + Type);
               break;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="callingTask"></param>
      /// <param name="mgDataTab"></param>
      /// <param name="pathParentTask"></param>
      /// <param name="mgd"></param>
      /// <returns></returns>
      private void ExecuteOpenUrlCommand()
      {
         MGData mgd = null;
         MGDataTable mgDataTab = MGDataTable.Instance;
         bool destinationSubformSucceeded = false;
         bool refreshWhenHidden = true;
         int mgdID = 0;
         MgControl subformCtrl = null;
         List<Int32> oldTimers = new List<Int32>(), newTimers = new List<Int32>();
         bool moveToFirstControl = true;
         Task guiParentTask;
         MGData guiParentMgData = null;
         Task callingTask = (_callingTaskTag != null ? (Task)mgDataTab.GetTaskByID(_callingTaskTag) : null);
         Task pathParentTask = (_pathParentTaskTag != null ? (Task)mgDataTab.GetTaskByID(_pathParentTaskTag) : null);

         // TODO (Ronak): It is wrong to set Parent task as a last focus task when 
         // non-offline task is being called from an offline task.

         //When a nonOffline task was called from an Offline task (ofcourse via MP event),
         //the calling task id was not sent to the server because the server is unaware of the 
         //offline task. So, when coming back from the server, assign the correct calling task.
         //Task lastFocusedTask = ClientManager.Instance.getLastFocusedTask();
         //if (lastFocusedTask != null && lastFocusedTask.IsOffline)
         //   _callingTaskTag = lastFocusedTask.getTaskTag();

         guiParentTask = callingTask = (Task)mgDataTab.GetTaskByID(_callingTaskTag);
         if (callingTask != null)
            mgd = callingTask.getMGData();

         //QCR#712370: we should always perform refreshTables for the old MgData before before opening new window
         ClientManager.Instance.EventsManager.refreshTables();

         //ditIdx is send by server only for subform opening for refreshWhenHidden
         if ((_subformCtrlName != null) || (_ditIdx != Int32.MinValue))
         {
            subformCtrl = (_ditIdx != Int32.MinValue
                             ? (MgControl)callingTask.getForm().getCtrl(_ditIdx)
                             : ((MgForm)callingTask.getForm()).getSubFormCtrlByName(_subformCtrlName));
            if (subformCtrl != null)
            {
               var subformTask = subformCtrl.getSubformTask();
               guiParentTask = (Task)subformCtrl.getForm().getTask();
               mgdID = guiParentTask.getMgdID();
               guiParentMgData = guiParentTask.getMGData();
               if (guiParentMgData.getTimerHandlers() != null)
                  oldTimers = guiParentMgData.getTimerHandlers().getTimersVector();

               if (_ditIdx != Int32.MinValue) //for refresh when hidden
               {
                  refreshWhenHidden = false;
                  moveToFirstControl = false;
               }
               else //for destination
               {
                  destinationSubformSucceeded = true;
                  // Pass transaction ownership 
                  if (_transOwner != null)
                  {
                     var newTransOwnerTask = (Task)mgDataTab.GetTaskByID(_transOwner);
                     if (newTransOwnerTask != null)
                        newTransOwnerTask.setTransOwnerTask();
                  }

                  if (subformTask != null)
                  {
                     subformTask.setDestinationSubform(true);
                     subformTask.stop();
                  }

                  if (!ClientManager.Instance.validReturnToCtrl())
                     ClientManager.Instance.ReturnToCtrl = GUIManager.getLastFocusedControl();
               }

               subformCtrl.setSubformTaskId(_newId);
            }
         }

         MGData parentMgData;
         if (callingTask == null)
            parentMgData = MGDataTable.Instance.getMGData(0);
         else
            parentMgData = callingTask.getMGData();

         if (!destinationSubformSucceeded && refreshWhenHidden)
         {
            mgdID = mgDataTab.getAvailableIdx();
            Debug.Assert(mgdID > 0);
            mgd = new MGData(mgdID, parentMgData, _isModal, ForceModal);
            mgd.copyUnframedCmds();
            MGDataTable.Instance.addMGData(mgd, mgdID, false);

            MGDataTable.Instance.currMgdID = mgdID;
         }

         Obj = _key;
         _key = null;

         try
         {
            // Large systems appear to consume a lot of memory, the garbage collector is not always
            // "quick" enough to catch it, so free memory before initiating a memory consuming job as
            // reading a new MGData.
            if (GC.GetTotalMemory(false) > 30000000)
               GC.Collect();

            ClientManager.Instance.ProcessResponse(_newTaskXML, mgdID, new OpeningTaskDetails(callingTask, pathParentTask), null);
         }
         finally
         {
            ClientManager.Instance.EventsManager.setIgnoreUnknownAbort(false);
         }

         if (callingTask != null && subformCtrl != null)
            callingTask.PrepareForSubform(subformCtrl);

         if (destinationSubformSucceeded || !refreshWhenHidden)
         {
            subformCtrl.initSubformTask();
            if (destinationSubformSucceeded)
            {
               var subformTask = subformCtrl.getSubformTask();
               moveToFirstControl = !callingTask.RetainFocus;
               subformTask.setIsDestinationCall(true);
            }
         }

         if (subformCtrl != null)
         {
            if (guiParentMgData.getTimerHandlers() != null)
               newTimers = guiParentMgData.getTimerHandlers().getTimersVector();
            guiParentMgData.changeTimers(oldTimers, newTimers);
         }

         Task nonInteractiveTask = ClientManager.Instance.StartProgram(destinationSubformSucceeded, moveToFirstControl, _varList, _returnVal);

         if (destinationSubformSucceeded || !refreshWhenHidden)
            guiParentTask.resetRcmpTabOrder();

         // in local tasks, ismodal is calculated after the main display, so we need to update the command member
         _isModal = mgd.IsModal;

         // If we have a non interactive task starting, we need to create an eventLoop for it , just like modal.
         // This is because we cannot allow the tasks above it to catch events.
         if (nonInteractiveTask == null)
         {
            // a non interactive parent will cause the called task to behave like modal by having its own events loop.
            if (callingTask != null && ((_isModal && !destinationSubformSucceeded && refreshWhenHidden) || !callingTask.IsInteractive))
               ClientManager.Instance.EventsManager.EventsLoop(mgd);
         }
         else
            ClientManager.Instance.EventsManager.NonInteractiveEventsLoop(mgd, nonInteractiveTask);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="expression"></param>
      private void ExecuteResultCommand(Expression expression)
      {
         // set value to 'global' Expression variable
         if (_isNull)
            expression.setResultValue(null, StorageAttribute.NONE);
         else
         {
            if (_val == null)
               _val = "";
            expression.setResultValue(_val, _attr);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgDataTab"></param>
      private void ExecuteVerifyCommand()
      {
         MGDataTable mgDataTab = MGDataTable.Instance;

         Task task = (Task)(mgDataTab.GetTaskByID(TaskTag) ?? mgDataTab.GetTaskByID(_callingTaskTag));
         if (task == null)
            task = ClientManager.Instance.getLastFocusedTask();

         // In order to keep the behavior same as in Online, verify operation warning messages 
         // will be written as error messages in Client log (QCR #915122)
         if (_errLogAppend)
            Logger.Instance.WriteErrorToLog(_text + ", program : " + ClientManager.Instance.getPrgName());

         //Blank Message will not be shown, same as in Online
         if (!String.IsNullOrEmpty(_text))
         {
            if (_display == ConstInterface.DISPLAY_BOX)
            {
               MgForm currForm;
               int style;

               // on verify box, show translated value. (status is handled in property).
               String mlsTransText = ClientManager.Instance.getLanguageData().translate(_text);
               String mlsTransTitle;

               if (task != null && task.isStarted())
                  currForm = (MgForm)task.getTopMostForm();
               else
                  currForm = null;

               if (Type == ClientCommandType.EnhancedVerify)
               {
                  mlsTransTitle = ClientManager.Instance.getLanguageData().translate(_title);
                  style = Operation.getButtons(_buttonsID);
                  style |= Operation.getImage(_image);
                  style |= Operation.getDefaultButton(_defaultButton);
                  if (task != null)
                  {
                     // Verify command return value can only be used with main program fields
                     if (Task.isMainProgramField(_returnValStr))
                        _returnVal = Operation.InitField(_returnValStr, task);
                  }
               }
               else
               {
                  String options = ClientManager.Instance.getMessageString(MsgInterface.BRKTAB_STOP_MODE_TITLE);
                  _title = ConstUtils.getStringOfOption(options, "EW", _mode);
                  mlsTransTitle = ClientManager.Instance.getLanguageData().translate(_title);
                  //add the icon according to the mode :is Error \ Warning
                  style = Styles.MSGBOX_BUTTON_OK |
                          ((_mode == 'E')
                              ? Styles.MSGBOX_ICON_ERROR
                              : Styles.MSGBOX_ICON_WARNING);
               }

               int returnValue = Commands.messageBox(currForm, mlsTransTitle, mlsTransText, style);

               if (Type == ClientCommandType.EnhancedVerify && task != null)
                  Operation.setoperVerifyReturnValue(returnValue, _returnVal);
            }
            // display message on status only if we have a task
            //Blank Message will not be shown, same as in Online
            else if (_display == ConstInterface.DISPLAY_STATUS && task != null)
            {
               task.DisplayMessageToStatusBar(_text);
            }
         }

         if (_sendAck)
            task.CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.NO_TASKS_OR_COMMANDS);
      }

      /// <summary>
      /// 
      /// </summary>
      private void ExecuteAbortCommand()
      {
         MGDataTable mgDataTab = MGDataTable.Instance;
         int oldMgdID = MGDataTable.Instance.currMgdID;
         MGData mgd = null;

         var task = (Task)mgDataTab.GetTaskByID(TaskTag);

         // Pass transaction ownership 
         if (_transOwner != null)
         {
            var newTransOwnerTask = (Task)mgDataTab.GetTaskByID(_transOwner);
            if (newTransOwnerTask != null)
               newTransOwnerTask.setTransOwnerTask();
         }

         // On special occasions, the server may send abort commands on tasks which were not parsed yet
         if (task == null && ClientManager.Instance.EventsManager.ignoreUnknownAbort())
            return;
         Debug.Assert(task != null);
         mgd = task.getMGData();
         task.stop();
         mgd.abort();

         MGDataTable.Instance.currMgdID = mgd.GetId();
         GUIManager.Instance.abort((MgForm)task.getForm());
         MGDataTable.Instance.currMgdID = (mgd.GetId() != oldMgdID || mgd.getParentMGdata() == null
                                                   ? oldMgdID
                                                   : mgd.getParentMGdata().GetId());

         if (!ClientManager.Instance.validReturnToCtrl())
         {
            MgControl mgControl = GUIManager.getLastFocusedControl();
            ClientManager.Instance.ReturnToCtrl = mgControl;
            if (mgControl != null)// Refresh the status bar.
               ((MgForm)mgControl.getForm()).RefreshStatusBar();
         }
      }

      /// <summary>
      /// code to be used when RangeAdd is called on the server, with a local data field
      /// </summary>
      private void ExecuteAddRangeCommand()
      {
         MGDataTable mgDataTab = MGDataTable.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);
         FieldDef fieldDef = task.DataView.getField((int)UserRange.veeIdx-1);
         int parsedLen;

         AddUserRangeDataviewCommand command = CommandFactory.CreateAddUserRangeDataviewCommand(TaskTag, UserRange);
         command.Range = UserRange;
         if(!UserRange.nullMin)
            command.Range.min = RecordUtils.deSerializeItemVal(UserRange.min, fieldDef.getType(), fieldDef.getSize(), true, fieldDef.getType(), out parsedLen);
         if(!UserRange.nullMax)
            command.Range.max = RecordUtils.deSerializeItemVal(UserRange.max, fieldDef.getType(), fieldDef.getSize(), true, fieldDef.getType(), out parsedLen);
         
         task.DataviewManager.Execute(command);
      }

      /// <summary>
      /// Execute subform refresh that is sent from the server
      /// </summary>
      private void ExecuteClientRefreshCommand()
      {
         MGDataTable mgDataTab = MGDataTable.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);
         Debug.Assert(task.IsSubForm);

         if (task.AfterFirstRecordPrefix && !task.isInEndTask())
         {
            IClientCommand command = CommandFactory.CreateSubformRefreshCommand(((Task)task).getParent().getTaskTag(), task.getTaskTag(), true);
            ((Task)task).DataviewManager.Execute(command);
         }
      }

      /// <summary>
      ///   combine two commands. Necessary only for abort CMDs. Sometimes the server sends two aborts
      ///   for the same task. Together, they contain the whole abort information.
      /// </summary>
      /// <param name = "cmd">the command to be combined</param>
      protected internal void combine(Command cmd)
      {
         _url = cmd._url;
      }

      /// <summary>
      ///   sets value
      /// </summary>
      /// <param name = "value">- true or false</param>
      internal void setCloseSubformOnly(bool value)
      {
         _closeSubformOnly = value;
      }

      /// <summary>
      ///   sets value
      /// </summary>
      /// <param name = "value">- true or false</param>
      internal void setCheckOnly(bool value)
      {
         _checkOnly = value;
      }

      /// <summary>
      /// return the menu Uid
      /// </summary>
      /// <returns></returns>
      internal int GetMenuUid()
      {
         return _menuUid;
      }

      /// <summary>
      /// return the dit index
      /// </summary>
      /// <returns></returns>
      internal int GetDitIdx()
      {
         return _ditIdx;
      }

      /// <summary>
      /// return direction for column sort.
      /// </summary>
      /// <returns></returns>
      internal int GetDirection()
      {
         return _direction;
      }

      /// <summary>
      /// return the incremental locate string
      /// </summary>
      /// <returns></returns>
      internal string GetIncrmentalSearchString()
      {
         return _incrmentalSearchString;
      }

      internal int GetFieldId()
      {
         return _fldId;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal bool GetKeepUserSort()
      {
         return _keepUserSort;
      }

      #region Nested type: QueryType

      internal enum QueryType
      {
         NULL,
         GLOBAL_PARAMS,
         CACHED_FILE
      } ;

      #endregion




      public bool IsBlocking
      {
         get { return isBlocking(); }
      }

      public int Frame
      {
         get
         {
            return getFrame();
         }
         set
         {
            setFrame(value);
         }
      }

      public bool WillReplaceWindow { get { return isReplaceWindowCmd(); } }

      public void HandleAttribute(string attribute, string valueStr)
      {
         switch (attribute)
         {
            case XMLConstants.MG_ATTR_TASKID:
               TaskTag = valueStr;
               break;

            case ConstInterface.MG_ATTR_HANDLERID:
               {
                  int i = valueStr.IndexOf(",");
                  string handlerId = i > -1
                                  ? valueStr.Substring(i + 1)
                                  : valueStr;
                  _handlerId = handlerId;
               }
               break;

            case ConstInterface.MG_ATTR_OBJECT:
               Obj = valueStr;
               break;

            case ConstInterface.MG_ATTR_MESSAGE:
               XmlParser.unescape(valueStr);
               break;

            case ConstInterface.MG_ATTR_HTML:
               _html = XmlParser.unescape(valueStr);
               break;

            case ConstInterface.MG_ATTR_MODAL:
               _isModal = (valueStr[0] == '1');
               break;

            case ConstInterface.MG_ATTR_URL:
               _url = XmlParser.unescape(valueStr);
               break;

            case ConstInterface.MG_ATTR_SRCTASK:
               break;

            case XMLConstants.MG_ATTR_VALUE:
               _val = XmlParser.unescape(valueStr);
               break;

            case ConstInterface.MG_ATTR_CALLINGTASK:
               _callingTaskTag = valueStr;
               break;

            case ConstInterface.MG_ATTR_PATH_PARENT_TASK:
               _pathParentTaskTag = valueStr;
               break;

            case ConstInterface.MG_ATTR_ARGLIST:
               _varList = new ArgumentsList();
               _varList.fillList(valueStr, (Task)MGDataTable.Instance.GetTaskByID(TaskTag));
               break;

            case ConstInterface.MG_ATTR_TEXT:
               _text = XmlParser.unescape(valueStr);
               break;

            case ConstInterface.MG_ATTR_TITLE:
               _title = valueStr;
               break;

            case ConstInterface.MG_ATTR_IMAGE:
               _image = valueStr[0];
               break;

            case ConstInterface.MG_ATTR_BUTTONS:
               _buttonsID = valueStr[0];
               break;

            case ConstInterface.MG_ATTR_DEFAULT_BUTTON:
               _defaultButton = XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_RETURN_VAL:
               _returnValStr = valueStr;
               break;

            case ConstInterface.MG_ATTR_ERR_LOG_APPEND:
               _errLogAppend = XmlParser.getBoolean(valueStr);
               break;

            case ConstInterface.MG_ATTR_DISPLAY:
               _display = valueStr[0];
               break;

            case ConstInterface.MG_ATTR_MODE:
               _mode = valueStr[0];
               break;

            case ConstInterface.MG_ATTR_ACK:
               _sendAck = (XmlParser.getInt(valueStr) != 0);
               break;

            case ConstInterface.MG_ATTR_NULL:
               _isNull = (XmlParser.getInt(valueStr) == 1);
               break;

            case ConstInterface.MG_ATTR_PAR_ATTRS:
               _attr = (StorageAttribute)valueStr[0];
               break;

            case ConstInterface.MG_ATTR_OLDID:
               _oldId = StrUtil.rtrim(valueStr);
               break;

            case ConstInterface.MG_ATTR_NEWID:
               _newId = StrUtil.rtrim(valueStr);
               break;

            case ConstInterface.MG_ATTR_TOP:
               XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_LEFT:
               XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_WIDTH:
               XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_HEIGHT:
               XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_LEN:
               _htmlLen = XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_ENCRYPT:
               _encryptXML = (XmlParser.getInt(valueStr) == 1);
               break;

            case ConstInterface.MG_ATTR_SCAN:
               _htmlScan = (XmlParser.getInt(valueStr) == 1);
               break;

            case ConstInterface.MG_ATTR_STANDALONE:
               _standalone = (XmlParser.getInt(valueStr) == 1);
               break;

            case ConstInterface.MG_ATTR_KEY:
               _key = valueStr;
               break;

            case XMLConstants.MG_ATTR_DITIDX:
               _ditIdx = XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_TRANS_OWNER:
               _transOwner = valueStr;
               break;

            case ConstInterface.MG_ATTR_SUBFORM_CTRL:
               _subformCtrlName = valueStr;
               break;

            // code to be used when RangeAdd is called on the server, with a local data field
            case ConstInterface.MG_ATTR_FIELD_INDEX:
               // temporary code! will be moved to a factory
               if (UserRange == null)
                  UserRange = new UserRange() { nullMin = true, nullMax = true };
               UserRange.veeIdx = XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_MIN_VALUE:
               UserRange.min = valueStr;
               UserRange.nullMin = false;
               break;

            case ConstInterface.MG_ATTR_MAX_VALUE:
               UserRange.max = valueStr;
               UserRange.nullMax = false;
               break;
            // end of RangeAdd code

            default:
               Logger.Instance.WriteErrorToLog(
                  "There is no such tag in Command class. Insert case to Command.initElements for " + attribute);
               break;
         }      
      }
   }
}
