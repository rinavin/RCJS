using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using Constants = com.magicsoftware.util.Constants;
using com.magicsoftware.unipaas;
using com.magicsoftware.richclient.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.util
{
   internal class FlowMonitorQueue : IFlowMonitorQueue
   {
      static private FlowMonitorQueue _instance; // singleton

      /// <summary>
      ///   These are strings used by Flow Monitor & Remove Flow Monitor
      /// </summary>
      private const String S_EVENT_STR1 = ">>Starts ";
      private const String S_EVENT_STR2 = " Event";
      private const String S_EVENT_PROPAGATED = "Event was propagated";
      private const String E_EVENT_STR = "<<Ends Event";
      private const String TSK_CHNG_MODE = "Task Mode Change - ";
      private const String S_RECPRF_STR = "Starts Record Prefix";
      private const String E_RECPRF_STR = "Ends Record Prefix";
      private const String S_RECSUF_STR = "Starts Record Suffix";
      private const String E_RECSUF_STR = "Ends Record Suffix";
      private const String S_TASKSUF_STR = "Starts Task Suffix";
      private const String E_TASKSUF_STR = "Ends Task Suffix";
      private const String S_TASKPRF_STR = "Starts Task Prefix";
      private const String E_TASKPRF_STR = "Ends Task Prefix";
      private const String S_CTRLPRF_STR = "Starts Control Prefix - ";
      private const String E_CTRLPRF_STR = "Ends Control Prefix - ";
      private const String S_CTRLSUF_STR = "Starts Control Suffix - ";
      private const String E_CTRLSUF_STR = "Ends Control Suffix - ";
      private const String S_HANDLER_STR = "Starts handling event {0}";
      private const String E_HANDLER_STR = "Ends handling event {0}";
      private const String S_CTRLVER_STR = "Starts Control Verification for Control - ";
      private const String E_CTRLVER_STR = "Ends Control Verification for Control - ";
      private const String RECOMP_STR = "Recomputes - ";
      private const String S_VARIABLE_STR = "Starts Variable Change - ";
      private const String E_VARIABLE_STR = "Ends Variable Change - ";
      private const String VARIABLE_REASON_STR = " - Reason - Previous value";
      private const String INFORM_STR = " >> INFORMATION >> ";

      private const String FLW_STEP_FWD = "(Step Forward)";
      private const String FLW_FAST_FWD = "(Fast Forward)";
      private const String FLW_STEP_BWD = "(Step Backward)";
      private const String FLW_FAST_BWD = "(Fast Backward)";
      // private final String FLW_CANCELED         = "(Canceled)";
      // private final String FLW_RNG_FROM         = "(Range From)";
      // private final String FLW_RNG_TO           = "(Range To)";
      private const String FLW_NOT_EXEC = "[Not Executed]";
      private const String FLW_PERFIX = "Flow - ";
      private const String S_UPDATE_STR = "Starts Update";
      private const String E_UPDATE_STR = "Ends Update";

      // types of flow monitor displaied activities:
      private const char ACT_TASK = 'T';
      private const char ACT_TASK_FLW = 'F';
      private const char ACT_RECOMPUTE = 'R';
      private const char ACT_FLW_OPER = 'T';

      private readonly DateTimeFormatInfo _formatter;
      private readonly magicsoftware.util.Queue<ActivityItem> _queue = new magicsoftware.util.Queue<ActivityItem>();
      private bool _enabled;
      private bool _isFlowOperation;
      private bool _isRecompute;
      private bool _isTask;
      private bool _isTaskFlow;

      /// <summary>
      ///  tells whether flow monitor messages should be serialized or not?
      /// </summary>
      /// <returns></returns>
      internal bool ShouldSerialize { get; set; }

      /// <summary>
      /// singleton
      /// </summary>
      internal static FlowMonitorQueue Instance
      {
         get
         {
            if (_instance == null)
            {
               lock (typeof(FlowMonitorQueue))
               {
                  if (_instance == null)
                     _instance = new FlowMonitorQueue();
               }
            }
            return _instance;
         }
      }

      /// <summary>
      /// singleton
      /// </summary>
      private FlowMonitorQueue()
      {
         _formatter = new DateTimeFormatInfo
                        {
                           LongTimePattern = "HH:mm:ss:fff",
                           DateSeparator = "",
                           LongDatePattern = "",
                           ShortDatePattern = ""
                        };
      }

      #region IFlowMonitorQueue Members

      ///<param name="contextID"></param>
      /// <param name = "newTaskMode">new task mode for the task</param>
      public void addTaskCngMode(Int64 contextID, char newTaskMode)
      {
         String info = TSK_CHNG_MODE;
         ActivityItem act;

         if (_enabled && _isTask)
         {
            act = new ActivityItem(this, ACT_TASK, FlowMonitorInterface.FLWMTR_CHNG_MODE);

            switch (newTaskMode)
            {
               case Constants.TASK_MODE_MODIFY:
                  info += "Modify";
                  break;


               case Constants.TASK_MODE_CREATE:
                  info += "Create";
                  break;


               case Constants.TASK_MODE_DELETE:
                  info += "Delete";
                  break;


               case Constants.TASK_MODE_QUERY:
                  info += "Query";
                  break;


               default:
                  info = null;
                  break;
            }

            act.setInfo(info);
            _queue.put(act);
         }
      }

      #endregion

      /// <summary>
      ///   parse the data
      /// </summary>
      internal void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // find last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_FLWMTR_CONFIG) +
                                                                ConstInterface.MG_TAG_FLWMTR_CONFIG.Length);

            List<String> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            Logger.Instance.WriteDevToLog("in FlowMonitorQueue.FillData: " + Misc.CollectionToString(tokensVector));

            initElements(tokensVector);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete "/>" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in  FlowMonitorQueue.FillData() out of string bounds");
      }

      /// <summary>
      ///   parse the XML data
      /// </summary>
      /// <param name = "tokensVector">attribute/value/...attribute/value/ vector</param>
      private void initElements(IList tokensVector)
      {
         for (int j = 0;
              j < tokensVector.Count;
              j += 2)
         {
            string attribute = ((String)tokensVector[j]);
            string valueStr = ((String)tokensVector[j + 1]);
            Logger.Instance.WriteDevToLog(attribute + " value: " + valueStr);

            switch (attribute)
            {
               case ConstInterface.MG_ATTR_TASK:
                  _isTask = XmlParser.getBoolean(valueStr);
                  break;

               case ConstInterface.MG_ATTR_TASKFLW:
                  _isTaskFlow = XmlParser.getBoolean(valueStr);
                  break;

               case ConstInterface.MG_ATTR_RECOMP:
                  _isRecompute = XmlParser.getBoolean(valueStr);
                  break;

               case ConstInterface.MG_ATTR_FLWOP:
                  _isFlowOperation = XmlParser.getBoolean(valueStr);
                  break;

               case ConstInterface.MG_ATTR_ENABLED:
                  enable(XmlParser.getBoolean(valueStr));
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog("in FlowMonitorQueue.initElements(): unknown  attribute: " + attribute);
                  break;
            }
         }
      }

      /// <summary>
      ///   build XML and empty the inner queue
      /// </summary>
      internal void buildXML(StringBuilder message)
      {
         ActivityItem currAct;

         if (!_queue.isEmpty() && ShouldSerialize)
         {
            message.Append(XMLConstants.START_TAG + ConstInterface.MG_TAG_FLWMTR_MSG + XMLConstants.TAG_CLOSE);

            while (!_queue.isEmpty())
            {
               currAct = (ActivityItem)_queue.get();
               currAct.buildXML(message);
            } // fill activities

            message.Append("</" + ConstInterface.MG_TAG_FLWMTR_MSG + XMLConstants.TAG_CLOSE);
         } // the queue is not empty
      }

      /// <summary>
      ///   is the Flow Monitor Queue empty
      /// </summary>
      internal bool isEmpty()
      {
         return _queue.isEmpty();
      }

      /// <summary>
      ///   enable the flow monitor
      /// </summary>
      /// <returns>the previous state (enabled/disabled)</returns>
      internal bool enable(bool value)
      {
         bool wasEnabled = _enabled;
         _enabled = value;
         ShouldSerialize = _enabled;
         return wasEnabled;
      }

      /// <summary>
      ///   add Activities to the queue
      /// </summary>
      /// <param name = "triggeredBy"></param>
      /// <param name = "state">of the event task activitie</param>
      internal void addTaskEvent(String triggeredBy, int state)
      {
         ActivityItem act;

         if (_enabled && _isTask)
         {
            act = new ActivityItem(this, ACT_TASK, FlowMonitorInterface.FLWMTR_EVENT);

            String info;
            switch (state)
            {
               case FlowMonitorInterface.FLWMTR_START:
                  info = S_EVENT_STR1 + triggeredBy + S_EVENT_STR2;
                  break;

               case FlowMonitorInterface.FLWMTR_END:
                  info = E_EVENT_STR + triggeredBy;
                  break;

               case FlowMonitorInterface.FLWMTR_PROPAGATE:
                  info = S_EVENT_PROPAGATED;
                  break;

               default:
                  info = null;
                  break;
            }

            act.setInfo(info);
            _queue.put(act);
         }
      }

      /// <summary>
      ///   add task flow for record prefix or sufix
      /// </summary>
      /// <param name = "id">is FLWMTR_PREFIX or FLWMTR_SUFFIX
      /// </param>
      /// <param name = "state">of the event task activitie
      /// </param>
      internal void addTaskFlowRec(int id, int state)
      {
         ActivityItem act;

         if (_enabled && _isTaskFlow)
         {
            String info;
            switch (id)
            {
               case InternalInterface.MG_ACT_REC_PREFIX:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_RECPRF_STR
                            : E_RECPRF_STR;
                  id = FlowMonitorInterface.FLWMTR_PREFIX;
                  break;

               case InternalInterface.MG_ACT_REC_SUFFIX:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_RECSUF_STR
                            : E_RECSUF_STR;
                  id = FlowMonitorInterface.FLWMTR_SUFFIX;
                  break;

               case InternalInterface.MG_ACT_TASK_PREFIX:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_TASKPRF_STR
                            : E_TASKPRF_STR;
                  id = FlowMonitorInterface.FLWMTR_PREFIX;
                  break;

               case InternalInterface.MG_ACT_TASK_SUFFIX:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_TASKSUF_STR
                            : E_TASKSUF_STR;
                  id = FlowMonitorInterface.FLWMTR_SUFFIX;
                  break;

               default:
                  info = null;
                  break;
            }

            act = new ActivityItem(this, ACT_TASK_FLW, id);
            act.setInfo(info);
            _queue.put(act);
         }
      }

      /// <param name = "id">is MG_ACT_VARIABLE</param>
      /// <param name = "fldName"></param>
      /// <param name = "state">of the event task activitie</param>
      internal void addTaskFlowFld(int id, String fldName, int state)
      {
         addTaskFlowCtrl(id, fldName, state);
      }

      /// <param name = "id">is FLWMTR_CTRL_PREFIX or FLWMTR_CTRL_SUFFIX</param>
      /// <param name = "ctrlName"></param>
      /// <param name = "state">of the event task activitie</param>
      internal void addTaskFlowCtrl(int id, String ctrlName, int state)
      {
         ActivityItem act;

         if (_enabled && _isTaskFlow)
         {
            String info;
            switch (id)
            {
               case InternalInterface.MG_ACT_VARIABLE:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_VARIABLE_STR
                            : E_VARIABLE_STR;
                  id = FlowMonitorInterface.FLWMTR_VARCHG_VALUE;
                  break;

               case InternalInterface.MG_ACT_CTRL_PREFIX:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_CTRLPRF_STR
                            : E_CTRLPRF_STR;
                  id = FlowMonitorInterface.FLWMTR_CTRL_PREFIX;
                  break;


               case InternalInterface.MG_ACT_CTRL_SUFFIX:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_CTRLSUF_STR
                            : E_CTRLSUF_STR;
                  id = FlowMonitorInterface.FLWMTR_CTRL_SUFFIX;
                  break;

               case InternalInterface.MG_ACT_CTRL_VERIFICATION:
                  info = state == FlowMonitorInterface.FLWMTR_START
                            ? S_CTRLVER_STR
                            : E_CTRLVER_STR;
                  id = FlowMonitorInterface.FLWMTR_CTRL_SUFFIX;
                  break;

               default:
                  info = null;
                  break;
            }

            act = new ActivityItem(this, ACT_TASK_FLW, id);
            if (info != null)
            {
               info += ctrlName;
               if (id == FlowMonitorInterface.FLWMTR_VARCHG_VALUE && state == FlowMonitorInterface.FLWMTR_START)
                  info += VARIABLE_REASON_STR;
               act.setInfo(info);
            }
            _queue.put(act);
         }
      }

      /// <param name = "handlerId">isid of the handler
      /// </param>
      /// <param name = "state">of the event task activitie
      /// </param>
      internal void addTaskFlowHandler(String handlerId, int state)
      {
         ActivityItem act;

         if (_enabled && _isTaskFlow)
         {
            act = new ActivityItem(this, ACT_TASK_FLW, FlowMonitorInterface.FLWMTR_TSK_HANDLER);

            String info;
            switch (state)
            {
               case FlowMonitorInterface.FLWMTR_START:
                  info = String.Format(S_HANDLER_STR, handlerId);
                  break;

               case FlowMonitorInterface.FLWMTR_END:
                  info = String.Format(E_HANDLER_STR, handlerId);
                  break;

               default:
                  info = null;
                  break;
            }

            if (info != null)
            {
               // BRK_LEVEL_HANDLER_INTERNAL,BRK_LEVEL_HANDLER_SYSTEM,BRK_LEVEL_HANDLER_TIMER
               // BRK_LEVEL_HANDLER_EXPRESSION,BRK_LEVEL_HANDLER_ERROR,BRK_LEVEL_HANDLER_USER
               act.setInfo(info);
            }
            _queue.put(act);
         }
      }

      /// <param name = "triggeredByVarName">var name , has triggered Recompute
      /// </param>
      internal void addRecompute(String triggeredByVarName)
      {
         ActivityItem act;

         if (_enabled && _isRecompute)
         {
            act = new ActivityItem(this, ACT_RECOMPUTE, FlowMonitorInterface.FLWMTR_RECOMP);
            act.setInfo(RECOMP_STR + triggeredByVarName);
            _queue.put(act);
         }
      }

      /// <summary>
      ///   Add a field flow operation activity item.
      /// </summary>
      /// <param name = "oper">The operation being logged.</param>
      /// <param name = "flowMode">The task's flow mode.</param>
      /// <param name = "flowDirection">The task's flow direction.</param>
      /// <param name = "bExecuted">Will the operation be executed.</param>
      internal void addFlowFieldOperation(Operation oper, Flow flowMode, Direction flowDirection, bool bExecuted)
      {
         if (_enabled && _isFlowOperation)
         {
            ActivityItem act = new ActivityItem(this, ACT_FLW_OPER, FlowMonitorInterface.FLWMTR_DATA_OPER);
            StringBuilder buffer = new StringBuilder(FLW_PERFIX);
            oper.AddFlowDescription(buffer);
            buffer.Append(' ');
            switch (flowMode)
            {
               case Flow.FAST:
                  if (flowDirection == Direction.FORE)
                     buffer.Append(FLW_FAST_FWD);
                  else if (flowDirection == Direction.BACK)
                     // FLOW_BACK
                     buffer.Append(FLW_FAST_BWD);
                  break;
               // We add nothing for FLOW_NONE

               case Flow.STEP:
                  if (flowDirection == Direction.FORE)
                     buffer.Append(FLW_STEP_FWD);
                  else if (flowDirection == Direction.BACK)
                     // FLOW_BACK
                     buffer.Append(FLW_STEP_BWD);
                  break;
               // For operation mode NONE we write STEP_FORWARD - 
               // see FLWMTR_CTX::output_  case FLWMTR_DATA_OPER:

               case Flow.NONE:
                  buffer.Append(FLW_STEP_FWD);
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog("FlowMonitorQueue.addFlowFieldOperation unknown flow mode " + flowMode);
                  return;
            }

            if (!bExecuted)
               buffer.Append(FLW_NOT_EXEC);

            act.setInfo(buffer.ToString());
            _queue.put(act);
         }
      }

      /// <param name = "state">of the update
      /// </param>
      internal void addFlowOperationUpdate(int state)
      {
         ActivityItem act;

         if (_enabled && _isFlowOperation)
         {
            act = new ActivityItem(this, ACT_FLW_OPER, FlowMonitorInterface.FLWMTR_DATA_OPER); // MG_OPER_UPDATE
            if (state == FlowMonitorInterface.FLWMTR_START)
               act.setInfo(S_UPDATE_STR);
            else
               act.setInfo(E_UPDATE_STR);

            _queue.put(act);
         }
      }

      /// <param name = "info">string passed to status line verify action</param>
      internal void addFlowVerifyInfo(String info)
      {
         addFlowInfo(info);
      }

      /// <param name = "info">string added to flow monitor queue for InvokeOs</param>
      internal void addFlowInvokeOsInfo(String info)
      {
         addFlowInfo(info);
      }

      /// <summary>
      ///   user string passed to status line for actions
      /// </summary>
      /// <param name = "info">
      /// </param>
      private void addFlowInfo(String info)
      {
         if (!_enabled)
            return;

         ActivityItem act = new ActivityItem(this, ACT_FLW_OPER, FlowMonitorInterface.FLWMTR_DATA_OPER);
         StringBuilder buffer = new StringBuilder("");

         if (!info.Equals(""))
         {
            buffer.Append(INFORM_STR);
            buffer.Append(info);
         }
         act.setInfo(buffer.ToString());
         _queue.put(act);
      }

      #region Nested type: ActivityItem

      /// <summary>
      ///   all information about current Activity
      /// </summary>
      private class ActivityItem
      {
         private readonly FlowMonitorQueue _enclosingInstance;

         private readonly int _id;
         private readonly char _type; // ACTY_TASK|ACT_TASK_FLW|ACT_DATAVIEW|ACT_RECOMPUTE|ACT_FLW_OPER
         private String _info;
         private String _time;

         /// <summary>
         /// 
         /// </summary>
         /// <param name="enclosingInstance"></param>
         /// <param name="type"></param>
         /// <param name="id"></param>
         internal ActivityItem(FlowMonitorQueue enclosingInstance, char type, int id)
         {
            _enclosingInstance = enclosingInstance;
            _type = type;
            _id = id;
            setTime();
         }

         /// <summary>
         ///   save current time in the needed format
         /// </summary>
         private void setTime()
         {
            _time = DateTimeUtils.ToString(DateTime.Now, _enclosingInstance._formatter);
         }

         /// <summary>
         ///   set added information for the activity
         /// </summary>
         protected internal void setInfo(String info_)
         {
            _info = info_ != null
                      ? XmlParser.escape(info_)
                      : info_;
         }

         /// <summary>
         ///   build XML string for the activity item
         /// </summary>
         internal void buildXML(StringBuilder message)
         {
            message.Append(XMLConstants.START_TAG + ConstInterface.MG_TAG_FLWMTR_ACT);
            message.Append(" " + XMLConstants.MG_ATTR_TYPE + "=\"" + _type + "\"");
            message.Append(" " + XMLConstants.MG_ATTR_ID + "=\"" + _id + "\"");
            if (_info != null)
               message.Append(" " + ConstInterface.MG_ATTR_INFO + "=\"" + _info + "\"");
            message.Append(" " + ConstInterface.MG_ATTR_TIME + "=\"" + _time + "\"");
            message.Append(XMLConstants.TAG_TERM);
         }
      }

      #endregion
   }
}
