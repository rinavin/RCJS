using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.events;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.util;
using DataView = com.magicsoftware.richclient.data.DataView;
using Field = com.magicsoftware.richclient.data.Field;
using Record = com.magicsoftware.richclient.data.Record;
using Task = com.magicsoftware.richclient.tasks.Task;
using FieldsTable = com.magicsoftware.richclient.data.FieldsTable;
using com.magicsoftware.richclient.communications;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.commands.ServerToClient;

namespace com.magicsoftware.richclient.events
{
   /// <summary>
   ///   this class manage the events, put them in a queue and handle them
   /// </summary>
   internal class EventsManager : IEventsManager
   {
      private enum EventScope
      {
         NONE = ' ',
         TRANS = 'T'
      }

      // LOCAL CONSTANTS
      private const bool REAL_ONLY = true;
      private const int MAX_OPER = 9999;

      private readonly MgPriorityBlockingQueue _eventsQueue; // the queue of events to execute
      private readonly Stack _execStack; // Execution stack of raise event operation or calls to user
      private readonly Stack _rtEvents; // A stack of in-process run-time events (the last one is at the top).
      private readonly Stack _serverExecStack; // Execution stack of Execution stacks of operations that
      private EventsAllowedType _allowEvents = EventsAllowedType.ALL;
      private CompMainPrgTable _compMainPrgTab; // the order of handling main programs of components
      private MgControl _currCtrl;
      private Field _currField; // for every event executed this variable will contain the field attached to the control on which the event was fired
      private KeyboardItem _currKbdItem;
      private bool _endOfWork;
      private EventScope _eventScope = EventScope.NONE; // instructs how to process events which are propagated to slave tasks during execution.
      private ForceExit _forceExit = ForceExit.None;
      private Task _forceExitTask = null;
      private bool _ignoreUnknownAbort; // TRUE when 'abort' commands on unknown tasks may arrive
      private bool _initialized; // used to detect re-initialization
      private bool _isHandlingForceExit; // indicate we are currently handling a forceExit related handler
      private bool _isNonReversibleExit; // is the command nonreversible

      // was sent from the server. we need a stack of execstacks
      // here because we might call a new program before we handled
      // the previous server exec stack
      private bool _stopExecution;
      private bool _processingTopMostEndTask; // indication whether we are currently processing events from the 
      private MgControl _stopExecutionCtrl; // holds the ctrl on which we get the first stop execution
      private int _subver;
      private int _ver;
      private bool _isSorting;
      private MgControl _nextParkedCtrl;

      /// <summary>
      ///   private CTOR to prevent instantiation
      /// </summary>
      internal EventsManager()
      {
         _rtEvents = new Stack();

         _eventsQueue = new MgPriorityBlockingQueue();

         _execStack = new Stack();
         _execStack.Push(new ExecutionStack());

         _serverExecStack = new Stack();
         _serverExecStack.Push(null);

         _nextParkedCtrl = null;

         Events.AllowFormsLockSetEvent += setAllowFormsLock;
         Events.AllowFormsLockGetEvent += getAllowFormsLock;
         Events.GetEventTimeEvent += GetEventTime;
      }

      /// <summary>
      ///   QCR #802495, We have a form locking mechanism in FormsController.
      ///   This property is responsible for enabling/disabling this mechanism
      ///   The mechanism is disable if we are in Non interactive task with AllowEvents = true and Open Window = true
      ///   We need it to prevent blocking events in these tasks
      /// </summary>
      internal bool AllowFormsLock { get; set; }

      #region IEventsManager Members

      /// <summary>
      ///   add an event to the end of the queue
      /// </summary>
      /// <param name = "irtEvt">the event to be added </param>
      internal void addToTail(RunTimeEvent rtEvt)
      {
         _eventsQueue.put(rtEvt);
      }

      /// <summary>
      ///   add an keyboard event to the end of the queue
      /// </summary>
      /// <param name = "mgForm">form </param>
      /// <param name = "mgControl">control </param>
      /// <param name = "modifier">modifier </param>
      /// <param name = "start">start of selected text </param>
      /// <param name = "end">end of selected text  </param>
      /// <param name = "text">text </param>
      /// <param name = "im">im </param>
      /// <param name = "isActChar">is keyboard char </param>
      /// <param name = "suggestedValue">suggested value for choice </param>
      /// <param name = "code">code to be added in the queue </param>
      public void AddKeyboardEvent(MgFormBase mgForm, MgControlBase mgControl, Modifiers modifier, int keyCode,
                                   int start, int end, string text, ImeParam im,
                                   bool isActChar, string suggestedValue, int code)
      {
         RunTimeEvent rtEvt = mgControl != null
                                 ? new RunTimeEvent((MgControl)mgControl, true, ClientManager.Instance.IgnoreControl)
                                 : new RunTimeEvent((Task)mgForm.getTask(), true);

         rtEvt.setInternal(code);

         var newKbItem = new KeyboardItem(keyCode, modifier);

         if (isActChar && (mgControl != null && (mgControl.isTextControl() || mgControl.isRichEditControl())))
            newKbItem.setAction(InternalInterface.MG_ACT_CHAR);

         rtEvt.setKeyboardItem(newKbItem);

         if (code == InternalInterface.MG_ACT_CTRL_KEYDOWN)
         {
            rtEvt.setEditParms(start, end, text);
            rtEvt.setImeParam(im);
         }
         else
            rtEvt.setValue(suggestedValue);

         ClientManager.Instance.EventsManager.addToTail(rtEvt);
         return;
      }

      /// <summary>
      ///   QCR 760381 if after handling the event the current control is unparkable try
      ///   an move to the next parkable control
      /// </summary>
      internal void checkParkability()
      {
         MgControl current = GUIManager.getLastFocusedControl();

         /* QCR# 925464. The problem was that on deleting a record, CS of the control gets executed. 
         * So, the task's level is set to TASK. Then we go to server an on returning back from the server, 
         * we refresh the form. Here, since the last parked control i.e. "from" (numeric) is hidden, 
         * we try to park on the next parkable control from Property.RefreshDisplay(). This makes the 
         * task's level as CONTROL. Now, we try to execute RP, but since the level is CONTROL, 
         * we do not execute it. The solution is that checkParkability() which is called from 
         * Property.RefreshDisplay(), should try to park on the control only if the task's level is CONTROL.
         */
         if (current != null && (current.getForm().getTask()).getLevel() == Constants.TASK_LEVEL_CONTROL)
            // #992957 - we should skip direction check, when checking for parkability
            moveToParkableCtrl(current, false);
      }

      /// <summary>
      ///   handle GetDataViewContent event.
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "generation"></param>
      /// <param name = "varList"></param>
      /// <param name = "outputType"> Output Type of DataViewContent</param>
      internal String GetDataViewContent(TaskBase taskBase, int generation, String varList, DataViewOutputType outputType, int destinationDataSourceNumber, string listIndexesOfDestinationSelectedFields)
      {
         Task task = taskBase as Task;
         IClientCommand cmd = CommandFactory.CreateGetDataViewContentCommand(task.getTaskTag(), generation.ToString(), varList, outputType, destinationDataSourceNumber, listIndexesOfDestinationSelectedFields);
         task.getMGData().CmdsToServer.Add(cmd);

         //Execute MG_ACT_GET_DATAVIEW_CONTENT command.
         task.CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);

         //Get dataViewContent
         String retString = task.dataViewContent;

         //reset task.dataViewContent to null.
         task.dataViewContent = null;

         return retString;
      }

      /// <summary>
      /// Handle the WriteErrorMessagesToServerlog file event.
      /// </summary>
      /// <param name="taskBase"></param>
      /// <param name="errorMessage"></param>
      internal void WriteErrorMessageesToServerLog(TaskBase taskBase, string errorMessage)
      {
         Task task = taskBase as Task;
         IClientCommand cmd = CommandFactory.CreateWriteMessageToServerLogCommand(task.getTaskTag(), errorMessage);

         task.getMGData().CmdsToServer.Add(cmd);
         task.CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);

         ClientManager.Instance.ErrorToBeWrittenInServerLog = string.Empty;
      }

      /// <summary>
      ///   handle situation with no parkable controls
      /// </summary>
      /// <param name = "itask"></param>
      internal void HandleNonParkableControls(ITask itask)
      {
         var task = (Task)itask;
         //we can not stay in create mode with no parkable controls
         if (task.getMode() == Constants.TASK_MODE_CREATE && task.IsInteractive)
         {
            task.enableModes();
            char oldMode = task.getOriginalTaskMode();
            handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
            if (!GetStopExecutionFlag())
            {
               if (oldMode != Constants.TASK_MODE_CREATE)
               {
                  task.setMode(oldMode);
                  ((DataView)task.DataView).currRecCompute(true);
                  handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                  ((MgForm)task.getForm()).RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                  task.setLastParkedCtrl(null);
                  Manager.SetFocus(task, null, -1, true);
                  return;
               }
            }
            //we can not stay in create mode or in stop execution state with no parkable controls 
            exitWithError(task, MsgInterface.RT_STR_CRSR_CANT_PARK);
         }
         if (GetStopExecutionFlag() && task.IsInteractive && !task.HasMDIFrame)
            //there is no control to stay on  
            exitWithError(task, MsgInterface.RT_STR_CRSR_CANT_PARK);
         task.setLastParkedCtrl(null);
         ClientManager.Instance.ReturnToCtrl = null;
         Manager.SetFocus(task, null, -1, true);
      }

      /// <summary>
      ///   handle a single event
      /// </summary>
      /// <param name = "irtEvt">the event to process </param>
      /// <param name = "returnedFromServer">indicates whether we returned from the server - to be passed to handler.execute() </param>
      internal void handleEvent(RunTimeEvent rtEvt, bool returnedFromServer)
      {
         EventHandler handler = null;
         var pos = new EventHandlerPosition();
         Task task = rtEvt.getTask();
         var ctrl = (MgControl)task.getLastParkedCtrl();
         bool forceExitDone = false;
         bool propagate;
         bool bRcBefore;
         bool rtEvtChanged;
         bool endTaskError = false;
         EventHandler.RetVals handlerRetVals = null;
         RunTimeEvent oldRtEvt = null;
         Flow oldFlowMode = task.getFlowMode();
         Direction oldDirection = task.getDirection();
         ForceExit oldForceExit = getForceExit();
         Task oldForceExitTask = getForceExitTask();
         bool oldIsHandlingForceExit = _isHandlingForceExit;
         bool restoreIsForceExit = false;
         MgControl restoreCurrCtrl = null;
         Field restoreCurrField = null;

         // the action need to be check according to the task on the event. 
         // If the action is internal (under ACT_TOT), do nothing if its disabled or not allowed.
         if ((rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL))
         {
            // do nothing if action is 1. not enabled 2. wait == no and events are not allowed
            if (rtEvt.InternalEvent < InternalInterface.MG_ACT_TOT_CNT &&
                (!task.ActionManager.isEnabled(rtEvt.InternalEvent) || (!rtEvt.isImmediate() && NoEventsAllowed())))
               return;
         }
         else
            // do the handlers when 1. wait == yes or 2. events are allowed
            if (!rtEvt.isImmediate() && NoEventsAllowed())
               return;

         restoreCurrCtrl = _currCtrl;
         _currCtrl = ctrl; // save current control for HANDLER_CTRL function

         propagate = (rtEvt.getType() != ConstInterface.EVENT_TYPE_INTERNAL || rtEvt.getInternalCode() < 1000);

         try
         {
            // for CP,CS and CV, flow and direction is evaluated
            if (rtEvt.getInternalCode() == InternalInterface.MG_ACT_CTRL_PREFIX ||
                rtEvt.getInternalCode() == InternalInterface.MG_ACT_CTRL_SUFFIX ||
                rtEvt.getInternalCode() == InternalInterface.MG_ACT_CTRL_VERIFICATION)
            {
               // If we are entering the task, set FlowMode as Step
               if (task.getLastParkedCtrl() == null && task.getClickedControl() == null)
                  task.setFlowMode(Flow.STEP);
               else
                  task.setFlowMode(Flow.FAST);
               handleDirection(rtEvt);
            }

            pushRtEvent(rtEvt);
            // do some common processing and if the result is true then continue
            // with the user defined handlers
            bRcBefore = commonHandlerBefore(rtEvt);

            restoreCurrField = _currField;
            _currField = (Field)task.getCurrField();

            if (bRcBefore)
            {
               pos.init(rtEvt);

               // get the first handler for the current event
               handler = pos.getNext();

               if (handler != null && handler.getEvent() != null && handler.getEvent().UserEvt != null)
                  setForceExit(handler.getEvent().UserEvt.ForceExit, task);

               // execute the chain of handlers for the current event. the execution
               // continues till an event handler returns false.
               while (handler != null)
               {
                  Task handlerContextTask = null;
                  try
                  {
                     bool enabledCndCheckedAndTrue = false;

                     // if there is a handler change the handler's context task to the event's task
                     handlerContextTask = (Task)handler.getTask().GetContextTask();
                     if (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                         rtEvt.getInternalCode() >= 1000 &&
                         rtEvt.getInternalCode() != InternalInterface.MG_ACT_VARIABLE)
                        handler.getTask().SetContextTask(handler.getTask());
                     else
                        handler.getTask().SetContextTask(rtEvt.getTask());

                     // there is no need to handle ForceExit for non-user events
                     if (handler.getEvent().getType() == ConstInterface.EVENT_TYPE_USER && handler.isEnabled())
                     {
                        enabledCndCheckedAndTrue = true;
                        _isHandlingForceExit = false;
                        restoreIsForceExit = true;
                        // handle forceExit only once
                        if (handleForceExitBefore(handler.getEvent(), rtEvt))
                        {
                           forceExitDone = true;
                        }
                     }

                     // check if the current run time event is different from the event
                     // of the handler
                     if (!rtEvt.Equals(handler.getEvent()))
                     {
                        // the new runtime event is that of the handler combined with the
                        // data of the current runtime event.
                        // the purpose of this is to propagate the event of the handler
                        // in place of the original event
                        oldRtEvt = rtEvt;
                        rtEvt = new RunTimeEvent(handler.getEvent(), rtEvt);
                        rtEvtChanged = true;

                        // there is a new runtime event to handle so pop the old one and push
                        // the new one instead.
                        popRtEvent();
                        pushRtEvent(rtEvt);
                     }
                     else
                        rtEvtChanged = false;

                     if (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                         rtEvt.getInternalCode() == InternalInterface.MG_ACT_REC_SUFFIX)
                        rtEvt.getTask().setInRecordSuffix(true);

                     createEventArguments(rtEvt);
                     handlerRetVals = handler.execute(rtEvt, returnedFromServer, enabledCndCheckedAndTrue);
                     removeEventArguments(rtEvt);

                     if (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                         rtEvt.getInternalCode() == InternalInterface.MG_ACT_REC_SUFFIX)
                        rtEvt.getTask().setInRecordSuffix(false);
                     propagate = handlerRetVals.Propagate;

                     //all handlers from the same task should be executed for  .NET object (not including control)
                     if (rtEvt.DotNetObject != null)
                        propagate = true;

                     if (!handlerRetVals.Enabled && rtEvtChanged)
                     {
                        rtEvt = oldRtEvt;
                        oldRtEvt = null;
                        rtEvtChanged = false;
                        // there is a new runtime event to handle so pop the old one and push
                        // the new one instead.
                        popRtEvent();
                        pushRtEvent(rtEvt);
                     }

                     // check if we have fulfilled the end condition
                     // After executing a handler, before ending a task (if the end condition is TRUE), 
                     // we should check if the task is not already closed. This can happen like in case 
                     // of QCR #796476 --- a non-interactive task has called an interactive task from 
                     // task suffix. Now if we press Alt+F4, we get EXIT_SYSTEM act. And so we close 
                     // all tasks starting from Main Program. So, when we come out of the TS handler 
                     // execution, the task is already closed.
                     // For Offline : TP is executed at client side. If we are in Task Prefix, dont check endTask condition here.
                     if (rtEvt.getInternalCode() != InternalInterface.MG_ACT_TASK_PREFIX && task.isStarted() && task.getExecEndTask())
                     {
                        endTaskError = task.endTask(true, false, false);
                        if (endTaskError)
                           break;
                     }

                     if (GetStopExecutionFlag())
                     {
                        if (handleStopExecution(rtEvt))
                           break;
                     }
                  }
                  finally
                  {
                     // restoring the context
                     handler.getTask().SetContextTask(handlerContextTask);
                  }

                  if (!propagate)
                     break;

                  if (rtEvtChanged)
                     handler = pos.getNext(rtEvt);
                  // get the next handler for the current event
                  else
                     handler = pos.getNext();
                  if (handler != null)
                     FlowMonitorQueue.Instance.addTaskEvent(rtEvt.getBrkLevel(true),
                                                                 FlowMonitorInterface.FLWMTR_PROPAGATE);
               }
               if (propagate && !endTaskError)
                  commonHandler(rtEvt);
            }
            if (!endTaskError)
               commonHandlerAfter(rtEvt, bRcBefore, propagate);

            if (forceExitDone && _isHandlingForceExit)
            {
               _isHandlingForceExit = false;
               handleForceExitAfter(rtEvt);
            }

            setForceExit(oldForceExit, oldForceExitTask);
            if (restoreIsForceExit)
               _isHandlingForceExit = oldIsHandlingForceExit;
         }
         finally
         {
            _currCtrl = restoreCurrCtrl;
            _currField = restoreCurrField;

            popRtEvent();

            // restore the previous values
            if (rtEvt.getInternalCode() == InternalInterface.MG_ACT_CTRL_PREFIX ||
                rtEvt.getInternalCode() == InternalInterface.MG_ACT_CTRL_SUFFIX ||
                rtEvt.getInternalCode() == InternalInterface.MG_ACT_CTRL_VERIFICATION)
            {
               task.setFlowMode(Flow.NONE);
               task.setFlowMode(oldFlowMode);
               task.setDirection(Direction.NONE);
               task.setDirection(oldDirection);
            }
         }

         // update the display
         GUIManager.Instance.execGuiCommandQueue();
      }

      /// <summary>
      ///   This method simulates all events that are in selection sequence starting from focus event
      ///   It is used in :
      ///   actions CTRL+TAB && CTRL+SHIFT+TAB that move us to the next/previous item in Tab control
      ///   click or accelerator on push button
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "val"></param>
      /// <param name = "line">ctrl line</param>
      /// <param name = "produceClick">true should produce click event</param>
      internal void simulateSelection(MgControlBase ctrl, String val, int line, bool produceClick)
      {
         var mgControl = (MgControl)ctrl;
         var currTask = (Task)mgControl.getForm().getTask();
         bool orgCancelWasRaised = currTask.cancelWasRaised();

         if (mgControl.Type == MgControlType.CTRL_TYPE_TAB)
         {
            if (!mgControl.isModifiable() || !mgControl.IsParkable(false))
            {
               mgControl.restoreOldValue();

               MgControl lastParkedCtrl = GUIManager.getLastFocusedControl();
               if (lastParkedCtrl != null)
                  Manager.SetFocus(lastParkedCtrl, -1);

               return;
            }
         }

         // when a push button is about to raise a 'Cancel' or a 'Quit' event. we do not want to set the new value to the field.
         // That setting might cause a rec suffix and a variable change.
         if (mgControl.Type == MgControlType.CTRL_TYPE_BUTTON)
         {
            var aRtEvt = new RunTimeEvent(mgControl);
            var raiseAt = (RaiseAt)(mgControl.getProp(PropInterface.PROP_TYPE_RAISE_AT).getValueInt());
            if (aRtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                (aRtEvt.InternalEvent == InternalInterface.MG_ACT_CANCEL ||
                 aRtEvt.InternalEvent == InternalInterface.MG_ACT_RT_QUIT))
            {
               if (raiseAt == RaiseAt.TaskInFocus)
                  currTask = ClientManager.Instance.getLastFocusedTask();
               orgCancelWasRaised = currTask.cancelWasRaised();
               currTask.setCancelWasRaised(true);
            }

            if (raiseAt == RaiseAt.TaskInFocus)
               mgControl.setRtEvtTask(ClientManager.Instance.getLastFocusedTask());
         }

         //first we handle focus
         handleFocus(mgControl, line, produceClick);

         currTask.setCancelWasRaised(orgCancelWasRaised);

         if (_stopExecution)
            return;

         // QCR# 974047 : If we have a Push Button with Allow Parking = No and field is attached to it, don't handle the event. 
         // This is same as online. In online, if field is not attached to non parkable push Button, we handle the event
         // But if field is attached to it, we don't handle event.
         // So if control is push button, execute handleEvent only when field is not attached or ParkonCLick is No or 
         // ParkonCLick and allowparking is both Yes
         bool controlIsParkable = mgControl.IsParkable(false);

         if (mgControl.Type != MgControlType.CTRL_TYPE_BUTTON
             ||
             (mgControl.getField() == null ||
              mgControl.checkProp(PropInterface.PROP_TYPE_PARK_ON_CLICK, false, line) == false ||
              controlIsParkable))
         {
            // QCR #301652. For non-parkable button control with event raised at the container task will be changed so 
            // when the button is clicked then the task will get focus. (as in Online and as if the button was parkable).
            // The focus will be on the first control.
            if (mgControl.Type == MgControlType.CTRL_TYPE_BUTTON && !controlIsParkable)
            {
               Task subformTask = (Task)mgControl.getForm().getTask();
               if (subformTask.IsSubForm)
               {
                  currTask = ClientManager.Instance.getLastFocusedTask();
                  // Defect 115474. For the same task do not move to the first control.
                  if (subformTask != currTask && subformTask.pathContains(currTask))
                     subformTask.moveToFirstCtrl(false);
               }
            }

            if (mgControl.IsHyperTextButton())
               Commands.addAsync(CommandType.SET_TAG_DATA_LINK_VISITED, mgControl, line, true);

            //then ACT_HIT is sent 
            RunTimeEvent rtEvt;
            rtEvt = new RunTimeEvent(mgControl);
            ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl = mgControl;
            rtEvt.setInternal(InternalInterface.MG_ACT_CTRL_HIT);
            handleEvent(rtEvt, false);
            if (_stopExecution)
               return;
         }
         else
         {
            if (mgControl.IsHyperTextButton() && !controlIsParkable)
               Commands.addAsync(CommandType.SET_TAG_DATA_LINK_VISITED, mgControl, line, false);
         }

         //and selection is handled in the end if previous actions succeeded
         if (mgControl.Type != MgControlType.CTRL_TYPE_BUTTON)
            //for button\check we do not call this to prevent recursion 
            handleSelection(mgControl, line, val);

         if (_stopExecution)
            return;
         //then mouse up
         handleMouseUp(mgControl, line, produceClick);
      }

      /// <summary> activate handleInternalEvent with isQuit = false</summary>
      /// <param name="itask">a reference to the task</param>
      /// <param name="eventCode">the code of the event</param>
      internal void handleInternalEvent(ITask itask, int eventCode)
      {
         handleInternalEvent(itask, eventCode, false);
      }

      /// <summary>
      ///   activate handleInternalEvent with isQuit = false
      /// </summary>
      /// <param name = "itask">a reference to the task </param>
      /// <param name = "eventCode">the code of the event </param>
      /// <param name="subformRefresh">identifier if the event called by subform refresh</param>
      internal void handleInternalEvent(ITask itask, int eventCode, bool subformRefresh)
      {
         Task task = (Task)itask;

         // set SubformExecMode (for SubformExecMode function) only in record prefix.
         // SubformExecMode is changed in record prefix and it is the same until the 
         // next record prefix execution, because we do not open a subform task each time 
         // in RC as it is in Online. But when the subform is refreshed from the parent
         // its record prefix & suffix are also executed.
         if (eventCode == InternalInterface.MG_ACT_REC_PREFIX
             && task.IsSubForm
             && task.SubformExecMode != SubformExecModeEnum.FIRST_TIME) // FIRST_TIME is set only once in
         // doFirstRecordCycle.
         {
            task.SubformExecMode = (subformRefresh
                                       ? SubformExecModeEnum.REFRESH
                                       : SubformExecModeEnum.SET_FOCUS);
         }

         // for the subform refresh it is needed to execute MG_ACT_POST_REFRESH_BY_PARENT handler before RECORD_PREFIX handler
         if (subformRefresh)
            handleInternalEvent(itask, InternalInterface.MG_ACT_POST_REFRESH_BY_PARENT);
         if (task.DataView.isEmptyDataview())
         {
            if (eventCode == InternalInterface.MG_ACT_REC_PREFIX || eventCode == InternalInterface.MG_ACT_REC_SUFFIX)
            {
               if (eventCode == InternalInterface.MG_ACT_REC_PREFIX)
                  task.emptyDataviewOpen(subformRefresh);
               else
                  task.emptyDataviewClose();

               if (task.getForm() != null)
               {
                  if (eventCode == InternalInterface.MG_ACT_REC_PREFIX)
                     ((MgForm)task.getForm()).RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                  task.getForm().SelectRow();
               }
            }
            else
               handleInternalEvent(task, eventCode, EventSubType.Normal);
         }
         else
            handleInternalEvent(task, eventCode, EventSubType.Normal);
      }

      /// <summary>
      ///   activate handleInternalEvent with isQuit = false
      /// </summary>
      /// <param name = "ctrl">a reference to the control </param>
      /// <param name = "eventCode">the code of the event </param>
      internal void handleInternalEvent(MgControlBase ctrl, int eventCode)
      {
         handleInternalEvent((MgControl)ctrl, eventCode, EventSubType.Normal);
      }

      /// <summary>
      ///   return the value of the "stop execution" flag
      /// </summary>
      internal bool GetStopExecutionFlag()
      {
         return _stopExecution;
      }

      /// <summary>
      ///   return the ctrl on which we had a "stop execution"
      /// </summary>
      internal MgControlBase getStopExecutionCtrl()
      {
         return _stopExecutionCtrl;
      }

      /// <summary>
      ///   Builds a subformCtrl path after the frmCtrl's form and above the toCtrl's form.
      ///   NOTE: frmCtrl shud be same/higher level in the subform tree than toCtrl
      /// </summary>
      /// <param name = "pathArray"></param>
      /// <param name = "frmCtrl"></param>
      /// <param name = "toCtrl"></param>
      /// <returns></returns>
      internal MgControlBase buildSubformPath(List<MgControlBase> pathArray, MgControlBase frmCtrl, MgControlBase toCtrl)
      {
         MgForm pForm = null;
         MgControl parentSubformCtrl = null;

         pathArray.Clear();
         parentSubformCtrl = (MgControl)toCtrl.getForm().getSubFormCtrl();
         //lastParkedSubformCtrl = lastParkedCtrl.getForm().getSubFormCtrl();
         while (parentSubformCtrl != null && ((MgControl)frmCtrl).onDiffForm(parentSubformCtrl))
         {
            //if (lastParkedSubformCtrl != null && lastParkedSubformCtrl.getTask() == parentSubformCtrl.getTask())
            //break;
            pathArray.Add(parentSubformCtrl);
            pForm = (MgForm)parentSubformCtrl.getForm();

            parentSubformCtrl = (MgControl)pForm.getSubFormCtrl();
         }

         // The toCtrl can be on diff subform path or in same Subformtree but with
         // frmCtrl lower in the tree (backward direction click)
         if (parentSubformCtrl == null)
         {
            bool foundCommonParent = false;
            // check if it is on diff subformPath. If yes, find the common parent and
            // update the ArrayList with paths from common parent to toCtrl
            parentSubformCtrl = (MgControl)frmCtrl.getForm().getSubFormCtrl();
            while (parentSubformCtrl != null && ((MgControl)toCtrl).onDiffForm(parentSubformCtrl))
            {
               for (int index = 0;
                    index < pathArray.Count;
                    index++)
               {
                  if (pathArray[index].getForm().getTask() == parentSubformCtrl.getForm().getTask())
                  {
                     foundCommonParent = true;
                     while (pathArray.Count != 0 && pathArray.Count > (index /* + 1*/))
                        pathArray.RemoveAt(index /*+ 1*/);
                     break;
                  }
               }

               if (foundCommonParent)
                  break;

               pForm = (MgForm)parentSubformCtrl.getForm();
               parentSubformCtrl = (MgControl)pForm.getSubFormCtrl();
            }

            // if no common parent, reset parentSubformCtrl to null
            if (!foundCommonParent)
               parentSubformCtrl = null;
         }

         // clear the arraylist
         if (parentSubformCtrl == null)
            pathArray.Clear();

         return parentSubformCtrl;
      }

      /// <summary>
      ///   execute CVs from srcCtrl's next ctrl to the last control on the form
      /// </summary>
      /// <param name = "itask"></param>
      /// <param name = "srcCtrl"></param>
      /// <returns></returns>
      internal bool executeVerifyHandlersTillLastCtrl(ITask itask, MgControlBase srcCtrl)
      {
         var task = (Task)itask;
         bool bRc = true;
         MgControl frmCtrl = null;
         MgControl toCtrl = null;
         var form = (MgForm)task.getForm();
         Direction orgDirection = task.getDirection();

         if (srcCtrl == null)
         {
            // if srcCtrl is null, start execution from the first nonsubformCtrl
            srcCtrl = form.getFirstNonSubformCtrl();
            frmCtrl = (MgControl)srcCtrl;
         }
         else
            frmCtrl = form.getNextCtrlIgnoreSubforms((MgControl)srcCtrl, Direction.FORE);

         // execute CV from frmCtrl to the last ctrl
         // take care as frmCtrl shouldnt run into any subform
         if (frmCtrl != null)
         {
            toCtrl = form.getLastNonSubformCtrl();

            // #731490 - form must contain both frmCtrl and toCtrl in forward direction.
            if (toCtrl != null && form.ctrlTabOrderIdx(frmCtrl) > -1 && form.ctrlTabOrderIdx(toCtrl) > -1
                && form.ctrlTabOrderIdx(frmCtrl) <= form.ctrlTabOrderIdx(toCtrl))
               // execute CV from current control to the last control
               bRc = executeVerifyHandlers(task, null, frmCtrl, toCtrl);
         }

         //reset the org direction
         task.setDirection(Direction.NONE);
         task.setDirection(orgDirection);

         return bRc;
      }

      /// <summary>
      ///   print error and exit task
      /// </summary>
      /// <param name = "itask"></param>
      /// <param name = "msgId"></param>
      internal void exitWithError(ITask itask, string msgId)
      {
         var task = (Task)itask;
         String message = ClientManager.Instance.getMessageString(msgId);
         Commands.messageBox(task.getTopMostForm(), "Error", message, Styles.MSGBOX_ICON_ERROR | Styles.MSGBOX_BUTTON_OK);
         handleInternalEvent(task, InternalInterface.MG_ACT_EXIT);
      }

#endregion

      /// <summary>
      ///   initializes the static section of the events manager. Usually does nothing but in the
      ///   event of applet re-load (for example, because of a browser refresh) it initializes the
      ///   static part of this class.
      /// </summary>
      internal void Init()
      {
         if (_initialized)
         {
            _isHandlingForceExit = false;
            _forceExit = ForceExit.None;
            _forceExitTask = null;
            _currField = null;
            setEndOfWork(true);
         }

         _initialized = true;
      }

      /// <summary>
      ///   Add internal event to the queue
      /// </summary>
      /// <param name = "itask">task</param>
      /// <param name = "code">code of internal event</param>
      internal void addInternalEvent(ITask itask, int code)
      {
         addGuiTriggeredEvent(itask, code, false);
      }

      /// <summary>
      /// add gui triggered event
      /// </summary>
      /// <param name="itask"></param>
      /// <param name="code"></param>
      /// <param name="onMultiMark"></param>
      public void addGuiTriggeredEvent(ITask itask, int code, bool onMultiMark)
      {
         var task = (Task)itask;
         var rtEvt = new RunTimeEvent(task);
         rtEvt.setInternal(code);
         addToTail(rtEvt);
      }

      /// <summary>
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "code"></param>
      /// <returns></returns>
      internal void addInternalEvent(MgControlBase ctrl, int code)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl);
         rtEvt.setInternal(code);
         addToTail(rtEvt);
      }

      /// <summary>
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "code"></param>
      /// <returns></returns>
      public void addInternalEvent(MgControlBase ctrl, int DisplayLine, int code)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl, DisplayLine, false);
         rtEvt.setInternal(code);
         addToTail(rtEvt);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="code"></param>
      /// <param name="priority"></param>
      public void addInternalEvent(MgControlBase ctrl, int code, Priority priority)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl);
         rtEvt.setInternal(code);
         rtEvt.setPriority(priority);
         addToTail(rtEvt);
      }

      /// <summary>
      /// will be used for multimarking in the future
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="code"></param>
      /// <param name="line"></param>
      /// <param name="modifiers"></param>
      public void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line, Modifiers modifiers)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl, line, false);
         rtEvt.setInternal(code);
         addToTail(rtEvt);
      }

      /// <summary> handle Column Click event on Column </summary>
      /// <param name="guiColumnCtrl"></param>
      /// <param name="direction"></param>
      /// <param name="columnHeader"></param>
      public void AddColumnClickEvent(MgControlBase columnCtrl, int direction, String columnHeader)
      {
         var rtEvt = new RunTimeEvent((MgControl)columnCtrl, direction, 0);
         rtEvt.setInternal(InternalInterface.MG_ACT_COL_CLICK);

         //Prepare an argument list with argument as columnHeaderString.
         var argsList = new GuiExpressionEvaluator.ExpVal[1];
         argsList[0] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.UNICODE, false, columnHeader);
         var args = new ArgumentsList(argsList);
         rtEvt.setArgList(args);

         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>
      /// handle column filter event
      /// </summary>
      /// <param name="columnCtrl"></param>
      /// <param name="columnHeader"></param>
      /// <param name="index"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      public void AddColumnFilterEvent(MgControlBase columnCtrl, String columnHeader, int x, int y, int width, int height)
      {
         MgControlBase mgControl = columnCtrl.getColumnChildControl();

         //If there is no control attached to the column, there is no need to invoke the filter. 
         //If there is control without variable, we also shouldn't invoke the filter.
         if (mgControl != null && mgControl.getField() != null) 
         {
            var rtEvt = new RunTimeEvent((MgControl)columnCtrl, columnHeader, x, y, width, height);
            rtEvt.setInternal(InternalInterface.MG_ACT_COL_FILTER);

            NUM_TYPE tmp = new NUM_TYPE();
            var argsList = new GuiExpressionEvaluator.ExpVal[6];
            argsList[0] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.UNICODE, false, columnHeader);
            tmp.NUM_4_LONG(mgControl.GetVarIndex());
            argsList[1] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.NUMERIC, false, tmp.toXMLrecord());
            tmp.NUM_4_LONG(x);
            argsList[2] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.NUMERIC, false, tmp.toXMLrecord());
            tmp.NUM_4_LONG(y);
            argsList[3] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.NUMERIC, false, tmp.toXMLrecord());
            tmp.NUM_4_LONG(width);
            argsList[4] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.NUMERIC, false, tmp.toXMLrecord());
            tmp.NUM_4_LONG(height);
            argsList[5] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.NUMERIC, false, tmp.toXMLrecord());


            var args = new ArgumentsList(argsList);
            rtEvt.setArgList(args);

            ClientManager.Instance.EventsManager.addToTail(rtEvt);
         }
      }

#if PocketPC
   /// <summary>
   ///   Add internal event that was triggered by GUI queue (includes timer events) to the queue
   /// </summary>
   /// <param name = "ctrl">control </param>
   /// <param name = "code">code of internal event </param>
      internal void addGuiTriggeredEvent(int code)
      {
      RunTimeEvent rtEvt = new RunTimeEvent(true);
      rtEvt.setInternal(code);
      addToTail(rtEvt);
      }
#endif

      /// <summary>
      ///   Add internal event that was triggered by GUI queue (includes timer events) to the queue
      /// </summary>
      /// <param name = "ctrl">control </param>
      /// <param name = "code">code of internal event </param>
      /// <param name = "line">line in a table control </param>
      public void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl, line, true);
         rtEvt.setInternal(code);
         addToTail(rtEvt);
      }

      /// <summary>
      ///   Add internal event that was triggered by GUI queue (includes timer events) to the queue
      /// </summary>
      /// <param name = "ctrl">control </param>
      /// <param name = "line">the line for multiline control </param>
      /// <param name = "code">code of internal event </param>
      internal void addGuiTriggeredEvent(MgControlBase ctrl, int line, int code, bool isProduceClick)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl, line, true);
         rtEvt.setInternal(code);
         rtEvt.setProduceClick(isProduceClick);
         addToTail(rtEvt);
      }

      /// <summary>
      ///   Add internal event that was triggered by GUI queue (includes timer events) to the queue
      /// </summary>
      /// <param name = "ctrl">control </param>
      /// <param name = "code">code of internal event </param>
      /// <param name = "list"></param>
      public void addGuiTriggeredEvent(MgControlBase ctrl, int code, List<MgControlBase> list)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl, list, true);
         rtEvt.setInternal(code);
         addToTail(rtEvt);
      }

      /// <summary> Add internal event that was triggered by GUI queue (includes timer events) to the queue </summary>
      /// <param name = "ctrl">control </param>
      /// <param name = "code">code of internal event </param>
      /// <param name = "line">line of table control </param>
      /// <param name = "dotNetArgs">dotNet args list </param>
      public void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line, Object[] dotNetArgs, bool onMultimark)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl, true);
         rtEvt.setInternal(code);
         rtEvt.DotNetArgs = dotNetArgs;
         addToTail(rtEvt);
      }

      /// <summary> Add internal event that was triggered by GUI queue (includes timer events) to the queue </summary>
      /// <param name = "ctrl">control </param>
      /// <param name = "code">code of internal event </param>
      /// <param name = "line">line of table control </param>
      /// <param name = "dotNetArgs">dotNet args list </param>
      /// <param name="raisedBy"></param>
      /// <param name="onMultiMark">true if mutimark continues</param>
      public void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line, Object[] dotNetArgs, RaisedBy raisedBy, bool onMultimark)
      {
         var rtEvt = new RunTimeEvent((MgControl)ctrl, true);
         rtEvt.setInternal(code);
         rtEvt.DotNetArgs = dotNetArgs;
         addToTail(rtEvt);
      }

      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name = "task">reference to the task </param>
      /// <param name = "code">code of internal event </param>
      public void addGuiTriggeredEvent(ITask task, int code)
      {
         var rtEvt = new RunTimeEvent((Task)task, true);
         rtEvt.setInternal(code);
         addToTail(rtEvt);
      }

      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name="task"></param>
      /// <param name="code"></param>
      /// <param name="raisedBy">relevant only for online (for which subform are handled as orphans)</param>
      public void addGuiTriggeredEvent(ITask task, int code, RaisedBy raisedBy)
      {
         addGuiTriggeredEvent(task, code);
      }

      /// <summary>
      ///   returns the first event from the queue and remove it from there
      /// </summary>
      private RunTimeEvent getEvent()
      {
         return (RunTimeEvent)_eventsQueue.poll();
      }

      public ForceExit getForceExit()
      {
         return _forceExit;
      }

      private void setForceExit(ForceExit forceExit, Task task)
      {
         _forceExit = forceExit;
         setForceExitTask(task);
      }
      
      private Task getForceExitTask()
      {
         return _forceExitTask;
      }

      private void setForceExitTask(Task task)
      {
         _forceExitTask = task;
      }

    

      /**
      * returns true if the EventsManager is currently handling a user event with
      * Force Exit = 'Pre-Record Update'
      */
      internal bool isForceExitPreRecordUpdate(Task task)
      {
         return (_isHandlingForceExit && getForceExit() == ForceExit.PreRecordUpdate && task == getForceExitTask());
      }

      /// <summary>
      ///   moves to a parkable ctrl if 'current' is NonParkable.
      ///   If 'current' is null, moves to first parkableCtrl.
      /// </summary>
      private void moveToParkableCtrl(MgControl current, bool checkDir)
      {
         bool cursorMovedNext = false;

         // QCR 760381 if after handling the event the current control is unparkable
         // try an move to the next parkable control
         if (current != null &&
             (!current.IsParkable(ClientManager.Instance.MoveByTab) ||
              (checkDir && !current.allowedParkRelatedToDirection())))
         {
            cursorMovedNext = ((MgForm)current.getForm()).moveInRow(current, Constants.MOVE_DIRECTION_NEXT);
            // if could not move to the next control check for errors
            if (!cursorMovedNext)
            {
               if (GetStopExecutionFlag())
               {
                  // cancel the errors and try to move to the previous control
                  setStopExecution(false);
                  // we change the level in order not to perform control suffix
                  var currTask = (Task)current.getForm().getTask();
                  currTask.setLevel(Constants.TASK_LEVEL_RECORD);
                  bool cursorMovedPrev = ((MgForm)current.getForm()).moveInRow(current, Constants.MOVE_DIRECTION_PREV);

                  if (!cursorMovedPrev)
                  {
                     // if there is no previous control go to the next control
                     // (remember errors are now disabled)
                     // if we can't then there are no controls to park on
                     cursorMovedNext = ((MgForm)current.getForm()).moveInRow(current, Constants.MOVE_DIRECTION_NEXT);

                     if (!cursorMovedNext)
                        HandleNonParkableControls(current.getForm().getTask());
                  }
               }
               else
                  HandleNonParkableControls(current.getForm().getTask());
            }
         }
         else if (current == null)
         {
            // fixed bug #:237159 we can get to this code before we activate control prefix, so the getLastFocusedTask is null. 
            //if there is no parkable controls - check if one of the controls became parkable
            Task task = ClientManager.Instance.getLastFocusedTask();
            if (task != null)
            {
               task.setLevel(Constants.TASK_LEVEL_RECORD);
               task.moveToFirstCtrl(false);

               if (GUIManager.getLastFocusedControl() != null)
                  cursorMovedNext = true;
            }
         }

         return;
      }

      /// <summary>
      ///   Main events loop, runs in worker thread for main window and for every
      ///   modal window Responsible for handling events
      /// </summary>
      /// <param name = "mgData"> </param>
      internal void EventsLoop(MGData mgData)
      {
         EventsAllowedType savedAllowEvents = getAllowEvents();
         bool allowFormsLockSave = AllowFormsLock;

         setAllowEvents(EventsAllowedType.ALL);
         AllowFormsLock = true;

         pushNewExecStacks();
         while (!mgData.IsAborting)
         {
            _eventsQueue.waitForElement();
            handleEvents(mgData, 0);

            // if we are aborting the task, we should not checkparkability. Because we have closed the task and we would
            // get the values of previous eventsloop. This would result in a loop as in #793138. The previous eventsloop
            // would take care of parkability if there is no parkable ctrl on that form.
            if (!mgData.IsAborting)
               checkParkability();

            // The temporary keys created in DNObjectsCollection is saved in 'tempDNObjectsCollectionKeys'. If the '_eventsQueue' is empty,
            // there is no reference to these temporary keys in DNObjectsCollection, and hence must be freed.
            removeTempDNObjectCollectionKeys();
         }
         popNewExecStacks();

         setAllowEvents(savedAllowEvents);
         AllowFormsLock = allowFormsLockSave;
      }

      /// <summary>
      ///   Non interactive events loop, runs in worker thread for non interactive tasks
      ///   Responsible for cycling forward on records and handling events.
      /// </summary>
      /// <param name = "mgData"></param>
      internal void NonInteractiveEventsLoop(MGData mgData, Task task)
      {
         EventsAllowedType savedAllowEvents = getAllowEvents();
         bool allowFormsLockSave = AllowFormsLock;

         bool saveOrgOpenFormDesignerIsEnable = task.ActionManager.isEnabled(InternalInterface.MG_ACT_OPEN_FORM_DESIGNER);
         if (saveOrgOpenFormDesignerIsEnable)
            task.ActionManager.enable(InternalInterface.MG_ACT_OPEN_FORM_DESIGNER, false);

         setNonInteractiveAllowEvents(task.isAllowEvents(), task);

         pushNewExecStacks();
         while (!mgData.IsAborting)
         {

            if (task.getMode() == Constants.TASK_MODE_DELETE)
               handleInternalEvent(task, InternalInterface.MG_ACT_CYCLE_NEXT_DELETE_REC);
            else
               handleInternalEvent(task, InternalInterface.MG_ACT_CYCLE_NEXT_REC);

            // poll only 1 event.
            if (!mgData.IsAborting && task.isAllowEvents())
               handleEvents(mgData, 1);
         }
         popNewExecStacks();

         setAllowEvents(savedAllowEvents);
         AllowFormsLock = allowFormsLockSave;

         if (saveOrgOpenFormDesignerIsEnable)
            task.ActionManager.enable(InternalInterface.MG_ACT_OPEN_FORM_DESIGNER, true);
      }

      /// <summary>
      ///   get and remove events from the queue and handle them one by one till the queue is empty
      /// </summary>
      /// <param name = "baseMgData"> mgdata of the window that openeded that habdle event</param>
      /// <param name = "EventsToHandleCnt"></param>
      internal void handleEvents(MGData baseMgData, long eventsToHandleCnt)
      {
         MGData currMgd;
         Task task = null;
         long delta = 0;
         long eventsPolledCnt = 0;

         setStopExecution(false);

         // get and remove events from the queue till its empty
         while (!_eventsQueue.isEmpty() && !_stopExecution &&
                (eventsToHandleCnt == 0 || eventsPolledCnt < eventsToHandleCnt))
         {
            RunTimeEvent rtEvt = getEvent();
            if (rtEvt != null && rtEvt.isGuiTriggeredEvent())
            {
               delta = Misc.getSystemMilliseconds();
               _currKbdItem = null;

               bool blockedByModalWindow = rtEvt.isBlockedByModalWindow(baseMgData);
               if (blockedByModalWindow &&
                   (rtEvt.getInternalCode() != InternalInterface.MG_ACT_TIMER
                    && rtEvt.DotNetObject == null)) //QCR #726198, events from .NET objects which are not on form 
               //should be handled on parent of modal window also
               {
                  continue;
               }

               // Idle timer, timer events and enable/disable events should not change the current window.
               // This is because the current window determines the current task and changing the task
               // causes some task-scoped events to run on sub-tasks as well.
               // TODO: Determine if there are any other such events, where the current window must not be changed.
               //       Perhaps there is no point in having this line at all. [Ronen Mashal 22/4/2009]
               if (!rtEvt.isIdleTimer() &&
                   !blockedByModalWindow &&
                   rtEvt.getInternalCode() != InternalInterface.MG_ACT_ENABLE_EVENTS &&
                   rtEvt.getInternalCode() != InternalInterface.MG_ACT_DISABLE_EVENTS &&
                   rtEvt.getTask() != null && !rtEvt.getTask().getMGData().IsAborting)
               {
                  MGDataCollection.Instance.currMgdID = rtEvt.getTask().getMGData().GetId();
               }
               //ClientManager.Instance.WriteGuiToLog("started processing event -------> "); // TODO:add event type
            }
            currMgd = MGDataCollection.Instance.getCurrMGData();
            if (currMgd != null)
            {
               while (currMgd != null && currMgd.IsAborting)
               {
                  currMgd = currMgd.getParentMGdata();
                  if (currMgd != null)
                     MGDataCollection.Instance.currMgdID = MGDataCollection.Instance.getMgDataIdx(currMgd);
               }
            }
            else
            {
               /* QCRs #777217 & #942968                             */
               /* It might happen that we have removed the MGData    */
               /* from the MGTable in Manager.CloseEvent().  */
               /* It was done for fixing QCR# 296277 relating to mem */
               /* leaks.                                             */
               /* In that case, mgd would be null here.              */
               /* In such a scenario, take the parent mgd from the   */
               /* related task and continue processing the events.   */
               task = rtEvt.getTask();
               while (currMgd == null && task != null && task.getParent() != null)
               {
                  task = (Task)task.getParent();
                  currMgd = task.getMGData();
                  if (currMgd.IsAborting)
                     currMgd = null;

                  if (currMgd != null)
                     MGDataCollection.Instance.currMgdID = MGDataCollection.Instance.getMgDataIdx(currMgd);
               }
            }
            if (currMgd == null)
               break;

            if (rtEvt.getTask() == null)
            {
               task = ClientManager.Instance.getLastFocusedTask();
               while (task != null && (task.isAborting() || task.InEndTask))
                  task = (Task)task.getParent();
               if (task != null)
               {
                  rtEvt.setCtrl((MgControl)task.getLastParkedCtrl());
                  rtEvt.setTask(task);
               }
            }
            else
               task = rtEvt.getTask();

            if (task != null)
            {
               handleEvent(rtEvt, false);
               refreshTables();
            }

            GUIManager.Instance.execGuiCommandQueue();
            _currCtrl = null; // delete current processed control

            if (task != null)
               task.CommandsProcessor.SendMonitorOnly();
            else
            {
               Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx(0);
               mainProg.CommandsProcessor.SendMonitorOnly();
            }

            if (rtEvt != null && rtEvt.isGuiTriggeredEvent())
            {
               //logging for performance
               if (delta > 0)
               {
                  delta = Misc.getSystemMilliseconds() - delta;
                  //ClientManager.Instance.WriteGuiToLog("end processing event -------> " +  "null" + "(" + delta + ")");
               }
            }

            // advance EventsPolled only if the event is internal. otherwise internals to be executed will be never reached by non interactive tasks.
            if (eventsToHandleCnt > 0 && rtEvt != null && rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                rtEvt.InternalEvent < InternalInterface.MG_ACT_TOT_CNT)
               eventsPolledCnt++;
         } // while (!_eventsQueue.isEmpty() ...
      }

      /// <summary>
      ///   send guiCommand to refresh all tables of this mgdata
      /// </summary>
      internal void refreshTables()
      {
         MGData mgd = MGDataCollection.Instance.getCurrMGData();
         if (mgd != null && !mgd.IsAborting)
         {
            for (int i = 0;
                 i < mgd.getTasksCount();
                 i++)
            {
               // refresh all tables in the window
               Task currTask = mgd.getTask(i);
               var form = (MgForm)currTask.getForm();
               if (form != null && form.Opened)
               {
                  if (!form.isSubForm() && !form.IsMDIFrame)
                     Commands.addAsync(CommandType.REFRESH_TMP_EDITOR, form);
                  var table = (MgControl)form.getTableCtrl();
                  if (table != null)
                  {
                     // in the end of gui triggered event, we must refresh all table child properties
                     // for the rows that were update
                     Commands.addAsync(CommandType.REFRESH_TABLE, table, 0, false);
                  }
               }
            }
         }
      }

      /// <summary>
      ///   Handle stop execution while executing any handler
      /// </summary>
      /// <param name = "rtEvt"></param>
      /// <returns></returns>
      private bool handleStopExecution(RunTimeEvent rtEvt)
      {
         bool stop = false;

         if (!GetStopExecutionFlag())
            return stop;

         Task task = rtEvt.getTask();
         MgControl currCtrl = rtEvt.Control;
         MgControl lastParkedCtrl = GUIManager.getLastFocusedControl();
         bool endTaskDueToVerifyError = false;

         if (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL)
         {
            switch (rtEvt.getInternalCode())
            {
               case InternalInterface.MG_ACT_TASK_PREFIX:
                  endTaskDueToVerifyError = true;
                  break;

               case InternalInterface.MG_ACT_REC_PREFIX:
                  // An error in the record prefix sends us right back to the start of the record prefix.
                  // It might create an endless loop but it is OK to do so. It is a developer bug.
                  endTaskDueToVerifyError = true;
                  break;

               case InternalInterface.MG_ACT_REC_SUFFIX:
                  // it will be handled in commonhandlerafter
                  setStopExecutionCtrl(null);
                  setStopExecutionCtrl((MgControl)task.getLastParkedCtrl());
                  break;

               case InternalInterface.MG_ACT_CTRL_PREFIX:
                  // Error might have occurred in CP.
                  setStopExecution(false);
                  task.InCtrlPrefix = false;

                  if (lastParkedCtrl == null) //only for the first time
                  {
                     task.setCurrField(null);

                     // Show the message Box continusly if user selects 'No', otherwise exit task
                     if (!GUIManager.Instance.confirm((MgForm)task.getForm(), MsgInterface.CONFIRM_STR_CANCEL))
                     {
                        MgControl prevCtrl = currCtrl.getPrevCtrl();

                        // if there is no prevCtrl, invoke CP on this ctrl again. we want to execute all operations
                        // before the stopexecution operation.
                        if (prevCtrl == null)
                           prevCtrl = currCtrl;

                        task.setCurrVerifyCtrl(currCtrl);

                        //reset the stop execution ctrl
                        setStopExecutionCtrl(null);
                        setStopExecutionCtrl(currCtrl);

                        task.setDirection(Direction.NONE);
                        task.setDirection(Direction.BACK);

                        handleInternalEvent(prevCtrl, InternalInterface.MG_ACT_CTRL_PREFIX);
                     }
                     endTaskDueToVerifyError = true;
                  }
                  else
                  {
                     // CP got stop execution while executing the operations in this handler and is parkable.
                     // we move to the next parkable in opp. direction
                     Direction orgDir = task.getDirection();
                     bool isDirFrwrd = false;

                     task.setCurrField(lastParkedCtrl.getField());

                     if (currCtrl.getField().getId() > lastParkedCtrl.getField().getId()
                         || currCtrl.onDiffForm(lastParkedCtrl)
                         || ((MgForm)task.getForm()).PrevDisplayLine != currCtrl.getForm().DisplayLine)
                        isDirFrwrd = true;

                     task.setDirection(Direction.NONE);
                     task.setDirection((isDirFrwrd
                                           ? Direction.BACK
                                           : Direction.FORE)); // reverse dir

                     MgControl nextCtrl = currCtrl.getNextCtrlOnDirection(task.getDirection());

                     if (nextCtrl != null)
                     {
                        // if clicked, we are unable to park on it, so set it back to null.
                        ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl = null;
                        task.setCurrVerifyCtrl(currCtrl);

                        // if we get a error in executing operations in CP handler, we have already executed the CVs
                        // from the last stopExecutionCtrl, so we need to reset it to curCtrl.
                        setStopExecutionCtrl(null);
                        setStopExecutionCtrl(currCtrl);

                        handleInternalEvent(nextCtrl, InternalInterface.MG_ACT_CTRL_PREFIX);
                     }
                     else
                        Manager.SetFocus(lastParkedCtrl, -1);

                     // reset the direction
                     task.setDirection(Direction.NONE);
                     task.setDirection(orgDir);
                  }

                  setStopExecution(true);
                  break;

               case InternalInterface.MG_ACT_CTRL_SUFFIX:
                  // It must be set to false so that next time for fast mode CS must be executed.
                  currCtrl.setInControlSuffix(false);
                  break;

               case InternalInterface.MG_ACT_CTRL_VERIFICATION:
                  if (currCtrl != GUIManager.getLastFocusedControl() && task.getCurrVerifyCtrl() != currCtrl)
                  {
                     setStopExecution(false);
                     setStopExecutionCtrl(currCtrl);
                     task.setCurrVerifyCtrl(currCtrl);
                     // if in fast mode, we are now unable to park on the clicked ctrl and it must be set to null.
                     ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl = null;
                     // since we want to park here due to error, we must reset this flag.
                     task.InCtrlPrefix = false;

                     // if we have tabbed and next ctrl has property tab_into as false, and we got an stop execution,
                     // then return to last parked ctrl
                     if (ClientManager.Instance.MoveByTab && !currCtrl.checkProp(PropInterface.PROP_TYPE_TAB_IN, true) &&
                         lastParkedCtrl != null)
                        handleInternalEvent(lastParkedCtrl, InternalInterface.MG_ACT_CTRL_PREFIX);
                     else
                        handleInternalEvent(currCtrl, InternalInterface.MG_ACT_CTRL_PREFIX);
                     setStopExecution(true);
                  }
                  break;
            }

            if (endTaskDueToVerifyError)
            {
               task.endTask(true, false, true);
               stop = true;
            }
         }

         return stop;
      }

      /// <summary>
      ///   take care of "Force Exit" before the handler execute its operations returns true
      ///   if the force exit was handled
      /// </summary>
      /// <param name = "event">the event of the handler
      /// </param>
      /// <param name = "rtEvt">the run-time event
      /// </param>
      private bool handleForceExitBefore(Event evt, RunTimeEvent rtEvt)
      {
         ForceExit forceExit = evt.UserEvt.ForceExit;
         MgControl ctrl = null;
         Task task = null;
         bool isNewRec = false;
         int oldIdx = 0;
         Record oldRec = null;

         if (forceExit == 0 || forceExit == ForceExit.None || rtEvt.isImmediate())
            return false;
         _isHandlingForceExit = true;
         task = rtEvt.getTask();
         oldRec = ((DataView)task.DataView).getCurrRec();
         if (oldRec != null && oldRec.getMode() == DataModificationTypes.Insert && oldRec.isNewRec())
         {
            isNewRec = true;
            oldIdx = oldRec.getId();
         }
         if (forceExit != ForceExit.None && forceExit != ForceExit.Editing &&
             task.getLevel() == Constants.TASK_LEVEL_CONTROL)
         {
            ctrl = (MgControl)task.getLastParkedCtrl();
            if (ctrl != null)
               handleInternalEvent(ctrl, InternalInterface.MG_ACT_CTRL_SUFFIX);
         }

         // Should we exit the record?
         if ((forceExit == ForceExit.PreRecordUpdate || forceExit == ForceExit.PostRecordUpdate) &&
             task.getLevel() == Constants.TASK_LEVEL_RECORD)
         {
            handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
            if (task.getMode() == Constants.TASK_MODE_CREATE && !_stopExecution)
               task.setMode(Constants.TASK_MODE_MODIFY);
            var currRec = ((DataView)task.DataView).getCurrRec();
            // If we failed exiting a newly created record - restore insert mode of the record
            if (currRec != null && _stopExecution && isNewRec && oldIdx == currRec.getId() &&
                task.getMode() == Constants.TASK_MODE_CREATE)
            {
               // restore insert mode of the record
               currRec.clearMode();
               currRec.restart(DataModificationTypes.Insert);
            }
         }

         if (forceExit == ForceExit.PostRecordUpdate && !_stopExecution)
         {
            if (task.getLevel() == Constants.TASK_LEVEL_TASK)
               handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
         }

         //QCR #303762 - additional support for forceExit = editing 
         if (forceExit == ForceExit.Editing)
         {
            MgControl evtCtrl = rtEvt.Control;
            //if there is a control that is attached to a field and the control was changed
            //in case of multiline go inside anyway because there are some edit actions that are not going through
            //textMaskEdit that is incharge of setting the ModifiedByUser (such as delprev, delchar)
            if (evtCtrl != null && evtCtrl.getField() != null && (evtCtrl.ModifiedByUser || evtCtrl.isMultiline()))
            {
               String ctrlVal = Manager.GetCtrlVal(evtCtrl);
               String evtCtrlval;
               if (evtCtrl.isRichEditControl())
                  evtCtrlval = evtCtrl.getRtfVal();
               else
                  evtCtrlval = evtCtrl.Value;
               //test the new control value
               if (evtCtrl.validateAndSetValue(ctrlVal, true))
               {
                  ValidationDetails vd = evtCtrl.buildPicture(evtCtrlval, ctrlVal);
                  vd.evaluate();
                  ((Field)evtCtrl.getField()).setValueAndStartRecompute(evtCtrl.getMgValue(vd.getDispValue()),
                                                                        vd.getIsNull(), true, false, false);
               }
            }
         }

         return true;
      }

      /// <summary>
      ///   take care of "Force Exit" after handling the event </summary>
      /// <param name = "rtEvt">the run-time event </param>
      private void handleForceExitAfter(RunTimeEvent rtEvt)
      {
         Task task = null;
         ForceExit forceExit = rtEvt.UserEvt.ForceExit;

         task = rtEvt.getTask();

         //QCR#776295: for pre record update, commit the record after handler's execution.
         if (forceExit == ForceExit.PreRecordUpdate)
         {
            commitRecord(task, true);

            if (!task.transactionFailed(ConstInterface.TRANS_RECORD_PREFIX) && !task.isAborting())
            {
               // check whether to evaluate the end condition after the record suffix
               if (task.evalEndCond(ConstInterface.END_COND_EVAL_AFTER))
                  task.endTask(true, false, false);
            }
         }

         // For post-record type we already did rec-prefix and ctrl prefix in the handleForceExitBefore
         if (forceExit != ForceExit.PostRecordUpdate)
         {
            task = rtEvt.getTask();
            if (task.getLevel() == Constants.TASK_LEVEL_TASK)
               handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
         }

         MgControl ctrl = GUIManager.getLastFocusedControl();
         if (ctrl != null && forceExit != ForceExit.Editing)
            handleInternalEvent(ctrl, InternalInterface.MG_ACT_CTRL_PREFIX);

         //QCR 801873 - update the display after handler of force exit editing
         if (forceExit == ForceExit.Editing)
         {
            MgControl evtCtrl = rtEvt.Control;
            //if there is a control that is attached to a field 
            if (evtCtrl != null && evtCtrl.getField() != null)
               ((Field)evtCtrl.getField()).updateDisplay();
         }
      }

      /// <summary>
      ///   a special routine to handle data and timer events
      ///   it receives a HandlersTable and execute them one by one
      /// </summary>
      /// <param name = "handlersTab">the handlers table</param>
      /// <param name = "rtEvt">a referennce to the timer runtime event - for expression events it must be null</param>
      internal void handleEvents(HandlersTable handlersTab, RunTimeEvent rtEvt)
      {
         int i;
         EventHandler handler;

         try
         {
            pushRtEvent(rtEvt);
            for (i = 0;
                 i < handlersTab.getSize();
                 i++)
            {
               handler = handlersTab.getHandler(i);
               // the rtEvt is used to pass the ctrl, therefor it must be valid (not null)
               if (rtEvt.getType() == ConstInterface.EVENT_TYPE_EXPRESSION || handler.isSpecificHandlerOf(rtEvt) ||
                   handler.isNonSpecificHandlerOf(rtEvt) || handler.isGlobalHandlerOf(rtEvt))
               {
                  handler.execute(rtEvt, false, false);
               }
            }
         }
         finally
         {
            popRtEvent();
         }
      }

      /// <summary>
      ///   Takes care of keydown Event
      /// </summary>
      /// <param name = "task">task</param>
      /// <param name = "ctrl">control pressed</param>
      /// <param name = "kbdItem">keybord item</param>
      internal void handleKeyDown(Task task, MgControl ctrl, RunTimeEvent evt)
      {
         MGData mgd;
         RunTimeEvent rtEvt;
         KeyboardItem kbdItem = evt.getKbdItmAlways();
         int keyCode = kbdItem.getKeyCode();
         Modifiers modifier = kbdItem.getModifier();
         _currKbdItem = kbdItem;

         // If keyboard buffering is responsible for this event, we need to find the right control to receive 
         // this event
         if (evt.IgnoreSpecifiedControl)
         {
            ctrl = getCurrCtrl();
            task = ctrl.getForm().getTask() as Task;
            // The control receiving keys from keyboard buffering must be a textbox
            if (ctrl.Type != MgControlType.CTRL_TYPE_TEXT)
               return;
         }

         Logger.Instance.WriteDevToLog("Start handling KEYDOWN event. Key code: " + keyCode);

         try
         {
            // TODO :ALEX 140655 delete next 2 lines after fixing the BUG
            //if (ctrl != getLastFocusedControl())
            //   processFocus(ctrl, ctrlName);
            if (ctrl != null)
               ctrl.refreshPrompt();

            // DOWN or UP invoked on a SELECT/RADIO control
            if (_currKbdItem.equals(ClientManager.Instance.KBI_DOWN) || _currKbdItem.equals(ClientManager.Instance.KBI_UP))
            {
               if (ctrl != null &&
                   (ctrl.Type == MgControlType.CTRL_TYPE_LIST || ctrl.Type == MgControlType.CTRL_TYPE_RADIO))
                  return;
            }

            rtEvt = new RunTimeEvent(task, ctrl);
            rtEvt.setSystem(_currKbdItem);
            rtEvt.DotNetArgs = evt.DotNetArgs;
            rtEvt.setEditParms(evt.getStartSelection(), evt.getEndSelection(), evt.getValue());
            rtEvt.setImeParam(evt.getImeParam());
            try
            {
               handleEvent(rtEvt, false);
            }
            finally
            {
               if (_stopExecution)
               {
                  MgControl returnToCtrl = ClientManager.Instance.ReturnToCtrl;
                  if (returnToCtrl != null && ClientManager.Instance.validReturnToCtrl())
                     (returnToCtrl.getForm().getTask()).setLastParkedCtrl(returnToCtrl);
               }
            }
            mgd = MGDataCollection.Instance.getCurrMGData();
            // check if the MGData is still alive after handling the events above
            if (mgd != null && !mgd.IsAborting)
               handleExpressionHandlers();
            Logger.Instance.WriteDevToLog("End handling KEYDOWN event. Key code: " + keyCode);
            //if a key was pressed on table child, make sure that is is visible
            if (ctrl != null && ctrl.IsRepeatable && ctrl == ClientManager.Instance.ReturnToCtrl)
            {
               ((MgForm)ctrl.getForm()).bringRecordToPage();
            }
         }
         finally
         {
            ClientManager.Instance.setMoveByTab(false);
         }
      }

      /// <summary>
      ///   for radio button we handle the keys down,up,left,right
      ///   we are handling those keys
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "evt"></param>
      private void selectionControlHandleArrowKeyAction(MgControl ctrl, RunTimeEvent evt)
      {
         String suggestedValue = evt.getValue();
         KeyboardItem kbdItem = evt.getKbdItmAlways();
         int keyCode = kbdItem.getKeyCode();
         MgControl nextSibling = null;

         //for radio button when we on the last option and we have more then one control attoced to the same varibel
         //we need to move to the next control
         if (ctrl.Type == MgControlType.CTRL_TYPE_RADIO)
         {
            if (ctrl.Value.Equals(suggestedValue) &&
                (keyCode == GuiConstants.KEY_DOWN || keyCode == GuiConstants.KEY_UP))
            {
               // when more then one control with this variable, we move to a diffrent control
               if (ctrl.isPartOfDataGroup())
               {
                  // find the next control according to zorder (which is the order of control in array)
                  if (keyCode == GuiConstants.KEY_DOWN)
                     nextSibling = ctrl.getNextSiblingControl();
                  else
                     nextSibling = ctrl.getPrevSiblingControl();

                  if (nextSibling != null)
                  {
                     ctrl = nextSibling;
                     ctrl.setControlToFocus();
                     if (keyCode == GuiConstants.KEY_UP)
                     {
                        //we're going up, so go to last value
                        String[] displayVals = ctrl.getDispVals(0, true);
                        suggestedValue = "" + (displayVals.Length - 1);
                     }
                     else
                     {
                        //we're going down, so go the first value
                        suggestedValue = "0";
                     }
                  }
               }
            }
         }

         if (ctrl != null)
         {
            int lineToBeDisplay = ctrl.getForm().DisplayLine;
            simulateSelection(ctrl, suggestedValue, lineToBeDisplay, false);
            Manager.SetFocus(ctrl, -1);
         }
      }

      /// <summary>Handle Selection Event</summary>
      /// <param name = "ctrl">control</param>
      /// <param name = "line">line</param>
      /// <param name = "value">value</param>
      private void handleSelection(MgControl ctrl, int line, String val)
      {
         if (ctrl.Type == MgControlType.CTRL_TYPE_TAB
             || ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX
             || ctrl.Type == MgControlType.CTRL_TYPE_RADIO
             || ctrl.Type == MgControlType.CTRL_TYPE_COMBO
             || ctrl.Type == MgControlType.CTRL_TYPE_LIST)
         {
            bool checkFocus = (ctrl.Type != MgControlType.CTRL_TYPE_TAB &&
                               ctrl.Type != MgControlType.CTRL_TYPE_CHECKBOX &&
                               ctrl.Type != MgControlType.CTRL_TYPE_BUTTON);
            bool allowSelection = (checkFocus
                                      ? (ctrl.getForm().getTask().getLastParkedCtrl() == ctrl)
                                      : true);
            if (!allowSelection || !ctrl.isModifiable() || !ctrl.IsParkable(false))
               ctrl.restoreOldValue();
            else
            {
               if (ctrl.isChoiceControl())
                  Commands.setGetSuggestedValueOfChoiceControlOnTagData(ctrl, ctrl.IsRepeatable
                                                                                 ? line
                                                                                 : 0, true);

               if (ctrl.isDifferentValue(val, ctrl.IsNull, true))
               {
                  // #931545 - var change must be invoked first before control verification.
                  ctrl.ModifiedByUser = true;
                  handleCtrlModify(ctrl, InternalInterface.MG_ACT_SELECTION);
               }

               if (ctrl.isChoiceControl())
                  Commands.setGetSuggestedValueOfChoiceControlOnTagData(ctrl, ctrl.IsRepeatable
                                                                                 ? line
                                                                                 : 0, false);
               // For multiple selection ListBox value to field and ctrl would be set on leaving 
               // control, before executing control suffix. So variable change internal event would be 
               // handled before executing control suffix.
               if (!ctrl.IsMultipleSelectionListBox())
                  ctrl.validateAndSetValue(val, true);
            }
         }
      }

      /// <summary>
      ///   Handle Mouse Up Event
      /// </summary>
      /// <param name = "ctrl">contol
      /// </param>
      /// <param name = "line">line of multiline control
      /// </param>
      /// <param name = "produceClick">if true, produce click event
      /// </param>
      private void handleMouseUp(MgControl ctrl, int line, bool produceClick)
      {
         RunTimeEvent rtEvt;
         bool canGotoCtrl;
         MgControl returnToCtrl;
         bool StopFocus;

         if (ctrl == null)
            return;

         //QCR # 6144 see decumentation for the _currClickedRadio memebr
         if (ctrl.Type == MgControlType.CTRL_TYPE_RADIO)
            //TODO: check, probably not needed in WebClient
            ClientManager.Instance.RuntimeCtx.CurrentClickedRadio = ctrl;
         try
         {
            if (ctrl.Type != MgControlType.CTRL_TYPE_TABLE)
            {
               if (!ctrl.checkProp(PropInterface.PROP_TYPE_ENABLED, true, line))
                  return;

               canGotoCtrl = canGoToControl(ctrl, line, true);
               StopFocus = ctrl.isFocusedStopExecution();

               returnToCtrl = ClientManager.Instance.ReturnToCtrl;

               if ((!canGotoCtrl || ((_stopExecution || StopFocus) && returnToCtrl != ctrl)) &&
                   returnToCtrl != null)
               {
                  MGDataCollection.Instance.currMgdID = ((Task)returnToCtrl.getForm().getTask()).getMgdID();
                  if (returnToCtrl.IsRepeatable)
                     ((MgForm)returnToCtrl.getForm()).bringRecordToPage();
                  Manager.SetFocus(returnToCtrl, -1);
                  returnToCtrl.getForm().getTask().setLastParkedCtrl(returnToCtrl);
               }

               // need't insert CLICK event if the verification has failed
               if (_stopExecution || StopFocus)
                  return;
            }

            if (produceClick &&
                (ctrl.Type == MgControlType.CTRL_TYPE_BUTTON || ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX ||
                 ctrl.Type == MgControlType.CTRL_TYPE_LIST))
            {
               rtEvt = new RunTimeEvent(ctrl);
               rtEvt.setInternal(InternalInterface.MG_ACT_WEB_CLICK);
               addToTail(rtEvt);
            }
         }
         finally
         {
            ClientManager.Instance.RuntimeCtx.CurrentClickedRadio = null;
         }
      }

      /// <summary>
      ///   Handle Focus Event
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "line">line of multiline control</param>
      /// <param name = "onClick"></param>
      private void handleFocus(MgControl ctrl, int line, bool onClick)
      {
         // for wide control, don't execute MG_ACT_CTRL_PREFIX for the ctrl and MG_ACT_CTRL_SUFFIX for the prev ctrl
         if (ctrl.isWideControl())
            return;

         Task currTask;

         try
         {
            // when CloseTasksOnParentActivate is true, try and close all child tasks before focusing.
            if (ClientManager.Instance.getEnvironment().CloseTasksOnParentActivate())
            {
               // we send the task of the top most form since our focus might be inside a subform, and that is equal to clicking on its form.
               CloseAllChildProgs((Task)ctrl.getTopMostForm().getTask(), null, false);
               if (_stopExecution)
                  return;

               // static cannot get focus, meaning we are here just for the CloseTasksOnParentActivate.
               if (ctrl.isStatic())
                  return;
            }

            if (onClick)
               ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl = ctrl;

            MgControl prevCtrl = GUIManager.getLastFocusedControl();
            if (ctrl == prevCtrl && !ctrl.isRepeatableOrTree())
               // we received focus twice
               return;
            if (ctrl.isTreeControl() && line == 0)
               //we are not on any tree item, use current line
               line = ctrl.getDisplayLine(true);
            _nextParkedCtrl = ctrl; // save the NEXT control (used to check the direction)

            if (ctrl == null || ctrl.Type == MgControlType.CTRL_TYPE_TABLE)
            {
               if (prevCtrl == null)
                  HandleNonParkableControls(ctrl.getForm().getTask());
               return;
            }
            Task prevTask = ClientManager.Instance.getLastFocusedTask();

            if ((ctrl.Type == MgControlType.CTRL_TYPE_BUTTON) &&
                ((RaiseAt)(ctrl.getProp(PropInterface.PROP_TYPE_RAISE_AT).getValueInt()) ==
                 RaiseAt.TaskInFocus))
               currTask = prevTask;
            else
               currTask = (Task)ctrl.getForm().getTask();

            bool canGotoCtrl = canGoToControl(ctrl, line, onClick);

            // Go to the control - insert prefix
            if (canGotoCtrl)
            {
               int eventLine = (ctrl.isRepeatableOrTree() || ctrl.isRadio()
                                   ? line
                                   : int.MinValue);
               var rtEvt = new RunTimeEvent(ctrl, eventLine);
               rtEvt.setInternal(InternalInterface.MG_ACT_CTRL_PREFIX);
               handleEvent(rtEvt, false);
            }
            else if (prevCtrl != null)
            {
               if (!prevCtrl.isModifiable())
               {
                  if (prevCtrl.isChoiceControl())
                     prevCtrl.restoreOldValue();
               }
               else if (!currTask.cancelWasRaised())
               {
                  // For DotNet control we should not get the Value as it is irrelavant for them.
                  // We are using the value only to validate & set into the control, but for a DotNet control
                  // we should not validate & set value, as there is nothing to validate(i.e. we don't have a picture for it).
                  if (!prevCtrl.IsDotNetControl())
                  {
                     //TODO : restore it - Rinajs
                     String newVal = Manager.GetCtrlVal(prevCtrl);
                     if (!prevCtrl.validateAndSetValue(newVal, true))
                        setStopExecution(true);
                  }
               }
               ClientManager.Instance.ReturnToCtrl = prevCtrl;
            }
            else
               HandleNonParkableControls(ctrl.getForm().getTask());

            if (!_stopExecution && ClientManager.Instance.ReturnToCtrl == ctrl)
               ClientManager.Instance.setLastFocusedCtrl(ctrl);

            return;
         }
         finally
         {
            ctrl.setFocusedStopExecution(_stopExecution);
            if (_stopExecution)
            {
               if (ctrl.isTreeControl())
               {
                  //in tree we must return focus to previous line and undo SWT default selection
                  ctrl.getForm().SelectRow(true);
               }
               else
               {
                  MgControl returnToCtrl = ClientManager.Instance.ReturnToCtrl;

                  // Whenever we have a call prog and a verify error, we try to restore focus to 'ReturnToCtrl'.
                  // Now, since we have closed the task (invoked by call prog), Clr tries to focus on the last
                  // focused ctrl on the task. We have also set focus on 'ReturnToCtrl'. These 2 focuses
                  // coincide. So, we have invoked sleep, making sure that 'ReturnToCtrl' gets last focus.
                  if (ctrl != returnToCtrl)
                     Thread.Sleep(50);

                  if (returnToCtrl != null)
                  {
                     MGDataCollection.Instance.currMgdID = ((Task)returnToCtrl.getForm().getTask()).getMgdID();
                     Manager.SetFocus(returnToCtrl, -1);
                     returnToCtrl.getForm().getTask().setLastParkedCtrl(returnToCtrl);
                  }
               }
            }
         }
      }

      /// <summary>
      ///   returns true if we can go to the control and try to park on in
      /// </summary>
      /// <param name = "ctrl">control</param>
      /// <param name = "line">line</param>
      /// <param name = "onClick">real click is performed, this not click simulation</param>
      /// <returns></returns>
      private bool canGoToControl(MgControl ctrl, int line, bool onClick)
      {
         bool canGotoCtrl;

         if (onClick && (ctrl.Type == MgControlType.CTRL_TYPE_SUBFORM ||
                                             ctrl.Type == MgControlType.CTRL_TYPE_BROWSER))
            return false;

         // V9.4SP4d change of behavior - we do not "exit" from the current ctrl
         // (i.e. not performing ctrl suffix or ctrl verifications) if the target ctrl
         // is a button with no data attached or IMG or hypertext.
         canGotoCtrl = false;
         Task prevTask = ClientManager.Instance.getLastFocusedTask();
         var currTask = (Task)ctrl.getForm().getTask();

         if (!currTask.IsInteractive)
            return false;

         if (ClientManager.Instance.getEnvironment().getSpecialExitCtrl() || ctrl.getField() != null || ctrl.IsRepeatable || ctrl.IsDotNetControl() ||
             (prevTask != null && prevTask != currTask))
         {
            if (onClick)
            {
               switch (ctrl.Type)
               {
                  // if clicking on a button and the park_on_click is NO, then do not try to park on it.
                  case MgControlType.CTRL_TYPE_BUTTON:
                     if (line == ctrl.getDisplayLine(false) && (!ctrl.checkProp(PropInterface.PROP_TYPE_PARK_ON_CLICK, true, line) || (ctrl.getField() == null)))
                        canGotoCtrl = false;
                     else
                        canGotoCtrl = true;
                     break;

                  // if clicking on a .Net control and it's AllowParking is NO, then do not try to park on it.
                  // we don't want to leave the current control in this case.
                  // it must work as a button with park_on_click is NO.
                  case MgControlType.CTRL_TYPE_DOTNET:
                     canGotoCtrl = ctrl.checkProp(PropInterface.PROP_TYPE_ALLOW_PARKING, true, line);
                     break;

                  default:
                     canGotoCtrl = true;
                     break;
               }
            }
            else
               canGotoCtrl = true;
         }
         else if (ctrl.Type != MgControlType.CTRL_TYPE_BUTTON && ctrl.Type != MgControlType.CTRL_TYPE_IMAGE)
            canGotoCtrl = true;

         return canGotoCtrl;
      }

      /// <summary>
      ///   Handle Timer Events
      /// </summary>
      /// <param name = "evt">tuntime timer event</param>
      private void handleTimer(RunTimeEvent evt)
      {
         MGData mgd;
         evt.setType(ConstInterface.EVENT_TYPE_TIMER);
         if (evt.isIdleTimer())
            handleExpressionHandlers();
         else
         {
            mgd = MGDataCollection.Instance.getMGData(evt.getMgdID());
            handleEvents(mgd.getTimerHandlers(), evt);
         }
      }

      /// <summary>
      ///   process expression handlers
      /// </summary>
      internal void handleExpressionHandlers()
      {
         int i;
         MGData mgd;
         RunTimeEvent rtEvt;

         rtEvt = new RunTimeEvent(ClientManager.Instance.getLastFocusedTask(), GUIManager.getLastFocusedControl());
         rtEvt.setExpression(null);
         rtEvt.setMainPrgCreator(rtEvt.getTask());
         for (i = 0;
              i < MGDataCollection.Instance.getSize();
              i++)
         {
            mgd = MGDataCollection.Instance.getMGData(i);
            if (mgd != null && !mgd.IsAborting)
               handleEvents(mgd.getExpHandlers(), rtEvt);
         }
      }

      /// <summary>
      ///   resize of page
      /// </summary>
      /// <param name = "ctrl">table</param>
      /// <param name = "rowsInPage"></param>
      private void handleResize(MgControl ctrl, int rowsInPage)
      {
         ((MgForm)ctrl.getForm()).setRowsInPage(rowsInPage);
#if PocketPC
   // On mobile, when the soft keyboard appears the table size changes. This call will
   // make sure the table scrolls to keep the focused control visible
         ((MgForm)ctrl.getForm()).bringRecordToPage();
#endif
      }

      /// <summary>
      ///   get data for Row
      /// </summary>
      /// <param name = "ctrl">
      /// </param>
      /// <param name = "sendAll">if true, send all records
      /// </param>
      /// <param name = "row">
      /// </param>
      private void handleRowDataCurPage(MgControl table, int desiredTopIndex, bool sendAll,
                                        LastFocusedVal lastFocusedVal)
      {
         var form = (MgForm)table.getForm();
         form.setRowData(desiredTopIndex, sendAll, lastFocusedVal);
      }

      /// <summary>
      ///   transfers data to GUI thread in free worker thread time
      /// </summary>
      /// <param name = "ctrl"></param>
      private void handleTransferDataToGui(MgControl ctrl)
      {
         var form = (MgForm)ctrl.getForm();
         form.transferDataToGui();
      }
      /// <summary>
      /// check if it is allowed and open combo drop down list
      /// </summary>
      /// <param name="ctrl">comboboox control</param>
      /// <param name="line">row applicale for table control</param>
      private void handleComboDropDown(MgControl ctrl, int line)
      {
         if (ctrl.isParkable(true, false) && ctrl.getDisplayLine(true) == line && ctrl.isModifiable())
            ctrl.processComboDroppingdown(line);
      }

      /// <summary>
      ///   When internal act asks for enable/disable of an act list.
      ///   call enableList with onlyIfChanged = true to minimize calls to menus refresh.
      /// </summary>
      /// <param name = "enable"></param>
      private void handleEnableEvents(RunTimeEvent evt, bool enable)
      {
         evt.getTask().ActionManager.enableList(evt.getActEnableList(), enable, true);
      }

      /// <summary>
      ///   Evaluates the direction of flow.
      /// </summary>
      /// <param name = "rtEvt">
      /// </param>
      private void handleDirection(RunTimeEvent rtEvt)
      {
         Task task = rtEvt.getTask();
         MgControl ctrl = rtEvt.Control;
         MgControl lastParkedCtrl = GUIManager.getLastFocusedControl();

         // Set direction to forward if task executed first time.
         if (task.getLastParkedCtrl() == null)
         {
            task.setDirection(Direction.FORE);
            return;
         }

         if (lastParkedCtrl == null)
            return;

         if ((!task.getInRecordSuffix() && !task.getInCreateLine()) || ((DataView)task.DataView).getCurrRec().Modified)
         {
            // preform ver only if we are not here because of a rec suffix
            if (lastParkedCtrl.getField() == null)
               return;

            if (ctrl != null)
            {
               if (lastParkedCtrl == ctrl)
                  task.setDirection(Direction.FORE);
               else
               {
                  // find ctrl in prev direction according to taborder. If found set direction to back.
                  for (MgControl prevCtrl = lastParkedCtrl.getPrevCtrl();
                       prevCtrl != null;
                       prevCtrl = prevCtrl.getPrevCtrl())
                  {
                     if (prevCtrl == ctrl)
                     {
                        task.setDirection(Direction.BACK);
                        break;
                     }
                  }

                  if (task.getDirection() == Direction.NONE)
                  {
                     // find ctrl in forward direction according to taborder. If found set direction to forward.
                     for (MgControl nextCtrl = lastParkedCtrl.getNextCtrl();
                          nextCtrl != null;
                          nextCtrl = nextCtrl.getNextCtrl())
                     {
                        if (nextCtrl == ctrl)
                        {
                           task.setDirection(Direction.FORE);
                           break;
                        }
                     }
                  }
               }

               if (task.getDirection() == Direction.BACK)
               {
                  bool setForwardDirection = false;

                  // As in OL, in case the user clicks on another row in same task, set task's direction as Forward.
                  if (ctrl.getForm() == lastParkedCtrl.getForm() &&                    // same task
                     ((Task)ctrl.getForm().getTask()).getFlowMode() == Flow.FAST &&    // click
                      rtEvt.getDisplayLine() >= 0 &&
                      ctrl.getForm().DisplayLine != rtEvt.getDisplayLine())            // another row
                     setForwardDirection = true;

                  // 1. we may have done tab from the last control on the form, so we get lastmoveinrowdir as BEGIN.
                  // 2. we may also have tabbed from last ctrl on subform and moved to parent form. On parent form,
                  // we invoked moveInView (i.e moving to next record).
                  // In both case, we would get dir_begin on curr task, and must reset the direction to FORE.
                  if (((Task)lastParkedCtrl.getForm().getTask()).getFlowMode() == Flow.STEP &&
                      ctrl.getForm().getTask().getLastMoveInRowDirection() == Constants.MOVE_DIRECTION_BEGIN)
                     setForwardDirection = true;

                  if (setForwardDirection)
                  {
                     task.setDirection(Direction.NONE);
                     task.setDirection(Direction.FORE);
                  }
               }
            }
         }
      }

      /// <summary>
      ///   Handles control Prefix
      /// </summary>
      /// <param name = "evt"></param>
      /// <returns></returns>
      private bool handleCtrlPrefix(RunTimeEvent evt)
      {
         Task task = evt.getTask();
         MgForm form;
         int displayLine;
         MgControl ctrl, lastParkedCtrl;
         Field field;
         bool cursorMoved;
         MgControl srcCtrl;
         RunTimeEvent rtEvt;
         var currClickedCtrl = (MgControl)ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl;

         form = (MgForm)task.getForm();
         displayLine = evt.getDisplayLine();
         ctrl = evt.Control;

         if (form.isLineMode())
         {
            if (displayLine == Int32.MinValue || ctrl.isRadio())
               displayLine = form.DisplayLine;
         }
         else
            displayLine = Int32.MinValue;

         // note that the method exitWide() is change the event (ctrl & internal action)
         ctrl = exitWide(evt);

         lastParkedCtrl = GUIManager.getLastFocusedControl();

         task.locateQuery.Init(ctrl);

         // prevent double prefix on the same control
         // For Fast Mode, CS is executed from here, whereas for step mode it is done
         // in moveInRow. so, for step mode double execution of CS must be stopped.
         if (lastParkedCtrl != null &&
             ((Task)lastParkedCtrl.getForm().getTask()).getMGData().GetId() == ((Task)ctrl.getForm().getTask()).getMGData().GetId() &&
             (lastParkedCtrl.getForm().getTask()).getLevel() == Constants.TASK_LEVEL_CONTROL)
         {
            if ((ctrl != lastParkedCtrl || (ctrl.isRepeatableOrTree() && displayLine != ctrl.getDisplayLine(true))) &&
                !lastParkedCtrl.isInControlSuffix())
            //QCR #970806
            {
               handleInternalEvent(lastParkedCtrl, InternalInterface.MG_ACT_CTRL_SUFFIX);
               if (_stopExecution)
               {
                  if (ctrl.Type == MgControlType.CTRL_TYPE_LIST || ctrl.Type == MgControlType.CTRL_TYPE_RADIO ||
                      ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX || ctrl.isTabControl())
                  {
                     ctrl.resetPrevVal();
                     ctrl.RefreshDisplay();
                  }
                  return false;
               }
            }
            else
               return false;
         }

         if (task.InCtrlPrefix)
            return false;

         task.InCtrlPrefix = true;

         form.UpdateStatusBar(GuiConstants.SB_INSMODE_PANE_LAYER,
                              form.GetMessgaeForStatusBar(GuiConstants.SB_INSMODE_PANE_LAYER, ClientManager.Instance.RuntimeCtx.IsInsertMode()), false);

         // make sure it has not come again through moveinrow.
         // eg: if clicked ctrl on subform is non parkable, first time it executes RP CV for subforms and then 
         // enters moveinrow for parkable ctrl. we shouldnot execute this block when we come for CP of parkable ctrl.
         // For tab, we come here directly for parkable ctrl, and this block must always execute if on diff forms.
         // isSort : if clicked on table header, we park on colomn ctrl after sort, and we must include subforms
         // in executing CVs
         if (task.getLastMoveInRowDirection() == Constants.MOVE_DIRECTION_NONE || _isSorting)
         {
            // If the current ctrl is on a subform, then execute in sequence 
            // 1. RS of nested subforms until subform task containg these 2 subforms. we must also execute RS if clicked
            //    in subform and parked back on a ctrl's task. (#722115- currClickedCtrl and ctrl should not be on different window)
            // 2. CV execution of LastParkedCtrl till end
            // 3. RP-CVs for all intermediate subforms
            MgControl prevCtrl = lastParkedCtrl;
            if (currClickedCtrl != null &&
                ((Task)currClickedCtrl.getForm().getTask()).getMgdID() == ((Task)ctrl.getForm().getTask()).getMgdID() &&
                ctrl.onDiffForm(currClickedCtrl))
               prevCtrl = currClickedCtrl;

            if (prevCtrl != null && ctrl.onDiffForm(prevCtrl))
            {
               // RS of nested subforms until subform task contaning these 2 subforms
               var prevTask = (Task)prevCtrl.getForm().getTask();
               prevTask.execSubformRecSuffix((Task)ctrl.getForm().getTask());
               if (_stopExecution)
               {
                  if (ctrl.Type == MgControlType.CTRL_TYPE_LIST || ctrl.Type == MgControlType.CTRL_TYPE_RADIO ||
                      ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX || ctrl.isTabControl())
                  {
                     ctrl.resetPrevVal();
                     ctrl.RefreshDisplay();
                  }
                  task.InCtrlPrefix = false;
                  return false;
               }

               // If subform and step, set the flow and direction of ctrl's task to prevCtrl's task.
               if (prevTask.getFlowMode() == Flow.STEP)
               {
                  task.setDirection(Direction.NONE);
                  task.setFlowMode(Flow.NONE);
                  task.setDirection(prevTask.getDirection());
                  task.setFlowMode(prevTask.getFlowMode());
               }

               // handle CV execution of prevCtrl till end CVs for Subforms
               // and RP-CVs for all intermediate subforms
               if (!executeVerifyHandlerSubforms(task, ctrl))
               {
                  if (_stopExecution)
                  {
                     if (ctrl.isListBox() || ctrl.isRadio() || ctrl.isTabControl() || ctrl.isCheckBox())
                     {
                        ctrl.resetPrevVal();
                        ctrl.RefreshDisplay();
                     }
                  }

                  task.InCtrlPrefix = false;
                  return false;
               }

               // reset dir and flow of prevCtrl's task back to FLOW_NONE
               prevTask.setDirection(Direction.NONE);
               prevTask.setFlowMode(Flow.NONE);

               if ((ctrl.getForm().getTask()).isAborting())
               {
                  task.InCtrlPrefix = false;
                  return false;
               }

               doSyncForSubformParent(task);
            }

            // Execute RP of the current task before executing the CVs before 'ctrl'
            if (task.DataView.isEmptyDataview())
               handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
            else
            {
               task.SubformExecMode = SubformExecModeEnum.SET_FOCUS;
               rtEvt = new RunTimeEvent(ctrl, displayLine);
               rtEvt.setInternal(InternalInterface.MG_ACT_REC_PREFIX);
               handleEvent(rtEvt, false);
            }
            if (GetStopExecutionFlag())
            {
               task.InCtrlPrefix = false;
               return false;
            }
         }
         else if (task.getInCreateLine())
         {
            // if task is in create mode and we are switching between forms, set the flow and direction of ctrl's task
            // to lastparkedctrl's task. we may have tabbed from last ctrl on subform, and landed on some other form in
            // create mode. (#988152)
            if (lastParkedCtrl != null && ctrl.onDiffForm(lastParkedCtrl))
            {
               var lastParkedTask = (Task)lastParkedCtrl.getForm().getTask();

               if (lastParkedTask.getFlowMode() == Flow.STEP)
               {
                  task.setDirection(Direction.NONE);
                  task.setFlowMode(Flow.NONE);
                  task.setDirection(lastParkedTask.getDirection());
                  task.setFlowMode(lastParkedTask.getFlowMode());

                  // reset dir and flow of lastParkedCtrl's task back to FLOW_NONE
                  lastParkedTask.setDirection(Direction.NONE);
                  lastParkedTask.setFlowMode(Flow.NONE);
               }
            }
         }

         Flow orgFlow = task.getFlowMode();
         Direction orgDirection = task.getDirection();

         //QCR# 806015: If we have a single field with allow parking = Yes and Tab Into = No, then on pressing tab if cursor is not moved, we come to 
         //park on lastParkedCtrl(which is same as ctrl) since TabInto is not allowed.
         //So to avaoid infinite recursion, we should not again try to park on it.
         if ((!ctrl.IsParkable(false) && ctrl != lastParkedCtrl) ||
             (!ctrl.IgnoreDirectionWhileParking && !ctrl.allowedParkRelatedToDirection()))
         {
            task.InCtrlPrefix = false;

            if (lastParkedCtrl == null)
            {
               // if we have not parked on any ctrl and got a stopexecution, move in prev direction.
               if (getStopExecutionCtrl() != null)
               {
                  task.setCurrVerifyCtrl(_currCtrl);

                  task.setDirection(Direction.NONE);
                  task.setDirection(Direction.BACK);
                  cursorMoved = form.moveInRow(ctrl, Constants.MOVE_DIRECTION_PREV);
               }
               else
                  task.moveToFirstCtrl(true);
            }
            else
            {
               if (ctrl.getField() == null)
               {
                  cursorMoved = lastParkedCtrl.invoke();
                  if (!cursorMoved)
                     cursorMoved = form.moveInRow(null, Constants.MOVE_DIRECTION_NEXT);
               }
               else
               {
                  bool isDirFrwrd = false;

                  if (ctrl.onDiffForm(lastParkedCtrl)
                      || form.ctrlTabOrderIdx(ctrl) > form.ctrlTabOrderIdx(lastParkedCtrl)
                      || form.PrevDisplayLine != ctrl.getForm().DisplayLine)
                     isDirFrwrd = true;

                  // if we had a stopexecution and ctrl is non parkable, then we are here
                  // to find the next parkable ctrl in reverse direction.
                  if (getStopExecutionCtrl() != null)
                     isDirFrwrd = !isDirFrwrd;

                  if (isDirFrwrd)
                  {
                     task.setDirection(Direction.NONE);
                     task.setDirection(Direction.FORE);
                     cursorMoved = form.moveInRow(ctrl, Constants.MOVE_DIRECTION_NEXT);
                  }
                  else
                  {
                     task.setDirection(Direction.NONE);
                     task.setDirection(Direction.BACK);
                     cursorMoved = form.moveInRow(ctrl, Constants.MOVE_DIRECTION_PREV);
                  }
               }

               task.setDirection(Direction.NONE);
               task.setFlowMode(Flow.NONE);
               task.setDirection(orgDirection);
               task.setFlowMode(orgFlow);

               // TODO - Maybe, we donot require this error as we always try to park on some control in moveinrow.
               if (!cursorMoved && !task.DataView.isEmptyDataview())
                  exitWithError(task, MsgInterface.RT_STR_CRSR_CANT_PARK);
            }
            return false;
         }

         // if we have a stop execution ctrl, we need to use it as a srctrl, otherwise use
         // the ctrl on which we executed last CS.
         srcCtrl = (MgControl)getStopExecutionCtrl() ?? (MgControl)(ctrl.getForm().getTask()).getCurrVerifyCtrl();

         // execute the verify handlers for the current task
         if (srcCtrl != ctrl || task.isMoveInRow()
             || (lastParkedCtrl == null && getStopExecutionCtrl() != null)
             || form.PrevDisplayLine != ctrl.getForm().DisplayLine)
         {
            MgControl frmCtrl = getCtrlExecCvFrom(task, srcCtrl, ctrl);
            MgControl toCtrl = getCtrlExecCvTo(task, frmCtrl, ctrl);
            executeVerifyHandlers((Task)ctrl.getForm().getTask(), ctrl, frmCtrl, toCtrl);
            if (_stopExecution)
            {
               if (ctrl.isListBox() || ctrl.isRadio() || ctrl.isTabControl() || ctrl.isCheckBox())
               {
                  ctrl.resetPrevVal();
                  ctrl.RefreshDisplay();
               }

               task.InCtrlPrefix = false;
               return false;
            }
         }

         //Defect 122008 : We are clearing out the status bar panes which will be enabled later in control prefix.
         //Generally we enable/disable the panes in control suffix and enable them in control prefix. But in defect scenario we don't execute control suffix as we are moving to other task.
         //Hence, it is better to clear the panes now and those will be enabled as per proper conditions.
         form.UpdateStatusBar(GuiConstants.SB_ZOOMOPTION_PANE_LAYER, form.GetMessgaeForStatusBar(GuiConstants.SB_ZOOMOPTION_PANE_LAYER, false), false);
         form.UpdateStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER, form.GetMessgaeForStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER, false), false);

         task.setLevel(Constants.TASK_LEVEL_CONTROL);

         ctrl.KeyStrokeOn = false;
         ctrl.ModifiedByUser = false;

         task.setCurrField(ctrl.getField());

         if (ctrl.IsRepeatable)
            form.bringRecordToPage();

         // enable-disable actions for Subforms
         if (lastParkedCtrl != ctrl)
         {
            if (((lastParkedCtrl == null) && ctrl.getForm().isSubForm()) ||
                ((lastParkedCtrl != null) && (ctrl.getForm().isSubForm() || lastParkedCtrl.getForm().isSubForm())))
            {
               Task lastParkedTask = null;
               if (lastParkedCtrl != null)
                  lastParkedTask = (Task)lastParkedCtrl.getForm().getTask();

               if (lastParkedTask != task)
               {
                  Task parentTask;
                  bool isSonSubform;
                  bool firstTime = true;

                  if (lastParkedTask != null)
                  {
                     isSonSubform = lastParkedTask.IsSubForm;

                     for (parentTask = lastParkedTask;
                          (parentTask != null) && isSonSubform;
                          parentTask = (Task)parentTask.getParent())
                     {
                        if (!firstTime)
                        {
                           isSonSubform = parentTask.IsSubForm;
                           parentTask.ActionManager.enable(InternalInterface.MG_ACT_ZOOM, false);
                           parentTask.ActionManager.enable(InternalInterface.MG_ACT_DELLINE, false);
                           parentTask.enableCreateActs(false);
                        }
                        else
                           firstTime = false;
                     }
                  }

                  isSonSubform = task.IsSubForm;

                  for (parentTask = (Task)task.getParent();
                       (parentTask != null) && isSonSubform;
                       parentTask = (Task)parentTask.getParent())
                  {
                     isSonSubform = parentTask.IsSubForm;

                     if (parentTask.getEnableZoomHandler())
                        parentTask.ActionManager.enable(InternalInterface.MG_ACT_ZOOM, true);
                     parentTask.setCreateDeleteActsEnableState();
                  }

                  // Whenever we are moving between tasks, re-evaluate AllowModes.
                  task.enableModes();
               }
            }
         }

         if (ctrl.isChoiceControl())
            task.ActionManager.enable(InternalInterface.MG_ACT_ARROW_KEY, true);
         if (ctrl.isComboBox() && !task.ActionManager.isEnabled(InternalInterface.MG_ACT_COMBO_DROP_DOWN))
            task.ActionManager.enable(InternalInterface.MG_ACT_COMBO_DROP_DOWN, true);

         // enable actions and states
         if (task.getEnableZoomHandler() || ctrl.useZoomHandler())
            task.ActionManager.enable(InternalInterface.MG_ACT_ZOOM, true);

         ctrl.enableTextAction(true);
         if (ctrl.isTextControl() || ctrl.isRichEditControl() || ctrl.isChoiceControl())
            ctrl.SetKeyboardLanguage(false);

         //For RichEdit Control, Enter key should be used as NextLine, So, do not refreshDefaultButton. It will be null.
         if (!ctrl.isRichEditControl())
            ctrl.refreshDefaultButton();

         if (ctrl.Type == MgControlType.CTRL_TYPE_BUTTON)
            task.ActionManager.enable(InternalInterface.MG_ACT_BUTTON, true);
         task.setCreateDeleteActsEnableState();
         if (ctrl.isModifiable() && (task.getMode() == Constants.TASK_MODE_CREATE))
            task.ActionManager.enable(InternalInterface.MG_ACT_RT_COPYFLD, true);
         field = (Field)(ctrl.getField());
         if ((field != null && field.NullAllowed) && ctrl.isModifiable() &&
             (ctrl.Type != MgControlType.CTRL_TYPE_BUTTON && ctrl.Type != MgControlType.CTRL_TYPE_DOTNET))
            task.ActionManager.enable(InternalInterface.MG_ACT_RT_EDT_NULL, true);
         if (ctrl.isTreeControl())
         {
            task.setKeyboardMappingState(Constants.ACT_STT_TREE_PARK, true);
            task.ActionManager.enable(InternalInterface.MG_ACT_TREE_RENAME_RT, true);
         }
         Manager.MenuManager.refreshMenuActionForTask(ctrl.getForm().getTask());

         return true;
      }

      /// <summary>
      ///   do some default operations for an event and return true to tell the caller
      ///   to continue handling the event
      /// </summary>
      /// <param name = "evt">the event to handle</param>
      /// <returns> tells the caller whether to continue handling the event</returns>
      private bool commonHandlerBefore(RunTimeEvent evt)
      {
         RunTimeEvent evtSave = evt;
         Task task = evt.getTask();
         MgForm form;
         int displayLine;
         MgControl ctrl, lastFocusedCtrl;
         DataView dv;
         Record rec = null;
         Record prevRec = null;
         String val;
         int intEvtCode;
         int oldDisplayLine = Int32.MinValue;
         bool arrowPressed = false;

         if (_stopExecution)
            return false;

         if (task.isAborting() && !task.TaskService.AllowEventExecutionDuringTaskClose(evt))
            return false;

         if (evt.getType() == ConstInterface.EVENT_TYPE_SYSTEM)
         {
            // handled internally
            if (evt.getKbdItm() != null)
            {
               int keyCode = evt.getKbdItm().getKeyCode();
               if (keyCode == GuiConstants.KEY_RIGHT || keyCode == GuiConstants.KEY_LEFT)
                  arrowPressed = true;
            }
            int actId = getMatchingAction(task, evt.getKbdItm(), false);
            if (actId != 0 && evt.Control != null)
            //&& actId != MG_ACT_SELECT) 
            {
               evt = new RunTimeEvent(evt, evt);
               evt.setInternal(actId);
               evt.setArgList(null);
               MgControl mgControl = exitWide(evt);
               evtSave.setCtrl(mgControl);
               if (mgControl == null)
                  return false;
            }
         }

         // QCR# 939253 - we have to process commonHandlerBefore even for internal event converted
         // from system Event
         if (evt.getType() == ConstInterface.EVENT_TYPE_INTERNAL)
         {
            form = (MgForm)task.getForm();
            displayLine = evt.getDisplayLine();
            dv = (DataView)task.DataView;
            rec = (Record)dv.getCurrRec();

            //note that the method exitWide() is change the event (ctrl & internal action)
            ctrl = exitWide(evt);
            intEvtCode = evt.getInternalCode();
            MgTreeBase tree = null;
            if (form != null)
               tree = form.getMgTree();

            lastFocusedCtrl = GUIManager.getLastFocusedControl();

            //before all events exept of MG_ACT_CHAR, MG_ACT_EDT_DELPRVCH, MG_ACT_TIMER execute incremental locate if it is needed
            if ((intEvtCode < 1000) && (intEvtCode != InternalInterface.MG_ACT_CHAR) &&
                (intEvtCode != InternalInterface.MG_ACT_EDT_DELPRVCH))
            {
               task.locateQuery.InitServerReset = true;
               locateInQuery(task);
            }

            // for internal events
            switch (intEvtCode)
            {
               //case InternalInterface.MG_ACT_CTRL_DBLCLICK:
               //   if (selectProg(ctrl, false))
               //      return false;
               case InternalInterface.MG_ACT_WEB_ON_DBLICK:
                  break;

               case InternalInterface.MG_ACT_CTRL_FOCUS:
                  handleFocus(ctrl, displayLine, evt.isProduceClick());
                  return false;

               case InternalInterface.MG_ACT_CTRL_KEYDOWN:
                  handleKeyDown(task, ctrl, evt);
                  return false;

               case InternalInterface.MG_ACT_CTRL_MOUSEUP:
                  handleMouseUp(ctrl, displayLine, evt.isProduceClick());
                  return false;

               case InternalInterface.MG_ACT_ARROW_KEY:
                  selectionControlHandleArrowKeyAction(ctrl, evt);
                  break;

               case InternalInterface.MG_ACT_DEFAULT_BUTTON:
                  onActDefaultButton(form, evt);
                  return false;

               case InternalInterface.MG_ACT_SELECTION:
                  onActSelection(ctrl, form, evt);
                  return false;

               case InternalInterface.MG_ACT_TIMER:
                  handleTimer(evt);
                  return false;

               case InternalInterface.MG_ACT_INCREMENTAL_LOCATE:
                  locateInQuery(evt.getTask());
                  break;

               case InternalInterface.MG_ACT_RESIZE:
                  handleResize(ctrl, displayLine);
                  return false;

               case InternalInterface.MG_ACT_ROW_DATA_CURR_PAGE:
                  handleRowDataCurPage(ctrl, displayLine, evt.isSendAll(), evt.LastFocusedVal);
                  return false;

               case InternalInterface.MG_ACT_DV_TO_GUI:
                  handleTransferDataToGui(ctrl);
                  return false;

               case InternalInterface.MG_ACT_ENABLE_EVENTS:
                  handleEnableEvents(evt, true);
                  return false;

               case InternalInterface.MG_ACT_COMBO_DROP_DOWN:
                  handleComboDropDown(ctrl, displayLine);
                  return false;


               case InternalInterface.MG_ACT_DISABLE_EVENTS:
                  handleEnableEvents(evt, false);
                  return false;

               case InternalInterface.MG_ACT_TREE_COLLAPSE:
                  if (tree != null)
                  {
                     if (displayLine > 0)
                     {
                        if (tree.isAncestor(displayLine, form.DisplayLine))
                           moveToLine(form, displayLine, ctrl);
                     }
                     else if (arrowPressed && !tree.isExpanded(form.DisplayLine))
                     {
                        int newLine = tree.calculateLine(Constants.MOVE_DIRECTION_PARENT,
                                                         Constants.MOVE_UNIT_TREE_NODE, 0);
                        if (newLine != MgTreeBase.NODE_NOT_FOUND)
                           moveToLine(form, newLine, ctrl);
                        return false;
                     }
                  }

                  break;

               case InternalInterface.MG_ACT_TREE_EXPAND:
                  if (tree != null && displayLine <= 0 && arrowPressed && tree.isExpanded(form.DisplayLine))
                  {
                     int newLine = tree.calculateLine(Constants.MOVE_DIRECTION_FIRST_SON,
                                                      Constants.MOVE_UNIT_TREE_NODE, 0);
                     if (newLine != MgTreeBase.NODE_NOT_FOUND)
                        moveToLine(form, newLine, ctrl);
                     return false;
                  }
                  break;

               case InternalInterface.MG_ACT_TBL_REORDER:
                  if (form.isAutomaticTabbingOrder())
                     form.applyTabOrderChangeAfterTableReorder(evt.ControlsList);
                  break;

               case InternalInterface.MG_ACT_TASK_PREFIX:
                  task.setLevel(Constants.TASK_LEVEL_TASK);
                  break;

               case InternalInterface.MG_ACT_TASK_SUFFIX:
                  task.setLevel(Constants.TASK_LEVEL_TASK);
                  break;

               case InternalInterface.MG_ACT_REC_PREFIX:
                  // Defect 115903. Do not execute RP if the subform parent task didn't finished yet 
                  // it's CommonHandlerBefore for RP.
                  if (task.IsSubForm && ((Task)task.getParent()).InCommonHandlerBeforeRP)
                     return false;

                  // Recovery from transaction restart was completed. We may now clear the flag
                  task.setAfterRetry(ConstInterface.RECOVERY_NONE);
                  task.DataSynced = false;

                  if (!task.isStarted())
                     return false;                  


                  // if the form contains a table and the control was in the table
                  // move to the new row
                  if (form.getRowsInPage() > Int32.MinValue && displayLine > Int32.MinValue)
                  {
                     if (!form.hasTree())
                     {
                        // ensure a valid row
                        while (!dv.recExists(displayLine) && displayLine > 0)
                        {
                           displayLine--;
                        }
                     }
                     if (displayLine >= 0)
                     {
                        prevRec = (Record)dv.getCurrRec();
                        char oldTaskMode = task.getMode();
                        int oldRecId = prevRec.getId();

                        try
                        {
                           oldDisplayLine = form.DisplayLine;
                           //oldDisplayLine = form.getCurrRow()
                           form.PrevDisplayLine = oldDisplayLine;
                           form.setCurrRowByDisplayLine(displayLine, true, false);
                           // refresh the display if the previous record was deleted or canceled
                           if (prevRec != null && !dv.recExistsById(prevRec.getId()))
                           {
                              form.RefreshDisplay(Constants.TASK_REFRESH_TABLE);
                              // The following refresh is essential. If it fails because of an
                              // exception then the old curr row is restored (in the catch
                              // statement). It can happen when the user clicks on a line which
                              // becomes blank due to the deletion of the previous record.
                              form.RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                           }
                           rec = (Record)dv.getCurrRec();
                        }
                        catch (RecordOutOfDataViewException e)
                        {
                           MgTreeBase mgTree = form.getMgTree();
                           if (_stopExecution)
                           {
                              if (mgTree == null && oldTaskMode == Constants.TASK_MODE_CREATE && oldDisplayLine > 0 &&
                                  !dv.recExistsById(oldRecId))
                              {
                                 int prevLine = form.getPrevLine(oldDisplayLine);
                                 form.restoreOldDisplayLine(prevLine);
                              }
                              else
                                 form.restoreOldDisplayLine(oldDisplayLine);
                              form.RefreshDisplay(Constants.TASK_REFRESH_FORM);
                              setStopExecution(false);
                              lastFocusedCtrl = (MgControl)task.getLastParkedCtrl();
                              if (lastFocusedCtrl != null)
                                 lastFocusedCtrl.invoke();
                              setStopExecution(true);
                              return false;
                           }
                           else if (e.noRecord() && mgTree != null)
                           {
                              //the record of the node does not exists
                              ((MgTree)mgTree).handleNoRecordException(displayLine);
                              form.restoreOldDisplayLine(oldDisplayLine);
                              form.RefreshDisplay(Constants.TASK_REFRESH_FORM);
                              lastFocusedCtrl = (MgControl)task.getLastParkedCtrl();
                              if (lastFocusedCtrl != null)
                                 lastFocusedCtrl.invoke();
                              setStopExecution(true);
                              return false;
                           }
                           else
                              Logger.Instance.WriteExceptionToLog(e.StackTrace);
                        }
                     }
                     else
                        throw new ApplicationException("in EventsManager.commonHandlerBefore(): invalid line number: " +
                                                       displayLine);
                  }
                  if (rec == null)
                     throw new ApplicationException(
                        "in EventsManager.commonHandlerBefore(): no current record available !");

                  ReturnResult retResult = task.TaskTransactionManager.CheckAndOpenLocalTransaction(ConstInterface.TRANS_RECORD_PREFIX);
                  if (!retResult.Success)
                        return false;

                  // only for non interactive tasks. Increase the task's counter for every record we pass.
                  // do it before the evalEndCond since it might include counter(0).
                  // increase only if going into the rec prefix.
                  if (!task.IsInteractive && task.getLevel() == Constants.TASK_LEVEL_TASK)
                     task.increaseCounter();

                  // check the "task end condition" before entering the record
                  if (task.evalEndCond(ConstInterface.END_COND_EVAL_BEFORE))
                  {
                     task.endTask(true, false, false);
                     return false;
                  }
                  if (form.HasTable())
                     form.bringRecordToPage();

                  form.RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);

                  // to prevent double prefix on the same record
                  if (task.getLevel() != Constants.TASK_LEVEL_TASK)
                     return false;

                  Logger.Instance.WriteDevToLog("RECORD PREFIX: " + task.queryTaskPath());

                  ((Record)dv.getCurrRec()).setForceSaveOrg(false);
                  ((Record)dv.getCurrRec()).Synced = false;

                  dv.saveOriginal();

                  // do Record Prefix for all fathers that their Record Prefix is not done earlier
                  if (task.IsSubForm && task.PerformParentRecordPrefix)
                  {
                     var parentTask = (Task)task.getParent();
                     while (parentTask.IsSubForm)
                     {
                        if (parentTask.getLevel() == Constants.TASK_LEVEL_TASK)
                        {
                           handleInternalEvent(parentTask, InternalInterface.MG_ACT_REC_PREFIX);
                           parentTask = (Task)parentTask.getParent();
                           if (GetStopExecutionFlag())
                              break;
                        }
                        else
                           break;
                     }
                  }

                  // handle sub-forms
                  task.InCommonHandlerBeforeRP = true;
                  if (task.CheckRefreshSubTasks())
                     dv.computeSubForms();
                  task.InCommonHandlerBeforeRP = false;
                  task.setLevel(Constants.TASK_LEVEL_RECORD);
                  //enable-disable actions for a record
                  task.enableRecordActions();
                  break;

               case InternalInterface.MG_ACT_REC_SUFFIX:
                  // Force record suffix:
                  bool forceSuffix = false;
                  bool isSelectionTable;

                  task.ConfirmUpdateNo = false;

                  // No record -> no suffix
                  if (rec == null)
                     return false;

                  forceSuffix = task.checkProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, false);

                  // do not execute the "before record suffix" when deleting the record and
                  // the task mode is delete (the second round)
                  if ((rec.Modified || forceSuffix) && rec.getMode() == DataModificationTypes.Delete &&
                      task.getMode() == Constants.TASK_MODE_DELETE)
                     return true;

                  // to prevent double suffix on the same record
                  if (task.getLevel() == Constants.TASK_LEVEL_TASK)
                     return false;

                  if (task.getLevel() == Constants.TASK_LEVEL_CONTROL)
                  {
                     task.setInRecordSuffix(true);
                     task.setDirection(Direction.FORE);
                     handleInternalEvent(task.getLastParkedCtrl(), InternalInterface.MG_ACT_CTRL_SUFFIX);
                     task.setDirection(Direction.NONE);
                     task.setInRecordSuffix(false);
                     if (_stopExecution)
                        return false;
                  }

                  task.handleEventOnSlaveTasks(InternalInterface.MG_ACT_REC_SUFFIX);
                  if (_stopExecution)
                     return false;
                  Logger.Instance.WriteDevToLog("RECORD SUFFIX: " + task.queryTaskPath());

                  // before getting out of the record we have to execute the control validation
                  // handlers from the current control (+1) up to the last control in the record.
                  // CV of the currentCtrl is executed from ControlSuffix
                  if ((rec.Modified || forceSuffix) && ctrl != null)
                  {
                     MgControl frmCtrl = ctrl;

                     // if lastFocussedCtrl and ctrl are on diff forms, we need to execute
                     // CVs from the subformCtrl (containing the ctrl but on task) till last ctrl.
                     if (lastFocusedCtrl != null && frmCtrl.onDiffForm(lastFocusedCtrl))
                     {
                        MgControl firstCtrl = ((MgForm)task.getForm()).getFirstNonSubformCtrl();

                        // if task does not contain 'lastFocusedCtrl', then this task does not exists in nested subforms 
                        // (with lastFocusedCtrl) and is due to Run Task in Parent Screen. In such a case, execute CVs
                        // from first ctrl, if tab order of this task is higher than 'lastFocusedCtrl'.
                        if (firstCtrl != null && ((MgForm)task.getForm()).ctrlTabOrderIdx(lastFocusedCtrl) == -1
                            && MgControl.CompareTabOrder(firstCtrl, lastFocusedCtrl) > 0)
                           frmCtrl = null; // execute CV from first ctrl on the form
                        else
                           frmCtrl = lastFocusedCtrl;
                     }

                     executeVerifyHandlersTillLastCtrl(task, frmCtrl);
                     if (_stopExecution)
                        return false;
                  }

                  // indicate next verifyCtrl is the first ctrl in the record
                  task.setCurrVerifyCtrl(null);

                  task.setLevel(Constants.TASK_LEVEL_TASK);

                  isSelectionTable = task.checkProp(PropInterface.PROP_TYPE_SELECTION, false);

                  // Query mode -> do suffix only if we force it or somehow the record was updated
                  if (task.getMode() == Constants.TASK_MODE_QUERY && !forceSuffix && !rec.Modified &&
                      !isSelectionTable)
                     return false;

                  if (!rec.Modified && !forceSuffix && (!isSelectionTable || !task.InSelect))
                  {
                     if (rec.getMode() == DataModificationTypes.Delete)
                        break;
                     return false;
                  }

                  if (task.getMode() != Constants.TASK_MODE_DELETE && rec.Modified)
                  {
                     if (!updateConfirmed(task))
                        return false;
                  }
                  break;

               case InternalInterface.MG_ACT_CTRL_PREFIX:
                  if (handleCtrlPrefix(evt))
                     return true;
                  else
                  {
                     // did not succeed in entering the ctrl 
                     evt.Control.InControl = false;
                     return false;
                  }

               case InternalInterface.MG_ACT_CANCEL:
                  Manager.CleanMessagePane(task); // QCR #371321
                  break;

               case InternalInterface.MG_ACT_CTRL_SUFFIX:

                  // if we have a stopexecution ctrl we should not execute CS.
                  if (ctrl == null || getStopExecutionCtrl() != null)
                     return false;

                  // to prevent double suffix on the same control
                  if (task.getLevel() != Constants.TASK_LEVEL_CONTROL)
                     return false;

                  //#997666.For DotNet control we should not get the Value as it is irrelevant for them.
                  // QCR #278140. For a subform control we should not get the value as it is irrelevant for it.
                  // We are using the value only to validate & set into the control, but for a DotNet control
                  // we should not validate & set value, as there is nothing to validate(i.e. we don't have a picture for it).
                  if (!ctrl.IsDotNetControl() && !ctrl.isSubform())
                  {
                     val = Manager.GetCtrlVal(ctrl);

                     //We are resetting the flag as we are moving from one control to other control.
                     task.CurrentEditingControl = null;

                     // validate the controls value
                     //Fixed bug #:758788, the browser control will not be update from the filed.
                     // Patch for QCR #729706:
                     // Magic thread puts the following GUI commands:
                     //    PROP_SET_IMAGE_FILE_NAME to set the image on the Image control.
                     //    PROP_SET_TEXT to set the text of the TextBox control.
                     //    SET_FOCUS to focus on the TextBox control.
                     // Now, when the execution is by F7, if the file is not retrieved from the server 
                     // (i.e. if there is 404 error), HttpClient re-tries to get the file for HTTPtimeout. 
                     // During this time all the GUI events in GuiCommandsQueue remains pending unhandled.
                     // In the mean time, Magic thread executes the Exit event and during this, it tries 
                     // to execute the Ctrl Suffix of the TextBox control.
                     // Since SET_FOCUS is still pending in the queue, the logical control (if allowTesting=No) 
                     // is not mapped with any actual windows control.
                     // Now when we try to get the value of this logical control, LogicalControl.getEditorControl() 
                     // returns null and so, StaticText.getValue() returns LogicalControl.Text instead of Control.Text
                     // Because PROP_SET_TEXT is also pending, LogicalControl.Text is null. 
                     // And so there is an exception when using it.
                     // If AllowTesting=Yes, the val will be a blank string. So, there is no crash.
                     // But here we have another problem. Suppose, the field had an Init expression.
                     // Now, MgControl's value would be the inited value and the val is a blank string.
                     // So, we will have variable change.
                     // Since the problem is related to a very specific scenario -- when the image control was 
                     // the first dropped control on the form, image cannot be accessed at the time of task 
                     // loading and task needs to be closed, we are putting a patch here by checking val for NULL.
                     // The ideal solution would be --- instead of sending the image file name from Magic thread 
                     // to Gui thread and loading the image from the Gui thread, we can load the image from the 
                     // Magic thread (using sync execution i.e. via GuiInteractive) and send it to the Gui thread.
                     if (!task.cancelWasRaised() &&
                         (ctrl.Type != MgControlType.CTRL_TYPE_BROWSER && val != null &&
                          !ctrl.validateAndSetValue(val, true)))
                     {
                        setStopExecution(true);
                        return false;
                     }
                  }

                  // when stop execution in on, don't clear the msg from status.
                  if (!_stopExecution)
                     Manager.CleanMessagePane(task);

                  // reset InControl before executing control verification
                  ctrl.InControl = false;
                  ctrl.ClipBoardDataExists = false;

                  // handle control verification
                  // QCR 754616
                  bool forceRecSuffix = false;
                  forceRecSuffix = task.checkProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, false);
                  // if we have a verify handler, it must always be executed if not in EndTask. Otherwise,
                  // it should be executed if rec is modified or forceRecSuffix is yes (CVs of next ctrl till
                  // last ctrl is executed from RecordSuffix).
                  // We should not execute CVs if in a create mode (up-arrow pressed) and rec is not modified (#729814). But,
                  // we should execute CVs if we traverse the record using Tab/Click (#937816).
                  if (ctrl.HasVerifyHandler &&
                      ((!task.InEndTask &&
                        (task.getMode() != Constants.TASK_MODE_CREATE || !((MgForm)task.getForm()).IsMovingInView)) ||
                       rec.Modified || forceRecSuffix))
                  {
                     if (!executeVerifyHandler(ctrl))
                     {
                        // if control verification get 'stop execution' (e.g. verify error), the focus will be remain on current control.
                        // So set InControl to true
                        ctrl.InControl = true;
                        return false;
                     }
                  }

                  task.setCurrVerifyCtrl(ctrl);
                  ctrl.setInControlSuffix(true);
                  break;

#if PocketPC
               // for now, the windows mobile does not have the off-line feature, so we can assume it will
               // always use the remote commands processor
               case InternalInterface.MG_ACT_HIBERNATE_CTX:
                  RemoteCommandsProcessor.GetInstance().Hibernate();
                  break;

               case InternalInterface.MG_ACT_VERIFY_RESUMED_CTX:
                  RemoteCommandsProcessor.GetInstance().Resume();
                  List<MgFormBase> forms = MGDataCollection.Instance.GetTopMostForms();
                  Commands.addAsync(CommandType.RESUME_SHOW_FORMS, forms);
                  break;

               case InternalInterface.MG_ACT_CLICK:
                  task.locateQuery.InitServerReset = true;
                  break;
#endif

               case InternalInterface.MG_ACT_MOVE_TO_FIRST_CTRL:
                  form.moveToFirstCtrl(false, evt.IsFormFocus);
                  break;
            }
         }
         // end of internal events processing
         else if (task.locateQuery.Buffer.Length != 0)
            locateInQuery(task);

         return true;
      }

      /// <summary>
      ///   handle the MG_ACT_SELECTION
      /// </summary>
      /// <param name = "ctrl"> </param>
      /// <param name = "form"> </param>
      /// <param name = "evt"> </param>
      private void onActSelection(MgControl ctrl, MgForm form, RunTimeEvent evt)
      {
         int displayLine = evt.getDisplayLine();
         if (displayLine == Int32.MinValue)
            displayLine = form.DisplayLine;
         bool simulate = (ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX
                          || ctrl.Type == MgControlType.CTRL_TYPE_LIST
                          || ctrl.Type == MgControlType.CTRL_TYPE_RADIO
                          || ctrl.Type == MgControlType.CTRL_TYPE_BUTTON
                          || ctrl.Type == MgControlType.CTRL_TYPE_TAB && !evt.isProduceClick());
         if (simulate)
            simulateSelection(ctrl, evt.getValue(), displayLine, evt.isProduceClick());
         else
            handleSelection(ctrl, displayLine, evt.getValue());
      }

      /// <summary>
      ///   Handle the MG_ACT_DEFAULT_BUTTON
      /// </summary>
      /// <param name = "form">
      /// </param>
      /// <param name = "evt">
      /// </param>
      private void onActDefaultButton(MgForm form, RunTimeEvent evt)
      {
         MgControl ctrl = form.getDefaultButton(true);
         if (ctrl == null)
            return;
         onActSelection(ctrl, form, evt);
      }

      /// <summary>
      ///   move to an other line and focus on control
      /// </summary>
      /// <param name = "form"> </param>
      /// <param name = "displayLine">line to move </param>
      /// <param name = "ctrl">control to focus </param>
      private void moveToLine(MgForm form, int displayLine, MgControl ctrl)
      {
         var rtEvt = new RunTimeEvent(ctrl, displayLine);
         rtEvt.setInternal(InternalInterface.MG_ACT_REC_PREFIX);
         handleEvent(rtEvt, false);
         if (ctrl != null)
            ctrl.invoke();
         //needed to reselect previous row if we failed to move
         form.SelectRow(true);
      }

      /// <summary>
      ///   calculate arguments for events
      /// </summary>
      /// <param name = "evt"> </param>
      private void createEventArguments(RunTimeEvent evt)
      {
         if (evt.getArgList() == null)
         {
            if (evt.getType() == ConstInterface.EVENT_TYPE_INTERNAL)
            {
               switch (evt.getInternalCode())
               {
                  case InternalInterface.MG_ACT_TREE_EXPAND:
                  case InternalInterface.MG_ACT_TREE_COLLAPSE:
                     Task task = evt.getTask();
                     var form = (MgForm)task.getForm();
                     //we do not have tree
                     if (form.getMgTree() == null)
                        break;
                     int line = evt.getDisplayLine();
                     var argsList = new ArgumentsList();
                     if (line == Int32.MinValue)
                        line = form.DisplayLine;
                     //expand and collapse get 2 arguments tree level and tree value
                     int treeLevel = form.getMgTree().getLevel(line);
                     String level = DisplayConvertor.Instance.disp2mg("" + treeLevel, null,
                                                                           new PIC("3", StorageAttribute.NUMERIC,
                                                                                   task.getCompIdx()),
                                                                           task.getCompIdx(),
                                                                           BlobType.CONTENT_TYPE_UNKNOWN);
                     String treeValue = ((MgControl)form.getTreeCtrl()).getTreeValue(line);
                     bool isNull = ((MgControl)(form.getTreeCtrl())).isTreeNull(line);
                     char nullChar = (isNull
                                         ? '1'
                                         : '0');
                     var vals = new[] { level, treeValue };
                     argsList.buildListFromParams(2,
                                                  "" + (char)StorageAttribute.NUMERIC + (char)((MgControl)form.getTreeCtrl()).getNodeIdField().getType(),
                                                  vals, "0" + nullChar);
                     evt.setArgList(argsList);

                     break;

                  default:
                     break;
               }
            }
            else if (evt.getType() == ConstInterface.EVENT_TYPE_DOTNET)
            {
               if (evt.DotNetArgs != null)
                  evt.setDotNetArgs(evt.DotNetArgs);
            }
         }
      }

      /// <summary>
      ///   remove arguments for events
      /// </summary>
      /// <param name = "evt"> </param>
      private void removeEventArguments(RunTimeEvent evt)
      {
         if (evt.getArgList() == null)
         {
            if (evt.getType() == ConstInterface.EVENT_TYPE_DOTNET)
            {
               if (evt.DotNetArgs != null)
                  evt.removeDotNetArgs();
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      /// <param name="doSuffix"></param>
      /// <returns></returns>
      internal bool DoTaskLevelRecordSuffix(Task task, ref bool doSuffix)
      {
         bool shouldReturn = false;
         if (task.getLevel() == Constants.TASK_LEVEL_RECORD)
         {
            doSuffix = false;
            handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
            if (GetStopExecutionFlag())
               shouldReturn = true;
         }

         return shouldReturn;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      /// <param name="form"></param>
      internal void DoTaskLevelRecordPrefix(Task task, MgForm form, bool moveInRow)
      {
         if (!GetStopExecutionFlag())
         {
            handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);

            if (moveInRow)
            {
               if (!GetStopExecutionFlag() && form != null)
                  form.moveInRow(null, Constants.MOVE_DIRECTION_BEGIN);
            }
         }
      }
      /// <summary>
      ///   do the magic default operations according to the internal event code
      /// </summary>
      /// <param name = "evt">the event to handle </param>
      /// <returns> tells the caller whether to continue handling the event </returns>
      private void commonHandler(RunTimeEvent evt)
      {
         Task task = evt.getTask();
         MGData mgData = task.getMGData();
         var form = (MgForm)task.getForm();
         MgControl ctrl = evt.Control;
         MgControl lastParkedCtrl;
         DataView dv;
         Record rec;
         int intEvtCode;
         bool doSuffix = true;
         char oldMode;
         CommandsProcessorBase server = task.CommandsProcessor;

         if (_stopExecution)
            return;

         // if this is a user event that converts to a system or internal even, convert it
         if (evt.getType() == ConstInterface.EVENT_TYPE_USER)
            evt = new RunTimeEvent(evt.getUserEvent(), evt);

         try
         {
            pushRtEvent(evt);

            if (evt.getType() == ConstInterface.EVENT_TYPE_SYSTEM)
            {
               int actId = 0;

               actId = getMatchingAction(task, evt.getKbdItm(), true);
               if (actId != 0)
               // handled internally
               {
                  evt = new RunTimeEvent(evt, evt);
                  evt.setInternal(actId);
                  evt.setArgList(null);
               }
            } // end of system events processing

            // execute the common handling of internal events only if they are not immediate.
            // an event is immediate when it was raised by a raise event operation that its
            // wait condition is yes or evaluates to true.
            IClientCommand cmd;
            if (evt.getType() == ConstInterface.EVENT_TYPE_INTERNAL && !evt.isImmediate() &&
                ActAllowed(evt.getInternalCode()) && !_processingTopMostEndTask)
            {
               CommandsTable cmdsToServer = mgData.CmdsToServer;
               dv = (DataView)task.DataView;
               rec = (Record)dv.getCurrRec();

               intEvtCode = evt.getInternalCode();

               // for internal events
               switch (intEvtCode)
               {
                  case InternalInterface.MG_ACT_UPDATE_DN_CONTROL_VALUE:
                     //Get the control and thereafter field.
                     MgControl mgCtrl = (MgControl)evt.Control;
                     Field ctrlFld = (Field)mgCtrl.getField();

                     //Update the field with dot net object value.
                     if (ctrlFld != null)
                     {
                        mgCtrl.IsInteractiveUpdate = true;
                        ctrlFld.UpdateWithDNObject(evt.DotNetArgs[0], false);
                     }
                     break;

                  case InternalInterface.MG_ACT_DEFAULT_BUTTON:
                     onActDefaultButton(form, evt);
                     break;

                  case InternalInterface.MG_ACT_BUTTON:
                  case InternalInterface.MG_ACT_CTRL_HIT:
                     // if button is not parkable, raise its event only if the action was not triggered by gui.
                     // direct click on the button will get here as ctrl hit , but is not triggered by gui (goes via selection event 1st)
                     // other ctrl_hit actions should consider the parkability  
                     if (ctrl.Type == MgControlType.CTRL_TYPE_BUTTON && 
                        (intEvtCode == InternalInterface.MG_ACT_BUTTON || !evt.isGuiTriggeredEvent() || ctrl.isParkable(true,false)))
                     {
                        var aRtEvt = new RunTimeEvent(ctrl);
                        if (ctrl.getRtEvtTask() != null)
                           aRtEvt.setTask(ctrl.getRtEvtTask());
                        if (ConstInterface.EVENT_TYPE_NONE != aRtEvt.getType())
                        {
                           if (ctrl.useZoomHandler())
                           {
                              Task taskToEnable = aRtEvt.getTask();
                              if (taskToEnable != null)
                                 taskToEnable.ActionManager.enable(InternalInterface.MG_ACT_ZOOM, true);
                           }
                           aRtEvt.setArgList(ctrl.ArgList);
                           aRtEvt.setPublicName();
                           addToTail(aRtEvt);
                        }
                     }
                     else if (ctrl.Type == MgControlType.CTRL_TYPE_TAB)
                     {
                        //Fixed bug #724150: as we doing for online for tab control we send ACT_ZOOM (Choiceinp.cpp ::DitGet())
                        addInternalEvent(ctrl, InternalInterface.MG_ACT_ZOOM);
                     }
                     else if (ctrl.Type == MgControlType.CTRL_TYPE_RADIO)
                     {
                        ctrl.setControlToFocus();
                     }

                     break;
#if !PocketPC
                  case InternalInterface.MG_ACT_CONTEXT_MENU:
                     if (evt.getArgList() != null)
                     {
                        String left = evt.getArgList().getArgValue(0, StorageAttribute.ALPHA, 0);
                        String top = evt.getArgList().getArgValue(1, StorageAttribute.ALPHA, 0);
                        String line = evt.getArgList().getArgValue(2, StorageAttribute.ALPHA, 0);
                        Commands.ShowContextMenu(ctrl, task.getForm(), Int32.Parse(left), Int32.Parse(top), Int32.Parse(line));
                     }
                     break;

                  case InternalInterface.MG_ACT_OPEN_FORM_DESIGNER:
                     {
                        bool orgValue = ClientManager.Instance.EventsManager.AllowFormsLock;

                        // get the parameter value
                        bool adminMode = true;
                        if ((evt.getArgList() != null) && (evt.getArgList().getSize() == 1))
                        {
                           string adminModeStr = evt.getArgList().getArgValue(0, StorageAttribute.BOOLEAN, 0);
                           adminMode = adminModeStr == null || adminModeStr.Equals("1");                    
                        }
                        ClientManager.Instance.EventsManager.AllowFormsLock = false;
                        Commands.OpenFormDesigner(task.getForm().getTopMostForm(), adminMode);
                        ClientManager.Instance.EventsManager.AllowFormsLock = orgValue;
                     }
                     break;
#endif

                  case InternalInterface.MG_ACT_HIT:
                     // MG_ACT_HIT like in online, is being set when clicking on form or on the subform area.
                     // in case CloseTasksOnParentActivate is on, try and close all child tasks.
                     // we send the task of the top most form since our focus might be inside a subform, and that is equal to clicking on its form.
                     if (ClientManager.Instance.getEnvironment().CloseTasksOnParentActivate())
                        CloseAllChildProgs((Task)task.getTopMostForm().getTask(), null, false);
                     break;

                  case InternalInterface.MG_ACT_TOGGLE_INSERT:
                     {
                        ClientManager.Instance.RuntimeCtx.ToggleInsertMode();
                        MgForm mgForm = (MgForm)task.getTopMostForm();
                        if (mgForm != null)
                           mgForm.UpdateStatusBar(GuiConstants.SB_INSMODE_PANE_LAYER, mgForm.GetMessgaeForStatusBar(GuiConstants.SB_INSMODE_PANE_LAYER,
                                                  ClientManager.Instance.RuntimeCtx.IsInsertMode()), false);
                     }
                     break;

                  case InternalInterface.MG_ACT_CHAR:
                  case InternalInterface.MG_ACT_EDT_NXTCHAR:
                  case InternalInterface.MG_ACT_EDT_PRVCHAR:
                  case InternalInterface.MG_ACT_EDT_DELCURCH:
                  case InternalInterface.MG_ACT_EDT_DELPRVCH:
                  case InternalInterface.MG_ACT_CLIP_COPY:
                  case InternalInterface.MG_ACT_CUT:
                  case InternalInterface.MG_ACT_CLIP_PASTE:
                  case InternalInterface.MG_ACT_EDT_PRVLINE:
                  case InternalInterface.MG_ACT_EDT_NXTLINE:
                  case InternalInterface.MG_ACT_EDT_BEGNXTLINE:
                  case InternalInterface.MG_ACT_EDT_BEGFLD:
                  case InternalInterface.MG_ACT_EDT_ENDFLD:
                  case InternalInterface.MG_ACT_EDT_MARKALL:
                  case InternalInterface.MG_ACT_EDT_BEGFORM:
                  case InternalInterface.MG_ACT_EDT_ENDFORM:
                  case InternalInterface.MG_ACT_EDT_PRVPAGE:
                  case InternalInterface.MG_ACT_EDT_NXTPAGE:
                  case InternalInterface.MG_ACT_BEGIN_DROP:
                  case InternalInterface.MG_ACT_EDT_MARKPRVCH:
                  case InternalInterface.MG_ACT_EDT_MARKNXTCH:
                  case InternalInterface.MG_ACT_EDT_MARKTOBEG:
                  case InternalInterface.MG_ACT_EDT_MARKTOEND:
                  case InternalInterface.MG_ACT_EDT_NXTWORD:
                  case InternalInterface.MG_ACT_EDT_PRVWORD:
                  case InternalInterface.MG_ACT_EDT_BEGLINE:
                  case InternalInterface.MG_ACT_EDT_ENDLINE:

#if PocketPC
                     case InternalInterface.MG_ACT_MASK_CONTROL_TEXT:
#endif
                     if (form != null && task.getLastParkedCtrl() != null)
                     {
                        // Incremental search
                        if (intEvtCode == InternalInterface.MG_ACT_CHAR ||
                            intEvtCode == InternalInterface.MG_ACT_EDT_DELPRVCH)
                        {
                           if ((task.getMode() == Constants.TASK_MODE_QUERY) && !ctrl.isModifiable() &&
                               task.checkProp(PropInterface.PROP_TYPE_ALLOW_LOCATE_IN_QUERY, false) &&
                               (ctrl.DataType == StorageAttribute.ALPHA ||
                                ctrl.DataType == StorageAttribute.UNICODE ||
                                ctrl.DataType == StorageAttribute.NUMERIC) &&
                               evt.getImeParam() == null)
                           {
                              addLocateCharacter(evt);
                              break;
                           }
                        }

                        // In case of the ACT_BEGIN_DROP : Drop occurs on the current control, 
                        // hence use current control instead of last parked control.
                        MgControlBase mgControl = (intEvtCode == InternalInterface.MG_ACT_BEGIN_DROP) ? ctrl : task.getLastParkedCtrl();
                        TextMaskEditor.EditReturnCode ret = Manager.TextMaskEditor.ProcessAction(mgControl, intEvtCode,
                                                                                   evt.getValue(), evt.getStartSelection(), evt.getEndSelection(), evt.getImeParam());
                        handleCtrlModify(ctrl, intEvtCode);
                        if (ret == TextMaskEditor.EditReturnCode.EDIT_AUTOSKIP)
                           handleInternalEvent(task, InternalInterface.MG_ACT_TBL_NXTFLD);
                        else if (ret == TextMaskEditor.EditReturnCode.EDIT_RESTART && ctrl != null)
                           ctrl.resetDisplayToPrevVal();
                        //EDIT_CONTINUE:
                        else
                        {
                        }
                     }

                     if (ctrl != null)
                        ctrl.AutoWideModeCheck(intEvtCode);
                     break;

                  case InternalInterface.MG_ACT_CLOSE:
                     var rtEvt = new RunTimeEvent(evt, evt);
                     rtEvt.setInternal(InternalInterface.MG_ACT_EXIT);
                     addToTail(rtEvt);
                     break;

                  case InternalInterface.MG_ACT_EXIT:
                     // if 'exit' accepted on MDI, exit the top most task. same as when exiting the system.
                     Task taskToEnd = (task.isMainProg() ? MGDataCollection.Instance.StartupMgData.getFirstTask() : task);
                     taskToEnd.endTask(evt.reversibleExit(), false, evt.ExitDueToError());

                     break;

                  case InternalInterface.MG_ACT_EXIT_SYSTEM:
                     // end the top most task with form.
                     MGDataCollection.Instance.StartupMgData.getFirstTask().endTask(evt.reversibleExit(), false,
                                                                                    false);
                     //GetTopMostTaskWithForm(task).endTask(evt.reversibleExit(), false, false);
#if PocketPC
   // Topic #13 (MAGIC version 1.8\SP1 for WIN) RC mobile - improve performance: spec, section 4.3.1
                     ClientManager.Instance.setRespawnOnExit(false);
#endif
                     break;

                  case InternalInterface.MG_ACT_TBL_NXTLINE:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_ROW, Constants.MOVE_DIRECTION_NEXT);
                     break;

                  case InternalInterface.MG_ACT_TBL_PRVLINE:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_ROW, Constants.MOVE_DIRECTION_PREV);
                     break;

                  case InternalInterface.MG_ACT_TBL_NXTPAGE:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_PAGE, Constants.MOVE_DIRECTION_NEXT);
                     break;

                  case InternalInterface.MG_ACT_TBL_PRVPAGE:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_PAGE, Constants.MOVE_DIRECTION_PREV);
                     break;

                  case InternalInterface.MG_ACT_TBL_BEGPAGE:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_PAGE, Constants.MOVE_DIRECTION_BEGIN);
                     break;

                  case InternalInterface.MG_ACT_TBL_ENDPAGE:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_PAGE, Constants.MOVE_DIRECTION_END);
                     break;

                  case InternalInterface.MG_ACT_TBL_BEGTBL:
                     // #918588: If ctrl+home/ctrl+end pressed when parked on listbox
                     // then Begin table event should not be handled.
                     lastParkedCtrl = (MgControl)task.getLastParkedCtrl();
                     if (lastParkedCtrl == null || !lastParkedCtrl.isListBox())
                     {
                        if (form != null)
                           form.moveInView(Constants.MOVE_UNIT_TABLE, Constants.MOVE_DIRECTION_BEGIN);
                     }
                     break;

                  case InternalInterface.MG_ACT_TBL_ENDTBL:
                     // #918588: If ctrl+home/ctrl+end pressed when parked on listbox
                     // then Begin table event should not be handled.
                     lastParkedCtrl = (MgControl)task.getLastParkedCtrl();
                     if (lastParkedCtrl == null || !lastParkedCtrl.isListBox())
                     {
                        if (form != null)
                           form.moveInView(Constants.MOVE_UNIT_TABLE, Constants.MOVE_DIRECTION_END);
                     }
                     break;

                  case InternalInterface.MG_ACT_TBL_BEGLINE:
                     //Fixed bug #:178230, 
                     if (form != null)
                     {
                        Flow orgFlow = task.getFlowMode();
                        Direction orgDir = task.getDirection();
                        if (!_isSorting)
                        {
                           task.setFlowMode(Flow.FAST);
                           task.setDirection(Direction.BACK);
                        }
 
                        form.moveInRow(null, Constants.MOVE_DIRECTION_BEGIN);
                        if (!_isSorting)
                        {
                           task.setFlowMode(orgFlow);
                           task.setDirection(orgDir);
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_TBL_ENDLINE:
                     if (form != null)
                     {
                        Flow orgFlow = task.getFlowMode();
                        Direction orgDir = task.getDirection();
                        if (!_isSorting)
                        {
                           task.setFlowMode(Flow.FAST);
                           task.setDirection(Direction.FORE);
                        }

                        form.moveInRow(null, Constants.MOVE_DIRECTION_END);
                        if (!_isSorting)
                        {
                           task.setFlowMode(orgFlow);
                           task.setDirection(orgDir);
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_TREE_MOVETO_FIRSTCHILD:
                     if (form != null)
                     {
                        //before going to first son make sure the node is expanded
                        doExpandCollapse(task, true, form.DisplayLine);
                        form.moveInView(Constants.MOVE_UNIT_TREE_NODE, Constants.MOVE_DIRECTION_FIRST_SON);
                     }
                     break;

                  case InternalInterface.MG_ACT_TREE_MOVETO_NEXTSIBLING:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_TREE_NODE, Constants.MOVE_DIRECTION_NEXT_SIBLING);
                     break;

                  case InternalInterface.MG_ACT_TREE_MOVETO_PREVSIBLING:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_TREE_NODE, Constants.MOVE_DIRECTION_PREV_SIBLING);
                     break;

                  case InternalInterface.MG_ACT_TREE_MOVETO_PARENT:
                     if (form != null)
                        form.moveInView(Constants.MOVE_UNIT_TREE_NODE, Constants.MOVE_DIRECTION_PARENT);
                     break;

                  case InternalInterface.MG_ACT_TBL_NXTFLD:
                     //         case MG_ACT_NEXT_TAB:
                     if (form != null)
                     {
                        ClientManager.Instance.setMoveByTab(true);
                        task.setFlowMode(Flow.STEP);
                        task.setDirection(Direction.FORE);
                        form.moveInRow(null, Constants.MOVE_DIRECTION_NEXT);
                        task.setFlowMode(Flow.NONE);
                        task.setDirection(Direction.NONE);
                        ClientManager.Instance.setMoveByTab(false);
                     }
                     break;

                  case InternalInterface.MG_ACT_TBL_PRVFLD:
                     //         case MG_ACT_PREVIOUS_TAB:
                     if (form != null)
                     {
                        ClientManager.Instance.setMoveByTab(true);
                        task.setFlowMode(Flow.STEP);
                        task.setDirection(Direction.BACK);
                        form.moveInRow(null, Constants.MOVE_DIRECTION_PREV);
                        task.setFlowMode(Flow.NONE);
                        task.setDirection(Direction.NONE);
                        ClientManager.Instance.setMoveByTab(false);
                     }
                     break;

                  case InternalInterface.MG_ACT_TAB_NEXT:
                     if (form != null)
                        form.changeTabSelection(ctrl, Constants.MOVE_DIRECTION_NEXT);
                     break;

                  case InternalInterface.MG_ACT_TAB_PREV:
                     if (form != null)
                        form.changeTabSelection(ctrl, Constants.MOVE_DIRECTION_PREV);
                     break;

                  case InternalInterface.MG_ACT_RT_COPYFLD:
                     if (form != null)
                     {
                        form.ditto();

                        // in case we have a table on the form, we need to activate REFRESH_TABLE so that the text
                        // on the control will be set to the new value. otherwise when we get to MG_ACT_TBL_NXTFLD
                        // handling, the value on the control is still the previous value
                        var table = (MgControl)form.getTableCtrl();
                        if (table != null)
                           Commands.addAsync(CommandType.REFRESH_TABLE, table, 0, false);
                     }
                     handleInternalEvent(task, InternalInterface.MG_ACT_TBL_NXTFLD);
                     break;

                  case InternalInterface.MG_ACT_WEB_MOUSE_OVER:
                     break;

                  case InternalInterface.MG_ACT_WEB_ON_DBLICK:
                     {
                        // Unlike Online, here SELECT or ZOOM comes after the DBCLICK. 
                        // If there is a DBCLICK handler with propogate = 'NO' , these actions will not be raised.
                        MgControlType ctrlType = ctrl.Type;
                        int putAction = InternalInterface.MG_ACT_NONE;
                        if (ctrlType != MgControlType.CTRL_TYPE_TABLE && ctrlType != MgControlType.CTRL_TYPE_LABEL &&
                            ctrlType != MgControlType.CTRL_TYPE_IMAGE && ctrlType != MgControlType.CTRL_TYPE_TREE)
                        {
                           if (ctrl.getForm().getTask().ActionManager.isEnabled(InternalInterface.MG_ACT_SELECT))
                              putAction = InternalInterface.MG_ACT_SELECT;
                           else
                           {
                              if (ctrl.getForm().getTask().ActionManager.isEnabled(InternalInterface.MG_ACT_ZOOM))
                                 putAction = InternalInterface.MG_ACT_ZOOM;
                           }

                           if (putAction != InternalInterface.MG_ACT_NONE)
                              addInternalEvent(ctrl, putAction);
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_HELP:
                     String helpIdx = (ctrl != null
                                          ? getHelpUrl(ctrl)
                                          : form.getHelpUrl());

                     if (helpIdx != null)
                     {
                        WindowType parentWindowType = form.getTopMostForm().ConcreteWindowType;
                        bool openAsApplicationModal = false;
                        // if the parent of the task is Modal\floating\tool, then his child will be also the same window type.
                        if (parentWindowType == WindowType.Modal)
                           openAsApplicationModal = true;

                        //Get the help object.
                        MagicHelp hlpObject = task.getHelpItem(Int32.Parse(helpIdx));

                        //Check the help type and call the respective function.
                        switch (hlpObject.GetHelpType())
                        {
                           case HelpType.Internal:
                              //Internal help not implemented for RC.
                              break;

                           case HelpType.Windows:
                              //Windows help not implemented for RC.
                              break;
                           case HelpType.URL:
                              String helpUrl = ((URLHelp)hlpObject).urlHelpText; ;
                              helpUrl = ClientManager.Instance.getEnvParamsTable().translate(helpUrl).ToString();
                              GUIManager.Instance.showContentFromURL(helpUrl, openAsApplicationModal);
                              break;
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_DELLINE:
                     if (HandleActionDelline(evt, task, dv, rec, false))
                        return;
                     break;

                  case InternalInterface.MG_ACT_TREE_CRE_SON:
                     if (form == null)
                        break;
                     if (form.getMgTree() == null)
                        break;
                     //fallthrou
                     goto case InternalInterface.MG_ACT_CRELINE;

                  case InternalInterface.MG_ACT_CRELINE:
                     if (task.DataView.isEmptyDataview())
                     {
                        if (form.getMgTree() != null)
                           exitWithError(task, MsgInterface.RT_STR_NO_CREATE_ON_TREE);
                        else
                           gotoCreateMode(task, cmdsToServer);

                        break;
                     }
                     if (task.getInCreateLine())
                        break;

                     task.setInCreateLine(true);

                     try
                     {
                        if (task.getLevel() == Constants.TASK_LEVEL_CONTROL)
                        {
                           task.setDirection(Direction.FORE);
                           handleInternalEvent(task.getLastParkedCtrl(), InternalInterface.MG_ACT_CTRL_SUFFIX);
                           task.setDirection(Direction.NONE);
                           if (GetStopExecutionFlag())
                              return;
                        }

                        // verify that the current record is not already a new one
                        if (task.getMode() == Constants.TASK_MODE_CREATE && rec != null && !rec.Modified)
                        {
                           if (task.checkProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, false))
                           {
                              doSuffix = false;
                              handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                              if (GetStopExecutionFlag() || task.isAborting())
                                 return;
                           }
                           else
                           {
                              task.setLevel(Constants.TASK_LEVEL_TASK);
                              handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);

                              if (!GetStopExecutionFlag() && form != null)
                              {
                                 // set curr VerifyCtrl to null so that CV execution begins frm first ctrl
                                 task.setCurrVerifyCtrl(null);
                                 form.moveInRow(null, Constants.MOVE_DIRECTION_BEGIN);
                              }
                              return;
                           }
                        }

                        if (DoTaskLevelRecordSuffix(task, ref doSuffix))
                           return;

                        if (((MgForm)task.getForm()).getFirstParkableCtrl(false) != null || !task.IsInteractive)
                        {
                           if (form != null)
                           {
                              int parentId = 0;
                              int prevLine = 0;
                              if (form.getMgTree() != null)
                              {
                                 if (intEvtCode == InternalInterface.MG_ACT_TREE_CRE_SON)
                                 {
                                    parentId = form.DisplayLine;
                                    prevLine = 0; //create son creates new node as first sone
                                 }
                                 else
                                 {
                                    parentId = form.getMgTree().getParent(form.DisplayLine);
                                    //create line creates new node after current line
                                    prevLine = form.DisplayLine;
                                 }
                              }

                              if ((task.getMode() != Constants.TASK_MODE_CREATE) || !task.ConfirmUpdateNo)
                                 form.addRec(doSuffix, parentId, prevLine);
                           }

                           DoTaskLevelRecordPrefix(task, form, true);

                        }

                        if (task.getLastParkedCtrl() == null && task.IsInteractive)
                           exitWithError(task, MsgInterface.RT_STR_CRSR_CANT_PARK);
                     }
                     finally
                     {
                        task.setInCreateLine(false);
                     }
                     break;

                  case InternalInterface.MG_ACT_CANCEL:
                     HandleActCancel(evt, task, form, ctrl, rec, true);
                     break;

                  case InternalInterface.MG_ACT_RT_QUIT:
                     handleInternalEvent(task, InternalInterface.MG_ACT_RT_QUIT);
                     break;

                  case InternalInterface.MG_ACT_RTO_CREATE:
                     if (task.getMode() == Constants.TASK_MODE_CREATE ||
                         task.getProp(PropInterface.PROP_TYPE_ALLOW_OPTION).getValueBoolean())
                        gotoCreateMode(task, cmdsToServer);
                     break;

                  case InternalInterface.MG_ACT_RTO_QUERY:
                     if (task.getMode() != Constants.TASK_MODE_QUERY &&
                         task.getProp(PropInterface.PROP_TYPE_ALLOW_OPTION).getValueBoolean() == false)
                        break;
                     if (task.HasMDIFrame)
                        break;
                     task.enableModes();
                     oldMode = task.getOriginalTaskMode();
                     handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                     if (!GetStopExecutionFlag())
                     {
                        task.setMode(Constants.TASK_MODE_QUERY);
                        if (oldMode != Constants.TASK_MODE_CREATE)
                        {
                           dv.currRecCompute(true);
                           handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                           if (ctrl != null)
                              ctrl.invoke();
                           form.RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                        }
                        else
                        {
                           // we already executed record suffix - so we need to prevent the view refresh
                           // from executing it again
                           task.setPreventRecordSuffix(true);
                           task.setPreventControlChange(true);
                           handleInternalEvent(task, InternalInterface.MG_ACT_RT_REFRESH_VIEW);
                           if (ctrl != null)
                              ctrl.invoke();
                           task.setPreventRecordSuffix(false);
                           task.setPreventControlChange(false);
                           if (!dv.isEmpty() && ((Record)dv.getCurrRec()).getMode() != DataModificationTypes.Insert)
                              task.setMode(Constants.TASK_MODE_QUERY);
                           else
                              task.setMode(Constants.TASK_MODE_CREATE);
                           form.RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                        }
                        task.setOriginalTaskMode(Constants.TASK_MODE_QUERY);
                        task.setCreateDeleteActsEnableState();
                     }
                     break;

                  case InternalInterface.MG_ACT_RTO_MODIFY:
                     if (task.getMode() != Constants.TASK_MODE_MODIFY &&
                         task.getProp(PropInterface.PROP_TYPE_ALLOW_OPTION).getValueBoolean() == false)
                        break;
                     task.enableModes();
                     oldMode = task.getOriginalTaskMode();
                     handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                     if (!GetStopExecutionFlag())
                     {
                        task.setMode(Constants.TASK_MODE_MODIFY);

                        if (oldMode != Constants.TASK_MODE_CREATE)
                        {
                           dv.currRecCompute(true);
                           handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                           if (ctrl != null)
                              ctrl.invoke();
                        }
                        else
                        {
                           // we already executed record suffix - so we need to prevent the view refresh
                           // from executing it again
                           task.setPreventRecordSuffix(true);
                           task.setPreventControlChange(true);
                           handleInternalEvent(task, InternalInterface.MG_ACT_RT_REFRESH_VIEW);
                           if (ctrl != null)
                              ctrl.invoke();
                           task.setPreventRecordSuffix(false);
                           task.setPreventControlChange(false);
                           if (!dv.isEmpty() && ((Record)dv.getCurrRec()).getMode() != DataModificationTypes.Insert)
                              task.setMode(Constants.TASK_MODE_MODIFY);
                           else
                              task.setMode(Constants.TASK_MODE_CREATE);
                           form.RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                        }
                        task.setOriginalTaskMode(Constants.TASK_MODE_MODIFY);
                        task.setCreateDeleteActsEnableState();
                     }
                     break;

                  case InternalInterface.MG_ACT_SELECT:
                     /* preform rec suffix */
                     try
                     {
                        task.InSelect = true;
                        handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                     }
                     finally
                     {
                        task.InSelect = false;
                     }
                     if (!_stopExecution)
                     {
                        /* close the selection program */
                        handleInternalEvent(task, InternalInterface.MG_ACT_EXIT);
                     }
                     break;

                  case InternalInterface.MG_ACT_ZOOM:
                     selectProg(ctrl, false);
                     break;

                  case InternalInterface.MG_ACT_WIDE:
                     //fixed bug #:779224 , the action can be send by push button so the the wide MUST be on the lastFocused control.               
                     int winId = task.getMGData().GetId();
                     ctrl = GUIManager.getLastFocusedControl(winId);
                     if (ctrl != null)
                        ctrl.onWide(false);
                     break;

                  case InternalInterface.MG_ACT_SUBFORM_REFRESH:
                     if ((evt.getArgList() != null) && (evt.getArgList().getSize() == 1))
                     {
                        MgControl subformCtrl = null;
                        string RefreshedSubformName = evt.getArgList().getArgValue(0, StorageAttribute.ALPHA, 0);

                        // QCR #283114. Argument of 'Subform refresh event' can be skipped or not exist.
                        if (RefreshedSubformName == null)
                           break;

                        // RS must be executed only if the current subform is refreshed.
                        // If its brother is refreshed we do not need to execute RS of the current task.
                        for (Task parentTask = task; parentTask.getForm() != null && parentTask.getMgdID() == task.getMgdID(); parentTask = parentTask.getParent())
                        {
                           subformCtrl = (MgControl)parentTask.getForm().getCtrlByName(RefreshedSubformName, MgControlType.CTRL_TYPE_SUBFORM);
                           if (subformCtrl != null)
                           {
                              Task subformTask = subformCtrl.getSubformTask();
                              // If the subform task was not opened yet, but subform refresh is done,
                              // it's needed to open the subform. QCR #415914.
                              if (subformTask == null)
                              {
                                 // QCR #287562. Open the subform task only if the subform control is connected to the task.
                                 if ((SubformType)subformCtrl.getProp(PropInterface.PROP_TYPE_SUBFORM_TYPE).getValueInt() != SubformType.None)
                                    OpenSubform(parentTask, subformCtrl);
                              }
                              else
                                 parentTask.SubformRefresh(subformTask, true);
                              break;
                           }
                        }
                     }
                     // if it is SubformRefresh that returned from the server, so search the task according to its task id (tag)
                     else if ((evt.getArgList() != null) && (evt.getArgList().getSize() == 2))
                     {
                        Task subformTask = task, parentTask;
                        String taskTag = evt.getArgList().getArgValue(1, StorageAttribute.NUMERIC, 0);

                        while (subformTask.IsSubForm)
                        {
                           parentTask = (Task)subformTask.getParent();
                           if (subformTask.getTaskTag() == taskTag)
                           {
                              parentTask.SubformRefresh(subformTask, true);
                              break;
                           }
                           subformTask = parentTask;
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_SUBFORM_OPEN:
                     OpenSubform(task, evt.Control);
                     break;

                  case InternalInterface.MG_ACT_RTO_SEARCH:
                     handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                     if (!GetStopExecutionFlag())
                     {
                        cmd = CommandFactory.CreateEventCommand(task.getTaskTag(), intEvtCode);
                        task.getTaskCache().clearCache();
                        task.DataviewManager.Execute(cmd);

                        // After view refresh, the server always returns curr rec as first rec.
                        if (!form.isScreenMode())
                        {
                           dv.setTopRecIdx(0);
                           if (form.HasTable())
                           {
                              form.restoreOldDisplayLine(form.recordIdx2DisplayLine(dv.getCurrRecIdx()));
                              Commands.addAsync(CommandType.UPDATE_TMP_EDITOR_INDEX, form.getTableCtrl());
                           }
                        }

                        handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                        if (ctrl != null)
                           ctrl.invoke();
                     }
                     break;

                  case InternalInterface.MG_ACT_RT_REFRESH_SCREEN:
                  case InternalInterface.MG_ACT_RT_REFRESH_VIEW:
                  case InternalInterface.MG_ACT_RT_REFRESH_RECORD:
                     handleRefreshEvents(evt);
                     break;

                  case InternalInterface.MG_ACT_ACT_CLOSE_APPL:
                     cmd =
                        CommandFactory.CreateEventCommand(
                           MGDataCollection.Instance.StartupMgData.getFirstTask().getTaskTag(),
                           InternalInterface.MG_ACT_BROWSER_ESC);
                     cmdsToServer.Add(cmd);
                     server.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
                     break;

                  case InternalInterface.MG_ACT_SERVER_TERMINATION:
                     cmd = CommandFactory.CreateEventCommand(task.getTaskTag(), intEvtCode);
                     cmdsToServer.Add(cmd);
                     CommandsProcessorManager.GetCommandsProcessor(task).Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
                     break;

                  case InternalInterface.MG_ACT_RT_EDT_NULL:
                     if (_currField != null && _currField.NullAllowed)
                     {
                        // the next lines is an immitation of the behaviour of an online program:
                        // 1) the record must be updated in this event
                        // 2) a "next field" event is raised
                        _currField.setValueAndStartRecompute(_currField.getMagicDefaultValue(), true, true, true, false);
                        _currField.updateDisplay();

                        //We need to refresh the table before invoking MG_ACT_TBL_NXTFLD.
                        //Reason: The new (null) value is set on the logical control. MG_ACT_TBL_NXTFLD invokes CS of the current control.
                        //CS gets the value from the actual control, which still contains the old value. And, so, it updates the field 
                        //again with the old value.
                        var table = (MgControl)form.getTableCtrl();
                        if (table != null)
                           Commands.addAsync(CommandType.REFRESH_TABLE, table, 0, false);

                        handleInternalEvent(task, InternalInterface.MG_ACT_TBL_NXTFLD);
                     }
                     break;

                  case InternalInterface.MG_ACT_ROLLBACK:
                     // for offline we need to cancel the edit and then go rollback the transaction, the same we doing for remove while return from the server 
                     // see dataview.cs case ConstInterface.RECOVERY_ROLLBACK:
                     // for offline we will not get to ConstInterface.RECOVERY_ROLLBACK
                     task.TaskService.BeforeRollback(task);
                     HandleRollbackAction(task, cmdsToServer, RollbackEventCommand.RollbackType.ROLLBACK);
                     break;

                  case InternalInterface.MG_ACT_EDT_UNDO:
                     if (task.getLastParkedCtrl() != null)
                     {
                        var fld = (Field)task.getLastParkedCtrl().getField();
                        if (fld != null)
                        {
                           fld.updateDisplay();
                           Manager.SetSelect(task.getLastParkedCtrl());
                        }
                     }
                     handleCtrlModify(ctrl, intEvtCode);
                     break;

                  case InternalInterface.MG_ACT_TREE_EXPAND:
                  case InternalInterface.MG_ACT_TREE_COLLAPSE:
                     if (form != null)
                     {
                        MgTreeBase mgTree = form.getMgTree();
                        if (mgTree != null)
                        {
                           bool isGuiTriggeredEvent = evt.isGuiTriggeredEvent();
                           bool expand = (evt.getInternalCode() == InternalInterface.MG_ACT_TREE_EXPAND);
                           //if it is reaised by raise event - use currect line, if it is raised by GUI - use line from expand
                           int line = (isGuiTriggeredEvent
                                          ? evt.getDisplayLine()
                                          : form.DisplayLine);
                           //if event was sent from gui, we do not need to update gui level
                           doExpandCollapse(task, expand, line);
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_TREE_RENAME_RT:
                     var treeControl = (MgControl)form.getTreeCtrl();
                     //fixed bug #:775798 :when treeControl.TmpEditorIsShow need to exist the 
                     //                    editor of the tree
                     if (treeControl.TmpEditorIsShow)
                        TreeControlExitEditing(task, ctrl);
                     else
                     {
                        if (treeControl != null && treeControl.isModifiable())
                        {
                           treeControl.TmpEditorIsShow = true;
                           treeControl.enableTreeEditingAction(true);
                           Commands.addAsync(CommandType.SHOW_TMP_EDITOR, treeControl, form.DisplayLine, 0);
                           Manager.SetSelect(treeControl);
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_OK:
                  case InternalInterface.MG_ACT_TREE_RENAME_RT_EXIT:
                     TreeControlExitEditing(task, ctrl);
                     break;

                  case InternalInterface.MG_ACT_INDEX_CHANGE:
                  case InternalInterface.MG_ACT_COL_SORT:
                     // can sort only on column with an attached field.
                     // can change index only if the dataview is remote.
                     if ((intEvtCode == InternalInterface.MG_ACT_INDEX_CHANGE && task.DataviewManager.HasRemoteData) || 
                         (intEvtCode == InternalInterface.MG_ACT_COL_SORT && ctrl.getField() != null))
                     {
                        // QCR #296325 & QCR #430288.
                        // Execute CS of the current control, because it can be a parameter for a subform. 
                        // If it's value was changed, CS will refresh the subform before the sort.
                        Task lastFocusedTask = ClientManager.Instance.getLastFocusedTask();
                        MgControl focusedCtrl = GUIManager.getLastFocusedControl(GUIManager.LastFocusMgdID);
                        if (focusedCtrl != null)
                           handleInternalEvent(focusedCtrl, InternalInterface.MG_ACT_CTRL_SUFFIX);

                        if (!GetStopExecutionFlag())
                           task.ExecuteNestedRS(lastFocusedTask);

                        // QCR #438753. Execute RS of the sorted task.
                        if (!GetStopExecutionFlag() && task.getLevel() != Constants.TASK_LEVEL_TASK)
                           handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);

                        if (!GetStopExecutionFlag())
                        {
                           task.getTaskCache().clearCache();

                           if (intEvtCode == InternalInterface.MG_ACT_INDEX_CHANGE)
                           {
                              MgForm taskForm = (MgForm)task.getForm();
                              if (taskForm.HasTable())
                                 form.clearTableColumnSortMark(true);

                              cmd = CommandFactory.CreateIndexChangeCommand(task.getTaskTag(), ((DataView)task.DataView).GetCurrentRecId(), evt.getArgList());
                           }
                           else
                           {
                              Commands.addAsync(CommandType.CHANGE_COLUMN_SORT_MARK, form.getControlColumn(evt.Control), evt.Direction, false);

                              cmd = CommandFactory.CreateColumnSortCommand(task.getTaskTag(), evt.Direction, ctrl.getDitIdx(), ctrl.getField().getId(), ((DataView)task.DataView).GetCurrentRecId());
                           }

                           task.DataviewManager.Execute(cmd);
                           dv.setTopRecIdx(0);
                           form.restoreOldDisplayLine(0);
                           _isSorting = true;
                           // QCR's #165815 & #291974. If the focused task and the sorted task is the same task or 
                           // the focused task is a child of the sorted task, go to the first control in the first line.
                           // If the focused task is the parent task, do not leave the current control.
                           if (task == ClientManager.Instance.getLastFocusedTask() || !task.pathContains(ClientManager.Instance.getLastFocusedTask()))
                           {
                              handleInternalEvent(task, InternalInterface.MG_ACT_TBL_BEGLINE);
                           }
                           else 
                           {
                              handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                              if (!GetStopExecutionFlag() && !task.getPreventControlChange())
                                 // QCR #428697. Execute RS of all nested subforms.
                                 lastFocusedTask.ExecuteNestedRS(task);
                              if (!GetStopExecutionFlag() && focusedCtrl != null)
                                 focusedCtrl.invoke();
                           }
                           _isSorting = false;
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_COL_CLICK:
                     // can sort on column only if event is raised due to click action
                     // and a field is attached to column.                     

                     // before sorting, if CloseTasksOnParentActivate is on, try and close all child tasks.
                     // we send the task of the top most form since our focuse might be inside a subform, and that is equal to clicking on its form.
                     if (ClientManager.Instance.getEnvironment().CloseTasksOnParentActivate())
                        CloseAllChildProgs((Task)task.getTopMostForm().getTask(), null, false);

                     if (!GetStopExecutionFlag() && evt.isGuiTriggeredEvent() && ctrl.isColumnSortable())
                     {
                        MgControl columnChildControl = (MgControl)ctrl.getColumnChildControl();
                        if (columnChildControl != null)
                        {
                           //handle Sort on Column
                           var aRtEvt = new RunTimeEvent(columnChildControl, evt.Direction, 0);
                           aRtEvt.setInternal(InternalInterface.MG_ACT_COL_SORT);
                           ClientManager.Instance.EventsManager.addToTail(aRtEvt);
                        }
                     }
                     break;

                  case InternalInterface.MG_ACT_ALIGN_LEFT:
                  case InternalInterface.MG_ACT_ALIGN_RIGHT:
                  case InternalInterface.MG_ACT_BULLET:
                  case InternalInterface.MG_ACT_CENTER:
                  case InternalInterface.MG_ACT_INDENT:
                  case InternalInterface.MG_ACT_UNINDENT:
                  case InternalInterface.MG_ACT_CHANGE_COLOR:
                  case InternalInterface.MG_ACT_CHANGE_FONT:
                     ((MgControlBase)ctrl).ProcessRichTextAction(intEvtCode);
                     break;

                  case InternalInterface.MG_ACT_PRESS:
                     if (ctrl != null)
                        Commands.addAsync(CommandType.PROCESS_PRESS_EVENT, ctrl, ctrl.getDisplayLine(true), 0);
                     else
                        Commands.addAsync(CommandType.PROCESS_PRESS_EVENT, form, 0, 0);
                     break;

                  case InternalInterface.MG_ACT_SWITCH_TO_OFFLINE:
                     // Ronen (MH): This is for simulation purposes only. In the future the event will be implemented differently.
                     // NOT NEED TO PORT TO MOBILE !!!
                     ((IConnectionStateManager)ClientManager.Instance.GetService(typeof(IConnectionStateManager))).ConnectionDropped();
                     //setStopExecution(true, true);
                     break;

#if !PocketPC
                  case InternalInterface.MG_ACT_PREV_RT_WINDOW:
                  case InternalInterface.MG_ACT_NEXT_RT_WINDOW:
                     Manager.ActivateNextOrPreviousMDIChild(intEvtCode == InternalInterface.MG_ACT_NEXT_RT_WINDOW ? true : false);
                     break;
#endif
               }
            }
            // end of internal events processing
            else if (evt.getType() == ConstInterface.EVENT_TYPE_MENU_OS)
            {
               String errMsg = null;
               int exitCode = 0;
               ProcessLauncher.InvokeOS(evt.getOsCommandText(), evt.getOsCommandWait(), evt.getOsCommandShow(), ref errMsg, ref exitCode);
               if (errMsg != null)
               {
                  Manager.WriteToMessagePane((Task)(evt.getTask().GetContextTask()), errMsg, false);
                  FlowMonitorQueue.Instance.addFlowInvokeOsInfo(errMsg);
                  Logger.Instance.WriteExceptionToLog(errMsg);
               }

            }
            else if (evt.getType() == ConstInterface.EVENT_TYPE_MENU_PROGRAM)
            {
               bool startupMgDataClosed = false;
               String programinternalName = evt.PublicName;
               MgForm topMostFramForm = (MgForm)ClientManager.Instance.RuntimeCtx.FrameForm;
               Task creatorTask = evt.ActivatedFromMDIFrame ? (Task)topMostFramForm.getTask() : evt.getTask();

               string menuPath = MenuManager.GetMenuPath(ClientManager.Instance.getLastFocusedTask());
               cmd = CommandFactory.CreateMenuCommand(creatorTask.getTaskTag(), evt.getMenuUid(), evt.getMenuComp(), menuPath);
               // Get the relevant menu entry
               TaskDefinitionId taskDefinitionId = Manager.MenuManager.TaskCalledByMenu(evt.getMenuUid(),
                           MGDataCollection.Instance.GetMainProgByCtlIdx(evt.getMenuComp()));
               server = CommandsProcessorManager.GetCommandsProcessor(taskDefinitionId);
               if (programinternalName == null ||
                   evt.PrgFlow != ConstInterface.MENU_PROGRAM_FLOW_RC ||
                   !server.AllowParallel())
               {
                  // if program is activated from Mdi frame menu, check for 'close tasks' prop on task.
                  // if the prop is true, it means we need to close all running tasks before activating the new task.
                  // if one of the closing tasks is the startup task (run with f7) we will need to avoid closing the mdi when this task is closed,
                  // and then, set the new opened task as our new startup task.
                  if (evt.ActivatedFromMDIFrame)
                  {
                     MGData StartUpMgd = MGDataCollection.Instance.StartupMgData;
                     if (topMostFramForm.getTask().checkProp(PropInterface.PROP_TYPE_CLOSE_TASKS_BY_MDI_MENU, false))
                     {
                        MGData topMostFrameMgd = ((Task)topMostFramForm.getTask()).getMGData();
                        if (topMostFrameMgd != StartUpMgd)
                           MGDataCollection.Instance.StartupMgData = topMostFrameMgd;

                        // close all child progs. send startup mgd and and indication that the close is from the mdi frame menu
                        CloseAllChildProgs(creatorTask, StartUpMgd, true);

                        // if startUpMgd is aborting, it means that it was successfully closed
                        startupMgDataClosed = StartUpMgd.IsAborting;

                        // did not closed, set the saved start up mgd back to be the ClientManager.Instance's startup mgd.
                        if (!startupMgDataClosed)
                           MGDataCollection.Instance.StartupMgData = StartUpMgd;
                     }
                  }

                  // send open prog from menu only if there was no problem.
                  if (!GetStopExecutionFlag())
                  {
                     int CurrStartUpMgdId = MGDataCollection.Instance.StartupMgData.GetId();
                     // stat up mgdata was closed. we need to set the current mgdata to the current startup.
                     // this can only be true if working with mdi fram menu and the close tasks flag is on.
                     if (startupMgDataClosed)
                        MGDataCollection.Instance.currMgdID = CurrStartUpMgdId;

                     creatorTask.getMGData().CmdsToServer.Add(cmd);

                     server.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);

                     // when running with F7. If the original StartUp mgData was closed, and a new prog was opened, we need to 
                     // set the new mgd as the startup mgd.
                     // StartUpMgDataClosed can only be true if working with mdi frame menu and the close tasks flag is on.
                     if (startupMgDataClosed)
                     {
                        if (!GetStopExecutionFlag())
                        {
                           if (MGDataCollection.Instance.currMgdID != CurrStartUpMgdId)
                              // curr mgd is different , meaning a new program was opened. This is our new startup mgdata.
                              MGDataCollection.Instance.StartupMgData = MGDataCollection.Instance.getCurrMGData();
                        }
                        else
                           // something went wrong with the opening of the new task (which is supposed to be our new startup task). close all.
                           handleInternalEvent(creatorTask, InternalInterface.MG_ACT_EXIT);
                     }
                  }
               }
               else
               {
                  var argList = new ArgumentsList();
                  argList.fillListByMainProgVars(evt.MainProgVars, evt.getTask().getCtlIdx());
                  Operation.callParallel(evt.getTask(), evt.PublicName, argList,
                                         evt.CopyGlobalParams, evt.PrgDescription);
               }
            }
         }
         finally
         {
            popRtEvent();
         }
      }

      /// <summary>
      /// Creates and executes subform open
      /// </summary>
      /// <param name="task"></param>
      /// <param name="subformControl"></param>
      private void OpenSubform(Task task, MgControl subformControl)
      {
         task.TaskService.RemoveRecomputes(task, null);
         IClientCommand cmd = CommandFactory.CreateSubformOpenCommand(task.getTaskTag(), subformControl.getDitIdx());
         task.getMGData().CmdsToServer.Add(cmd);
         task.CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      /// <param name="cmdsToServer"></param>
      /// <param name="rollbackType"></param>
      /// <returns></returns>
      private static void HandleRollbackAction(Task task, CommandsTable cmdsToServer, RollbackEventCommand.RollbackType rollbackType)
      {
         CommandsProcessorBase server = task.CommandsProcessor;

         ClientRefreshCommand.ClientIsInRollbackCommand = true;
         // for remote program : (with\without local data) the rollback event in the server MUST 
         //                       be execute on the task that open the transaction
         // for offline program the rollback transaction need to be execute on the task that open the transaction
         task = task.TaskService.GetOwnerTransactionTask(task);

         IClientCommand cmd = CommandFactory.CreateRollbackEventCommand(task.getTaskTag(), rollbackType);
         cmdsToServer.Add(cmd);
         server.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);

         ClientRefreshCommand.ClientIsInRollbackCommand = false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="evt"></param>
      /// <param name="task"></param>
      /// <param name="form"></param>
      /// <param name="ctrl"></param>
      /// <param name="rec"></param>
      /// <param name="lastParkedCtrl"></param>
      /// <param name="oldRecMode"></param>
      /// <param name="oldMode"></param>
      /// <returns></returns>
      private void HandleActCancel(RunTimeEvent evt, Task task, MgForm form, MgControl ctrl, Record rec, bool isMG_ACT_CANCEL)
      {
         // Prevent recursive mode
         // fixed bugs #:285498, 297455\427703\
         if (task.InHandleActCancel)
            return;

         task.InHandleActCancel = true;

         MgControl lastParkedCtrl = null;
         DataModificationTypes oldRecMode;
         char oldMode = task.getMode();
         // When do we show the 'Confirm Cancel window' ? when all the following conditions apply:
         // 1. Task mode is not query 
         // 2. rec is not Updated. Updated is true when there was an Update operation with 'force update'. If it is true, then there is no meaning to show the
         //    window since the update stays anyway (like in online when we go to CancelWasTriggered).
         // 3. There must be a modification, otherwise there is nothing to cancel. So either Modified is true, meaning a field was moidfied , or
         //    LastParkedCtrl().modifiedByUser(), meaning that we are in the middle of changing the field's value (The modified is false untill we exit the field).
         // Create mode also goes here (rec removed and mode changes to modify) , but also will show the confirm cancel only if some change was done to the new record.
         if (oldMode != Constants.TASK_MODE_QUERY && false == rec.Updated)
         {
            if (isMG_ACT_CANCEL)
            {
               if (rec.Modified ||
                   (task.getLastParkedCtrl() != null && task.getLastParkedCtrl().ModifiedByUser))
               {
                  if (task.checkProp(PropInterface.PROP_TYPE_CONFIRM_CANCEL, false))
                  {
                     if (!(GUIManager.Instance.confirm((MgForm)task.getForm(), MsgInterface.CONFIRM_STR_CANCEL)))
                     {
                        task.InHandleActCancel = false;
                        return;
                     }
                  }
               }
            }

            oldRecMode = rec.getMode();
            
            if (isMG_ACT_CANCEL)
            {
               // When canceling, we have to rollback the transaction.
               // Unless the event does not allow to rollback in cancel. That can happen if the cancel was initiated by the server's's rollback.
               if (task.isTransactionOnLevel(ConstInterface.TRANS_RECORD_PREFIX) && evt.RollbackInCancel())
               {                  
                  HandleRollbackAction(task, task.getMGData().CmdsToServer, RollbackEventCommand.RollbackType.CANCEL);
                  // The rollback local need to execute only for Remote program 
                  // for offline program the command is execute in HandleRollbackAction .
                  task.TaskService.HandleRollbackLocalTransactionForCancelAction(task);
               }
            }

            HandleCancelEdit(evt, task, form, rec, oldRecMode);
         }
         // Qcr #747870 cancel should perform the record prefix if not quiting
         else
         {
            task.setLevel(Constants.TASK_LEVEL_TASK);

            // we rollback a parent task while focused on subform/task. We do not want the ctrl on the subform to do ctrl suffix.
            lastParkedCtrl = GUIManager.getLastFocusedControl();
            if (lastParkedCtrl != null)
               lastParkedCtrl.getForm().getTask().setLevel(Constants.TASK_LEVEL_TASK);

            if (!(evt.isQuit()))
            {
               handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
               task.setCurrVerifyCtrl(null);
               ((MgForm)task.getForm()).moveInRow(null, Constants.MOVE_DIRECTION_BEGIN);
            }
         }

         //QCR# 941277: If Cancel event is called from Quit event, then don't execute Exit event
         // if user has pressed 'No' on Confirm cancel box. 
         if (evt.isQuit())
            handleInternalEvent(ctrl, InternalInterface.MG_ACT_EXIT);

         task.InHandleActCancel = false;
      }

      /// <summary>
      /// cancel the edit 
      /// </summary>
      /// <param name="evt"></param>
      /// <param name="task"></param>
      /// <param name="form"></param>
      /// <param name="rec"></param>
      /// <param name="oldRecMode"></param>
      private static void HandleCancelEdit(RunTimeEvent evt, Task task, MgForm form, Record rec, DataModificationTypes oldRecMode)
      {
         // if the synced record (i.e. the record from the parent task of the subform) before Rollback was in 'Insert' mode,
         // so it must be in 'Insert' more after return from the server
         if (rec.Synced && oldRecMode == DataModificationTypes.Insert)
            rec.setMode(oldRecMode);

         if (form != null)
         {
            // reset the last parked control
            if (task.getLastParkedCtrl() != null)
               task.getLastParkedCtrl().resetPrevVal();

            form.cancelEdit(true, evt.isQuit());
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="evt"></param>
      /// <param name="task"></param>
      /// <param name="dv"></param>
      /// <param name="rec"></param>
      /// <param name="nonInteractiveDelete"></param>
      /// <returns></returns>
      private bool HandleActionDelline(RunTimeEvent evt, Task task, DataView dv, Record rec, bool nonInteractiveDelete)
      {
         MgControl lastParkedCtrl;
         Record newCurrRec;
         DataModificationTypes oldRecMode;

         if (task.checkProp(PropInterface.PROP_TYPE_ALLOW_DELETE, true))
         {
            lastParkedCtrl = (MgControl)task.getLastParkedCtrl();
            if (lastParkedCtrl != null)
               handleInternalEvent(lastParkedCtrl, InternalInterface.MG_ACT_CTRL_SUFFIX);
            if (!GetStopExecutionFlag())
            {
               // no confirm message dialog if in non interactive delete.
               bool skipConfirmDialog = nonInteractiveDelete;

               if (!skipConfirmDialog)
               {
                  if (task.mustConfirmInDeleteMode() ||
                      task.checkProp(PropInterface.PROP_TYPE_CONFIRM_UPDATE, false))
                  {
                     bool confirmed = GUIManager.Instance.confirm((MgForm)task.getForm(),
                                                          MsgInterface.CONFIRM_STR_DELETE);
                     if (!confirmed)
                     {
                        if (lastParkedCtrl != null)
                           handleInternalEvent(lastParkedCtrl, InternalInterface.MG_ACT_CTRL_PREFIX);

                        return true;
                     }
                  }
               }

               oldRecMode = rec.getMode();

               // Force the change to delete mode
               rec.clearMode();
               rec.setMode(DataModificationTypes.Delete);

               if (oldRecMode == DataModificationTypes.Insert)
                  rec.setSendToServer(false);

               // execute task suffix in Modify mode or when there is 'force record suffix'.
               if (rec.Modified || task.checkProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, false))
               {
                  task.setMode(Constants.TASK_MODE_MODIFY);
                  handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                  // make sure that the record suffix completed successfully
                  if (_stopExecution)
                  {
                     if (!rec.SendToServer)
                     {
                        dv.addCurrToModified();
                        rec.setSendToServer(true);
                     }

                     rec.clearMode();
                     rec.setMode(oldRecMode);
                     setStopExecution(false);
                     handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                     setStopExecution(true);
                     return true;
                  }
               }

               // execute record suffix in Delete mode
               // set the InDeleteProcess flag , so we will not try to send updates on the record after
               // we delete it in the commonHandlerAfter (rec suffix). 
               rec.setInDeleteProcess(true);

               task.setMode(Constants.TASK_MODE_DELETE);
               handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
               rec.setInDeleteProcess(false);

               if (_stopExecution)
               {
                  if (!rec.SendToServer)
                  {
                     dv.addCurrToModified();
                     rec.setSendToServer(true);
                  }

                  rec.clearMode();
                  rec.setMode(oldRecMode);
                  setStopExecution(false);
                  task.setMode(Constants.TASK_MODE_MODIFY);
                  handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                  setStopExecution(true);

                  return true;
               }

               // If we're deleting an inserted record, the server is not aware of it
               // make sure that upon delete, it will be removed from the modified rec list
               if (oldRecMode == DataModificationTypes.Insert)
               {
                  rec.clearMode();
                  rec.setMode(oldRecMode);
               }

               newCurrRec = (Record)dv.getCurrRec();

               try
               {
                  // QCR #438175: when the user deletes a record, which is the single
                  // record in the dv, then when its record suffix is executed and the
                  // server sends a new record to the client and set it to be the
                  // current record.
                  // This fix makes the deleted record to be the current for a
                  // moment just to delete it from the clients cache. Then, it makes
                  // the new record to be the current again and enters into create mode.
                  // I think that a better solution is to decide whether the client is
                  // responsible for adding a new record or the server, and make sure
                  // that it is done systemwide.
                  // Ehud - 10/09/2001
                  if (rec != newCurrRec)
                     dv.setCurrRec(rec.getId(), false);

                  // no need to del the curr rec and position on the nex rec if the task is aborting.
                  // The delCurrRec may cause a crash if it tries to get the next rec from the next chunk.
                  if (!task.isAborting())
                    delCurrRec(task, false);
               }
               finally
               {
                  // QCR #438175 - fix continue
                  if (rec != newCurrRec)
                  {
                     dv.setCurrRec(newCurrRec.getId(), false);
                     if (dv.getSize() == 1)
                        task.setMode(Constants.TASK_MODE_CREATE);
                  }

                  // In interactive, the mode might change to modify or create. However, for non interactive (delete mode)
                  // the mode should be set back to delete and will be used in the next record cycle.
                  if (nonInteractiveDelete)
                     task.setMode(Constants.TASK_MODE_DELETE);

               }
            }
         }
         return false;
      }

      private void TreeControlExitEditing(Task task, MgControl ctrl)
      {
         if (ctrl != null && ctrl.isTreeControl())
         {
            Logger.Instance.WriteDevToLog("MG_ACT_TREE_RENAME_RT_EXIT");
            String val = Manager.GetCtrlVal(ctrl);
            ctrl.validateAndSetValue(val, true);
            ctrl.enableTreeEditingAction(false);
            if (ctrl == task.getLastParkedCtrl())
               Manager.SetFocus(ctrl, 0);
            ctrl.TmpEditorIsShow = false;
         }
      }

      /// <summary>
      ///   1. Handle record cycle (Control, Record Suffix & Prefix)
      ///   2. Send to the server request with locate query
      /// </summary>
      /// <param name = "evt">the event to handle with all its properties</param>
      private void locateInQuery(Task task)
      {
         var form = (MgForm)task.getForm();
         var dv = (DataView)task.DataView;
         CommandsProcessorBase server = task.CommandsProcessor;
         CommandsTable cmdsToServer = task.getMGData().CmdsToServer;
         IClientCommand cmd = null;
         MgControl ctrl = task.locateQuery.Ctrl;

         if (ClientManager.Instance.InIncrementalLocate() || (task.locateQuery.Buffer.Length == 0))
            return;

         ClientManager.Instance.SetInIncrementalLocate(true);
         handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
         
         if (task.isAborting())
            return;

         if (!GetStopExecutionFlag())
         {
            cmd = CommandFactory.CreateLocateQueryCommand(task.getTaskTag(), ctrl.getDitIdx(),
                                                   task.locateQuery.Buffer.ToString(),
                                                   task.locateQuery.ServerReset,
                                                   ctrl.getField().getId());

            task.getTaskCache().clearCache();
            task.DataviewManager.Execute(cmd);

            // After view refresh, the server always returns curr rec as first rec.
            dv.setTopRecIdx(0);
            form.restoreOldDisplayLine(form.recordIdx2DisplayLine(dv.getCurrRecIdx()));

            if (!form.isScreenMode())
            {
               if (form.HasTable())
               {
                  Commands.addAsync(CommandType.UPDATE_TMP_EDITOR_INDEX, form.getTableCtrl());
               }
            }

            task.locateQuery.ServerReset = false;
            task.locateQuery.InitServerReset = false;
            task.locateQuery.ClearIncLocateString();

            handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
            if (ctrl.invoke())
            {
               PIC pic = ctrl.getPIC();
               int newPos = task.locateQuery.Offset;

               for (int i = 0;
                    i < newPos;
                    i++)
               {
                  if (pic.picIsMask(i))
                     newPos++;
               }
               Manager.SetSelection(ctrl, newPos, newPos, newPos);
            }
            else if (!form.moveInRow(ctrl, Constants.MOVE_DIRECTION_NEXT))
               HandleNonParkableControls(task);
         }
         ClientManager.Instance.SetInIncrementalLocate(false);
      }

      /// <summary>
      ///   adds the pressed character to the locate string and restart the timer
      /// </summary>
      /// <param name = "evt"></param>
      private void addLocateCharacter(RunTimeEvent evt)
      {
         Task task = evt.getTask();

         if (evt.getInternalCode() == InternalInterface.MG_ACT_CHAR)
         {
            //get locate character according to the picture
            PIC pic = evt.Control.getPIC();
            int pos = task.locateQuery.Offset + task.locateQuery.Buffer.Length;

            if (pos < pic.getMaskSize())
            {
               char locateChar = evt.getValue()[0];
               locateChar = pic.validateChar(locateChar, pos);
               if (locateChar != 0)
                  task.locateQuery.Buffer.Append(locateChar);
            }
         }
         else //backspace
            task.locateQuery.IncLocateStringAddBackspace();

         task.locateQuery.FreeTimer();
         task.locateQuery.Timer = new Timer(incLocateTimerCB, task, ConstInterface.INCREMENTAL_LOCATE_TIMEOUT,
                                            Timeout.Infinite);
      }

      /// <summary>
      ///   handle view refresh, screen refresh and record flush events
      /// </summary>
      /// <param name = "evt">the event to handle with all its properties</param>
      private void handleRefreshEvents(RunTimeEvent evt)
      {
         Task task = evt.getTask();
         var form = (MgForm)task.getForm();
         var dv = (DataView)task.DataView;
         int intEvtCode = evt.getInternalCode();
         IClientCommand cmd = null;

         //for tree refresh screen acts as refresh view
         if (form.hasTree() && intEvtCode == InternalInterface.MG_ACT_RT_REFRESH_SCREEN)
            intEvtCode = InternalInterface.MG_ACT_RT_REFRESH_VIEW;
         // check if we try to view refresh with allow_modify=n in create mode
         if (intEvtCode == InternalInterface.MG_ACT_RT_REFRESH_VIEW && task.getMode() == Constants.TASK_MODE_CREATE &&
             !task.checkProp(PropInterface.PROP_TYPE_ALLOW_MODIFY, true))
            handleInternalEvent(task, InternalInterface.MG_ACT_EXIT);
         else
         {
            // Defect 77417. Execute RS's of the nested subforms if the current task is one of the nested subforms.
            task.ExecuteNestedRS(ClientManager.Instance.getLastFocusedTask());
            if (!GetStopExecutionFlag())
               handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
            if (!GetStopExecutionFlag() && !task.InEndTask && !task.isAborting())
            {
               // if it is not a real view refresh try to take requested d.v from cache
               if (intEvtCode != InternalInterface.MG_ACT_RT_REFRESH_SCREEN)
               {
                  int currentRow = 0;
                  if (!evt.getRefreshType())
                  {
                     // use the current row to park on it, if needed (for exampl: used for MG_ACT_RT_VIEW_REFRESH, in rollback action ot cancel)
                     if (evt.RtViewRefreshUseCurrentRow() && task.IsInteractive)
                        currentRow = form.getCurrRecPosInForm();

                     cmd = CommandFactory.CreateInternalRefreshCommand(task.getTaskTag(), intEvtCode, dv.CurrentRecId, currentRow);
                  }
                  else
                  {                   
                     if (task.IsInteractive)
                        currentRow = form.getCurrRecPosInForm();
                     cmd = CommandFactory.CreateRealRefreshCommand(task.getTaskTag(), intEvtCode, currentRow, evt.getArgList(), dv.CurrentRecId);
                  }
               }
               // screen refresh event
               else
               {
                  int id;

                  if (task.getForm() == null)
                     id = 0;
                  else if (task.getForm().isScreenMode())
                     id = ((DataView)task.DataView).getCurrRec().getId();
                  else
                     id = ((DataView)task.DataView).getRecByIdx(((DataView)task.DataView).getTopRecIdx()).getId();

                  cmd = CommandFactory.CreateScreenRefreshCommand(task.getTaskTag(), id, dv.CurrentRecId);
               }

               // real view refresh causes the cache of the current sub form task to be cleared
               if (intEvtCode != InternalInterface.MG_ACT_RT_REFRESH_SCREEN)
                  //screen refresh does not clear the cache
                  task.getTaskCache().clearCache();

               task.DataviewManager.Execute(cmd);

               // QCR #289222. Do not continue with Refresh if the task must be ended.
               if (!GetStopExecutionFlag() && !task.InEndTask && !task.isAborting())
               {
                  // After view refresh, the server always returns curr rec as first rec.
                  if (intEvtCode == InternalInterface.MG_ACT_RT_REFRESH_VIEW && !form.isScreenMode())
                  {
                     dv.setTopRecIdx(0);
                     if (form.HasTable())
                     {
                        form.clearTableColumnSortMark(true);
                        form.restoreOldDisplayLine(form.recordIdx2DisplayLine(dv.getCurrRecIdx()));
                     }
                  }

                  // After record has been flushed it is not considered a new record anymore.
                  if (intEvtCode == InternalInterface.MG_ACT_RT_REFRESH_RECORD &&
                      task.getMode() != Constants.TASK_MODE_QUERY && !task.getAfterRetry())
                  {
                     if (task.getMode() == Constants.TASK_MODE_CREATE)
                     {
                        task.setMode(Constants.TASK_MODE_MODIFY);
                        ((DataView)task.DataView).getCurrRec().setOldRec();
                     }
                     dv.currRecCompute(true);
                  }
                  else if (intEvtCode == InternalInterface.MG_ACT_RT_REFRESH_SCREEN &&
                           task.getMode() != Constants.TASK_MODE_QUERY && !task.getAfterRetry())
                  {
                     dv.currRecCompute(true);
                  }

                  // QCR #430059. If the focused task and the refreshed task is the same task or 
                  // the focused task is a child of the refreshed task, go to the first control in the first line.
                  // If the focused task is the parent task, do not leave the current control.
                  handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                  if (!GetStopExecutionFlag() && !task.getPreventControlChange())
                  {
                     if (task == ClientManager.Instance.getLastFocusedTask() || !task.pathContains(ClientManager.Instance.getLastFocusedTask()))
                        handleInternalEvent(task, InternalInterface.MG_ACT_TBL_BEGLINE);
                     else
                        handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
                  }
               }
            }
         }
      }

      /// <summary>
      ///   Go to create mode
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "cmds"></param>
      private void gotoCreateMode(Task task, CommandsTable cmds)
      {
         IClientCommand cmd;
         task.enableModes();
         // QCR #417733. Execute RS's of the nested subforms if the current task is one of the nested subforms.
         task.ExecuteNestedRS(ClientManager.Instance.getLastFocusedTask());
         if (!GetStopExecutionFlag())
            handleInternalEvent(task, InternalInterface.MG_ACT_REC_SUFFIX);
         if (!GetStopExecutionFlag())
         {
            cmd = CommandFactory.CreateEventCommand(task.getTaskTag(), InternalInterface.MG_ACT_RTO_CREATE);
            task.DataviewManager.Execute(cmd);
            if (task.getLevel() == Constants.TASK_LEVEL_TASK)
               handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);

            task.moveToFirstCtrl(true);
            task.setCreateDeleteActsEnableState();
            task.setOriginalTaskMode(Constants.TASK_MODE_CREATE);
         }
      }

      /// <summary>
      ///   performs expand or collapse
      /// </summary>
      /// <param name = "task">task</param>
      /// <param name = "expand">true for expand false for collapse</param>
      /// <param name = "line">line for expand</param>
      private void doExpandCollapse(Task task, bool expand, int line)
      {
         IClientCommand cmd;
         MGData mgData = task.getMGData();
         CommandsTable cmdsToServer = mgData.CmdsToServer;
         MgTreeBase mgTree = task.getForm().getMgTree();

         if (mgTree != null)
         {
            if (expand && !mgTree.isChildrenRetrieved(line))
            {
               mgTree.setChildrenRetrieved(line, true);
               mgTree.setNodeInExpand(line);
               mgTree.deleteChildren(line);
               String path = ((MgTree)mgTree).getPath(line);
               String values = ((MgTree)mgTree).getParentsValues(line);
               String treeIsNulls = ((MgTree)mgTree).getNulls(line);
               //send to server
               cmd = CommandFactory.CreateExpandCommand(task.getTaskTag(), path, values, treeIsNulls);
               task.DataviewManager.Execute(cmd);
               ((MgTree)mgTree).updateAfterExpand(line);
            }
            //if expand perfromed on the leaf , do not change expand status
            else if ((expand && mgTree.hasChildren(line)) || !expand)
               mgTree.setExpanded(line, expand, true);
         }
      }

      /// <summary>
      ///   do some default operations for an event after all the handlers finished
      /// </summary>
      /// <param name = "rcBefore">the RC returned from the commonHandlerBefore</param>
      /// <param name = "propogate">if event was propogated</param>
      /// <param name = "Event">the event to handle</param>
      private void commonHandlerAfter(RunTimeEvent evt, bool rcBefore, bool propagate)
      {
         Task task = evt.getTask();
         DataView dv;
         Record rec = null;
         MgControl ctrl;
         bool recModified = false;
         bool recModeIsDelete = false;
         bool forceSuffix;

         if (_stopExecution && evt.getInternalCode() != InternalInterface.MG_ACT_REC_SUFFIX &&
             evt.getInternalCode() != InternalInterface.MG_ACT_CTRL_SUFFIX)
            return;

         if (evt.getType() == ConstInterface.EVENT_TYPE_INTERNAL)
         {
            dv = (DataView)task.DataView;
            rec = (Record)dv.getCurrRec();
            ctrl = evt.Control;

            switch (evt.getInternalCode())
            {
               case InternalInterface.MG_ACT_REC_PREFIX:
                  if (rcBefore)
                  {
                     // handle record prefix for the sub tasks too
                     if (task.CheckRefreshSubTasks())
                        task.doSubformRecPrefixSuffix();
                     task.handleEventOnSlaveTasks(InternalInterface.MG_ACT_REC_PREFIX);
                     task.enableModes();
                  }
                  break;

               case InternalInterface.MG_ACT_REC_SUFFIX:
                  task.isFirstRecordCycle(false);
                  //Once are ready to end the RS, it means we cannot be the FIRST record cycle
                  if (task.getPreventRecordSuffix())
                  {
                     task.setPreventRecordSuffix(false);
                     return;
                  }

                  // QCR #115951. Even if record suffix is not executed, in the next time do not execute RP & RS of the subform.
                  task.DoSubformPrefixSuffix = false;

                  if (_stopExecution)
                  {
                     // QCR #984031: if the delete operation failed due to "verify error"
                     // in the record suffix then the record mode must be cleared
                     if (rec.getMode() == DataModificationTypes.Delete)
                        rec.clearMode();
                     // Qcr 783039 : If we are already in control level it means that control prefix was already done or that
                     // control suffix was not done. Either way there is no need to do control prefix again.
                     if (task.getLevel() != Constants.TASK_LEVEL_CONTROL)
                     {
                        var last = (MgControl)getStopExecutionCtrl();
                        if (last == null)
                           last = (MgControl)task.getLastParkedCtrl();

                        setStopExecution(false);
                        setStopExecutionCtrl(null);

                        //QCR 426283 is we have a sub-form that was never been entered
                        //make sure control level will not be entered
                        if (last != null)
                        {
                           // we donot want CS to be called again from CP incase of verify err in CV
                           task.setLevel(Constants.TASK_LEVEL_RECORD);

                           bool inCtrlPrefix = task.InCtrlPrefix;
                           task.InCtrlPrefix = false;
                           handleInternalEvent(last, InternalInterface.MG_ACT_CTRL_PREFIX);
                           task.InCtrlPrefix = inCtrlPrefix;
                        }

                        // in the future check the stopExecution flag here too and display a
                        // confirm cancel dialog that will close the browser if the value is true
                        setStopExecution(true);
                     }
                     return;
                  }
                  if (rec != null)
                  {
                     recModified = rec.Modified;
                     if (!_stopExecution && rec.isCauseInvalidation())
                     {
                        ((MgForm)task.getForm()).invalidateTable();
                        rec.setCauseInvalidation(false);
                     }
                     recModeIsDelete = (rec.getMode() == DataModificationTypes.Delete);

                     forceSuffix = task.checkProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, false);
                     // if the record is deleted then execute the "rec suffix after" only
                     // when the task mode is delete (second round)
                     // inDeleteProcess : if there is an 'END TASK COND' we will have a 3rd cycle in record suffix(when upd+del or force suff + del).
                     // 1st cycle : updae cycle (task mode will be update), 2nd : task mode is delete , this is when we send the delete to the server.
                     // 3rd - suffix that is called from the endTask. In the 3rd cycle the recModeIsDelete will be false although we are still in the delete process (we already sent the delete oper).
                     // A combination of force suffix + end task cond + del line , will crash the server on the 3rd cycle since we will ask for update off
                     // a record that the server had deleted on the 2nd cycle. That it why we use 'inDeleteProcess', so will will know not
                     // to add update oper on an already deleted record. (Qcr #790283).
                     // * there is still a problem in 'update' and 'insert' with 'end task cond' + 'force' : an extra update will be send
                     //   to the server on the rec suffix of the end task. but apart from server update it causes no real problem.
                     if (recModeIsDelete && task.getMode() == Constants.TASK_MODE_DELETE ||
                           !recModeIsDelete && !rec.inDeleteProcess() && (recModified || forceSuffix))
                     {
                        rec.setMode(DataModificationTypes.Update);
                        dv.addCurrToModified();
                        dv.setChanged(true);

                        if (recModified && task.getMode() == Constants.TASK_MODE_QUERY)
                        {
                           if (rec.realModified() &&
                                 !ClientManager.Instance.getEnvironment().allowUpdateInQueryMode(task.getCompIdx()))
                           {
                              // if real fields were modified
                              dv.cancelEdit(REAL_ONLY, false);
                              Manager.WriteToMessagePanebyMsgId(task, MsgInterface.RT_STR_UPDATE_IN_QUERY, false);
                              ((MgForm)task.getForm()).RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                           }
                           // we must continue here and try to commit the transaction because
                           // there might be also some sub tasks to this task
                        }
                     }

                     //QCR#776295: for pre record update, commit the record after handler's execution.
                     // Fixed bug #:306929, ForceExitPre is relevant only for the task that raised the event
                     // see also bug#:292876
                     if (!isForceExitPreRecordUpdate(task))
                     {
                        if ((!recModeIsDelete || task.getMode() == Constants.TASK_MODE_DELETE) )
                           commitRecord(task, evt.reversibleExit());
                        else
                           // Fixed bug #306929 with delete subtask
                           task.TaskTransactionManager.ExecuteLocalUpdatesCommand();
                     }

                     if (!task.transactionFailed(ConstInterface.TRANS_RECORD_PREFIX) && !task.isAborting())
                     {
                        // check whether to evaluate the end condition after the record suffix
                        // in case there are 2 cycles (update/force suffix + delete), do the end task only in the 2nd cycle.
                        if ((rec.getMode() == DataModificationTypes.Delete &&
                              task.getMode() == Constants.TASK_MODE_DELETE) ||
                              rec.getMode() != DataModificationTypes.Delete)
                           if (task.evalEndCond(ConstInterface.END_COND_EVAL_AFTER))
                              task.endTask(true, false, false);
                     }
                  } // rec != null
                  break;

               case InternalInterface.MG_ACT_CTRL_PREFIX:
                  if (rcBefore)
                  {
                     // check parkability once again as property might have changed in the handler
                     // 924112 - ignore direction (if flag set) as we are forcing park inspite of failed direction
                     //QCR# 806015: If we have a single field with allow parking = Yes and Tab Into = No, then on pressing tab if cursor is not 
                     //moved, we come to park on lastParkedCtrl (which is same as ctrl) since TabInto is not allowed.
                     //So to avaoid infinite recursion, we should not again try to park on it.
                      if ((ctrl.IsParkable(ClientManager.Instance.MoveByTab) || ctrl == GUIManager.getLastFocusedControl()) &&
                          (ctrl.IgnoreDirectionWhileParking || ctrl.allowedParkRelatedToDirection()))
                      {
                          // set InControl true after Control Prefix
                          ctrl.InControl = true;

                          if (ctrl.ShouldRefreshOnControlEnter())
                             ((Field)ctrl.getField()).updateDisplay();

                          // if this code is reached then everything is ok in the control prefix
                          // user handler. Now, move the focus to the control.
                          task.setLastParkedCtrl(ctrl);
                          // we have parked on a ctrl set clicked ctrl to null
                          ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl = null;

                          // Refresh task mode on status bar.
                          ((MgForm)ctrl.getForm()).UpdateStatusBar(GuiConstants.SB_TASKMODE_PANE_LAYER, task.getModeString(), false);

                          // to take care of temporary editor
                          //Manager.setFocus(ctrl, ctrl.getDisplayLine(false));
                          ctrl.SetFocus(ctrl, ctrl.getDisplayLine(false), true, true);
                          // we have successfully parked on a ctrl, set setstopexecutionCtrl to null
                          setStopExecutionCtrl(null);

                          Manager.SetSelect(ctrl);
                          ClientManager.Instance.ReturnToCtrl = ctrl;
                          task.InCtrlPrefix = false;
                          task.setFlowMode(Flow.NONE);
                          selectProg(ctrl, true);
                      }
                      else
                      {
                          task.InCtrlPrefix = false;
                          moveToParkableCtrl(ctrl, true);
                      }
                  }

                  break;

               case InternalInterface.MG_ACT_CTRL_SUFFIX:
                  //disable actions and states. do it only if we are getting out of the control.
                  if (!GetStopExecutionFlag())
                  {
                     ctrl.enableTextAction(false);
                     if (ctrl.isTextControl() || ctrl.isRichEditControl() || ctrl.isChoiceControl())
                     {
                        MgControl nextControl = _nextParkedCtrl;

                        if (nextControl == null || !nextControl.IsDotNetControl())
                           //QCR #249107, when we are on our way to dotnet control we should not change language
                           ctrl.SetKeyboardLanguage(true);
                     }
                     ctrl.enableTreeEditingAction(false);
                     if (ctrl.Type == MgControlType.CTRL_TYPE_BUTTON)
                        task.ActionManager.enable(InternalInterface.MG_ACT_BUTTON, false);

                     if (task.getEnableZoomHandler() || ctrl.useZoomHandler())
                        task.ActionManager.enable(InternalInterface.MG_ACT_ZOOM, false);
                     task.ActionManager.enable(InternalInterface.MG_ACT_RT_COPYFLD, false);
                     task.ActionManager.enable(InternalInterface.MG_ACT_RT_EDT_NULL, false);

                     if (ctrl.isChoiceControl())
                        task.ActionManager.enable(InternalInterface.MG_ACT_ARROW_KEY, false);

                     if (ctrl.isTreeControl())
                     {
                        task.setKeyboardMappingState(Constants.ACT_STT_TREE_PARK, false);
                        task.ActionManager.enable(InternalInterface.MG_ACT_TREE_RENAME_RT, false);
                     }
                  }

                  if (rcBefore)
                  {
                     if (GetStopExecutionFlag())
                     {
                        // If control suffix get 'stop execution' (e.g. verify error), the focus will be remain on current control.
                        // So set InControl to true
                        ctrl.InControl = true;
                        return;
                     }

                     // reset the nextParkedCtrl (set in ClientManager.processFocus)
                     _nextParkedCtrl = null;

                     // Update fields that are assigned to a text control and with type that has default display 
                     // value (numeric, date, time, logical etc)
                     Field fld = (Field)ctrl.getField();
                     if (fld != null && (ctrl.Type == MgControlType.CTRL_TYPE_TEXT || ctrl.Type == MgControlType.CTRL_TYPE_TREE))
                        fld.updateDisplay();

                     Manager.SetUnselect(ctrl);
                     task.setLevel(Constants.TASK_LEVEL_RECORD);
                     ctrl.setInControlSuffix(false);

                     if (task.evalEndCond(ConstInterface.END_COND_EVAL_IMMIDIATE))
                        task.endTask(true, false, false);
                  }
                  break;

               case InternalInterface.MG_ACT_TASK_PREFIX:
                  task.setLevel(Constants.TASK_LEVEL_TASK);
                  break;

               case InternalInterface.MG_ACT_TASK_SUFFIX:
                  task.setLevel(Constants.TASK_LEVEL_NONE);
                  break;

               // simulates user asking for the next rec.
               case InternalInterface.MG_ACT_CYCLE_NEXT_REC:
                  var form = (MgForm)task.getForm();
                  if (!task.isMainProg() && form != null)
                  {
                     form.moveInView(form.isLineMode()
                                        ? Constants.MOVE_UNIT_ROW
                                        : Constants.MOVE_UNIT_PAGE,
                                     Constants.MOVE_DIRECTION_NEXT);
                     // end of data will signal us to end the task (unless in create mode).
                     if (task.getExecEndTask())
                        task.endTask(true, false, false);
                  }
                  break;

               // delete record in non interactive task
               case InternalInterface.MG_ACT_CYCLE_NEXT_DELETE_REC:
                  // check empty dataview before trying to delete, in case task started with empty dataview.
                  if (task.DataView.isEmptyDataview())
                     (task).setExecEndTask();
                  else
                     HandleActionDelline(evt, task, dv, rec, true);

                  // end of data will signal us to end the task (unless in create mode).
                  if (task.getExecEndTask())
                     task.endTask(true, false, false);
                  break;


#if !PocketPC
               case InternalInterface.MG_ACT_BEGIN_DRAG:
                  if (propagate)
                     Manager.DragSetData(ctrl, evt.getDisplayLine());

                  Manager.BeginDrag(ctrl, task.getTopMostForm(), evt.getDisplayLine());
                  break;
#endif
               // !PocketPC
            }
         }
      }

      /// <summary>
      ///   creates a non reversible event and handle it
      /// </summary>
      /// <param name = "task">a reference to the task </param>
      /// <param name = "eventCode">the code of the event </param>
      internal void handleNonReversibleEvent(Task task, int eventCode)
      {
         RunTimeEvent rtEvt;
         bool exitCommand = (eventCode == InternalInterface.MG_ACT_EXIT || eventCode == InternalInterface.MG_ACT_CLOSE);

         rtEvt = new RunTimeEvent(task);
         rtEvt.setInternal(eventCode);
         if (exitCommand)
            _isNonReversibleExit = true;
         try
         {
            rtEvt.setNonReversibleExit();
            rtEvt.setCtrl((MgControl)task.getLastParkedCtrl());
            rtEvt.setArgList(null);
            handleEvent(rtEvt, false);
         }
         finally
         {
            if (exitCommand)
               _isNonReversibleExit = false;
         }
      }

      /// <summary>
      ///   creates a new internal Event and handle it
      /// </summary>
      /// <param name = "task">a reference to the task</param>
      /// <param name = "eventCode">the code of the event</param>
      /// <param name = "isQuit">indicates whether MG_ACT_CANCEL is called from MG_ACT_RT_QUIT</param>
      internal void handleInternalEvent(Task task, int eventCode, EventSubType eventSubType)
      {
         var lastParkedCtrl = (MgControl)task.getLastParkedCtrl();
         RunTimeEvent rtEvt;

         if (eventCode < InternalInterface.MG_ACT_TOT_CNT && task != null && !(eventSubType == EventSubType.CancelIsQuit) && !task.ActionManager.isEnabled(eventCode))
            return;

         if (lastParkedCtrl != null)
            handleInternalEvent(lastParkedCtrl, eventCode, eventSubType);
         else
         {
            if (eventCode == InternalInterface.MG_ACT_RT_QUIT)
               handleInternalEvent(task, InternalInterface.MG_ACT_CANCEL, EventSubType.CancelIsQuit);
            else
            {
               rtEvt = new RunTimeEvent(task);
               rtEvt.setInternal(eventCode);
               rtEvt.SetEventSubType(eventSubType);
               if (InternalInterface.BuiltinEvent(eventCode))
                  handleEvent(rtEvt, false);
               else
                  commonHandler(rtEvt);
            }
         }
      }

      /// <summary>
      ///   creates a new internal Event and handle it
      /// </summary>
      /// <param name = "ctrl">a reference to the control</param>
      /// <param name = "eventCode">the code of the event</param>
      /// <param name = "isQuit">indicates whether MG_ACT_CANCEL is called from MG_ACT_RT_QUIT</param>
      internal void handleInternalEvent(MgControl ctrl, int eventCode, EventSubType eventSubType)
      {
         RunTimeEvent rtEvt;

         if (eventCode == InternalInterface.MG_ACT_RT_QUIT)
            handleInternalEvent(ctrl, InternalInterface.MG_ACT_CANCEL, EventSubType.CancelIsQuit);
         else
         {
            rtEvt = new RunTimeEvent(ctrl);
            rtEvt.setInternal(eventCode);
            rtEvt.SetEventSubType(eventSubType); // (use for act_cancel & act_rt_view_refresh
            // basically MG_ACT_VIEW_REFRESH represent the view refresh event raised by the user
            // however we use internally in the code to refresh the display (mainly in sub forms refreshes)
            // in order the distinguish between the real use and the internal use we set this flag to false
            // we need to distinguish between the two since we make different actions when caching a subform
            if (eventCode == InternalInterface.MG_ACT_RT_REFRESH_VIEW)
               rtEvt.setIsRealRefresh(false);

            if (InternalInterface.BuiltinEvent(eventCode))
               handleEvent(rtEvt, false);
            else
               commonHandler(rtEvt);
         }
      }

      /// <summary>
      ///   returns the current field
      /// </summary>
      internal Field getCurrField()
      {
         return _currField;
      }

      /// <summary>
      ///   returns current control
      /// </summary>
      protected internal MgControl getCurrCtrl()
      {
         return _currCtrl;
      }

      /// <summary>
      ///   returns the current task
      /// </summary>
      internal Task getCurrTask()
      {
         if (_currField == null)
            return null;
         return (Task)_currField.getTask();
      }

      /// <summary>
      ///   set the "stop execution" flag to true
      /// </summary>
      internal void setStopExecution(bool stop)
      {
         setStopExecution(stop, true);
      }

      /// <summary>
      ///   set the "stop execution" flag to true
      /// </summary>
      internal void setStopExecution(bool stop, bool clearSrvrEvents)
      {
         if (stop)
            if (clearSrvrEvents)
               _eventsQueue.clear();
            else
            {
               var tmpVec = new List<RunTimeEvent>();
               RunTimeEvent rtEvt;
               int i;

               // We were requested to leave the "server events" in the queue.
               // Do it, but also remove the "protection" from these events so next
               // time we need to clear the queue they will also be deleted.
               while (!_eventsQueue.isEmpty())
               {
                  rtEvt = (RunTimeEvent)_eventsQueue.poll();
                  if (rtEvt.isFromServer())
                  {
                     rtEvt.resetFromServer();
                     tmpVec.Add(rtEvt);
                  }
               }

               for (i = 0;
                    i < tmpVec.Count;
                    i++)
                  _eventsQueue.put(tmpVec[i]);
            }
         _stopExecution = stop;
      }

      /// <summary>
      ///   set the ctrl on which we had a "stop execution"
      /// </summary>
      internal void setStopExecutionCtrl(MgControl ctrl)
      {
         if (_stopExecutionCtrl == null || ctrl == null)
            _stopExecutionCtrl = ctrl;
      }

      /// <summary>
      ///   delete the current record
      /// </summary>
      /// <param name = "task">the task to refer to </param>
      /// <param name = "inForceDel">true if the delete is done because of force delete </param>
      private void delCurrRec(Task task, bool inForceDel)
      {
         var dv = (DataView)task.DataView;

         task.setMode(Constants.TASK_MODE_MODIFY);

         // delete the record and run record suffix for the new current record
         ((MgForm)task.getForm()).delCurrRec();

         // no curr-rec ==> we deleted the last record and failed to create a new one (e.g.
         // CREATE mode is not allowed) ==> end the task. An exception to the rule is when
         // we are already in the process of creating a record.
         if (!task.DataView.isEmptyDataview())
         {
            if (dv.getCurrRecIdx() >= 0)
            {
               // QCR #656495: If we got to this point and the current record is still the
               // deleted one then the record prefix will be handled elsewhere.
               if (((Record)dv.getCurrRec()).getMode() != DataModificationTypes.Delete && !inForceDel)
               {
                  var ctrl = (MgControl)task.getLastParkedCtrl();
                  handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
                  if (!GetStopExecutionFlag())
                     if (ctrl == null)
                        task.moveToFirstCtrl(true);
                     else
                        handleInternalEvent(ctrl, InternalInterface.MG_ACT_CTRL_PREFIX);
               }
            }
            else if (!task.getInCreateLine())
               handleNonReversibleEvent(task, InternalInterface.MG_ACT_EXIT);
         }
         else
         {
            HandleNonParkableControls(task);
            handleInternalEvent(task, InternalInterface.MG_ACT_REC_PREFIX);
         }
      }

      /// <summary>
      /// execute a select program and returns true if the program was executed  </summary>
      /// <param name="ctrl">the source control</param>
      /// <param name="prompt">if true then the caller is executing the called program at the end of control prefix</param>
      /// <returns></returns>
      protected bool selectProg(MgControl ctrl, bool prompt)
      {
         IClientCommand cmd;
         char selectMode = '0';
         String value, mgVal;
         bool progExecuted = false;
         var task = (Task)ctrl.getForm().getTask();

         // check if there is select program to the control
         if (ctrl.HasSelectProgram())
         {
            //check if the control is parkable
            if (ctrl.IsParkable(false))
            {
               // Get the select mode:
               // SELPRG_MODE_PROMPT - when get into the control open the select program
               // SELPRG_MODE_AFTER  - when select program was called park on the next control
               // SELPRG_MODE_BEFORE - when select program was called stay on the current control
               selectMode = ctrl.GetSelectMode();
               if (prompt)
               {
                  if (selectMode == Constants.SELPRG_MODE_PROMPT)
                  {
                     // yeah, it's odd, but this is the way online task works (Ehud)
                     handleInternalEvent(ctrl, InternalInterface.MG_ACT_ZOOM);
                     progExecuted = true;
                     // #299198 select the control again after a selection prog
                     // is executed in prompt mode
                     Manager.SetSelect(ctrl);
                  }
               }
               else
               {
                  value = Manager.GetCtrlVal(ctrl);

                  // validate the controls value
                  if (!ctrl.validateAndSetValue(value, false))
                     setStopExecution(true);
                  else
                  {
                     String encodedVal = null;
                     mgVal = ctrl.getMgValue(value);
                     var rec = ((DataView)task.DataView).getCurrRec();
                     encodedVal = rec.getXMLForValue(ctrl.getField().getId(), mgVal);
                     cmd = CommandFactory.CreateExecOperCommand(task.getTaskTag(), null, Int32.MinValue, ctrl.getDitIdx(), encodedVal);
                     task.getMGData().CmdsToServer.Add(cmd);

                     // get the task ID from the property
                     Property prop = ctrl.getProp(PropInterface.PROP_TYPE_SELECT_PROGRAM);
                     TaskDefinitionId taskDefinitionId = prop.TaskDefinitionId;

                     CommandsProcessorBase commandsProcessorServer = CommandsProcessorManager.GetCommandsProcessor(taskDefinitionId);
                     commandsProcessorServer.Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
                     //if (ctrl.getTask().hasSubTasks())
                     //   ctrl.setDoNotFocus();
                     if (selectMode == Constants.SELPRG_MODE_AFTER)
                        handleInternalEvent(ctrl, InternalInterface.MG_ACT_TBL_NXTFLD);
                     progExecuted = true;
                  }
               }
            }
         }
         return progExecuted;
      }

      /// <summary>
      ///   Executes CV for subforms.
      /// 
      ///   For eg:
      ///   /\
      ///   \
      ///   Subform1
      ///   /        \
      ///   subform2       subform3 (with CommonParent to LastParked Ctrl)
      ///   /                 \
      ///   subform4 with Last         subform4
      ///   parked ctrl                   \
      ///   subform5
      ///   /     \
      ///   subform6    subform7 with DestCtrl
      /// 
      ///   Execution of CVs for Subforms is done in three steps :
      ///   Step 1 : Build the 'subformCtrl path' after the srcCtrl's form or commmon
      ///   parent's form to the subformCtrl with the destCtrl
      ///   eg: subformCtrl path = subform4, subform5 and subform7
      ///   Step 2 : if LastParked Ctrl is on a form having Subform3, execute CV for the form.
      ///   Step 3 : For each subformctrl in 'subformCtrl path', execute CVs of ctrls
      ///   in subformCtrl's form.
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "destCtrl"></param>
      /// <returns></returns>
      private bool executeVerifyHandlerSubforms(Task task, MgControl destCtrl)
      {
         Task pTask = null, pSubformCtrlTask = null;
         MgForm pForm = null;
         bool bRc = true;
         MgControl srcCtrl, frmCtrl, pSubformCtrl;
         MgControl pSubformCommonParent; // parentsubfrm of destCtrl with common parent to srcCtrl 
         List<MgControlBase> subformPathList;
         MgControl lastParkedCtrl = GUIManager.getLastFocusedControl();

         if (lastParkedCtrl == null || !destCtrl.onDiffForm(lastParkedCtrl))
            return bRc;

         // since there is zoom in or out of the subform, task of the destCtrl must have
         // currVerifyCtrl to null so as to begin CV execution from first Ctrl.
         task.setCurrVerifyCtrl(null);

         if (!destCtrl.getForm().isSubForm())
            return bRc;

         pTask = (Task)lastParkedCtrl.getForm().getTask();
         pForm = (MgForm)pTask.getForm();

         pTask.setFlowMode(Flow.NONE);
         pTask.setDirection(Direction.NONE);
         pTask.setFlowMode(task.getFlowMode());
         pTask.setDirection(task.getDirection());

         srcCtrl = (MgControl)pTask.getCurrVerifyCtrl(); // lastParkedCtrl;
         subformPathList = new List<MgControlBase>();

         // STEP 1: For subforms tree, build a subformCtrl path to execute CVs of
         // ctrls in the form after the srcCtrl's form or commmon parent's form
         // to the subformCtrl with the destCtrl.
         pSubformCommonParent = (MgControl)buildSubformPath(subformPathList, lastParkedCtrl, destCtrl);

         // STEP 2: Execute CVs of the form containing the srcCtrl (lastparked ctrl)
         // only if srcCtrl is on parentForm or parents of parentForm containig destCtrl.
         // It execute CVs of all ctrl after SrcCtrl til the lastCtrl on the form or
         // the ctrl after which focus switched into subform tree with dest ctrl.
         // Note: a. srcCtrl should not be the last control.
         // b. srcCtrl shud not be lower in subform tree than destCtrl (backward direction click).
         // c. srcCtrl and destCtrl shud not be on diff subform tree
         if (srcCtrl != pForm.getLastNonSubformCtrl() && pSubformCommonParent != null
             && !lastParkedCtrl.onDiffForm(pSubformCommonParent))
         {
            frmCtrl = ((MgForm)pTask.getForm()).getNextCtrlIgnoreSubforms(srcCtrl, Direction.FORE);
            if (frmCtrl != null && pForm.ctrlTabOrderIdx(destCtrl) < pForm.ctrlTabOrderIdx(frmCtrl))
               frmCtrl = null;

            // execute verify handlers for ctrls on the form having lastParked ctrl
            bRc = executeVerifyHandlerSubform(pTask, frmCtrl, destCtrl);
         }

         // STEP 3: For each subformctrl of 'SubformCtrl' array, execute CVs of ctrls
         // in subformctrl's form.
         for (int index = subformPathList.Count - 1;
              bRc && index >= 0;
              index--)
         {
            pSubformCtrl = (MgControl)subformPathList[index];
            pSubformCtrlTask = (Task)pSubformCtrl.getForm().getTask();

            // execute RP of 'pSubformCtrl' task and all tasks in between
            execSubformRecPrefix(pSubformCtrl, destCtrl);
            if (GetStopExecutionFlag())
            {
               bRc = false;
               break;
            }

            frmCtrl = ((MgForm)pSubformCtrlTask.getForm()).getFirstNonSubformCtrl();

            // execute verify handlers of ctrls on the form having
            // 'pSubformCtrl' subform ctrl
            bRc = executeVerifyHandlerSubform(pSubformCtrlTask, frmCtrl, destCtrl);
         }

         // execute RP of all tasks between Parent task and Current CP's task
         if (bRc)
         {
            Task parentTask = null;
            // If there is no 2+level nesting, use lastparked task. Otherwise, the last executed subformCtrl's task.
            if (pSubformCtrlTask == null)
               parentTask = pTask;
            else
               parentTask = pSubformCtrlTask;

            // execute RP of tasks between 'parentTask' and current Task
            executeAllRecordPrefix(parentTask, task, destCtrl, true);
            if (GetStopExecutionFlag())
               bRc = false;
         }

         pTask.setFlowMode(Flow.NONE);
         pTask.setDirection(Direction.NONE);

         return bRc;
      }

      /// <summary>
      ///   executes RecPrefix of subformCtrl's task, and (for 2+ level nested subform only) we execute RP
      ///   between the subformCtrl's task and its parent subformCtrl's task.
      /// </summary>
      /// <param name = "subformCtrl"></param>
      /// <param name = "destCtrl"> To execute CVs, this ctrl is used to compare taborder of RP task. If RP task less, CV fired.</param>
      private void execSubformRecPrefix(MgControl subformCtrl, MgControl destCtrl)
      {
         var subformCtrlTask = (Task)subformCtrl.getForm().getTask();
         var parentSubform = (MgControl)subformCtrl.getForm().getSubFormCtrl();

         if (parentSubform != null)
         {
            // execute RP of tasks in between.
            // For 2+ level nested subform, we execute RP between the subformCtrl's task and its parent subformCtrl's task.
            executeAllRecordPrefix((Task)parentSubform.getForm().getTask(), subformCtrlTask, destCtrl, true);
            if (GetStopExecutionFlag())
               return;
         }

         // execute RP of subformCtrl task
         handleInternalEvent(subformCtrlTask, InternalInterface.MG_ACT_REC_PREFIX);
      }

      /// <summary>
      ///   executes RP between 'parentTask' and 'childTask', and for each task, execute CVs from First ctrl till last ctrl,
      ///   only if taborder of task is less than destCtrl.
      /// </summary>
      /// <param name = "parentTask"></param>
      /// <param name = "childTask"></param>
      /// <param name = "destCtrl"> To execute CVs, this ctrl is used to compare taborder of RP task. If RP task less, CV fired.</param>
      private void executeAllRecordPrefix(Task parentTask, Task childTask, MgControl destCtrl, bool syncParent)
      {
         var taskPathList = new List<Task>();
         var task = (Task)childTask.getParent();

         if (task == parentTask)
            return;

         // get all task in between
         while (task != null && task != parentTask)
         {
            taskPathList.Add(task);
            task = (Task)task.getParent();
         }

         if (task != null)
         {
            bool bRc = true;

            for (int index = taskPathList.Count - 1;
                 index >= 0 && bRc;
                 index--)
            {
               Task pTask = taskPathList[index];

               if (syncParent)
                  doSyncForSubformParent(pTask);

               handleInternalEvent(pTask, InternalInterface.MG_ACT_REC_PREFIX);
               if (GetStopExecutionFlag())
                  break;

               // execute CVs from First ctrl till last ctrl, only if taborder is less than destCtrl
               if (destCtrl != null)
               {
                  MgControl firstCtrl = ((MgForm)pTask.getForm()).getFirstNonSubformCtrl();

                  if (firstCtrl != null && MgControl.CompareTabOrder(firstCtrl, destCtrl) < 0)
                     bRc = executeVerifyHandlersTillLastCtrl(pTask, null);
               }
            }
         }
      }

      /// <summary>
      ///   Executes CVs for a single subform
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "frmCtrl"></param>
      /// <param name = "destCtrl"></param>
      /// <returns></returns>
      private bool executeVerifyHandlerSubform(Task task, MgControl frmCtrl, MgControl destCtrl)
      {
         var pForm = (MgForm)task.getForm();
         bool bRc = true;
         MgControl curCtrl, toCtrl;

         // frmCtrl must be on the task
         if (frmCtrl == null || frmCtrl.getForm().getTask() != task)
            return bRc;

         curCtrl = frmCtrl;
         toCtrl = frmCtrl;
         // Find toCtrl as the lastctrl on the form after which we moved into subform.
         // Subfrom can be at any tab order and clicked and neednot be the last ctrl         
         while ((curCtrl = curCtrl.getNextCtrl()) != null && curCtrl != destCtrl)
         {
            if (!frmCtrl.onDiffForm(curCtrl))
               toCtrl = curCtrl;
         }
         if (curCtrl == null || toCtrl == null)
            toCtrl = pForm.getLastNonSubformCtrl();

         // frmCtrl and toCtrl shud always be in forwrd direction
         if (frmCtrl != null && toCtrl != null
             && pForm.ctrlTabOrderIdx(frmCtrl) <= pForm.ctrlTabOrderIdx(toCtrl))
            bRc = executeVerifyHandlers(task, null, frmCtrl, toCtrl);

         return bRc;
      }

      /// <summary>
      ///   finds the ctrl from which to start CV execution from
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "srcCtrl"></param>
      /// <returns></returns>
      private MgControl getCtrlExecCvFrom(Task task, MgControl srcCtrl, MgControl destCtrl)
      {
         MgControl execCvFromCtrl = null;
         var Form = (MgForm)task.getForm();
         MgControl firstNonSubFormCtrl = Form.getFirstNonSubformCtrl();

         if (srcCtrl == null)
         {
            MgControl lastParkedCtrl = GUIManager.getLastFocusedControl();

            // if subform and task contains lastparkedCtrl
            // if clicked on existing rec, we must find the fromCtrl based on direction on task. But if moved to a new
            // record, we must start from firstNonSubFormCtrl.
            if (lastParkedCtrl != null && destCtrl != null && lastParkedCtrl.onDiffForm(destCtrl)
                && Form.ctrlTabOrderIdx(lastParkedCtrl) > -1
                && Form.PrevDisplayLine == task.getForm().DisplayLine)
               execCvFromCtrl = Form.getNextCtrlIgnoreSubforms(lastParkedCtrl, task.getDirection());

            // #756675 - If subfrom, we can get execCvFromCtrl as null if we have tabbed from last ctrl on
            // subform and there is no control on the parent after this control. If no subfrom, we should
            // always set to firstNonSubFormCtrl.
            if (execCvFromCtrl == null)
               execCvFromCtrl = firstNonSubFormCtrl;
         }
         else if (task.getDirection() == Direction.FORE)
         {
            execCvFromCtrl = srcCtrl.getNextCtrl();
            // if srcCtrl is the last ctrl, start executing CV from firstCtrl
            if (execCvFromCtrl == null)
               execCvFromCtrl = firstNonSubFormCtrl;
         }
         else
         // back
         {
            if (srcCtrl == firstNonSubFormCtrl)
               execCvFromCtrl = null;
            else
               execCvFromCtrl = srcCtrl.getPrevCtrl();
         }

         // if next/prev ctrl clicked or tab/shifttab and next/prev ctrl is parkable, then
         // execCvFromCtrl must be reset to null
         if (execCvFromCtrl != null)
         {
            if (task.getFlowMode() == Flow.STEP)
            {
               if (execCvFromCtrl.IsParkable(true))
                  execCvFromCtrl = null;
            }
            else
            {
               var clickedCtrl = (MgControl)task.getClickedControl();

               if (clickedCtrl != null && clickedCtrl == execCvFromCtrl && execCvFromCtrl.IsParkable(false))
                  execCvFromCtrl = null;
            }
         }

         return execCvFromCtrl;
      }

      /// <summary>
      ///   finds the ctrl from which to end CV execution to
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "fromCtrl"></param>
      /// <param name = "destCtrl"></param>
      /// <returns></returns>
      private MgControl getCtrlExecCvTo(Task task, MgControl fromCtrl, MgControl destCtrl)
      {
         var form = (MgForm)task.getForm();
         MgControl execCvToCtrl = null;

         MgControl firstNonSubFormCtrl = form.getFirstParkNonSubformCtrl();
         MgControl lastNonSubFormCtrl = form.getLastParkNonSubformCtrl();

         // if fromCtrl is null, we don't want to execute CVs
         if (fromCtrl == null)
            return null;

         if (task.getFlowMode() == Flow.FAST)
         {
            var clickedCtrl = (MgControl)task.getClickedControl();

            // ensure direction is forward if CV executed from first ctrl
            if (fromCtrl == firstNonSubFormCtrl)
            {
               task.setDirection(Direction.NONE);
               task.setDirection(Direction.FORE);
            }

            if (task.isMoveInRow() && (task.getLastMoveInRowDirection() != Constants.MOVE_DIRECTION_BEGIN &&
                                       task.getLastMoveInRowDirection() != Constants.MOVE_DIRECTION_END) &&
                clickedCtrl != null)
            {
               MgControl nextCtrl = null;
               MgControl prevCtrl = null;

               if (!clickedCtrl.IsParkable(false))
               {
                  nextCtrl = form.getNextParkableCtrl(clickedCtrl);
                  prevCtrl = form.getPrevParkableCtrl(clickedCtrl);

                  // For subforms, ensure that NextParkable/PrevParkable is on same form as formCtrl
                  if (nextCtrl != null && nextCtrl.onDiffForm(clickedCtrl))
                     nextCtrl = null;
               }
               else
               {
                  nextCtrl = clickedCtrl;
                  prevCtrl = clickedCtrl;
               }

               if (task.getDirection() == Direction.FORE)
               {
                  if (nextCtrl != null)
                     execCvToCtrl = nextCtrl.getPrevCtrl();
                  else
                     execCvToCtrl = clickedCtrl;
               }
               else
               {
                  if (prevCtrl != null)
                     execCvToCtrl = prevCtrl.getNextCtrl();
                  else
                     execCvToCtrl = clickedCtrl;
               }
            }
            else
            {
               if (task.getDirection() == Direction.FORE)
                  execCvToCtrl = destCtrl.getPrevCtrl();
               else
                  execCvToCtrl = destCtrl.getNextCtrl();

               // perform check -- required for alt+left/right 
               // (case1: srcCtrl equals destctrl, case2: srctrl and destCtrl are back-to-back).
               // The fromCtrl-toCtrl direction should be same as that on task. 
               if (fromCtrl != null && execCvToCtrl != null)
               {
                  int compareResult = MgControl.CompareTabOrder(fromCtrl, execCvToCtrl);
                  if (task.getDirection() == Direction.FORE)
                  {
                     if (compareResult > 0)
                        execCvToCtrl = null;
                  }
                  else if (compareResult < 0)
                     execCvToCtrl = null;
               }
            }
         }
         else // step
         {
            if (task.getDirection() == Direction.FORE)
            {
               MgControl nextParkableCtrl = form.getNextParkableCtrl(fromCtrl);
               if (nextParkableCtrl != null)
                  execCvToCtrl = nextParkableCtrl.getPrevCtrl();
               else
                  execCvToCtrl = lastNonSubFormCtrl;
            }
            else
            {
               MgControl prevParkableCtrl = form.getPrevParkableCtrl(fromCtrl);
               // For subforms, ensure that PrevParkable is on same form as formCtrl
               if (prevParkableCtrl != null && !prevParkableCtrl.onDiffForm(fromCtrl))
                  execCvToCtrl = prevParkableCtrl.getNextCtrl();
               else
                  execCvToCtrl = firstNonSubFormCtrl;
            }
         }

         // All frmCtrl, toCtrl and destCtrl should not be equal. Either all these ctrl should
         // be different or any two match is allowed, but the third ctrl must be different.
         if (fromCtrl == execCvToCtrl && destCtrl == fromCtrl)
            execCvToCtrl = null;

         return execCvToCtrl;
      }

      /// <summary>
      ///   Execute the verify handlers from 'frmCtrl' to 'toCtrl' in the form.
      ///   And, if toCtrl's nextCtrl is not the 'destCtrl', executes verify handlers
      ///   from toCtrl's nextCtrl to destCtrl's prev/nextCtrl depending on direction.
      /// 
      ///   Note: This function executes CVs for ctrls in the current form, not for any
      ///   ctrls in any subformCtrl.
      /// 
      ///   For eg:
      ///   ctrls    a     b     c     d     e     f     g     h
      ///   X     X           X     X           X     X     (X are non parkables)
      ///   -------------------------------------------------------------------------
      ///   Seq.  Parked   Clicked/Tab    Clicked     DestCtrl    frmCtrl  toCtrl
      ///   on       /ShiftTab      Ctrl
      ///   -------------------------------------------------------------------------
      ///   1.    c        Click             d              f           d        e
      ///   2.    c        Click             f              f           d        e
      ///   3.    c        Click             h              f           d        h
      ///   4.    c        Click             a              c           b        a
      ///   5.    c        Tab               -              f           d        e
      ///   6.    f        Tab               -              null        g        h (executed from moveinrow)
      ///   c           a        b (on next/same rec)
      ///   7.    f        STab              -              c           e        d
      ///   8.    c        STab              -              c           b        a
      ///   9.    c        Click       f (next rec.)        null        d        h (executed from moveinrow)
      ///   f           a        e (on next. rec)
      ///   10.   c        Click       f (prev rec.)        f           a        e (on prev. rec)
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "aDstCtrl"></param>
      /// <param name = "frmCtrl"></param>
      /// <param name = "toCtrl"></param>
      /// <returns></returns>
      private bool executeVerifyHandlers(Task task, MgControl aDstCtrl, MgControl frmCtrl, MgControl toCtrl)
      {
         bool bRc = true;
         Direction direction = Direction.FORE; // forward
         MgControl currCtrl = null;
         MgControl dstCtrl = null;
         Field currFld = null;
         //bool executeForSameControl = false;

         var form = (MgForm)task.getForm();
         Flow originalFlowMode = 0;

         if (null == frmCtrl || null == toCtrl)
            return bRc;

         originalFlowMode = task.getFlowMode();

         if (frmCtrl == toCtrl)
         {
            direction = task.getDirection();
            if (direction == Direction.NONE)
               direction = Direction.FORE;
         }
         else if (form.ctrlTabOrderIdx(frmCtrl) > form.ctrlTabOrderIdx(toCtrl))
            direction = Direction.BACK;

         if (task != frmCtrl.getForm().getTask())
            frmCtrl = form.getNextCtrlIgnoreSubforms(frmCtrl, direction);
         if (task != toCtrl.getForm().getTask())
            toCtrl = form.getNextCtrlIgnoreSubforms(toCtrl,
                                                    (direction == Direction.FORE
                                                        ? Direction.BACK
                                                        : Direction.FORE));

         // Check to confirm frm-to direction is same as earlier 'direction' 
         if (null == frmCtrl || null == toCtrl)
            return bRc;
         else if (frmCtrl != toCtrl)
         {
            if (form.ctrlTabOrderIdx(frmCtrl) > form.ctrlTabOrderIdx(toCtrl))
            {
               if (direction == Direction.FORE)
                  return bRc;
            }
            else if (direction == Direction.BACK)
               return bRc;
         }

         currCtrl = frmCtrl;
         dstCtrl = toCtrl;

         if (currCtrl != null && dstCtrl != null && form.ctrlTabOrderIdx(currCtrl) > form.ctrlTabOrderIdx(dstCtrl))
            direction = Direction.BACK;

         if (dstCtrl == null)
            dstCtrl = form.getLastTabbedCtrl();

         task.setDirection(Direction.NONE);
         task.setDirection(direction);

         while (bRc && currCtrl != null && currCtrl != toCtrl)
         {
            if (currCtrl != null && (currCtrl.Type == MgControlType.CTRL_TYPE_BROWSER || currCtrl.getField() != null) &&
                currCtrl.getForm().getTask() == task)

               if (currCtrl == null)
                  break;

            currFld = (Field)currCtrl.getField();

            if (currFld == null || (currCtrl != null && currCtrl.getForm().getTask() != task))
            {
               currCtrl = form.getNextCtrlIgnoreSubforms(currCtrl, direction);
               continue;
            }

            // check for verify handlers
            if (currCtrl != null)
            {
               if (bRc && currCtrl.HasVerifyHandler)
                  bRc = executeVerifyHandler(currCtrl);
            }

            currCtrl = form.getNextCtrlIgnoreSubforms(currCtrl, direction);
         }

         // execute the CV for toCtrl
         if (bRc && toCtrl.HasVerifyHandler)
            bRc = executeVerifyHandler(toCtrl);

         currCtrl = form.getNextCtrlIgnoreSubforms(currCtrl, direction);

         // execute CVs if currCtrl is not the destCtrl
         // eg. tabbing/click when the all next/prev ctrls are not parkable
         if (bRc && aDstCtrl != null && toCtrl != null && currCtrl != aDstCtrl)
         {
            Direction originalDirection = task.getDirection();
            MgControl fCtrl = null;
            MgControl tCtrl = null;
            bool cycleRecMain = form.isRecordCycle();

            // cycleRecMain is set and all ctrls after it is not parkable. If tabbed,
            // CV from srcCtrl to last ctrl is already executed. Cvs form firstCtrl
            // to destCtrl must be executed.
            if (task.getFlowMode() == Flow.STEP && direction == Direction.FORE && cycleRecMain)
            {
               fCtrl = getCtrlExecCvFrom(task, null, null);
               tCtrl = getCtrlExecCvTo(task, fCtrl, aDstCtrl);
            }
            else if (direction == Direction.FORE)
            {
               // if destCtrl is between frmCtrl and toCtrl, we must execute Cvs from
               // toCtrl's prev to destCtrl's next. reset Direction to FLOW_BACK
               if (toCtrl.getPrevCtrl() != aDstCtrl)
               {
                  fCtrl = toCtrl.getPrevCtrl();
                  tCtrl = aDstCtrl.getNextCtrl();
               }
               direction = Direction.BACK;
            }
            else
            {
               // if destCtrl is between toCtrl and frmCtrl, we must execute Cvs from
               // toCtrl's next to destCtrl's prev. reset Direction to Direction.FORE
               if (toCtrl.getNextCtrl() != aDstCtrl)
               {
                  fCtrl = toCtrl.getNextCtrl();
                  tCtrl = aDstCtrl.getPrevCtrl();
               }
               direction = Direction.FORE;
            }

            // set the new direction
            task.setDirection(Direction.NONE);
            task.setDirection(direction);

            // pass null as second parametr to avoid recursion
            if (fCtrl != null && tCtrl != null)
               bRc = executeVerifyHandlers(task, null, fCtrl, tCtrl);

            task.setDirection(Direction.NONE);
            task.setDirection(originalDirection);
         }

         task.setFlowMode(Flow.NONE);
         task.setFlowMode(originalFlowMode);

         return bRc;
      }

      /// <summary>
      ///   execute control verification for 'ctrl'
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <returns></returns>
      private bool executeVerifyHandler(MgControl ctrl)
      {
         bool bRc = true;

         handleInternalEvent(ctrl, InternalInterface.MG_ACT_CTRL_VERIFICATION);
         if (GetStopExecutionFlag())
            return false;

         return bRc;
      }

      /// <summary>
      ///   process users confirmation
      /// </summary>
      /// <param name = "task">to check confirm update</param>
      /// <returns> true - process command, false - stop command processing</returns>
      private bool updateConfirmed(Task task)
      {
         bool confirm;
         char answer;
         int ans;

         if (task == null)
            return false;

         confirm = task.checkProp(PropInterface.PROP_TYPE_CONFIRM_UPDATE, false);
         if (confirm && !task.DataSynced)
         {
            ans = GUIManager.Instance.confirm((MgForm)task.getForm(), MsgInterface.CRF_STR_CONF_UPD,
                                      Styles.MSGBOX_ICON_QUESTION | Styles.MSGBOX_BUTTON_YES_NO_CANCEL);
            if (ans == Styles.MSGBOX_RESULT_YES)
               answer = 'Y';
            // Yes
            else if (ans == Styles.MSGBOX_RESULT_NO)
               answer = 'N';
            // No
            else
               answer = 'C'; // Cancel
         }
         else
            answer = 'Y';

         switch (answer)
         {
            case 'Y':
               break;

            case 'N':
               ((MgForm)task.getForm()).cancelEdit(false, false);
               ((MgForm)task.getForm()).RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
               task.ConfirmUpdateNo = true;
               return false; // not continue record suffix

            case 'C':
               setStopExecution(true);
               setStopExecutionCtrl(GUIManager.getLastFocusedControl());
               Manager.SetFocus(GUIManager.getLastFocusedControl(), -1);
               return false; // stop event processing

            default:
               Logger.Instance.WriteExceptionToLog("in ClientManager.Instance.updateConfirmed() illegal confirmation code: " +
                                              confirm);
               break;
         }
         return true;
      }

      /// <summary>
      ///   Set version number
      /// </summary>
      /// <param name = "ver">version number</param>
      internal void setVer(int ver)
      {
         _ver = ver;
      }

      /// <summary>
      ///   Set subversion number
      /// </summary>
      /// <param name = "subver">subversion number</param>
      internal void setSubVer(int subver)
      {
         _subver = subver;
      }

      /// <summary>
      ///   Set the order of handling main programs of components
      /// </summary>
      internal void setCompMainPrgTab(CompMainPrgTable compMainPrgTable)
      {
         _compMainPrgTab = compMainPrgTable;
      }

      /// <summary>
      ///   set end of work flag
      /// </summary>
      internal void setEndOfWork(bool endOfWork_)
      {
         _endOfWork = endOfWork_;
      }

      /// <summary>
      ///   get end of work flag
      /// </summary>
      internal bool getEndOfWork()
      {
         return _endOfWork;
      }

      /// <summary>
      ///   get the order of handling main programs of components
      /// </summary>
      internal CompMainPrgTable getCompMainPrgTab()
      {
         return _compMainPrgTab;
      }

      /// <summary>
      ///   get SubVersion number
      /// </summary>
      internal int getSubVer()
      {
         return _subver;
      }

      /// <summary>
      ///   get Version number
      /// </summary>
      internal int getVer()
      {
         return _ver;
      }

      /// <summary>
      ///   get the reversible exit flag
      /// </summary>
      internal bool inNonReversibleExit()
      {
         return _isNonReversibleExit;
      }

      /// <summary>
      ///   push a runtime event to the stack
      /// </summary>
      /// <param name = "rtEvt">the runtime event to push on the stack</param>
      internal void pushRtEvent(RunTimeEvent rtEvt)
      {
         _rtEvents.Push(rtEvt);
      }

      /// <summary>
      ///   pop a runtime event from the stack and return it
      /// </summary>
      internal RunTimeEvent popRtEvent()
      {
         if ((_rtEvents.Count == 0))
            return null;
         return (RunTimeEvent)_rtEvents.Pop();
      }

      /// <summary>
      ///   return the runtime event located on the top of the stack
      /// </summary>
      internal RunTimeEvent getLastRtEvent()
      {
         if ((_rtEvents.Count == 0))
            return null;
         return (RunTimeEvent)_rtEvents.Peek();
      }

      /// <summary>
      ///   set the value of a flag which indicates whether we should ignore unknown commands
      ///   arriving from the engine.
      /// </summary>
      internal void setIgnoreUnknownAbort(bool val)
      {
         _ignoreUnknownAbort = val;
      }

      /// <summary>
      ///   returns the value of the "ignore unknown abort" flag
      /// </summary>
      internal bool ignoreUnknownAbort()
      {
         return _ignoreUnknownAbort;
      }

      /// <summary>
      ///   compute help URL
      /// </summary>
      /// <param name = "current">control</param>
      protected internal String getHelpUrl(MgControl ctrl)
      {
         String helpIdx = null;
         MgForm form;

         if (ctrl != null)
         {
            helpIdx = ctrl.getHelpUrl();
            if (helpIdx == null && ctrl.getForm().getContainerCtrl() != null)
            {
               // if the control is descenden of the container control then we need to take the help from the
               // container control.
               var ContainerControl = (MgControl)ctrl.getForm().getContainerCtrl();
               if (ctrl.isDescendentOfControl(ContainerControl))
                  helpIdx = ContainerControl.getHelpUrl();
            }
            if (helpIdx == null)
            {
               form = (MgForm)ctrl.getForm();
               if (form != null)
                  helpIdx = form.getHelpUrl();
            }
         }
         return helpIdx;
      }

      /// <summary>
      ///   recover from a failure to overlay a task. When the server orders the client to run a
      ///   new task in a frame which already contains an active task, the client tries to term the
      ///   existing task and replace him. When this action fails, the client has to ask the server
      ///   to terminate the NEW TASK, and ignore the overlay action.
      /// </summary>
      /// <param name = "taskid">- the id of the NEW TASK</param>
      /// <param name = "mgd">   - the MGData of the OLD TASK.</param>
      internal void failTaskOverlay(String taskId, MGData mgd)
      {
         IClientCommand cmd;

         if (taskId != null)
         {
            // The server will return us 'abort' commands for the new task. Ignore them, since it wasnt started yet
            setIgnoreUnknownAbort(true);
            try
            {
               cmd = CommandFactory.CreateNonReversibleExitCommand(taskId, false);
               mgd.CmdsToServer.Add(cmd);
               RemoteCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);
            }
            finally
            {
               setIgnoreUnknownAbort(false);
            }
         }
      }

      /// <summary>
      ///   abort an overlayed MGData and return true on success
      /// </summary>
      /// <param name = "mgd">the MGData to abort</param>
      /// <param name = "newId">the overlaying task id, or NULL if there is no overlaying task</param>
      internal bool abortMgd(MGData mgd, String newId)
      {
         bool orgStopExecution = GetStopExecutionFlag();

         setStopExecution(false);

         mgd.getFirstTask().endTask(true, false, false);

         // The server may open a new task which the client would use to overlay
         // another existing task. This overlay may fail if closing the task failed.
         if (GetStopExecutionFlag())
         {
            // fail overlaying task only if there is one
            if (newId != null)
               failTaskOverlay(newId, mgd);

            return false;
         }
         else
            setStopExecution(orgStopExecution);

         return true;
      }

      /// <summary>
      ///   returns TRUE if current event should be propogtated to all tasks in a specific transaction
      /// </summary>
      internal bool isEventScopeTrans()
      {
         return (_eventScope == EventScope.TRANS);
      }

      /// <summary>
      ///   set event scope to TRANSACTION
      /// </summary>
      internal void setEventScopeTrans()
      {
         _eventScope = EventScope.TRANS;
      }

      /// <summary>
      ///   set event scope to NON TRANSACTION
      /// </summary>
      internal void clearEventScope()
      {
         _eventScope = EventScope.NONE;
      }

      /// <summary>
      ///   get time of fist event in sequence
      /// </summary>
      /// <returns> time of fitst event in sequence</returns>
      internal long GetEventTime()
      {
         return _eventsQueue.GetTime();
      }

      /// <param name = "task"></param>
      /// <param name = "kbItm"></param>
      /// <returns> the last enabled action for the specific key (in kbItem)</returns>
      /// <checkAccessKeyInMenus:>  if to check the keyboard in the menu </checkAccessKeyInMenus:>
      internal int getMatchingAction(Task task, KeyboardItem kbItm, bool checkAccessKeyInMenus)
      {
         int kbdStates;
         List<KeyboardItem> kbItemArray;
         int act = 0;
         int maxActCount = 0;
         int actCount;
         int actId;
         MenuEntry menuEntry = null;
         Task currTask = task;

         //when menu is define with no keyboard define, the kbItem is return null;
         if (kbItm == null)
            return act;

         // check the keyboard in the menu and process the on selection in the osCoommand & program & sys & user event.
         // a kbItm with MG_ACT_CHAR in it (i.e a char is to be writen), don't bother to check for access key. (Performance)
         if (checkAccessKeyInMenus && kbItm.getAction() != InternalInterface.MG_ACT_CHAR)
         {
            var topMostForm = (MgForm)task.getTopMostForm();
            if (topMostForm != null && topMostForm.ConcreteWindowType != WindowType.Modal)
               topMostForm = (MgForm)topMostForm.getTopMostFrameForm();
            if (topMostForm != null)
            {
               menuEntry = ((MgFormBase)topMostForm).getMenuEntrybyAccessKey(kbItm, MenuStyle.MENU_STYLE_PULLDOWN);
               currTask = (Task)topMostForm.getTask();
            }

            //fixed bug #:988877, need to check the keyboard on the context menu of the last control
            if (menuEntry == null)
            {
               MgControl mgControl = GUIManager.getLastFocusedControl(task.getMGData().GetId());
               if (mgControl != null)
                  menuEntry = mgControl.getContextMenuEntrybyAccessKeyToControl(kbItm);
            }

            if (menuEntry != null)
            {
               if (menuEntry.menuType() == GuiMenuEntry.MenuType.PROGRAM)
                  MenuManager.onProgramMenuSelection(task.ContextID, (MenuEntryProgram)menuEntry, (MgForm)currTask.getForm(),
                                                      (topMostForm != null && topMostForm.IsMDIFrame));
               else if (menuEntry.menuType() == GuiMenuEntry.MenuType.OSCOMMAND)
                  MenuManager.onOSMenuSelection(task.ContextID, (MenuEntryOSCommand)menuEntry, (MgForm)task.getForm());
               else if (menuEntry.menuType() == GuiMenuEntry.MenuType.SYSTEM_EVENT ||
                        menuEntry.menuType() == GuiMenuEntry.MenuType.USER_EVENT)
                  MenuManager.onEventMenuSelection((MenuEntryEvent)menuEntry, (MgForm)task.getForm(), currTask.getCtlIdx());
               // MENU_TYPE_INTERNAL_EVENT
               else
               {
               } // will be handle down

               if (!(menuEntry is MenuEntryEvent))
                  return act;
            }
         }

         // in case the there is act_char on the kbItm, no need to search.
         // we had a problem, some chars has the same keycode of other keyboard keys after translating to magic codes.
         // for example: char ' is keycode 39. right arrow is also trans in kbdConvertor to 39,
         // so ' might be mistaken for right arrow. so here, if we have an action of act_char we know its '.
         // NO kbd combination that prduces a character can be mapped into a magic action. this is why
         // we can do it. (only exceptions are 'plus' and 'minus' on keypad (todo)).
         if (kbItm.getAction() == InternalInterface.MG_ACT_CHAR)
            act = InternalInterface.MG_ACT_CHAR;
#if PocketPC
         else if (kbItm.getKeyCode() == GuiConstants.KEY_ACCUMULATED)
         act = InternalInterface.MG_ACT_MASK_CONTROL_TEXT;
#endif
         else
         {
            kbdStates = task.getKeyboardMappingState();
            kbItemArray = ClientManager.Instance.getKbdMap().getActionArrayByKeyboard(kbItm.getKeyCode(), kbItm.getModifier());
            if (kbItemArray != null)
            {
               for (int i = 0;
                    i < kbItemArray.Count;
                    i++)
               {
                  KeyboardItem kbdItem = kbItemArray[i];

                  actId = kbdItem.getAction();
                  if (task.ActionManager.isEnabled(actId) && (kbdStates == (kbdItem.getStates() | kbdStates)))
                  {
                     actCount = task.ActionManager.getActCount(actId);
                     if (actCount > maxActCount)
                     {
                        //new definition : SDI windows will not be closed by pressing Escape
                        // Unless the program was run from studio (Qcr 998911, 763547)
                        if (actId == InternalInterface.MG_ACT_EXIT && kbItm.getKeyCode() == GuiConstants.KEY_ESC)
                        {
                           var form = (MgForm)task.getForm();
                           if (form != null && !form.wideIsOpen())
                           {
                              if (form.getTopMostForm().IsMDIOrSDIFrame && !ClientManager.StartedFromStudio)
                                 continue;
                           }
                        }
                        maxActCount = actCount;
                        act = actId;
                     }
                  }
               }
            }
         }

         // There was no kbd mapping the that key, but it still belongs to an action.
         // see if the kbItm hold an action (originally made for MG_ACT_CHAR).
         if (act == 0)
         {
            //if menu was found and it is internal event put action to the queue only if there was no kbd mapping the that key.           
            if (checkAccessKeyInMenus && menuEntry != null &&
                menuEntry.menuType() == GuiMenuEntry.MenuType.INTERNAL_EVENT)
               MenuManager.onEventMenuSelection((MenuEntryEvent)menuEntry, (MgForm)task.getForm(),
                                                task.getCtlIdx());
            else if (kbItm.getAction() != 0)
               act = kbItm.getAction();
            // this is only TEMP hardcoded. the action will need to be in the keyboard mapping
            // also a change to the online (if time permits) also to turn the toggle insert into an act.
            else if (kbItm.getKeyCode() == 45 && kbItm.getModifier() == Modifiers.MODIFIER_NONE && kbItm.getStates() == 0)
               act = InternalInterface.MG_ACT_TOGGLE_INSERT;
         }

         act = GetNavigationAction(task, kbItm, act);

         return (act);
      }

      /// <summary>
      /// Get correct navigation action (Similar to Mainexe.cpp --> NavigationActsCallback)
      /// CTRL + TAB and SHIFT + CTRL + TAB actions are used for navigating between two windows and as well in different tabs in tab control. 
      /// 
      /// In online we traverse through each taskRt in TaskInfoPtr, until we get reach task with WindowType = NON CHILD WINDOW. This isn't handle for RIA.
      /// This is because in online when a parent task has a tab control and if CTRL+TAB is pressed, ACT_NEXT_RT_WINDOW is converted to ACT_TAB_NEXT and
      /// the focus shifts between controls of child window. This functionality isn't (ACT_TAB_NEXT should switch focus between controls) implemented for RIA as of now.
      /// </summary>
      /// <param name="task"></param>
      /// <param name="kbItm"></param>
      /// <param name="actionId"></param>
      /// <returns>correct navigation action</returns>
      private int GetNavigationAction(Task task, KeyboardItem kbItm, int actionId)
      {
         int actId = actionId;

         // convert only if the actions are raised through keyboard.
         if (kbItm != null && (actionId == InternalInterface.MG_ACT_NEXT_RT_WINDOW || actionId == InternalInterface.MG_ACT_PREV_RT_WINDOW) &&
             task.getForm().hasTabControl())
         {
            if (actionId == InternalInterface.MG_ACT_PREV_RT_WINDOW)
               actId = InternalInterface.MG_ACT_TAB_PREV;
            else if (actionId == InternalInterface.MG_ACT_NEXT_RT_WINDOW)
               actId = InternalInterface.MG_ACT_TAB_NEXT;
         }

         return actId;
      }

      /// <summary>
      ///   getter for the execStack
      /// </summary>
      /// <returns> the current execution stack</returns>
      internal ExecutionStack getExecStack()
      {
         return (ExecutionStack)_execStack.Peek();
      }

      /// <summary>
      ///   pushes one entry of an operation into the execution stack
      /// </summary>
      /// <param name = "taskId">- the id of the current task</param>
      /// <param name = "handlerId">- the id of the handler which holds the raise event command </param>
      /// <param name = "operIdx">- the idx of the command which created an execution stack entry (raise event of function call)</param>
      internal void pushExecStack(String taskId, String handlerId, int operIdx)
      {
         ((ExecutionStack)_execStack.Peek()).push(MGDataCollection.Instance.getTaskIdById(taskId), handlerId, operIdx);
      }

      /// <summary>
      ///   pops one entry from the execution stack - after the completion of an operation which can hold
      ///   other operations
      /// </summary>
      /// <returns> the entry that was popped</returns>
      internal ExecutionStackEntry popExecStack()
      {
         return ((ExecutionStack)_execStack.Peek()).pop();
      }

      /// <summary>
      ///   push a new entry to the stack of server exec stacks - this is to defer operations that should not
      ///   get influences by the current server execution stack - when performing the record prefix of a task 
      ///   that was just returned from the server, or when calling a modal task
      /// </summary>
      internal void pushNewExecStacks()
      {
         _serverExecStack.Push(null);
         _execStack.Push(new ExecutionStack());
      }

      /// <summary>
      ///   pops the new entry from the stack of server execution stacks when the sequence that needs to avoid
      ///   current server execution stack has ended
      /// </summary>
      internal void popNewExecStacks()
      {
         _serverExecStack.Pop();
         _execStack.Pop();
      }

      /// <summary>
      ///   pushes an execution stack entry into the server exec stack
      /// </summary>
      /// <param name = "taskId">- the id of the task in the entry</param>
      /// <param name = "handlerId">- handler id of the entry</param>
      /// <param name = "operIdx">- idx of the operation which the entry refers to</param>
      internal void pushServerExecStack(String taskId, String handlerId, int operIdx)
      {
         if (_serverExecStack.Peek() == null)
         {
            _serverExecStack.Pop();
            _serverExecStack.Push(new ExecutionStack());
         }

         ((ExecutionStack)_serverExecStack.Peek()).push(taskId, handlerId, operIdx);
      }

      /// <summary>
      ///   clears the server execution stack after we found the correct next operation that we need to continue from
      /// </summary>
      internal void clearServerExecStack()
      {
         if (_serverExecStack.Peek() != null)
            ((ExecutionStack)_serverExecStack.Peek()).clear();
      }

      /// <summary>
      ///   reverse the server execution stack - we get it from the server and push it in the same order 
      ///   it is written in the message, which actually means we have to reverse it in order to handle it correctly
      /// </summary>
      internal void reverseServerExecStack()
      {
         if (_serverExecStack.Peek() != null)
            ((ExecutionStack)_serverExecStack.Peek()).reverse();
      }

      /// <summary>
      ///   calculate according to the server's execution stack and the current execution stack on the client +
      ///   the current operation which we are on, which is the next operation that we need to execute
      ///   in the current handler
      /// </summary>
      /// <param name = "oper">- the operation which we are currently on</param>
      /// <param name = "clearWhenFound">indicates whether we should clear the server stack when we reach its end, 
      ///   or if we just using this method to check what the next operation is
      /// </param>
      /// <returns>s the next operation that should be executed by the client
      ///   if it returns -1 then the client should continue as usual 
      /// </returns>
      internal int getNextOperIdx(Operation oper, bool clearWhenFound)
      {
         ExecutionStack tmpServerExecStack;
         ExecutionStackEntry tmpServerExecStackEntry = null;
         int nextOperIdx = -1;

         // if the server exec stack is empry it means we did not return from the server, 
         // so we have to continue with executing the following operation in the current handler as usual
         if (!(_serverExecStack.Count == 0) && _serverExecStack.Peek() != null &&
             !((ExecutionStack)_serverExecStack.Peek()).empty())
         {
            // if the server's exec stack is deeper than the client's - 
            // we need to check the entry in the client's stack which corresponds to the server's stack size
            if (((ExecutionStack)_serverExecStack.Peek()).size() > ((ExecutionStack)_execStack.Peek()).size())
            {
               // we copy the server's execution stack and pop from it until the size of the server 
               // and the client is the same
               tmpServerExecStack = new ExecutionStack(((ExecutionStack)_serverExecStack.Peek()));
               while (tmpServerExecStack.size() > ((ExecutionStack)_execStack.Peek()).size())
               {
                  tmpServerExecStackEntry = tmpServerExecStack.pop();
               }

               // if all entries before the last one we popped are the same as in the client and 
               // the current entry is on the same handler as the server's last entry,
               // and the operation is after the current operation,
               // then this entry in the server indicates which is our next operation
               if ((_execStack.Peek()).Equals(tmpServerExecStack) &&
                   tmpServerExecStackEntry.TaskId == oper.getTaskTag() &&
                   tmpServerExecStackEntry.HandlerId == oper.getHandlerId() &&
                   tmpServerExecStackEntry.OperIdx >= oper.getServerId())
               {
                  nextOperIdx = tmpServerExecStackEntry.OperIdx;

                  // if the server exec stack is deeper than the client's by only 1,
                  // which means we are checking the top most entry on the server's execution stack, 
                  // the whole stack is the same now, so we can clear the server's stack - 
                  // we got to the point which we are on the exact operation which the server left execution. 
                  if (clearWhenFound &&
                      ((ExecutionStack)_serverExecStack.Peek()).size() ==
                      ((ExecutionStack)_execStack.Peek()).size() + 1)
                  {
                     ((ExecutionStack)_serverExecStack.Peek()).clear();
                  }
               }
               else
                  nextOperIdx = MAX_OPER;
            }
            else
            {
               nextOperIdx = MAX_OPER;
            }
         }

         return nextOperIdx;
      }

      /// <summary>
      ///   this method is check if the wide screen is open and close it
      ///   also when EXIT is press on a wide screen, no action is pass. (same in online)
      /// </summary>
      /// <param name = "evt"> </param>
      /// <returns> </returns>
      private MgControl exitWide(RunTimeEvent evt)
      {
         int intEvtCode = evt.getInternalCode();
         MgControl mgControl = evt.Control;

         if (mgControl != null)
         {
            bool exitWide;
            switch (intEvtCode)
            {
               // case MG_ACT_REPLACE_TEXT:
               // case MG_ACT_MARK_AND_REPLACE_TEXT:
               case InternalInterface.MG_ACT_NONE:
               case InternalInterface.MG_ACT_CHAR:
               case InternalInterface.MG_ACT_EDT_BEGNXTLINE:
               case InternalInterface.MG_ACT_BEGIN_DROP:
               case InternalInterface.MG_ACT_CLIP_PASTE:
               case InternalInterface.MG_ACT_CLIP_COPY:
               case InternalInterface.MG_ACT_CUT:
               //MG_ACT_CLIP_CUT:            
               case InternalInterface.MG_ACT_EDT_UNDO:
               case InternalInterface.MG_ACT_HELP:
               // case MG_ACT_MENU_HELP:
               case InternalInterface.MG_ACT_WIDE:
               case InternalInterface.MG_ACT_EDT_MARKALL:
               //MG_ACT_EDT_MARKALL:
               case InternalInterface.MG_ACT_EDT_PRVCHAR:
               case InternalInterface.MG_ACT_EDT_BEGFLD:
               case InternalInterface.MG_ACT_EDT_ENDFLD:
               case InternalInterface.MG_ACT_EDT_NXTCHAR:
               case InternalInterface.MG_ACT_EDT_PRVLINE:
               case InternalInterface.MG_ACT_EDT_NXTLINE:
               case InternalInterface.MG_ACT_EDT_BEGFORM:
               case InternalInterface.MG_ACT_EDT_ENDFORM:
               case InternalInterface.MG_ACT_EDT_BEGLINE:
               case InternalInterface.MG_ACT_EDT_ENDLINE:
               case InternalInterface.MG_ACT_EDT_MARKPRVCH:
               case InternalInterface.MG_ACT_EDT_MARKNXTCH:
               case InternalInterface.MG_ACT_EDT_MARKPRVLINE:
               case InternalInterface.MG_ACT_MARK_NEXT_LINE:
               //ACT_EDT_MARKNXTLINE
               case InternalInterface.MG_ACT_EDT_MARKTOBEG:
               case InternalInterface.MG_ACT_EDT_MARKTOEND:
               case InternalInterface.MG_ACT_EDT_NXTWORD:
               case InternalInterface.MG_ACT_EDT_PRVWORD:
               case InternalInterface.MG_ACT_EDT_PRVPAGE:
               case InternalInterface.MG_ACT_EDT_NXTPAGE:
               case InternalInterface.MG_ACT_EDT_BEGPAGE:
               case InternalInterface.MG_ACT_EDT_ENDPAGE:
               case InternalInterface.MG_ACT_TBL_BEGLINE:
               case InternalInterface.MG_ACT_TBL_ENDLINE:
               case InternalInterface.MG_ACT_EDT_DELCURCH:
               case InternalInterface.MG_ACT_EDT_DELPRVCH:
               case InternalInterface.MG_ACT_TBL_PRVLINE:
               case InternalInterface.MG_ACT_TBL_NXTLINE:
               case InternalInterface.MG_ACT_HIT:
               case InternalInterface.MG_ACT_CTRL_HIT:
                // Actions that not support in rich client
                //case ACT_CONTEXT_MENU:          
                //case ACT_WHATS_NEW:
                //case ACT_HELP_DEV_RESOURCES:
                //case ACT_ONLINE_HELP:
                    case InternalInterface.MG_ACT_WINSIZE:
               case InternalInterface.MG_ACT_WINMOVE:
               //case MG_ACT_WM_TIMER:
               //case MG_ACT_RT_CTX_LOST_FOCUS:
               //case MG_ACT_RT_CTX_GOT_FOCUS:
               //case MG_ACT_MARK_CURR:
               case InternalInterface.MG_ACT_WEB_MOUSE_OUT:
               case InternalInterface.MG_ACT_WEB_MOUSE_OVER:
               case InternalInterface.MG_ACT_WEB_CLICK:
               case InternalInterface.MG_ACT_WEB_ON_DBLICK:
               //INTERNAL EVENTS for WebClient
               case InternalInterface.MG_ACT_CTRL_FOCUS:
               case InternalInterface.MG_ACT_CTRL_MOUSEUP:
               case InternalInterface.MG_ACT_CTRL_KEYDOWN:
               case InternalInterface.MG_ACT_TIMER:
               //case MG_ACT_SELECTION:
               case InternalInterface.MG_ACT_RESIZE:
               //case MG_ACT_ROW_DATA_CURR_PAGE:
               //case MG_ACT_DV_TO_GUI:
               case InternalInterface.MG_ACT_ENABLE_EVENTS:
               case InternalInterface.MG_ACT_DISABLE_EVENTS:
               case InternalInterface.MG_ACT_OK:
               case InternalInterface.MG_ACT_CTRL_MODIFY:
                  exitWide = false;
                  break;

               default:
                  exitWide = true;
                  break;
            }

            var topMostForm = (MgForm)mgControl.getTopMostForm();
            var wideParentControl = (MgControl)topMostForm.getWideParentControl();
            if (topMostForm.wideIsOpen())
            {
               if (exitWide)
               {
                  //when close the wide set the event to be on the parent wide control and not on the wide
                  topMostForm.getWideControl().onWide(false);

                  if (evt.Control.isWideControl())
                  {
                     evt.setCtrl(wideParentControl);
                     mgControl = wideParentControl;
                  }

                  //When EXIT is press on a wide screen, no action is pass. (same in online)
                  if (intEvtCode == InternalInterface.MG_ACT_EXIT)
                  {
                     evt.setInternal(InternalInterface.MG_ACT_NONE);
                     mgControl = null;
                  }
               }
            }
            else
            {
               // the event was recieved on the wide control although it was already closed so we need to replace
               // the event's control with the control that initiated the wide control (a.k.a: Wide Parent Control.)
               if (evt.Control.isWideControl())
               {
                  evt.setCtrl(wideParentControl);
                  mgControl = wideParentControl;
               }
            }
         }

         return mgControl;
      }

      /// <returns>
      /// </returns>
      internal ClientManager getClientManager()
      {
         return ClientManager.Instance;
      }

      /// <summary>
      ///   set the processingTopMostEndTask
      /// </summary>
      /// <param name = "inProcessingTopMostEndTask"></param>
      internal void setProcessingTopMostEndTask(bool inProcessingTopMostEndTask)
      {
         _processingTopMostEndTask = inProcessingTopMostEndTask;
      }

      /// <summary>
      ///   get the processingTopMostEndTask
      /// </summary>
      internal bool getProcessingTopMostEndTask()
      {
         return _processingTopMostEndTask;
      }

      /// <summary>
      ///   set the AllowEvents
      /// </summary>
      /// <param name = "AllowEvents"></param>
      internal void setAllowEvents(EventsAllowedType AllowEvents)
      {
         _allowEvents = AllowEvents;
      }

      /// <summary>
      ///   set the AllowEvents for a non interactive task.
      /// </summary>
      /// <param name = "AllowEvents"></param>
      internal void setNonInteractiveAllowEvents(bool AllowEvents, Task task)
      {
         if (AllowEvents)
         {
            _allowEvents = EventsAllowedType.NON_INTERACTIVE;
            //QCR #802495, The Forms Controller mechanism is disable if we are in Non interactive task with AllowEvents = true and Open Window = true
            if (task.isOpenWin())
               AllowFormsLock = false;
            else
               AllowFormsLock = true;
         }
         else
         {
            _allowEvents = EventsAllowedType.NONE;
            AllowFormsLock = true;
         }
      }

      /// <summary>
      ///   get the AllowEvents
      /// </summary>
      /// <param name = "withinNonInteractiveLoop"></param>
      internal EventsAllowedType getAllowEvents()
      {
         return _allowEvents;
      }

      /// <summary>
      ///   Return TRUE if no events are allowed
      /// </summary>
      /// <returns></returns>
      internal bool NoEventsAllowed()
      {
         return _allowEvents == EventsAllowedType.NONE;
      }

      /// <summary>
      ///   check and execute control modify event
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "act"></param>
      private void handleCtrlModify(MgControl ctrl, int act)
      {
         // if cannot modify, do nothing
         if (ctrl == null || !ctrl.isModifiable())
            return;

         switch (act)
         {
            case InternalInterface.MG_ACT_EDT_DELCURCH:
            case InternalInterface.MG_ACT_EDT_DELPRVCH:
            case InternalInterface.MG_ACT_RT_COPYFLD:
            case InternalInterface.MG_ACT_CLIP_PASTE:
            case InternalInterface.MG_ACT_RT_EDT_NULL:
            case InternalInterface.MG_ACT_BEGIN_DROP:
            case InternalInterface.MG_ACT_EDT_UNDO:
            case InternalInterface.MG_ACT_CTRL_MODIFY:
            case InternalInterface.MG_ACT_CHAR:
            case InternalInterface.MG_ACT_ARROW_KEY:
            case InternalInterface.MG_ACT_EDT_BEGNXTLINE:
            case InternalInterface.MG_ACT_SELECTION:
               break;

            default:
               return;
         }

         RunTimeEvent rtEvt;
         rtEvt = new RunTimeEvent(ctrl);
         rtEvt.setInternal(InternalInterface.MG_ACT_CTRL_MODIFY);
         handleEvent(rtEvt, false);
      }

      /// <summary>
      ///   check if action is allowed according to the AllowedEvents flag
      /// </summary>
      /// <param name = "act"></param>
      private bool ActAllowed(int act)
      {
         bool allowed = true;

         switch (getAllowEvents())
         {
            case EventsAllowedType.ALL:
               allowed = true;
               break;

            case EventsAllowedType.NONE:
               // allow creline even in allow=no. we have no choice. RC mechanism is different then batch. it uses ACT_CREALINE
               // when in create mode. We will need to change it in the future so that when user raises creline event it will not work. just internally.
               allowed = (act == InternalInterface.MG_ACT_CRELINE);
               break;

            // Only the following actions are allowed when running inside a non interactive loop when allow events = YES.
            case EventsAllowedType.NON_INTERACTIVE:

               switch (act)
               {
                  case InternalInterface.MG_ACT_EXIT_SYSTEM:
                  case InternalInterface.MG_ACT_EXIT:
                  case InternalInterface.MG_ACT_CLOSE:
                  case InternalInterface.MG_ACT_CRELINE:
                  case InternalInterface.MG_ACT_ROLLBACK:
                     allowed = true;
                     break;

                  default:
                     allowed = false;
                     break;
               }
               break;
         }

         return allowed;
      }

      /// <summary>
      ///   This function commits the record and then depending on the success or failure of transaction, it performs other actions.
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "isReversibleExit"></param>
      private void commitRecord(Task task, bool isReversibleExit)
      {
         var dv = (DataView)task.DataView;
         var rec = (Record)dv.getCurrRec();

         bool forceSuffix = task.checkProp(PropInterface.PROP_TYPE_FORCE_SUFFIX, false);
         bool isSelectionTable = task.checkProp(PropInterface.PROP_TYPE_SELECTION, false);
         bool isNewRec = rec.isNewRec();
         bool savePrevCurrRec = true;
         bool recModified = rec.Modified;

         if (!dv.recInModifiedTab(rec.getId()))
            rec.clearFlagsHistory();
         task.TaskTransactionManager.checkAndCommit(isReversibleExit, ConstInterface.TRANS_RECORD_PREFIX, task.ConfirmUpdateNo);
         if (task.transactionFailed(ConstInterface.TRANS_RECORD_PREFIX))
         {
            // on failure of the transaction reenter the record level by
            // executing "record prefix" for the subforms which executed
            // the suffix successfully
            try
            {
               task.DataSynced = true;
               setStopExecution(false);
               task.handleEventOnSlaveTasks(InternalInterface.MG_ACT_REC_PREFIX);
            }
            finally
            {
               setStopExecution(true);
            }
         }
         else if (task.isAborting())
            setStopExecution(true);
         else
         {
            if (recModified)
               rec.setLateCompute(true);

            // QCR 783071 if after checkAndCommit all if O.K. the
            // return the level back to task level in order to let rec_prfix to be executed.
            // even though the task level should be TASK_LEVEL_TASK
            // the checkAndCommit process could have change it
            if (!_stopExecution)
               task.setLevel(Constants.TASK_LEVEL_TASK);

            rec.resetUpdated();
            rec.resetModified();

            // QCR #983178: cancel the record only after the transaction
            // handling was completed successfully and the record wasn't
            // modified
            if (!recModified && !forceSuffix && (!isSelectionTable || !task.InSelect))
            {
               if (isNewRec)
               {
                  // QCR #428965: save the previous current record BEFORE
                  // "cancel edit" so the correct record is saved
                  dv.setPrevCurrRec();
                  savePrevCurrRec = false;
                  rec.setNewRec();
                  ((MgForm)task.getForm()).cancelEdit(false, false);
               }
               else if (!rec.getForceSaveOrg())
                  dv.cancelEdit(REAL_ONLY, true);
               else
                  dv.saveOriginal();
            }
            // QCR #995222: if the record was modified then save the
            // original record again in case a task lavel transaction
            // will fail with retry strategy. Before this fix, if such
            // thing happened on a record which wasn't the first then
            // the "begin table" event raised a record suffix event and
            // caused "cancel edit".
            else
               dv.saveOriginal();
         }

         if (savePrevCurrRec)
            dv.setPrevCurrRec();
      }

      /// <summary>
      ///   Close all child tasks
      /// </summary>
      /// <param name = "task">parent task, whose children will be closed</param>
      /// <param name = "StartUpMgd">if this mgd belongs to a closing prog, ExitByMenu is set to the task.ExitingByMenu</param>
      /// <param name = "ExitByMenu">Indication if the close was initiate by a program activated from menu.</param>
      private void CloseAllChildProgs(Task task, MGData StartUpMgd, bool ExitByMenu)
      {
         // NEED TO HANDLE A PROBLEMATIC CASE OF DESTINATION IN PARENT TASK !!!!
         if (task.hasSubTasks())
         {
            TasksTable childTaskTable = task.getSubTasks();
            // loop on all child task and close them all (unless they are subform or already closing)
            // from MDI menu, it has no subtasks as childs, just progs.
            // if call is from parent activate, then only it will have real subtasks.
            int taskIdx = 0;
            if (childTaskTable.getSize() > 0)
            {
               do
               {
                  Task childTsk = childTaskTable.getTask(taskIdx);
                  if (childTsk != null && !childTsk.isAborting())
                  {
                     if (childTsk.IsSubForm)
                     {
                        CloseAllChildProgs(childTsk, null, ExitByMenu);
                        taskIdx++;
                     }
                     else
                     {
                        // if closing the startup task, we need to send the server an indication not to close all the mdi frame.
                        if (StartUpMgd != null && StartUpMgd.getFirstTask() == childTsk)
                           childTsk.setExitingByMenu(ExitByMenu);

                        // handle exit event on the task now (no by event queue).
                        handleInternalEvent(childTsk, InternalInterface.MG_ACT_EXIT);

                        childTsk.setExitingByMenu(false); // set it back, just in case it will be used again.
                        // taskIdx is unchanged. if exit is done then task is deleted from the TaskTable and same
                        // idx has a different task now. if error, we are getting out of here anyway.
                     }
                  }
                  else
                  {
                     taskIdx++;
                     continue;
                  }
               } while (taskIdx < childTaskTable.getSize() && !GetStopExecutionFlag());
            }
         }
      }

      /// <summary>
      ///   callback for incremental locate timer
      /// </summary>
      internal void incLocateTimerCB(object taskObj)
      {
         var task = (Task)taskObj;
         var rtEvt = new RunTimeEvent(task, true);

         rtEvt.setInternal(InternalInterface.MG_ACT_INCREMENTAL_LOCATE);
         rtEvt.setMainPrgCreator(task);
         rtEvt.setCtrl((MgControl)task.getLastParkedCtrl());
         addToTail(rtEvt);
      }

      /// <summary>
      ///   free the temporary keys created in DNObjectsCollection
      /// </summary>
      private void removeTempDNObjectCollectionKeys()
      {
         if (ClientManager.Instance.getTempDNObjectCollectionKeys().Count > 0)
         {
            // remove the keys from DNObjectsCollection
            foreach (int key in ClientManager.Instance.getTempDNObjectCollectionKeys())
               DNManager.getInstance().DNObjectsCollection.Remove(key);

            // clear the tempDNObjectsCollectionKeys
            ClientManager.Instance.getTempDNObjectCollectionKeys().Clear();
         }
      }

      /// <summary>
      /// </summary>
      /// <param name = "task"></param>
      /// <returns></returns>
      private static void doSyncForSubformParent(Task task)
      {
         Task parentTask = task.getParent();
         var parentDv = (DataView)parentTask.DataView;
         var parentRec = (Record)parentDv.getCurrRec();

         // do not touch the parent record if the parent dataview is 'empty dataview'.
         if (task.IsSubForm && parentRec != null && parentRec.Modified && !parentDv.isEmptyDataview())
         {
            parentRec.setMode(DataModificationTypes.Update);
            parentDv.addCurrToModified(false);
            ((Record)parentDv.getCurrRec()).Synced = true;
            parentDv.setChanged(true);
            IClientCommand dataViewExecuteLocalUpdatesCommand = CommandFactory.CreateDataViewCommand(parentTask.getTaskTag(), DataViewCommandType.ExecuteLocalUpdates);
            ReturnResult result = parentTask.DataviewManager.Execute(dataViewExecuteLocalUpdatesCommand);

         }
      }

      /// <summary>
      /// </summary>
      /// <param name = "val"></param>
      internal void setAllowFormsLock(bool val)
      {
         AllowFormsLock = val;
      }

      internal bool getAllowFormsLock()
      {
         return AllowFormsLock;
      }



      /// <summary>
      /// get the right commands processor to use in a select program command
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      private CommandsProcessorBase GetServerForSelectCommand(MgControl control)
      {
         // get the task ID from the property
         Property prop = control.getProp(PropInterface.PROP_TYPE_SELECT_PROGRAM);
         TaskDefinitionId taskDefinitionId = prop.TaskDefinitionId;

         return CommandsProcessorManager.GetCommandsProcessor(taskDefinitionId);
      }

      /// <summary>
      /// copy offline tasks from the execution stack to the server execution stack, to compensate for not sending to
      /// and not receiving those tasks from the server
      /// </summary>
      internal void CopyOfflineTasksToServerExecStack()
      {
         ExecutionStack clientStack = _execStack.Peek() as ExecutionStack;
         ExecutionStack serverStack = _serverExecStack.Peek() as ExecutionStack;

         // make sure we have the latest stack
         if (serverStack == null)
         {
            serverStack = new ExecutionStack();
            _serverExecStack.Pop();
            _serverExecStack.Push(serverStack);
         }

         // Create a copy of the client stack to go over it's entries. We have previously used an enumerator here 
         // which proved as bad practice, as the stack order varies on different platforms
         Stack execStackCopy = (Stack)clientStack.getStack().Clone();

         bool pushIntoServerStack = false;
         while (execStackCopy.Count > 0)
         {
            // once we have an offline task, copy all the entries from it onward to the server execution stack
            ExecutionStackEntry current = (ExecutionStackEntry)execStackCopy.Pop();
            uint taskTag = Convert.ToUInt32(current.TaskId);
            if (!pushIntoServerStack && taskTag > ConstInterface.INITIAL_OFFLINE_TASK_TAG)
               pushIntoServerStack = true;

            if (pushIntoServerStack)
               serverStack.push(current as ExecutionStackEntry);
         }

      }

#region Nested type: EventsAllowedType

      internal enum EventsAllowedType
      {
         NONE = 0,
         NON_INTERACTIVE = 1,
         ALL = 2
      }

#endregion
   }
}
