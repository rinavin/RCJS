using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.httpclient;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using EventHandler = com.magicsoftware.richclient.events.EventHandler;
using Field = com.magicsoftware.richclient.data.Field;
using com.magicsoftware.unipaas.management.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

#if PocketPC
using System.IO;
#else
using Process = com.magicsoftware.richclient.util.Process;
#endif

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   ///   data for <oper> tag
   /// </summary>
   internal class Operation
   {
      private readonly YesNoExp _condExp = new YesNoExp(true);
      private readonly YesNoExp _retainFocus = new YesNoExp(false);
      private readonly YesNoExp _waitExp = new YesNoExp(false);
      private readonly YesNoExp _syncData = new YesNoExp(false);//call operation: Sync data

      private ArgumentsList _argList;
      private String _assemblyNameSnippet; // Assembly name for snippet

      private int _blockClose;
      // in Block Begin/Else/Loop operation: the operation id which is the end of the execution block (else/end)

      private int _blockEnd; // in Block Begin/Else/Loop operation: the Block End operation that ends the nesting level

      private char _buttons; // buttons of verify opr
      private bool _checkByServer; // for Call operation with subform destination
      private IClientCommand _cmdToServer;
      private int _defaultButton; // default button of verify opr
      private Expression _defaultButtonExp; // expression attached to default button of verify opr
      private char _display = 'S'; // Status line | message Box
      private bool _errLogAppend; // append to error log for verify opr
      private EventHandler _evtHandler;
      private bool _execOnServer; // for block if which should be executed on the server
      private Expression _exp;
      private Field _field; // reference to the field
      private bool _globalParamsShouldBeCopied;
      private char _image; // image of verify opr
      private bool _incremental; // INCR->true, NORMAL->false (how attribute)
      private String _methodNameSnippet; // Entry point methodName for snippet
      private char _mode; // Error|Warning
      // but we still need to skip the block operations in the client
      private FlowDirection _operDirection = FlowDirection.Combined;
      private FlwMode _operFlowMode = FlwMode.Combine;
      private String _prgDescription;
      private String _publicName;
      private Field _returnVal; // return value of verify opr
      private RunTimeEvent _rtEvt; // for Raise event

      private int _serverId = -1;
      // the index of this operation in the server's flow (0 means it's the first operation in the handler, 1 for the second etc.). Valid for: Server
      private TaskDefinitionId calledTaskDefinitionId;
      internal TaskDefinitionId CalledTaskDefinitionId
      {
         get { return calledTaskDefinitionId; }
      }

      internal CallOperationMode OperCallMode { get; set; }

      private CallOsShow _show = CallOsShow.Normal;
      private String _subformCtrlName; // for Call operation with subform destination
      private char _subtype; // If|Else|Loop      
      private Task _task; // the task of the operation

      internal Task Task
      {
         get { return _task; }
         set { _task = value; }
      }
      private String _text; // the verify message
      private String _title; // title of verify opr
      private Expression _titleExp; // expression attached to title of verify opr
      private int _type; // operation number (like in Magic)
      private bool _undo = true;

      /// <summary>
      ///   Constructor
      /// </summary>
      protected internal Operation()
      {
      }

      private bool Immediate
      {
         get { return _waitExp.getVal(); }
      }

      /// <summary>
      ///   parse an operation
      /// </summary>
      protected internal void fillData(Task taskRef, EventHandler evtHandler)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;

         _task = taskRef;
         _evtHandler = evtHandler;

         while (initInnerObjects(parser, parser.getNextTag(), taskRef))
         {
         }
      }

      /// <summary>
      ///   init inner attributes of Operation object on parsing time
      /// </summary>
      private bool initInnerObjects(XmlParser parser, String foundTagName, Task taskRef)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_EVENT))
         {
            _rtEvt = new RunTimeEvent(taskRef);
            _rtEvt.fillData(parser, taskRef);
         }
         else if (foundTagName.Equals(ConstInterface.MG_TAG_OPER))
         {
            fillAttributes(parser, taskRef);
         }
         else if (foundTagName.Equals("/" + ConstInterface.MG_TAG_OPER))
         {
            parser.setCurrIndex2EndOfTag(); // setCurrIndex(XmlParser.isNextTagClosed(MG_TAG_CONTROL));
            return false;
         }
         else if (foundTagName.Equals(XMLConstants.MG_TAG_TASKDEFINITIONID_ENTRY))
         {
            InitTaskDefinitionID(parser);
         }
         else
         {
            Logger.Instance.WriteExceptionToLog(
               "There is no such tag in Operation. Insert else if to Operation.initInnerObjects for " + foundTagName);
            return false;
         }

         return true;
      }

      /// <summary>
      ///   get the attributes of the operation
      /// </summary>
      /// <param name = "taskRef">the task of the handler current</param>
      private void fillAttributes(XmlParser parser, Task taskRef)
      {
         List<String> tokensVector;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_OPER) + ConstInterface.MG_TAG_OPER.Length);

            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            initElements(tokensVector, taskRef);

            // init inner reference:
            parser.setCurrIndex(endContext + XMLConstants.TAG_CLOSE.Length); // to delete "/>" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in Command.FillData() out of string bounds");
      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every found	element</param>
      /// <param name = "taskRef">current</param>
      private void initElements(List<String> tokensVector, Task taskRef)
      {
         String attribute, valueStr;
         int expId;
         bool isGuiThreadExecution = false;

         for (int j = 0;
              j < tokensVector.Count;
              j += 2)
         {
            attribute = (tokensVector[j]);
            valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case XMLConstants.MG_ATTR_TYPE:
                  _type = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_FLD:
                  _field = InitField(valueStr, _task);
                  break;
               case XMLConstants.MG_ATTR_EXP:
                  expId = XmlParser.getInt(valueStr);
                  _exp = taskRef.getExpById(expId);
                  break;
               case ConstInterface.MG_ATTR_TEXT:
                  _text = XmlParser.unescape(valueStr);
                  break;
               case ConstInterface.MG_ATTR_MODE:
                  _mode = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_SUBTYPE:
                  _subtype = valueStr[0];
                  if (_type == ConstInterface.MG_OPER_BLOCK)
                  {
                     if (_subtype == 'E')
                        _type = ConstInterface.MG_OPER_ELSE;
                     else if (_subtype == 'L')
                        _type = ConstInterface.MG_OPER_LOOP;
                  }
                  break;
               case ConstInterface.MG_ATTR_CLOSE:
                  _blockClose = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_END:
                  _blockEnd = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_HOW:
                  _incremental = (valueStr.Equals("I"));
                  break;
               case ConstInterface.MG_ATTR_UNDO:
                  if (valueStr[0] == 'N')
                     _undo = false;
                  else if (valueStr[0] == 'Y')
                     _undo = true;
                  else
                     Logger.Instance.WriteExceptionToLog(
                        "in Operation.initElements(): No such value to the MG_ATTR_UNDO for " + valueStr);
                  break;
               case ConstInterface.MG_ATTR_DISPLAY:
                  _display = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_WAIT:
                  _waitExp.setVal(taskRef, valueStr);
                  break;
               case ConstInterface.MG_ATTR_RETAIN_FOCUS:
                  _retainFocus.setVal(taskRef, valueStr);
                  break;
               case ConstInterface.MG_ATTR_SYNC_DATA:
                  _syncData.setVal(taskRef, valueStr);
                  break;
               case ConstInterface.MG_ATTR_SHOW:
                  _show = (CallOsShow)XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_CND:
                  _condExp.setVal(taskRef, valueStr);
                  break;
               case ConstInterface.MG_ATTR_ARGLIST:
                  _argList = new ArgumentsList();
                  _argList.fillList(valueStr, _task);
                  break;
               case ConstInterface.MG_ATTR_REFRESHON:
                  _argList.RefreshOnString = valueStr.Trim();
                  break;
               case ConstInterface.MG_ATTR_SERVER_ID:
                  _serverId = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_OPER_CALLMODE:
                  OperCallMode = (CallOperationMode)valueStr[0];
                  break;

               case ConstInterface.MG_ATTR_SUBFORM_CTRL:
                  _subformCtrlName = XmlParser.unescape(valueStr);
                  break;
               case ConstInterface.MG_ATTR_CHECK_BY_SERVER:
                  _checkByServer = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_PUBLIC:
                  _publicName = valueStr;
                  break;
               case ConstInterface.MG_ATTR_PRG_DESCRIPTION:
                  _prgDescription = valueStr;
                  break;
               case ConstInterface.MG_ATTR_CPY_GLB_PRMS:
                  _globalParamsShouldBeCopied = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_EXEC_ON_SERVER:
                  _execOnServer = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_TITLE:
                  _title = XmlParser.unescape(valueStr);
                  break;
               case ConstInterface.MG_ATTR_TITLE_EXP:
                  expId = XmlParser.getInt(valueStr);
                  _titleExp = taskRef.getExpById(expId);
                  break;
               case ConstInterface.MG_ATTR_IMAGE:
                  _image = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_BUTTONS:
                  _buttons = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_DEFAULT_BUTTON:
                  _defaultButton = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_DEFAULT_BUTTON_EXP:
                  expId = XmlParser.getInt(valueStr);
                  _defaultButtonExp = taskRef.getExpById(expId);
                  break;
               case ConstInterface.MG_ATTR_RETURN_VAL:
                  _returnVal = InitField(valueStr, _task);
                  break;
               case ConstInterface.MG_ATTR_ERR_LOG_APPEND:
                  _errLogAppend = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_OPER_DIRECTION:
                  _operDirection = (FlowDirection)valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_OPER_FLOWMODE:
                  _operFlowMode = (FlwMode)valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_OPER_METHODNAME:
                  _methodNameSnippet = valueStr;
                  break;
               case ConstInterface.MG_ATTR_OPER_ASMURL:
                  _assemblyNameSnippet = addURLToAssemblyDictionary(valueStr, isGuiThreadExecution);
                  break;
               case ConstInterface.MG_ATTR_OPER_ASMNAME:
                  _assemblyNameSnippet = valueStr;
                  break;
               case ConstInterface.MG_ATTR_OPER_ASMCONTENT:
                  addContentToAssemblyDictionary(valueStr, isGuiThreadExecution);
                  break;
               case ConstInterface.MG_ATTR_IS_GUI_THREAD_EXECUTION:
                  isGuiThreadExecution = XmlParser.getBoolean(valueStr);
                  break;
               default:
                  Logger.Instance.WriteExceptionToLog(
                     "There is no such tag in Operation class. Insert case to Operation.initElements for " + attribute);
                  break;
            }
         }

         if (_serverId > -1)
            _cmdToServer = CommandFactory.CreateExecOperCommand(_task.getTaskTag(), "" + _evtHandler.getId(), _serverId, Int32.MinValue, null);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="parser"></param>
      internal void InitTaskDefinitionID(XmlParser parser)
      {
         string xmlBuffer = parser.ReadToEndOfCurrentElement();
         //int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         //string xmlBuffer = parser.getXMLsubstring(endContext + XMLConstants.TAG_TERM.Length);

         //parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length);

         TaskDefinitionIdTableSaxHandler handler = new TaskDefinitionIdTableSaxHandler(SetTaskDefinitionId);
         handler.parse(Encoding.UTF8.GetBytes(xmlBuffer));
      }

      /// <summary>
      /// callback for the sax parser
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      /// <param name="xmlId"></param>
      /// <param name="defaultTagList"></param>
      void SetTaskDefinitionId(TaskDefinitionId taskDefinitionId)
      {
         calledTaskDefinitionId = taskDefinitionId;
      }

      /// <summary>
      ///   Initialize the field
      /// </summary>
      /// <param name = "valueStr">a reference to the field</param>
      /// <param name = "task"></param>
      internal static Field InitField(String valueStr, Task task)
      {
         List<String> TaskField = XmlParser.getTokens(valueStr, ", ");
         Field field;
         int parent, fldId;

         if (TaskField.Count == 2)
         {
            parent = Int32.Parse(TaskField[0]);
            fldId = Int32.Parse(TaskField[1]);
            field = (Field)task.getField(parent, fldId);
            return field;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog(string.Format("Unknown field: '{0}'", valueStr));
            return null;
         }
      }

      /// <summary>
      ///   execute the operation
      /// </summary>
      /// <param name = "mprgCreator">relevant only when the operation belongs to a main program.
      ///   It identifies the task which caused this main program to participate in the runtime
      ///   flow (i.e. when running a task from a different component, we instantiate his main program
      ///   right above it.
      /// </param>
      /// <param name = "ctrl">the focus need to be returned after the VERIFY operation in Message Box needs only
      ///   all another operations can use null
      /// </param>
      /// <param name = "returnedFromServer">true if the server returned the execution to this operation</param>
      /// <returns> boolean for verify and block operations</returns>
      internal bool execute(bool returnedFromServer)
      {
         RunTimeEvent lastRtEvt = ClientManager.Instance.EventsManager.getLastRtEvent();
         Task mprgCreator = null;
         FlowMonitorQueue flowMonitor = FlowMonitorQueue.Instance;

         if (lastRtEvt != null)
            mprgCreator = lastRtEvt.getMainPrgCreator();

         if (returnedFromServer ||
             !returnedFromServer && (_type != ConstInterface.MG_OPER_SERVER || !_condExp.isServerExp()))
         {
            if (!canExecute())
            {
               if (_type != ConstInterface.MG_OPER_LOOP)
                  flowMonitor.addFlowFieldOperation(this, _task.getFlowMode(), _task.getDirection(), false);
               return false;
            }
         }
         if (_type != ConstInterface.MG_OPER_LOOP && _type != ConstInterface.MG_OPER_ENDBLOCK)
            // We call addFlowOperationSelect only at the begining and end of the loop at EventHandler::execute
            flowMonitor.addFlowFieldOperation(this, _task.getFlowMode(), _task.getDirection(), true);

         try
         {
            switch (_type)
            {
               case ConstInterface.MG_OPER_VERIFY:
                  return operVerify();
               case ConstInterface.MG_OPER_BLOCK:
                  return operBlock();
               case ConstInterface.MG_OPER_LOOP:
                  if (!getExecOnServer())
                     return operBlock();
                  else
                  {
                     operServer(mprgCreator);
                     break;
                  }
               case ConstInterface.MG_OPER_ELSE:
                  return operElse();
               case ConstInterface.MG_OPER_EVALUATE:
                  operEvaluate();
                  break;
               case ConstInterface.MG_OPER_UPDATE:
                  operUpdate(mprgCreator);
                  break;
               case ConstInterface.MG_OPER_USR_EXIT:
                  operInvokeOS();
                  break;
               case ConstInterface.MG_OPER_RAISE_EVENT:
                  operRaiseEvent(mprgCreator, returnedFromServer);
                  break;
               case ConstInterface.MG_OPER_SERVER:
                  operServer(mprgCreator);
                  break;
               case ConstInterface.MG_OPER_CALL:
                  CommandsProcessorBase server = CommandsProcessorManager.GetCommandsProcessor(this);
                  if (_assemblyNameSnippet != null)
                     operInvokeDotNet();
                  else if (_publicName != null && server.AllowParallel())
                  {
#if PocketPC
                     if (_publicName != null)
                         Logger.Instance.WriteWarningToLog(ClientManager.Instance.getMessageString(MsgInterface.STR_WARN_PARALLEL_NOT_SUPPORTED));
#endif
                     operCallParallel();
                  }
                  else if (_subformCtrlName == null)
                     operCall(mprgCreator);
                  // Subform Destination Call
                  else
                  {
                     Task subformTask;
                     bool isEndProg = true;

                     if (_checkByServer)
                     {
                        ExecOperCommand command = _cmdToServer as ExecOperCommand;
                        Debug.Assert(command != null);

                        command.CheckOnly = true;
                        operCall(mprgCreator);
                        command.CheckOnly = false;
                        if (ClientManager.Instance.EventsManager.getNextOperIdx(this, true) > -1)
                           isEndProg = false;
                     }

                     if (isEndProg)
                     {
                        MgControl destSubForm = ((MgForm)_task.getForm()).getSubFormCtrlByName(_subformCtrlName);

                        if (destSubForm != null)
                        {
                           subformTask = destSubForm.getSubformTask();
                           if (subformTask != null)
                           {
                              if (subformTask.endTask(true, false, true, false))
                              {
                                 Task parentTask = destSubForm.getForm().getTask() as Task;
                                 parentTask.TaskService.RemoveRecomputes(parentTask, subformTask);
                              }
                              else
                                 // if the task failed to terminate, do no proceed to call the new task.
                                 break;
                           }

                           if (GUIManager.getLastFocusedControl() != null &&
                               GUIManager.getLastFocusedControl().isDescendentOfControl(destSubForm))
                              _task.RetainFocus = false;
                           else
                              _task.RetainFocus = _retainFocus.getVal();
                        }
                        operCall(mprgCreator);
                     }
                  }
                  break;
               default:
                  Logger.Instance.WriteExceptionToLog("There is no such type of operation " + _type);
                  break;
            }
         }
         catch (Exception exception)
         {
            // If we have a dotnet exception, we should return false so that we can execute the immediate next operation.
            // Otherwise, throw the exception.
            if (exception is DNException)
               return false;

            throw exception;
         }

         return true;
      }

      /// <summary>
      ///   call a parallel rich-client program
      /// </summary>
      private void operCallParallel()
      {
         callParallel(_task, _publicName, _argList, _globalParamsShouldBeCopied, _prgDescription);
      }

      internal static void callParallel(Task task, String publicName, ArgumentsList argList,
                                        bool globalParamsShouldBeCopied, String prgDescription)
      {
         if (task.IsOffline)
         {
            Logger.Instance.WriteExceptionToLog("Can't run a parallel program from an offline task");
            return;
         }

         //  The rich-client will query the runtime-engine for the up-to-date list of global params 
         //  of the current context only if the callED (parallel) program requests copying of global params, 
         //  the callER program should query them from the runtime-engine
         if (globalParamsShouldBeCopied)
         {
            MGData mgData = task.getMGData();
            IClientCommand cmdGlbParms = CommandFactory.CreateQueryGlobalParamsCommand();
            mgData.CmdsToServer.Add(cmdGlbParms);
            CommandsProcessorManager.GetCommandsProcessor(task).Execute(CommandsProcessorBase.SendingInstruction.ONLY_COMMANDS);
         }

         // clone the execution properties of the current client
         MgProperties calledProgramProps = ClientManager.Instance.copyExecutionProps();
         ClientManager.Instance.setGlobalParams(null);

         // set the called program's internal name
         calledProgramProps[ConstInterface.REQ_PRG_NAME] = publicName;

         // Server sends prgDesciprtion only when PublicName is not specified.
         // Otherwise server sends a fictive prgDescription like : _Program_Id_<ISN>.
         // When public name is specified prgDescription is not used and it will be null.
         if (!String.IsNullOrEmpty(prgDescription))
            calledProgramProps[ConstInterface.REQ_PRG_DESCRIPTION] = prgDescription;

         calledProgramProps[ConstInterface.PARALLEL_EXECUTION] = "Y";

         // arguments to the parallel program
         if (argList != null)
         {
            String prgArgs = argList.toURL(true);
            calledProgramProps[ConstInterface.REQ_ARGS] = prgArgs;
         }

         var XMLdata = new StringBuilder();
         try
         {
            calledProgramProps.storeToXML(XMLdata);

            // If encoding was disabled, enable it to create the right command line for the parallel program
            Boolean encodingEnabled = HttpUtility.EncodingEnabled;
            if (!encodingEnabled)
               HttpUtility.EncodingEnabled = true;

            String spawningArguments = HttpUtility.UrlEncode(XMLdata.ToString(), Encoding.UTF8);

            if (!encodingEnabled)
               HttpUtility.EncodingEnabled = false;

            // spawn a new instance of the assembly inside the current process 
#if !PocketPC
            Process.StartCurrentExecutable(spawningArguments);
#else
            com.magicsoftware.richclient.util.Process.RenameAndStartCurrentExecutable(spawningArguments);
#endif
         }
         catch (Exception execption)
         {
            callOprShowError(task, execption.ToString());
         }
      }

      /// <summary>
      /// </summary>
      /// <param name = "ClientManager.Instance"></param>
      /// <param name = "task"></param>
      /// <param name = "errMsg"></param>
      private static void callOprShowError(Task task, String errMsg)
      {
         Manager.WriteToMessagePane((Task)task.GetContextTask(), errMsg, false);
         FlowMonitorQueue.Instance.addFlowInvokeOsInfo(errMsg);
         Logger.Instance.WriteExceptionToLog(errMsg);
      }

      /// <summary>
      ///   returns the condition value
      /// </summary>
      private bool getCondVal()
      {
         return _condExp.getVal();
      }

      /// <summary>
      ///   get the operation type
      /// </summary>
      internal int getType()
      {
         return _type;
      }

      /// <summary>
      ///   returns the server ID of this operation
      /// </summary>
      internal int getServerId()
      {
         return _serverId;
      }

      /// <summary>
      ///   execute Verify
      /// </summary>
      /// <returns> boolean is true if and only if mode==error</returns>
      private bool operVerify()
      {
         bool isError = (_mode == ConstInterface.FLW_VERIFY_MODE_ERROR || _mode == ConstInterface.FLW_VERIFY_MODE_REVERT);

         string textToDisplay = _exp == null
                                   ? _text
                                   : _exp.evaluate(StorageAttribute.UNICODE, 255);

         string titleToDisplay = _titleExp == null
                                    ? _title
                                    : _titleExp.evaluate(StorageAttribute.UNICODE, 255);

         textToDisplay = textToDisplay == null
                            ? ""
                            : StrUtil.rtrim(textToDisplay);

         titleToDisplay = titleToDisplay == null
                             ? ""
                             : StrUtil.rtrim(titleToDisplay);

         // InOrder to keep the behavior same as in Online, Verify Operation Warning messages will be written 
         // as Error messages in Client log (QCR #915122)
         if (_errLogAppend)
         {
            string PrgDescription = ClientManager.Instance.getPrgDescription();
            if (PrgDescription == null)
               PrgDescription = ClientManager.Instance.getPrgName();
            Logger.Instance.WriteExceptionToLog(textToDisplay + ", program : " + PrgDescription);
         }

         if (_display == ConstInterface.DISPLAY_STATUS)
         // Status line
         {
            //Blank Message will not be shown, same as in Online
            if (!String.IsNullOrEmpty(textToDisplay))
            {
               FlowMonitorQueue.Instance.addFlowVerifyInfo(textToDisplay);
               //Product #178, Topic #61: beep on the client side only upon user errors (e.g. duplicate record), otherwise only if started from the studio (F7)
               Boolean beep = ClientManager.StartedFromStudio;
               Manager.WriteToMessagePane((Task)_task.GetContextTask(), textToDisplay, beep);
            }
         }
         // Message Box
         else
         {
            //Blank Message will not be shown, same as in Online
            if (!String.IsNullOrEmpty(textToDisplay))
            {
               // mls translation when displaying to box. for status the rans is i the control property.
               String mlsTransTextToDisplay = ClientManager.Instance.getLanguageData().translate(textToDisplay);
               String mlsTransTitleToDisplay = ClientManager.Instance.getLanguageData().translate(titleToDisplay);

               int tmpdefaultButton = _defaultButtonExp == null
                                         ? _defaultButton
                                         : (_defaultButtonExp.evaluate(StorageAttribute.NUMERIC)).MgNumVal.NUM_2_LONG();

               int verifyMode = getButtons(_buttons);
               verifyMode |= getImage(_image);
               verifyMode |= getDefaultButton(tmpdefaultButton);

               if (UtilStrByteMode.isLocaleDefLangJPN())
               {
                  int delimPos = mlsTransTextToDisplay.IndexOf('|');
                  if (0 <= delimPos && delimPos < mlsTransTextToDisplay.Length)
                  {
                     mlsTransTitleToDisplay = mlsTransTextToDisplay.Substring(delimPos + 1);
                     mlsTransTextToDisplay = mlsTransTextToDisplay.Substring(0, delimPos);
                  }
               }

               MgForm mgForm = null;
               // get the object for which the messagebox will be displayed
               if (!((Task)_task.GetContextTask()).getMGData().IsAborting)
                  mgForm = (MgForm)((Task)_task.GetContextTask()).getTopMostForm();

               // if the form of the current task is null (main program) then use the form
               // of last focused task
               if (mgForm == null && ClientManager.Instance.getLastFocusedTask() != null)
                  mgForm = (MgForm)ClientManager.Instance.getLastFocusedTask().getTopMostForm();

               int returnValue = Commands.messageBox(mgForm, mlsTransTitleToDisplay, mlsTransTextToDisplay, verifyMode);
               setoperVerifyReturnValue(returnValue, _returnVal);
            }
         }

         // if we are in the revert mode and get a verify error, nullify the revert mode
         if (_mode == ConstInterface.FLW_VERIFY_MODE_ERROR && _task.getRevertFrom() > -1)
         {
            _task.setRevertFrom(-1);
            _task.setRevertDirection(Direction.FORE);
         }

         if (_mode == ConstInterface.FLW_VERIFY_MODE_REVERT)
         {
            // set the revert direction to the opposite of what it was.
            _task.setRevertFrom(_evtHandler.getOperationTab().serverId2operIdx(_serverId, 0));
            Direction dir = _task.getRevertDirection();
            _task.setRevertDirection(dir == Direction.FORE
                                        ? Direction.BACK
                                        : Direction.FORE);

            // reverse the direction of flow
            Direction flowDir = _task.getDirection();
            _task.setDirection(Direction.NONE);
            _task.setDirection(flowDir == Direction.FORE || flowDir == Direction.NONE
                                  ? Direction.BACK
                                  : Direction.FORE);
         }

         return isError;
      }

      /// <summary>
      ///   returns code assigned to corresponding buttonsID
      /// </summary>
      internal static int getButtons(char buttonsID)
      {
         int tmpbuttons = 0;

         switch (buttonsID)
         {
            case ConstInterface.BUTTONS_OK:
               tmpbuttons = Styles.MSGBOX_BUTTON_OK;
               break;
            case ConstInterface.BUTTONS_OK_CANCEL:
               tmpbuttons = Styles.MSGBOX_BUTTON_OK_CANCEL;
               break;
            case ConstInterface.BUTTONS_ABORT_RETRY_IGNORE:
               tmpbuttons = Styles.MSGBOX_BUTTON_ABORT_RETRY_IGNORE;
               break;
            case ConstInterface.BUTTONS_YES_NO_CANCEL:
               tmpbuttons = Styles.MSGBOX_BUTTON_YES_NO_CANCEL;
               break;
            case ConstInterface.BUTTONS_YES_NO:
               tmpbuttons = Styles.MSGBOX_BUTTON_YES_NO;
               break;
            case ConstInterface.BUTTONS_RETRY_CANCEL:
               tmpbuttons = Styles.MSGBOX_BUTTON_RETRY_CANCEL;
               break;
            default:
               break;
         }

         return tmpbuttons;
      }

      /// <summary>
      ///   returns code assigned to corresponding imageID
      /// </summary>
      internal static int getImage(char imageID)
      {
         int tmpImage = 0;

         switch (imageID)
         {
            case ConstInterface.IMAGE_EXCLAMATION:
               tmpImage = Styles.MSGBOX_ICON_EXCLAMATION;
               break;
            case ConstInterface.IMAGE_CRITICAL:
               tmpImage = Styles.MSGBOX_ICON_ERROR;
               break;
            case ConstInterface.IMAGE_QUESTION:
               tmpImage = Styles.MSGBOX_ICON_QUESTION;
               break;
            case ConstInterface.IMAGE_INFORMATION:
               tmpImage = Styles.MSGBOX_ICON_INFORMATION;
               break;
            default:
               break;
         }
         return tmpImage;
      }

      /// <summary>
      ///   returns code assigned to corresponding defaultButtonID
      /// </summary>
      internal static int getDefaultButton(int defaultButtonID)
      {
         int tmpdefaultButton = 0;

         switch (defaultButtonID)
         {
            case 1:
               tmpdefaultButton = Styles.MSGBOX_DEFAULT_BUTTON_1;
               break;
            case 2:
               tmpdefaultButton = Styles.MSGBOX_DEFAULT_BUTTON_2;
               break;
            case 3:
               tmpdefaultButton = Styles.MSGBOX_DEFAULT_BUTTON_3;
               break;
            default:
               break;
         }

         return tmpdefaultButton;
      }

      /// <summary>
      ///   Set Verify Operation Return Value
      /// </summary>
      internal static void setoperVerifyReturnValue(int returnValue, Field returnVal)
      {
         if (returnVal != null)
         {
            var retValueNum = new NUM_TYPE();
            String returnValueStr;

            retValueNum.NUM_4_LONG(returnValue);
            returnValueStr = retValueNum.toXMLrecord();

            returnVal.setValueAndStartRecompute(returnValueStr, false, true, true, false);
            returnVal.updateDisplay();
         }
      }

      /// <summary>
      ///   execute Block
      /// </summary>
      /// <returns> boolean true
      /// </returns>
      private bool operBlock()
      {
         return true;
      }

      /// <summary>
      ///   execute else / elseif
      /// </summary>
      /// <returns> boolean true
      /// </returns>
      private bool operElse()
      {
         return true;
      }

      /// <summary>
      ///   execute Evaluate
      /// </summary>
      private void operEvaluate()
      {
         String result;

         if (_field != null)
         {
            result = _exp.evaluate(_field.getType(), _field.getSize());
            _field.setValueAndStartRecompute(result, result == null, true, true, false);
            _field.updateDisplay();
         }
         else
            result = _exp.evaluate(StorageAttribute.BOOLEAN, 0);
      }

      /// <summary>
      ///   execute Update
      /// </summary>
      private void operUpdate(Task mprgCreator)
      {
         String result, oldVal, newVal, fieldVal;
         bool setRecordUpdated;
         bool recompute;
         NUM_TYPE nOld, nNew, nResult;

         FlowMonitorQueue flowMonitor = FlowMonitorQueue.Instance;
         flowMonitor.addFlowOperationUpdate(FlowMonitorInterface.FLWMTR_START);

         // for non-modifiable field go to the server, i.e. server can abort the task
         if (!_field.DbModifiable && (_task.getMode() != Constants.TASK_MODE_CREATE))
         {
            if(!CanModify(mprgCreator))
               return;
         }

         StorageAttribute fieldType = _field.getType();

         // if this is an incremental update
         if (_incremental)
         {
            if (!_field.IsLinkField)
            {
               fieldVal = _field.getValue(true);
               nResult = (fieldVal != null
                             ? new NUM_TYPE(fieldVal)
                             : null);

               // subtract the old value if not in create mode
               if (_task.getMode() != Constants.TASK_MODE_CREATE)
               {
                  // indicate we are evaluating the old value of the fields
                  _task.setEvalOldValues(true);
                  oldVal = (_field.isNull()
                               ? _field.getMagicDefaultValue()
                               : _exp.evaluate(fieldType, _field.getSize()));
                  _task.setEvalOldValues(false);
                  nOld = new NUM_TYPE(oldVal);
                  nResult = NUM_TYPE.sub(nResult, nOld);
               }

               if (_task.getMode() != Constants.TASK_MODE_DELETE)
               {
                  newVal = _exp.evaluate(fieldType, _field.getSize());
                  if (newVal != null)
                  {
                     nNew = new NUM_TYPE(newVal);
                     nResult = NUM_TYPE.add(nResult, nNew);
                  }
                  else
                     nNew = nResult = null;
               }
               if (nResult != null)
                  result = nResult.toXMLrecord();
               else
                  result = _field.getMagicDefaultValue();
            }
            else
            {
               /* If the incremental update is for a link field, go to server. */
               /* This is required to be done on server because on client, we do */
               /* not have the reference to the old link record. */
               /* And for incremental update, we need to work on 2 records as in */
               /* case of QCR# 900738.                                           */
               operServer(mprgCreator);
               return;
            }
         }
         // end of  incremental update
         else
            result = _exp.evaluate(fieldType, _field.getSize());

         //QCR 746053 if the cells type don't match throw error
         if (fieldType == StorageAttribute.BLOB_VECTOR)
            if (result != null)
               result = operUpdateVectors(_field, result);

         recompute = (_field.getTask() == _task);
         setRecordUpdated = (!recompute || !_undo);
         _field.setValueAndStartRecompute(result, result == null, true, setRecordUpdated, false);
         if (!_undo)
            _field.setUpdated();

         //QCR # 270949 : If field is updated with update operation, modified flag should be set in order to cause re-compute.
         _field.setModified();

         _field.updateDisplay();
         flowMonitor.addFlowOperationUpdate(FlowMonitorInterface.FLWMTR_END);
      }

      /// <summary>
      /// can a non-modifiable field be modified. the way to get the answer depends on the local/remote dataview
      /// </summary>
      /// <param name="mprgCreator">true if the field may be modified and the calling method should continue</param>
      /// <returns></returns>
      private bool CanModify(Task mprgCreator)
      {
         ExecOperCommand command = _cmdToServer as ExecOperCommand;

         command.Operation = this;
         command.MprgCreator = mprgCreator;
         return Task.DataviewManager.Execute(_cmdToServer).Success;
      }

      /// <summary>
      ///   updates the data content of a vector without changing its header data
      ///   such as cell type and size - is used only in update operations between vectors
      ///   for example vectors of alpha with cell size the differ or vector of memo with vector of alpha
      /// </summary>
      /// <param name = "vec">the flat representation of the the new vector
      ///   field the field to be updated
      /// </param>
      /// <returns> a flat representation of the updated vector after the Update</returns>
      internal static String operUpdateVectors(Field field, String vec)
      {
         VectorType newVec = null;
         String res = null;
         if (field.getType() == StorageAttribute.BLOB_VECTOR)
         {
            StorageAttribute srcAttr = VectorType.getCellsAttr(vec);
            StorageAttribute dstAttr = field.getCellsType();

            if (StorageAttributeCheck.isTheSameType(srcAttr, dstAttr))
            {
               newVec = new VectorType(vec);
               newVec.adjustToFit(field);
               res = newVec.ToString();
            }

            return res;
         }
         else
            throw new ApplicationException("in operUpdateVectors " + field.getName() + " is not of type vector");
      }

      /// <summary>
      ///   execute Invoke OS
      /// </summary>
      private void operInvokeOS()
      {
         String command;
         int retCode = -1;

         if (_exp == null)
            command = _text;
         else
            command = _exp.evaluate(StorageAttribute.ALPHA, 2048);

         command = ClientManager.Instance.getEnvParamsTable().translate(command);
         bool wait = _waitExp.getVal();

         String errMsg = null;
         int exitCode = 0;
         retCode = ProcessLauncher.InvokeOS(command, wait, _show, ref errMsg, ref exitCode);

         if (retCode == 0)
            retCode = exitCode;

         if (errMsg != null)
         {
            Manager.WriteToMessagePane((Task)_task.GetContextTask(), errMsg, false);
            FlowMonitorQueue.Instance.addFlowInvokeOsInfo(errMsg);
            Logger.Instance.WriteExceptionToLog(errMsg);
         }


         /* Set the field with the result. */
         if (_field != null)
         {
            String returnVal;

            var retCodeNum = new NUM_TYPE();
            retCodeNum.NUM_4_LONG(retCode);
            returnVal = retCodeNum.toXMLrecord();

            _field.setValueAndStartRecompute(returnVal, false, true, true, false);
            _field.updateDisplay();
         }
      }

      /// <summary>
      ///   execute Raise Event
      /// </summary>
      /// <param name = "mprgCreator">if the operation belongs to a main program it points to the task which
      ///   caused the main program to become active. Otherwise it's false.
      /// </param>
      /// <param name = "returnedFromServer">indicates whether we just came back from the server to be passed to ClientManager.Instance.EventsManager.handleEvent()</param>
      private void operRaiseEvent(Task mprgCreator, bool returnedFromServer)
      {
         bool immediate;
         RunTimeEvent aRtEvt;

         // create a new run time event using the original as template
         aRtEvt = _rtEvt.replicate();

         aRtEvt.setPublicName();

         immediate = Immediate;
         aRtEvt.setImmediate(immediate);
         aRtEvt.setMainPrgCreator(null);

         if (immediate)
         {
            aRtEvt.setCtrl((MgControl)_task.getLastParkedCtrl());
            aRtEvt.setArgList(_argList);

            if (aRtEvt.getTask().isMainProg())
               aRtEvt.setMainPrgCreator(mprgCreator);

            ClientManager.Instance.EventsManager.pushExecStack(_task.getTaskTag(), "" + _evtHandler.getId(), _serverId);
            ClientManager.Instance.EventsManager.handleEvent(aRtEvt, returnedFromServer);
            ClientManager.Instance.EventsManager.popExecStack();
         }
         else
         {
            // send arguments by value
            aRtEvt.setArgList(new ArgumentsList(_argList));
            aRtEvt.setTask(null);
            ClientManager.Instance.EventsManager.addToTail(aRtEvt);
         }
      }

      /// <summary>
      ///   execute browser Call operation
      /// </summary>
      /// <param name = "mprgCreator">if the operation belongs to a main program it points to the task which
      ///   caused the main program to become active. Otherwise it's false.
      /// </param>
      private void operCall(Task mprgCreator)
      {
         operServer(mprgCreator);
      }

      /// <summary>
      ///   execute Server operation
      /// </summary>
      /// <param name = "mprgCreator">if the operation belongs to a main program it points to the task which
      ///   caused the main program to become active. Otherwise it's false.
      /// </param>
      internal void operServer(Task mprgCreator)
      {
         ExecOperCommand command = _cmdToServer as ExecOperCommand;
         Debug.Assert(command != null);

         if (_task.isMainProg())
            command.MprgCreator = mprgCreator;
         else
            command.MprgCreator = null;
         command.SetExecutionStack(ClientManager.Instance.EventsManager.getExecStack());

         command.Operation = this;

         ClientManager.Instance.execRequestWithSubformRecordCycle(_task.getMGData().CmdsToServer, _cmdToServer, null, mprgCreator);
      }

      /// <summary>
      ///   returns the value of the Block End index
      /// </summary>
      internal int getBlockEnd()
      {
         return _blockEnd;
      }

      /// <summary>
      ///   sets the value of the Block End index
      /// </summary>
      protected internal void setBlockEnd(int val)
      {
         _blockEnd = val;
      }

      /// <summary>
      ///   returns the value of the Block Close index
      /// </summary>
      internal int getBlockClose()
      {
         return _blockClose;
      }

      /// <summary>
      ///   sets the value of the Block Close index
      /// </summary>
      protected internal void setBlockClose(int val)
      {
         _blockClose = val;
      }

      internal String getTaskTag()
      {
         return _task.getTaskTag();
      }

      internal String getHandlerId()
      {
         return "" + _evtHandler.getId();
      }

      internal bool getExecOnServer()
      {
         return _execOnServer;
      }

      /// <summary>
      /// </summary>
      /// <param name = "handler"></param>
      /// <returns></returns>
      private bool checkFlowForHandler(EventHandler handler)
      {
         int internalEvent;
         Event handlerEvt = handler.getEvent();

         switch (handlerEvt.getType())
         {
            case ConstInterface.EVENT_TYPE_INTERNAL:
               internalEvent = handlerEvt.getInternalCode();

               // return TRUE for all internal action (like Zoom) handlers except...
               if (internalEvent != InternalInterface.MG_ACT_TASK_PREFIX &&
                   internalEvent != InternalInterface.MG_ACT_TASK_SUFFIX &&
                   internalEvent != InternalInterface.MG_ACT_REC_PREFIX &&
                   internalEvent != InternalInterface.MG_ACT_REC_SUFFIX &&
                   internalEvent != InternalInterface.MG_ACT_CTRL_PREFIX &&
                   internalEvent != InternalInterface.MG_ACT_CTRL_SUFFIX &&
                   internalEvent != InternalInterface.MG_ACT_VARIABLE)
                  return true;

               break;

            case ConstInterface.EVENT_TYPE_SYSTEM:
            case ConstInterface.EVENT_TYPE_USER:
               return true;
         }

         return false;
      }

      /// <summary>
      /// </summary>
      /// <param name = "handler"></param>
      /// <returns></returns>
      private bool checkDirForHandler(EventHandler handler)
      {
         int internalEvent;
         Event handlerEvt = handler.getEvent();

         switch (handlerEvt.getType())
         {
            case ConstInterface.EVENT_TYPE_INTERNAL:
               internalEvent = handlerEvt.getInternalCode();

               // return TRUE for all internal action (like Zoom) handlers except...
               if (internalEvent != InternalInterface.MG_ACT_TASK_PREFIX &&
                   internalEvent != InternalInterface.MG_ACT_TASK_SUFFIX &&
                   internalEvent != InternalInterface.MG_ACT_REC_PREFIX &&
                   internalEvent != InternalInterface.MG_ACT_REC_SUFFIX &&
                   internalEvent != InternalInterface.MG_ACT_VARIABLE)
                  return true;

               break;

            case ConstInterface.EVENT_TYPE_SYSTEM:
            case ConstInterface.EVENT_TYPE_USER:
               return true;
         }

         return false;
      }

      /// <summary>
      ///   checks if the operation can be executed or not
      /// </summary>
      /// <returns></returns>
      private bool canExecute()
      {
         bool canExecute = false;
         bool checkFlow = checkFlowForHandler(_evtHandler);
         bool checkDir = checkDirForHandler(_evtHandler);

         Direction taskDirection = _task.getDirection();
         Flow taskFlowMode = _task.getFlowMode();

         // Oper can execute if Condition is true and operation flow matches the flow of task
         if (getCondVal())
         {
            bool validFlow = false, validDir = false;

            // set task dir to forward if none
            if (taskDirection == Direction.NONE)
               taskDirection = Direction.FORE;

            // set task flow mode to step if none
            if (taskFlowMode == Flow.NONE)
               taskFlowMode = Flow.STEP;

            if (checkDir)
            {
               switch (_operDirection)
               {
                  case FlowDirection.Combined:
                     validDir = true;
                     break;

                  case FlowDirection.Forward:
                     validDir = (taskDirection == Direction.FORE)
                                   ? true
                                   : false;
                     break;

                  case FlowDirection.Backward:
                     validDir = (taskDirection == Direction.BACK)
                                   ? true
                                   : false;
                     break;
               }
            }
            else
               validDir = true;

            // if direstion is valid, check for flow
            if (validDir)
            {
               if (checkFlow)
               {
                  switch (_operFlowMode)
                  {
                     case FlwMode.Combine:
                        validFlow = true;
                        break;

                     case FlwMode.Fast:
                        validFlow = (taskFlowMode == Flow.FAST)
                                       ? true
                                       : false;
                        break;

                     case FlwMode.Step:
                        validFlow = (taskFlowMode == Flow.STEP)
                                       ? true
                                       : false;
                        break;
                  }
               }
               else
                  validFlow = true;
            }

            canExecute = validFlow && validDir;
         }

         return canExecute;
      }

      /// <summary>
      ///   add the Code snippet URL to Assembly Dictionary (with snippet filename as Assembly Name), and
      ///   returns this assembly name
      /// </summary>
      /// <param name = "url"></param>
      /// <param name = "isGuiThreadExecution"></param>
      /// <returns></returns>
      private string addURLToAssemblyDictionary(string url, bool isGuiThreadExecution)
      {
         if (url != null && url.Length != 0)
         {
            String filename = "";
            int startIdx = url.IndexOf(ConstInterface.RC_TOKEN_CACHED_FILE);

            // get the file name from the url
            if (startIdx > 0)
            {
               string truncatedStr = url.Substring(startIdx + ConstInterface.RC_TOKEN_CACHED_FILE.Length);
               int endIdx = truncatedStr.IndexOf(ConstInterface.CACHED_ASSEMBLY_EXTENSION);
               filename = truncatedStr.Remove(endIdx, truncatedStr.Length - endIdx);
            }
            else
               throw new Exception(string.Format("Invalid AssemblyURL {0}", url));

            ReflectionServices.AddAssembly(filename, false, url, isGuiThreadExecution);

            return filename;
         }
         return null;
      }

      /// <summary>
      ///   Change the base64 string back into binari snippet and add the assembly
      /// </summary>
      /// <param name = "b64content"></param>
      private void addContentToAssemblyDictionary(string b64content, bool isGuiThreadExecution)
      {
         if (b64content != null && b64content.Length > 0)
         {
#if !PocketPC
            ReflectionServices.AddAssembly(_assemblyNameSnippet, Base64.decodeToByte(b64content), isGuiThreadExecution);
#else
            BinaryWriter bw = new BinaryWriter(File.Open(_assemblyNameSnippet, FileMode.Create));
            bw.Write(Base64.decodeToByte(b64content));
            bw.Close();
            ReflectionServices.AddAssembly(_assemblyNameSnippet, false, _assemblyNameSnippet, isGuiThreadExecution);
#endif
         }
      }

      /// <summary>
      ///   Execute operation Invoke dotnet
      /// </summary>
      private void operInvokeDotNet()
      {
         // if there exists no assembly name or methodname, return.
         if (_assemblyNameSnippet == null || _methodNameSnippet == null)
            return;

         // execute code snippet
         Object retObj = executeCodeSnippet();

         // get the field
         Field retValFld = _returnVal;

         // update the return field
         if (retValFld != null)
            retValFld.UpdateWithDNObject(retObj, true);
      }

      /// <summary>
      ///   execute code snippet
      /// </summary>
      /// <returns></returns>
      private object executeCodeSnippet()
      {
         Type classType;
         MemberInfo memberInfo = null;
         MethodInfo methodInfo = null;
         int assemblyHashCode;
         Object retObj = null;

         try
         {
            // get asm hash code
            assemblyHashCode = ReflectionServices.GetHashCode(_assemblyNameSnippet);

            // get the class type
            classType = ReflectionServices.GetType(assemblyHashCode, "Snippet");

            // get the member
            memberInfo = ReflectionServices.GetMemeberInfo(classType, _methodNameSnippet, true, null);

            if (memberInfo is MethodInfo)
               methodInfo = (MethodInfo)memberInfo;

            ParameterInfo[] paramInfos = ReflectionServices.GetParameters(methodInfo);
            Object[] arguements = null;
            Object[] argsOldValues = null;

            // get arguments
            if (paramInfos.Length > 0)
            {
               arguements = createSnippetArgs(paramInfos);

               //Save copy of snippet arguments to check if referenced objects are modified.
               argsOldValues = new object[arguements.Length];
               for (int i = 0; i < arguements.Length; i++)
                  argsOldValues[i] = arguements[i];
            }

            // invoke the snippet and get the return object
            retObj = ReflectionServices.InvokeSnippetMethod(assemblyHashCode, methodInfo, arguements);

            // update ref out parameters
            if (paramInfos.Length > 0)
               updateSnippetRefParams(arguements, paramInfos, argsOldValues);
         }
         catch (Exception exception)
         {
            DNException dnException = ClientManager.Instance.RuntimeCtx.DNException;
            dnException.set(exception);
            throw dnException;
         }
         return retObj;
      }

      /// <summary>
      ///   creates arguments for code snippet
      /// </summary>
      /// <param name = "paramInfos"></param>
      /// <returns></returns>
      private object[] createSnippetArgs(ParameterInfo[] paramInfos)
      {
         var arguments = new Object[paramInfos.Length];
         Argument arg = null;
         Field argFld = null;
         Object dotnetObj = null;
         String fldVal;
         Type parameterType;

         for (int i = 0;
              i < paramInfos.Length;
              i++)
         {
            arg = _argList.getArg(i);
            argFld = arg.getField();
            parameterType = ReflectionServices.GetType(paramInfos[i]);

            // if we received a field
            if (argFld != null)
            {
               fldVal = argFld.getValue(false);

               // if the field is a dotnet
               if (argFld.getType() == StorageAttribute.DOTNET)
               {
                  dotnetObj = DNManager.getInstance().DNObjectsCollection.GetDNObj(BlobType.getKey(fldVal));

                  // if it is a structure, do a shallow copy
                  if (dotnetObj != null && !dotnetObj.GetType().IsClass)
                     dotnetObj = DNConvert.doShallowCopy(dotnetObj);

                  arguments[i] = dotnetObj;
               }
               else
                  // non-dotnet fields
                  arguments[i] = DNConvert.convertMagicToDotNet(fldVal, argFld.getType(), parameterType);
            }
            else //otherwise must be an expression
            {
               Expression argExp = arg.getExp();

               // evaluate expression with return type as a dotnet object
               if (argExp != null)
               {
                  // evaluate the expression with expected type as STORAGE_ATTR_NONE.
                  GuiExpressionEvaluator.ExpVal expVal = argExp.evaluate(StorageAttribute.NONE);

                  if (expVal.Attr == StorageAttribute.DOTNET)
                  {
                     dotnetObj = DNManager.getInstance().DNObjectsCollection.GetDNObj(expVal.DnMemberInfo.dnObjectCollectionIsn);

                     // if it is a structure, do a shallow copy
                     if (dotnetObj != null && !dotnetObj.GetType().IsClass)
                        dotnetObj = DNConvert.doShallowCopy(dotnetObj);

                     arguments[i] = dotnetObj;
                  }
                  else
                     arguments[i] = GuiExpressionEvaluator.ConvertMagicToDotNet(expVal, parameterType);
               }
            }
         }

         return arguments;
      }

      /// <summary>
      ///   updates ref parameters to the code snippet
      /// </summary>
      /// <param name = "arguements">arguments with new values</param>
      /// <param name = "paramInfos">parameters info</param>
      /// <param name = "argsOldValues">arguments with old values</param>
      /// <returns></returns>
      private void updateSnippetRefParams(object[] arguements, ParameterInfo[] paramInfos, object[] argsOldValues)
      {
         ParameterInfo paramInfo;
         Field argFld = null;
         int key = 0;
         String fldVal;

         for (int i = 0;
              i < paramInfos.Length;
              i++)
         {

            paramInfo = paramInfos[i];

            if (paramInfo.ParameterType.IsByRef)
            {
               if (argsOldValues[i] == null && arguements[i] == null)
                  continue;

               // Check if the referenced parameter is updated, to avoid updating the field and perform 
               // recompute , when value is not actually updated.
               if ((argsOldValues[i] != null && arguements[i] != null) &&
                   (argsOldValues[i].Equals(arguements[i])))
                  continue;

               argFld = _argList.getArg(i).getField();

               // if the field is a dotnet
               if (argFld.getType() == StorageAttribute.DOTNET)
               {
                  // create temporary entry in DNObjectsCollection and add object to it. call setvalue to
                  // set the value to fld (from this temp entry) and do recompute.
                  key = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);

                  DNManager.getInstance().DNObjectsCollection.Update(key, arguements[i]);

                  fldVal = BlobType.createDotNetBlobPrefix(key);
               }
               else
                  fldVal = DNConvert.convertDotNetToMagic(arguements[i], argFld.getType());

               // set value and do recompute
               argFld.setValueAndStartRecompute(fldVal, false, true, true, false);
               argFld.updateDisplay();

               // remove the temporary entry created
               if (key != 0)
                  DNManager.getInstance().DNObjectsCollection.Remove(key);
            }
         }
      }

      /// <summary>
      /// </summary>
      /// <param name = "buffer"></param>
      /// <returns></returns>
      internal void AddFlowDescription(StringBuilder buffer)
      {
         switch (_type)
         {
            case ConstInterface.MG_OPER_VERIFY:
               buffer.Append("Verify: ");
               if (_exp == null)
                  buffer.Append(_text);
               else
                  buffer.Append("Exp #").Append(_exp.getId());
               break;

            case ConstInterface.MG_OPER_BLOCK:
               buffer.Append("Block If");
               break;

            case ConstInterface.MG_OPER_LOOP:
               buffer.Append("Block Loop");
               break;

            case ConstInterface.MG_OPER_ELSE:
               buffer.Append("Block Else");
               break;

            case ConstInterface.MG_OPER_EVALUATE:
               buffer.Append("Evaluate Exp #").Append(_exp.getId());
               break;

            case ConstInterface.MG_OPER_UPDATE:
               buffer.AppendFormat(null, "Update {0} with Exp #{1}", _field.getVarName(), _exp.getId());
               break;

            case ConstInterface.MG_OPER_ENDBLOCK:
               buffer.Append("End Block");
               break;

            case ConstInterface.MG_OPER_USR_EXIT:
               buffer.Append("Invoke OS");
               break;

            case ConstInterface.MG_OPER_RAISE_EVENT:
               buffer.Append("Raise Event:");
               _rtEvt.AppendDescription(buffer);
               buffer.AppendFormat(null, " (Wait={0})", (Immediate
                                                            ? 'Y'
                                                            : 'N'));
               break;

            case ConstInterface.MG_OPER_SERVER:
               buffer.Append("Run server-side operation");
               break;

            case ConstInterface.MG_OPER_CALL:
               buffer.Append("Call program");
               break;

            default:
               buffer.AppendFormat(null, "<<Unknown Operation Code {0}>>", _type);
               break;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal ArgumentsList GetArgList()
      {
         return _argList;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal Field GetReturnValueField()
      {
         return _field;
      }

      /// <summary>
      ///  returns subform control name for call with destination
      /// </summary>
      /// <returns></returns>
      internal String GetSubformControlName()
      {
         return _subformCtrlName;
      }

      /// <summary>
      /// return true is the operation is local call operation
      /// </summary>
      internal bool IsLocalCall
      {
         get
         {
            return getType() == ConstInterface.MG_OPER_CALL &&
            (OperCallMode == CallOperationMode.Program || OperCallMode == CallOperationMode.SubTask);
         }
      }
   }
}
