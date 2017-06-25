using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using Manager = com.magicsoftware.unipaas.Manager;
using Field = com.magicsoftware.richclient.data.Field;
using Constants = com.magicsoftware.util.Constants;
using DataView = com.magicsoftware.richclient.data.DataView;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.remote;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.local;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.events
{
   /// <summary>
   ///   data for <handler ...> ...</handler>
   /// </summary>
   internal class EventHandler
   {
      private readonly YesNoExp _enabledExp = new YesNoExp(true); // default is true always
      private readonly OperationTable _operationTab;
      private readonly YesNoExp _propagateExp = new YesNoExp(true);
      private bool _isHandlerOnForm;
      private MgControl _ctrl;
      private String _ctrlName;
      private int _dvLen; // The ìdvposÅEcontains the number of fields selected in the handler 
      private int _dvPos; // and the index of the first field of it in the Dataview.
      private Event _evt;
      private bool _handledByClient;
      private Field _handlerFld;
      private bool _hasParameters;
      private int _id;
      private char _level; // Control | Handler | Record | Task | Variable
      private BrkScope _scope; // Task|Subtask|Global
      private Task _task;
      private int _taskMgdID = -1;

      /// <summary>
      ///   CTOR
      /// </summary>
      internal EventHandler()
      {
         _operationTab = new OperationTable();
      }

      /// <summary>
      ///   parse the event handler and its sub-structures
      /// </summary>
      internal void fillData(Task taskRef)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         if (_task == null)
         {
            _task = taskRef;
            _taskMgdID = _task.getMgdID();
         }

         while (initInnerObjects(parser, parser.getNextTag()))
         {
         }
         DataView dv = (DataView)_task.DataView;
         _hasParameters = dv.ParametersExist(_dvPos, _dvLen);
      }

      /// <summary>
      ///   initializing of inner objects
      /// </summary>
      private bool initInnerObjects(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_HANDLER))
            initInnerElements(parser, _task);
         else if (foundTagName.Equals(ConstInterface.MG_TAG_OPER))
            _operationTab.fillData(_task, this);
         else if (foundTagName.Equals(ConstInterface.MG_TAG_EVENT) && _evt == null)
         {
            _evt = new Event();
            _evt.fillData(parser, _task);
            if (_evt.getType() == ConstInterface.EVENT_TYPE_TIMER ||
                (_evt.getType() == ConstInterface.EVENT_TYPE_USER &&
                 _evt.getUserEventType() == ConstInterface.EVENT_TYPE_TIMER))
               _task.getMGData().addTimerHandler(this);

            // for enable MG_ACT_ZOOM action
            // 1. handler on zoom action.
            // 2. handler on a user defined handler with a trigger of zoom action.
            if (((_evt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                  _evt.getInternalCode() == InternalInterface.MG_ACT_ZOOM) ||
                  (_evt.getType() == ConstInterface.EVENT_TYPE_USER && _evt.getUserEvent() != null &&
                   _evt.getUserEventType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                   _evt.getUserEvent().getInternalCode() == InternalInterface.MG_ACT_ZOOM)) &&
                  !_task.getEnableZoomHandler())
            {
               if (_handlerFld != null)
                  _handlerFld.setHasZoomHandler();
               else if (_ctrl != null)
                  _ctrl.HasZoomHandler = true;
               else
                  _task.setEnableZoomHandler();
            }
            if (_evt.getType() == ConstInterface.EVENT_TYPE_DOTNET)
            {
               _handlerFld = _evt.DotNetField;
            }
         }
         else if (foundTagName.Equals('/' + ConstInterface.MG_TAG_HANDLER))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog("there is no such tag in EventHandler.initInnerObjects() " + foundTagName);
            return false;
         }
         return true;
      }

      /// <summary>
      ///   Initializing of inner attributes of <handler> tag
      /// </summary>
      /// <param name = "taskRef"></param>
      private void initInnerElements(XmlParser parser, Task taskRef)
      {
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_HANDLER) + ConstInterface.MG_TAG_HANDLER.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            initElements(tokensVector, taskRef);
            parser.setCurrIndex(++endContext); // to delete ">" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in Handler.FillData() out of string bounds");
      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every found	element </param>
      /// <param name = "taskRef"> </param>
      private void initElements(List<String> tokensVector, Task taskRef)
      {
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case ConstInterface.MG_ATTR_HANDLEDBY:
                  _handledByClient = valueStr.ToUpper().Equals("C".ToUpper());
                  break;

               case XMLConstants.MG_ATTR_ID:
                  _id = XmlParser.getInt(valueStr);
                  break;

               case ConstInterface.MG_ATTR_LEVEL:
                  _level = valueStr[0];
                  break;

               case ConstInterface.MG_ATTR_OBJECT:
                  String valueStringCtrlName = XmlParser.unescape(valueStr);
                  _ctrlName = XmlParser.unescape(valueStringCtrlName).ToUpper(); // relevant for user handlers only
                  calculateCtrlFromControlName(taskRef);
                  break;

               case ConstInterface.MG_ATTR_VARIABLE:
                  _handlerFld = (Field)_task.DataView.getField(XmlParser.getInt(valueStr));
                  _handlerFld.setHasChangeEvent();
                  break;

               case ConstInterface.MG_ATTR_SCOPE:
                  _scope = (BrkScope)valueStr[0];
                  break;

               case ConstInterface.MG_ATTR_PROPAGATE:
                  _propagateExp.setVal(taskRef, valueStr);
                  break;

               case ConstInterface.MG_ATTR_ENABLED:
                  _enabledExp.setVal(taskRef, valueStr);
                  break;

               case ConstInterface.MG_ATTR_DVPOS:
                  int i = valueStr.IndexOf(",");
                  if (i > -1)
                  {
                     _dvLen = XmlParser.getInt(valueStr.Substring(0, i));
                     _dvPos = XmlParser.getInt(valueStr.Substring(i + 1));
                     for (int fldIdx = _dvPos; fldIdx < _dvPos + _dvLen; fldIdx++)
                     {
                        Field field = ((Field)_task.DataView.getField(fldIdx));
                        field.IsEventHandlerField = true;
                    }
                  }
                  break;

               case ConstInterface.MG_ATTR_HANDLER_ONFORM:
                  _isHandlerOnForm = XmlParser.getBoolean(valueStr);
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog(
                     "There is no such tag in EventHandler class. Insert case to EventHandler.initElements for " +
                     attribute);
                  break;
            }
         }
      }

      /// <summary>
      /// calculate the ctrl property from ctrl name property
      /// </summary>
      /// <param name="taskRef"></param>
      /// <param name="valueStringCtrlName"></param>
      internal void calculateCtrlFromControlName(Task taskRef)
      {
         if (!String.IsNullOrEmpty(_ctrlName))
         {
            _ctrl = taskRef.getForm() != null
                   ? ((MgForm)taskRef.getForm()).getCtrl(_ctrlName)
                   : null;
         }
      }

      /// <summary>
      ///   check if this event handler is a specific handler of an event
      /// </summary>
      /// <param name = "rtEvt">the event to check</param>
      /// <returns> boolean true if the events match</returns>
      protected internal bool isSpecificHandlerOf(RunTimeEvent rtEvt)
      {
         if (_ctrl == null && _ctrlName == null && _handlerFld == null && !_isHandlerOnForm)
            return false;
         // check the validity of the scope
         if (_scope == BrkScope.Task && !_task.Equals(rtEvt.getTask()))
            return false;
         // if the handlers event and the checked event are equal and
         // the handler handles the control attached to the checked event, return true
         // compare control name with the name of the control in the event
         if (_evt.equals(rtEvt))
         {
            // Variable change handlers do not have control names. Only fields need to be compared
            if (_handlerFld != null)
            {
               //this is a dot net event on object that belongs to the field of the handler
               if (rtEvt.DotNetObject != null && _handlerFld != null &&
                   _handlerFld.hasDotNetObject(rtEvt.DotNetObject))
                  return true;
               else
                  return _handlerFld == rtEvt.getFld();
            }

             
            if (rtEvt.Control != null && !_isHandlerOnForm)
            {
               String eventCtrlName = rtEvt.Control.getControlNameForHandlerSearch();

               if (eventCtrlName != null && _ctrlName.Equals(eventCtrlName, StringComparison.InvariantCultureIgnoreCase))
                  return true;
            }
            else if (rtEvt.Control == null && _isHandlerOnForm)
               return true;
         }
         return false;
      }

      /// <summary>
      ///   check if this event handler is a non-specific or global handler of an event
      /// </summary>
      /// <param name = "rtEvt">the event to check
      /// </param>
      /// <returns> boolean true if the events match
      /// </returns>
      protected internal bool isNonSpecificHandlerOf(RunTimeEvent rtEvt)
      {
         // if the handler handles a control return false
         if (_ctrl != null || _ctrlName != null || _handlerFld != null || _isHandlerOnForm)
            return false;
         // check the validity of the scope
         if (_scope == BrkScope.Task && !_task.Equals(rtEvt.getTask()) && rtEvt.getType() != ConstInterface.EVENT_TYPE_TIMER)
            return false;

         if (_scope == BrkScope.Global && !_task.Equals(rtEvt.getTask()))
            return false;

         // if the handlers event and the checked event are equal return true
         if (_evt.getType() == ConstInterface.EVENT_TYPE_USER &&
             _evt.getUserEventType() == ConstInterface.EVENT_TYPE_NOTINITED)
         {
            try
            {
               _evt.findUserEvent();
            }
            catch (ApplicationException)
            {
            }
         }

         return (_evt.equals(rtEvt));
      }

      /// <summary>
      ///   check if this event handler is a global handler of an event
      /// </summary>
      /// <param name = "rtEvt">the event to check
      /// </param>
      /// <returns> boolean true if the events match
      /// </returns>
      protected internal bool isGlobalHandlerOf(RunTimeEvent rtEvt)
      {
         // check the validity of the scope
         if (_scope != BrkScope.Global)
            return false;

         // if the handler handles a control return false
         if (_ctrl != null || _ctrlName != null || _handlerFld != null || _isHandlerOnForm)
            return false;

         // if the handlers event and the checked event are equal, return true
         return _evt.equals(rtEvt);
      }

      /// <summary>
      ///   check if this event handler is a specific global handler of an event
      /// </summary>
      /// <param name = "rtEvt">the event to check
      /// </param>
      /// <returns> boolean true if the events match
      /// </returns>
      protected internal bool isGlobalSpecificHandlerOf(RunTimeEvent rtEvt)
      {
         // check the validity of the scope
         if (_scope != BrkScope.Global)
            return false;

         if (_ctrl == null && _ctrlName == null && _handlerFld == null)
            return false;

         // if the handlers event and the checked event are equal, return true
         return (_ctrlName != null && _evt.equals(rtEvt) &&
                 _ctrlName.Equals(rtEvt.Control.Name, StringComparison.InvariantCultureIgnoreCase));
      }

      /// <summary>
      ///   execute the event handler
      /// </summary>
      /// <param name = "rtEvt">the event on which the handler is executed
      /// </param>
      /// <param name = "returnedFromServer">indicates whether we returned from the server and 
      ///   therefore might not need to check the enabled condition 
      /// </param>
      /// <param name = "enabledCndCheckedAndTrue">indicates that the enabled condition was already checked and there's no 
      ///   need to re-check it (because it might be false now, but we still need to 
      ///   execute the handler)
      /// </param>
      /// <returns> boolean this is the return code of the handler, if true then the events
      ///   manager must continue searching for the next event handler for the event
      /// </returns>
      internal RetVals execute(RunTimeEvent rtEvt, bool returnedFromServer, bool enabledCndCheckedAndTrue)
      {
         MgControl ctrl = null;
         int depth = 0;
         bool retVal;
         int mgdID;
         BoolRef isChangedCurrWnd;
         ArgumentsList args;
         bool taskEnd = false;
         String orgLevel = _task.getBrkLevel(); // THE ORIGINAL BREAK LEVEL STATE AND CURRENT EXECUTING CONTROL
         int orgLevelIndex = _task.getBrkLevelIndex();
         int initialLoopStackSize = 0;

         // set level for LEVEL function
         if (rtEvt != null && _evt.getType() != ConstInterface.EVENT_TYPE_EXPRESSION &&
             _evt.getType() != ConstInterface.EVENT_TYPE_DOTNET &&
             (_evt.getType() != ConstInterface.EVENT_TYPE_USER ||
              (_evt.getUserEvent().getType() != ConstInterface.EVENT_TYPE_EXPRESSION &&
               _evt.getUserEvent().getType() != ConstInterface.EVENT_TYPE_TIMER)))
         {
            // Unlike a handler, the user function should not change the level of the task.
            if (_evt.getType() != ConstInterface.EVENT_TYPE_USER_FUNC)
               _task.setBrkLevel(rtEvt.getBrkLevel(), _id);
         }
         else
            _task.setBrkLevel(_evt.getBrkLevel(), _id);

         args = rtEvt.getArgList();

         try
         {
            FlowMonitorQueue flowMonitor = FlowMonitorQueue.Instance;

            // check if the handler is enabled. if not, return true to continue searching
            // for the next event handler
            int nextOperIdx = -1;
            if (returnedFromServer)
            {
               // if we returned from the server - we need to check if the next operation is on the current
               // handler (using a call to getNextOperIdx() with false in the clearWhenFound parameter) 
               // and if it is - do not check the enabled condition, because it might have changed 
               // since it was evaluated on the server. we need to go into this handler even if it is not
               // currently enabled
               nextOperIdx = ClientManager.Instance.EventsManager.getNextOperIdx(_operationTab.getOperation(0), false);
            }
            if (nextOperIdx == -1 && !enabledCndCheckedAndTrue && !isEnabled())
               return new RetVals(true, false);

            // check the scope for Expression and Timer handlers
            if (_scope == BrkScope.Task &&
                (rtEvt.getType() == ConstInterface.EVENT_TYPE_TIMER ||
                 rtEvt.getType() == ConstInterface.EVENT_TYPE_EXPRESSION))
            {
               Task currTask = ClientManager.Instance.getLastFocusedTask();
               if (_task != currTask)
                  return new RetVals(true, true);
            }

            if (!_handledByClient)
            {
               // return false to stop searching for the more handlers because
               // the server had taken care of all of them
               return new RetVals(false, true);
            }

            if (rtEvt.getType() != ConstInterface.EVENT_TYPE_USER && !_evt.dataEventIsTrue())
               return new RetVals(false, true);

            if (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
                rtEvt.getInternalCode() == InternalInterface.MG_ACT_TASK_SUFFIX)
               taskEnd = true;

            if (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL)
            {
               switch (rtEvt.getInternalCode())
               {
                  case InternalInterface.MG_ACT_REC_PREFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_REC_PREFIX, FlowMonitorInterface.FLWMTR_START);
                     break;
                  case InternalInterface.MG_ACT_REC_SUFFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_REC_SUFFIX, FlowMonitorInterface.FLWMTR_START);
                     break;

                  case InternalInterface.MG_ACT_CTRL_SUFFIX:
                     flowMonitor.addTaskFlowCtrl(InternalInterface.MG_ACT_CTRL_SUFFIX, rtEvt.Control.Name,
                                                 FlowMonitorInterface.FLWMTR_START);
                     break;

                  case InternalInterface.MG_ACT_CTRL_PREFIX:
                     flowMonitor.addTaskFlowCtrl(InternalInterface.MG_ACT_CTRL_PREFIX, rtEvt.Control.Name,
                                                 FlowMonitorInterface.FLWMTR_START);
                     break;

                  case InternalInterface.MG_ACT_VARIABLE:
                     flowMonitor.addTaskFlowFld(InternalInterface.MG_ACT_VARIABLE, rtEvt.getFld().getName(),
                                                FlowMonitorInterface.FLWMTR_START);
                     break;

                  case InternalInterface.MG_ACT_CTRL_VERIFICATION:
                     flowMonitor.addTaskFlowCtrl(InternalInterface.MG_ACT_CTRL_VERIFICATION,
                                                 rtEvt.Control.Name, FlowMonitorInterface.FLWMTR_START);
                     break;

                  case InternalInterface.MG_ACT_TASK_PREFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_TASK_PREFIX, FlowMonitorInterface.FLWMTR_START);
                     break;

                  case InternalInterface.MG_ACT_TASK_SUFFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_TASK_SUFFIX, FlowMonitorInterface.FLWMTR_START);
                     break;

                  default:
                     flowMonitor.addTaskFlowHandler(_evt.getBrkLevel(true), FlowMonitorInterface.FLWMTR_START);
                     break;
               }
            }
            else
               flowMonitor.addTaskFlowHandler(_evt.getBrkLevel(true), FlowMonitorInterface.FLWMTR_START);

            try
            {
               Direction oldFlowDirection = _task.getDirection();

               if (rtEvt != null)
                  ctrl = rtEvt.Control;

               mgdID = MGDataCollection.Instance.currMgdID;
               isChangedCurrWnd = new BoolRef(false);

               // for timer events check if the task of the event is in the
               // current window and if not then change the current window
               // temporarily till the handler finishes
               if (_evt.getType() == ConstInterface.EVENT_TYPE_TIMER && mgdID != rtEvt.getMgdID())
               {
                  // set number of window for timer event
                  MGDataCollection.Instance.currMgdID = rtEvt.getMgdID();
                  isChangedCurrWnd = new BoolRef(true);
               }

               // put initial values to the local variables
               resetLocalVariables(args);

               initialLoopStackSize = _task.getLoopStackSize();
               RetVals retVals = null;
               if (_operationTab.getSize() > 0)
                  retVals = executeOperations(0, _operationTab.getSize() - 1, taskEnd, mgdID, depth, isChangedCurrWnd,
                                              false, false, -1);

               // re-initialise revert mode after completing execution of a handler's operations
               _task.setRevertFrom(-1);
               _task.setRevertDirection(Direction.FORE);

               // restore direction if changed due to verify revert
               _task.setDirection(Direction.NONE);
               _task.setDirection(oldFlowDirection);

               if (retVals != null)
                  return retVals;

               // check the endtask condition once again at the end of the handler
               if (!taskEnd)
                  _task.evalEndCond(ConstInterface.END_COND_EVAL_IMMIDIATE);

               // change current window back if it was changed for Timer
               if (isChangedCurrWnd.getVal())
               {
                  MGData oldMgd = MGDataCollection.Instance.getMGData(mgdID);

                  if (oldMgd != null && !oldMgd.IsAborting)
                     MGDataCollection.Instance.currMgdID = mgdID;
               }
            }
            // end while operation block
            catch (ServerError ex)
            {
               if (Logger.Instance.LogLevel != Logger.LogLevels.Basic)
                  Logger.Instance.WriteExceptionToLog(ex);

               throw;
            }
            catch (LocalSourceNotFoundException)
            {
               //re-throw the exception so that it will be caught by ClientManager.WorkThreadExecution()
               //and the application will be terminated.
               throw;
            }
            catch (Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex);
               return new RetVals(false, true);
            }
            finally
            {
               // get the propagate value
               retVal = _propagateExp.getVal();

               // remove loop counters that were left unused in the stack due to leaving
               // the handler on "stopExecution() == true"
               int currLoopStackSize = _task.getLoopStackSize();
               for (; currLoopStackSize > initialLoopStackSize; currLoopStackSize--)
                  _task.popLoopCounter();
            }

            // after executing the operations update the arguments back and recompute
            if (null != args)
               getArgValsFromFlds(args);

            if (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL)
            {
               switch (rtEvt.getInternalCode())
               {
                  case InternalInterface.MG_ACT_REC_PREFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_REC_PREFIX, FlowMonitorInterface.FLWMTR_END);
                     break;

                  case InternalInterface.MG_ACT_REC_SUFFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_REC_SUFFIX, FlowMonitorInterface.FLWMTR_END);
                     break;

                  case InternalInterface.MG_ACT_TASK_PREFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_TASK_PREFIX, FlowMonitorInterface.FLWMTR_END);
                     break;

                  case InternalInterface.MG_ACT_TASK_SUFFIX:
                     flowMonitor.addTaskFlowRec(InternalInterface.MG_ACT_TASK_SUFFIX, FlowMonitorInterface.FLWMTR_END);
                     break;

                  case InternalInterface.MG_ACT_CTRL_SUFFIX:
                     flowMonitor.addTaskFlowCtrl(InternalInterface.MG_ACT_CTRL_SUFFIX, ctrl.Name, FlowMonitorInterface.FLWMTR_END);
                     break;

                  case InternalInterface.MG_ACT_CTRL_PREFIX:
                     flowMonitor.addTaskFlowCtrl(InternalInterface.MG_ACT_CTRL_PREFIX, ctrl.Name, FlowMonitorInterface.FLWMTR_END);
                     break;

                  case InternalInterface.MG_ACT_VARIABLE:
                     flowMonitor.addTaskFlowFld(InternalInterface.MG_ACT_VARIABLE, rtEvt.getFld().getName(), FlowMonitorInterface.FLWMTR_END);
                     break;

                  case InternalInterface.MG_ACT_CTRL_VERIFICATION:
                     flowMonitor.addTaskFlowCtrl(InternalInterface.MG_ACT_CTRL_VERIFICATION, ctrl.Name, FlowMonitorInterface.FLWMTR_END);
                     break;

                  default:
                     flowMonitor.addTaskFlowHandler(_evt.getBrkLevel(true), FlowMonitorInterface.FLWMTR_END);
                     break;
               }
            }
            else
               flowMonitor.addTaskFlowHandler(_evt.getBrkLevel(true), FlowMonitorInterface.FLWMTR_END);
         }
         finally
         {
            // restoring  the original break level state and current executing control index
            _task.setBrkLevel(orgLevel, orgLevelIndex);
         }

         return new RetVals(retVal, true);
      }

      /// <summary>
      ///   execute the operations starting from fromIdx and ending at endIdx.
      /// </summary>
      private RetVals executeOperations(int fromIdx, int toIdx, bool taskEnd, int mgdID, int depth,
                                        BoolRef isChangedCurrWnd, bool inBlockIf, bool inReturnedFromServer,
                                        int inNextOperIdx)
      {
         Operation oper;
         int operType;
         int nextOperIdx = -1;
         bool returnedFromServer = false;
         MGData mgd = _task.getMGData();
         int execFlowIdx = (_task.getRevertDirection() == Direction.BACK) ? toIdx : fromIdx; // index of the operation to execute
         RetVals retvals;
         bool blockIfReturnedFromServer = false;
         bool executeBlock;
         bool blockLoopExeconServer;
         FlowMonitorQueue flowMonitor = FlowMonitorQueue.Instance;

         if (!inReturnedFromServer)
         {
            // we need to check if the server already started running this handler
            oper = _operationTab.getOperation(execFlowIdx);
            nextOperIdx = ClientManager.Instance.EventsManager.getNextOperIdx(oper, true);
            if (nextOperIdx > -1)
            {
               returnedFromServer = true;
               if (inBlockIf)
                  blockIfReturnedFromServer = true;
            }
            else
               returnedFromServer = false;
         }
         else
         {
            returnedFromServer = inReturnedFromServer;
            nextOperIdx = inNextOperIdx;
         }

         for (;
              (_task.getRevertDirection() == Direction.BACK ? execFlowIdx >= fromIdx : execFlowIdx <= toIdx) &&
              (!ClientManager.Instance.EventsManager.GetStopExecutionFlag() || _task.getRevertFrom() > -1) && !mgd.IsAborting;
         execFlowIdx += (_task.getRevertDirection() == Direction.BACK ? -1 : 1))
         {
            if (inBlockIf && blockIfReturnedFromServer && _operationTab.getOperation(toIdx).getServerId() < nextOperIdx)
               break;

            oper = _operationTab.getOperation(execFlowIdx);

            operType = oper.getType();

            blockIfReturnedFromServer = false;

            // skip to the correct operation after returning from the server
            if (returnedFromServer && oper.getServerId() < nextOperIdx)
               if (operType != ConstInterface.MG_OPER_BLOCK)
                  continue;
               else if (_operationTab.getOperation(oper.getBlockEnd()).getServerId() < nextOperIdx)
                  continue;

            if (!taskEnd)
               _task.evalEndCond(ConstInterface.END_COND_EVAL_IMMIDIATE);

            if (_taskMgdID != mgdID)
               isChangedCurrWnd.setVal(true);

            if (!_task.isMainProg() && _task.isOpenWin())
               MGDataCollection.Instance.currMgdID = _taskMgdID;

            switch (operType)
            {
               case ConstInterface.MG_OPER_VERIFY:
                  // if true returned then the verify condition was true and the mode is Error
                  if (oper.execute(returnedFromServer))
                  {
                     if (!_task.getBrkLevel().Equals("TS"))
                     {
                        ClientManager.Instance.EventsManager.setStopExecution(true);

                        MgControl currCtrl = (_ctrl != null ? _ctrl : ClientManager.Instance.EventsManager.getCurrCtrl());
                        //after verify error, we select the text of the ctrl we were on.
                        if (currCtrl != null)
                           Manager.SetSelection(currCtrl, 0, -1, -1);
                     }

                     if (_task.getInRecordSuffix() && _task.getLevel() == Constants.TASK_LEVEL_TASK)
                        _task.setLevel(Constants.TASK_LEVEL_RECORD);
                     //QCR #995108 - verify error in RS shouldn't trigger RP
                     /*
                     * if we are in the revert mode, dont stop immediately.
                     * execute the operations of the handler in the reverse order and then stop.
                     */
                     if (_task.getRevertFrom() == -1)
                        return new RetVals(false, true);
                  }
                  break;

               case ConstInterface.MG_OPER_LOOP:
                  // on getting a BlockLoop, execute the loop for the number of times intended,
                  // and then move to BlockEnd.
                  _task.enterLoop();
                  _task.increaseLoopCounter();
                  _task.setUseLoopStack(true);

                  flowMonitor.addFlowFieldOperation(oper, Flow.NONE, Direction.NONE, true);

                  blockLoopExeconServer = false;
                  if (!oper.getExecOnServer())
                  {
                     // execute the loop for the number of times intended
                     while (oper.execute(false))
                     {
                        // call executeOperations() to execute the operations within a BlockLoop
                        retvals = executeOperations(execFlowIdx + 1, oper.getBlockClose() - 1, taskEnd, mgdID, depth + 1,
                                                    isChangedCurrWnd, false, false, -1);
                        if (retvals != null)
                           return retvals;

                        // if we get a revert inside a loop, break the loop
                        if ((_task.getRevertFrom() > -1 && _task.getRevertFrom() >= execFlowIdx + 1 &&
                             _task.getRevertFrom() <= oper.getBlockEnd() - 1) ||
                             (mgd.IsAborting || ClientManager.Instance.EventsManager.GetStopExecutionFlag() && _task.getRevertFrom() == -1))
                           break;

                        _task.increaseLoopCounter();
                        _task.setUseLoopStack(true);
                     }
                  }
                  else
                  {
                     if (oper.execute(false))
                        blockLoopExeconServer = true;
                  }

                  if (_task.getRevertDirection() == Direction.FORE && !blockLoopExeconServer)
                  /*
                  * Once we finish executing the loop, 
                  * if we are moving in the forward direction, move to BlockEnd.
                  */
                     execFlowIdx = oper.getBlockEnd() - 1;
                  _task.leaveLoop();
                  break;

               case ConstInterface.MG_OPER_BLOCK:
                  // on getting a Block IF, execute the executable block (if or else)
                  // and then move to BlockEnd.
                  int startBlockIdx = execFlowIdx;
                  if (!oper.getExecOnServer() || (returnedFromServer && oper.getServerId() < nextOperIdx))
                  {
                     _task.setUseLoopStack(true);
                     if (!returnedFromServer || oper.getServerId() >= nextOperIdx)
                     // get the executable block between Block IF and Block END
                        execFlowIdx = getSignificantBlock(execFlowIdx, oper.getBlockEnd());
                     else
                        execFlowIdx = getStartBlockIfIdxByOper(execFlowIdx, nextOperIdx);
                  }
                  else
                  {
                     _task.setUseLoopStack(depth > 0);
                     oper.operServer(null);
                     nextOperIdx = ClientManager.Instance.EventsManager.getNextOperIdx(oper, true);
                     if (nextOperIdx > -1)
                     {
                        returnedFromServer = true;
                        if (inBlockIf)
                           blockIfReturnedFromServer = true;

                        if (nextOperIdx < _operationTab.getOperation(oper.getBlockEnd()).getServerId())
                        {
                           // find the significant block in which the next operation is
                           while (_operationTab.getOperation(oper.getBlockClose()).getServerId() < nextOperIdx)
                           {
                              execFlowIdx = oper.getBlockClose();
                              oper = _operationTab.getOperation(execFlowIdx);
                           }
                        }
                     }
                     else
                     {
                        returnedFromServer = false;
                        execFlowIdx = -1;
                     }
                  }
                  if (execFlowIdx != -1)
                  {
                     executeBlock = true;
                     oper = _operationTab.getOperation(execFlowIdx);

                     if (returnedFromServer)
                     {
                        if (_operationTab.getOperation(oper.getBlockClose()).getServerId() <= nextOperIdx)
                        {
                           executeBlock = false;
                           blockIfReturnedFromServer = true;
                        }
                     }

                     if (executeBlock)
                     {
                        // call executeOperations() to execute the operations in the executable block
                        retvals = executeOperations(execFlowIdx + 1, oper.getBlockClose() - 1, taskEnd, mgdID, depth + 1,
                                                    isChangedCurrWnd, true, returnedFromServer, nextOperIdx);
                        if (retvals != null)
                           if (!retvals.ReturnedFromServer)
                              return retvals;
                           else
                           {
                              returnedFromServer = retvals.ReturnedFromServer;
                              nextOperIdx = retvals.NextOperIdx;
                              blockIfReturnedFromServer = retvals.BlockIfReturnedFromServer;
                              if (_task.getRevertDirection() == Direction.BACK)
                                 execFlowIdx = startBlockIdx;
                              else
                                 execFlowIdx = oper.getBlockEnd() - 1;
                              continue;
                           }
                     }
                  }

                  /*
                  * Once we finish executing the block, 
                  * if we are moving in the forward direction, move to BlockEnd.
                  * otherwise, move to Block IF
                  */
                  if (_task.getRevertDirection() == Direction.BACK)
                     execFlowIdx = startBlockIdx;
                  else
                     execFlowIdx = oper.getBlockEnd() - 1;
                  break;

               case ConstInterface.MG_OPER_ENDBLOCK:
                  if (_task.getRevertDirection() == Direction.BACK)
                     execFlowIdx = getStartBlockIdxByEnd(execFlowIdx) + 1;
                  else
                     flowMonitor.addFlowFieldOperation(oper, Flow.NONE, Direction.NONE, true);
                  break;

               default:
                  // the rest of the operations (those that do not affect the flow)
                  _task.setUseLoopStack(depth > 0);
                  oper.execute(returnedFromServer);
                  break;
            }

            // if the server have executed a block of operations then get the next
            // operation index
            if (!blockIfReturnedFromServer)
            {
               nextOperIdx = ClientManager.Instance.EventsManager.getNextOperIdx(oper, true);
               if (nextOperIdx > -1)
               {
                  returnedFromServer = true;
                  if (inBlockIf)
                     blockIfReturnedFromServer = true;
               }
               else
                  returnedFromServer = false;
            }
         }

         if (blockIfReturnedFromServer)
            retvals = new RetVals(nextOperIdx, returnedFromServer, blockIfReturnedFromServer);
         else
            retvals = null;
         return retvals;
      }

      /// <summary>
      ///   get the executable Block (IF or ELSE)
      /// </summary>
      /// <param name = "startBlockIdx">the index of the Block IF</param>
      /// <param name = "endIdx">the Block End Index of the Block IF</param>
      /// <returns>The index of the block (IF or ELSE or next ELSE ...)that can be executed.
      ///   If no blocks can be executed, return -1
      /// </returns>
      private int getSignificantBlock(int startBlockIdx, int endIdx)
      {
         Operation oper;
         int operType = -1;
         int retIdx = -1;

         while (startBlockIdx < endIdx)
         {
            oper = _operationTab.getOperation(startBlockIdx);
            operType = oper.getType();

            if (operType != ConstInterface.MG_OPER_BLOCK && operType != ConstInterface.MG_OPER_ELSE &&
                operType != ConstInterface.MG_OPER_SERVER)
               break;

            if (oper.execute(false))
            {
               retIdx = startBlockIdx;
               break;
            }
            else
               startBlockIdx = oper.getBlockClose();
         }

         return retIdx;
      }

      /// <summary>
      ///   get the idx of the Block IF
      /// </summary>
      /// <param name = "endIdx">--- the index of the Block End
      /// </param>
      /// <returns> --- The index of the block IF whose BlockEnd Idx matches with the endIdx
      /// </returns>
      private int getStartBlockIdxByEnd(int endIdx)
      {
         Operation oper;
         int operType = -1;
         int retIdx = -1;
         bool found = false;

         for (int j = endIdx - 1; j >= 0 && !found; j--)
         {
            oper = _operationTab.getOperation(j);
            operType = oper.getType();

            if (operType == ConstInterface.MG_OPER_BLOCK || operType == ConstInterface.MG_OPER_LOOP)
            {
               if (oper.getBlockEnd() == endIdx)
               {
                  retIdx = j;
                  found = true;
               }
            }
         }
         return retIdx;
      }

      /// <summary>
      ///   get the idx of the Block IF
      /// </summary>
      /// <param name = "elseIdx">--- the index of the Block Else
      /// </param>
      /// <returns> --- The index of the block IF whose BlockEnd Idx matches with the endIdx
      /// </returns>
      private int getStartBlockIdxByElse(int elseIdx)
      {
         Operation oper;
         int operType = -1;
         int retIdx = -1;
         bool found = false;
         int elseBlockEnd = _operationTab.getOperation(elseIdx).getBlockEnd();

         for (int j = elseIdx - 1; j >= 0 && !found; j--)
         {
            oper = _operationTab.getOperation(j);
            operType = oper.getType();

            if (operType == ConstInterface.MG_OPER_BLOCK)
            {
               if (oper.getBlockEnd() == elseBlockEnd)
               {
                  retIdx = j;
                  found = true;
               }
            }
         }
         return retIdx;
      }

      /// <summary>
      ///   get the index of the Block IF or Else according to the index of one of the of operations in it
      /// </summary>
      /// <param name = "blockOperIdx">--- index of the if block for which we have to find the appropriate block to execute 
      /// </param>
      /// <param name = "srvrOperIdx">--- the server id of the next operation which the client should execute
      /// </param>
      /// <returns> --- The index of the block IF or Else which hold the operation which operIdx is its index
      /// </returns>
      private int getStartBlockIfIdxByOper(int blockOperIdx, int srvrOperIdx)
      {
         Operation oper;
         int operType = -1;
         int retIdx = -1;
         bool found = false;
         int j = _operationTab.serverId2operIdx(srvrOperIdx, blockOperIdx) - 1;

         if (j == -2)
            j = _operationTab.serverId2FollowingOperIdx(srvrOperIdx, blockOperIdx) - 1;

         while (j >= 0 && !found)
         {
            oper = _operationTab.getOperation(j);
            operType = oper.getType();

            if (operType == ConstInterface.MG_OPER_BLOCK)
            {
               if (j == blockOperIdx)
               {
                  retIdx = j;
                  found = true;
               }
               else
                  j--;
            }
            else if (operType == ConstInterface.MG_OPER_ELSE)
            {
               int blockStrt = getStartBlockIdxByElse(j);
               if (blockStrt == blockOperIdx)
               {
                  retIdx = j;
                  found = true;
               }
               else
                  j = blockStrt - 1;
            }
            else if (operType == ConstInterface.MG_OPER_ENDBLOCK)
               j = getStartBlockIdxByEnd(j) - 1;
            else
               j--;
         }
         return retIdx;
      }

      /// <summary>
      ///   get the level
      /// </summary>
      protected internal char getLevel()
      {
         return _level;
      }

      /// <summary>
      ///   return the event of this handler
      /// </summary>
      internal Event getEvent()
      {
         return _evt;
      }

      /// <summary>
      ///   get the Id of the event handler
      /// </summary>
      internal int getId()
      {
         return _id;
      }

      /// <summary>
      ///   get the control of the event handler
      /// </summary>
      internal MgControl getStaticCtrl()
      {
         return _ctrl;
      }

      internal MgControl getRunTimeCtrl()
      {
         return ClientManager.Instance.EventsManager.getLastRtEvent().Control;
      }

      /// <summary>
      ///   returns true if the handler is enabled
      /// </summary>
      internal bool isEnabled()
      {
         return _enabledExp.getVal();
      }

      /// <summary>
      ///   reset the values of the local variables using the argument values or the default
      ///   values or the init expression
      /// </summary>
      /// <param name = "args">the arguments list </param>
      private void resetLocalVariables(ArgumentsList args)
      {
         DataView dv = (DataView)_task.DataView;
         Field aField = null;
         int fieldIdx;
         bool varIsParm;
         bool argSet = false;
         int argIdx = 0;

         try
         {
            for (fieldIdx = _dvPos; fieldIdx < _dvPos + _dvLen; fieldIdx++)
            {
               aField = (Field)dv.getField(fieldIdx);
               varIsParm = (!_hasParameters || aField.isParam());
               argSet = false;

               // check if the field should get its value from an argument
               if (args != null && argIdx < args.getSize() && varIsParm)
               {
                  if (!args.getArg(argIdx).skipArg())
                  {
                     args.getArg(argIdx).setValueToField(aField);
                     argSet = true;
                  }
                  else
                  {
                     aField.invalidate(true, Field.CLEAR_FLAGS);
                     aField.compute(false);
                  }
                   
                  argIdx++;
               }
               else
               {
                  aField.invalidate(true, Field.CLEAR_FLAGS);
                  aField.compute(false);
                 
               }
               aField.updateDisplay();

               // if field not set from argument and is dotnet
               if (!argSet && aField.DNType != null)
               {
                  int key = BlobType.getKey(aField.getValue(false));
                  Object obj = null;

                  if (aField.DNType.IsValueType)
                     obj = ReflectionServices.CreateInstance(aField.DNType, null, null);

                  DNManager.getInstance().DNObjectsCollection.Update(key, obj);
               }
            }
         }
         catch (Exception exception)
         {
            DNException dnException = ClientManager.Instance.RuntimeCtx.DNException;
            dnException.set(exception);
         }
      }

      /// <summary>
      ///   return the result values to the field arguments
      /// </summary>
      /// <param name = "args">the arguments list </param>
      private void getArgValsFromFlds(ArgumentsList args)
      {
         DataView dv = (DataView) _task.DataView;
         Field aField = null;
         Field argFld = null;
         int j, k;
         Argument arg;
         String val;

         k = 0;
         for (j = _dvPos; j < _dvPos + _dvLen && k < args.getSize(); j++)
         {
            aField = (Field)dv.getField(j);
            if (!_hasParameters || aField.isParam())
            {
               arg = args.getArg(k);
               if (arg.getType() == ConstInterface.ARG_TYPE_FIELD)
               {
                  // update the field value from the the Param
                  argFld = arg.getField();
                  //A handler can be called either from the same task (of the handler) or from a 
                  //task lower down in the hierarchy.
                  //Here we are copying value from the handler task to the invoker task.
                  //Copying should not be done if any one of the task is aborting.
                  //But checking only the invoker task's isAborting is sufficient here because, 
                  //it will be true even if the handler task is aborting.
                  if (argFld.getTask() != null && !((Task)argFld.getTask()).isAborting())
                  {
                     val = aField.getValue(false);

                     // #785138- if src field or destfield is dotnet, perform conversion between dotnet and non-dotnet types.
                     if (aField.getType() == StorageAttribute.DOTNET || argFld.getType() == StorageAttribute.DOTNET)
                        val = Argument.convertArgs(val, aField.getType(), argFld.getType());

                     argFld.setValueAndStartRecompute(val, aField.isNull(), true, aField.isModified(), false);
                     argFld.updateDisplay();
                  }
               }
               k++;
            }

            //clear the object, if field in the handler is a dotnet and the task is not aborting (in this 
            //case, it is already cleared while terminating the task.)
            if (aField.getType() == StorageAttribute.DOTNET && !((Task)aField.getTask()).isAborting())
            {
               int key = BlobType.getKey(aField.getValue(false));
               DNManager.getInstance().DNObjectsCollection.Update(key, null);
            }
         }
      }

      /// <summary>
      ///   returns the handler's task
      /// </summary>
      internal Task getTask()
      {
         return _task;
      }

      /// <summary>
      ///   returns the number of parameters expected by the handler
      /// </summary>
      internal bool argsMatch(ExpressionEvaluator.ExpVal[] Exp_params)
      {
         bool argsMatched = true;
         DataView dv = (DataView) _task.DataView;
         int i = 0;
         int j = 0;
         Field field = null;
         int paramCount = 0;
         StorageAttribute fieldAttr;
         StorageAttribute argAttr;

         for (i = _dvPos; i < _dvPos + _dvLen; i++)
         {
            field = (Field)dv.getField(i);
            if (field.isParam())
            {
               paramCount++;
               if (j < Exp_params.Length)
               {
                  fieldAttr = field.getType();
                  argAttr = Exp_params[j].Attr;

                  // Dotnet conversions. magic types to Dotnet and viceversa, dotnet to dotnet.
                  if (StorageAttributeCheck.isTypeDotNet(fieldAttr) || StorageAttributeCheck.isTypeDotNet(argAttr))
                  {
                     if (StorageAttributeCheck.isTheSameType(fieldAttr, argAttr)) // dotnet to dotnet
                     {
                        try
                        {
                           // Allowed conversions - if cast is successful
                           ReflectionServices.DynCast(Exp_params[j].DnMemberInfo.value, field.DNType);
                        }
                        catch (Exception)
                        {
                           argsMatched = false;
                        }
                     }
                     else
                     {
                        if (StorageAttributeCheck.isTypeDotNet(fieldAttr)) // magic to dotnet
                           argsMatched = DNConvert.canConvert(argAttr, field.DNType);
                        else if (Exp_params[j].DnMemberInfo.value != null) // dotnet to magic
                           argsMatched = DNConvert.canConvert(fieldAttr, Exp_params[j].DnMemberInfo.value.GetType());
                     }

                     if (!argsMatched)
                        break;
                  }

                  // argument and parameter will not match if :
                  // 1. They are not of the same type
                  // 2. They Cannot be cast (alpha can be cast to unicode)
                  // 3. All those apply if the parameter is not null. if the param val is null it can go into any type of parameter.
                  else if (!Exp_params[j].IsNull && !StorageAttributeCheck.isTheSameType(fieldAttr, argAttr) && 
                           !StorageAttributeCheck.StorageFldAlphaUnicodeOrBlob(fieldAttr, argAttr))
                  {
                     argsMatched = false;
                     break;
                  }
                  j++;
               }
            }
         }

         if (paramCount != Exp_params.Length)
            argsMatched = false;

         return argsMatched;
      }

      /// <summary>
      ///   returns the OperationTable
      /// </summary>
      internal OperationTable getOperationTab()
      {
         return _operationTab;
      }

      /// <summary>
      ///   check is task scope is appropriate
      /// </summary>
      /// <param name = "isSameTask"></param>
      /// <returns></returns>
      internal bool checkTaskScope(bool isSameTask)
      {
         if (_scope == BrkScope.Task && !isSameTask)
            return false;
         return true;
      }

      public override string ToString()
      {
         return String.Format("(Hanlder for {0} on task {1}, scope {2})", _evt, _task, _scope);
      }

      #region Nested type: BoolRef

      /// <summary>
      ///   a wrapper class for boolean value which lets us pass it as a reference
      ///   that may be updated.
      /// </summary>
      private class BoolRef
      {
         private bool _b;

         /// <summary>
         ///   CTOR with initial value
         /// </summary>
         internal BoolRef(bool val)
         {
            setVal(val);
         }

         /// <summary>
         ///   set boolean value
         /// </summary>
         internal void setVal(bool val)
         {
            _b = val;
         }

         /// <summary>
         ///   get boolean value
         /// </summary>
         internal bool getVal()
         {
            return _b;
         }
      }

      #endregion

      #region Nested type: RetVals

      /// <summary>
      ///   used to return the results of the execute() method
      /// </summary>
      internal class RetVals
      {
         internal bool BlockIfReturnedFromServer { get; private set; }
         internal bool Enabled { get; private set; }
         internal int NextOperIdx { get; private set; }
         internal bool Propagate { get; private set; }
         internal bool ReturnedFromServer { get; private set; }

         /// <param name = "p">propagate </param>
         /// <param name = "e">enabled </param>
         internal RetVals(bool p, bool e)
         {
            Propagate = p;
            Enabled = e;
            NextOperIdx = 0;
            ReturnedFromServer = false;
            BlockIfReturnedFromServer = false;
         }

         /// <summary>
         /// 
         /// </summary>
         /// <param name="operIdx"></param>
         /// <param name="inReturnedFromServer"></param>
         /// <param name="inBlockIfReturnedFromServer"></param>
         internal RetVals(int operIdx, bool inReturnedFromServer, bool inBlockIfReturnedFromServer)
         {
            Propagate = false;
            Enabled = true;
            NextOperIdx = operIdx;
            ReturnedFromServer = inReturnedFromServer;
            BlockIfReturnedFromServer = inBlockIfReturnedFromServer;
         }
      }

      #endregion
   }
}
