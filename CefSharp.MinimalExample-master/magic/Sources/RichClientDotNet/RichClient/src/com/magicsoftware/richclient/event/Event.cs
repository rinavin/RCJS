using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using Field = com.magicsoftware.richclient.data.Field;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.events
{
   /// <summary>
   ///   this class represent the event entity
   /// </summary>
   internal class Event
   {
      // ATTENTION !
      // any change to the member variables must be followed by a change to the CTOR that
      // gets one argument of the type 'Event'.
      private static long _lastTimestamp; //last timestamp
      protected internal Expression Exp; // for expression events - the expression number
      protected ForceExit _forceExit = ForceExit.None; // The “force exit?is one of: Record, Control, None. This attribute is relevant only for events in the “user events?table.
      protected internal int InternalEvent { get; private set; } // internal event code
      protected KeyboardItem kbdItm; // for system events
      private CallOsShow _osCommandShow;
      private String _osCommandText;
      private bool _osCommandWait;
      protected String _paramAttrs;
      protected String _paramNulls;
      protected String[] _paramVals;
      protected int _parameters;
      protected internal String PrgDescription { get; set; }
      protected internal String PublicName { get; set; }
      private int _returnExp; /* expression number to specify the Function's return value */
      protected int _seconds = Int32.MinValue; // for timer events
      protected long _timestamp;
      protected char _type = Char.MinValue; // System|Internal|Timer|Expression|User|None
      private String _userDefinedFuncName;
      private long _userDefinedFuncNameHashCode;
      protected internal Event UserEvt { get; private set; } // a reference to a user event in the user event table
      private String _userEvtDesc = "";
      protected int _userEvtIdx = Int32.MinValue; // the index of the user event in the tasks user events table
      public String UserEvtTaskTag { get; set; } // the ID of the task where the user event is defined

      /// <summary>
      /// The ID of the task which Event is define in
      /// </summary>
      TaskDefinitionId ownerTaskDefinitionId;
      public TaskDefinitionId OwnerTaskDefinitionId
      {
         get
         {
            return ownerTaskDefinitionId;
         }
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal Event()
      {
         InternalEvent = Int32.MinValue;
         setTimestamp();
      }

      /// <summary>
      ///   copy CTOR
      /// </summary>
      protected internal Event(Event evt)
      {
         _type = evt._type;
         kbdItm = evt.kbdItm;
         InternalEvent = evt.InternalEvent;
         _seconds = evt._seconds;
         Exp = evt.Exp;
         _forceExit = evt._forceExit;
         UserEvt = evt.UserEvt;
         UserEvtTaskTag = evt.UserEvtTaskTag;
         _userEvtIdx = evt._userEvtIdx;
         DotNetEventName = evt.DotNetEventName;
         DotNetType = evt.DotNetType;
         DotNetField = evt.DotNetField;
      }

      /// <summary>
      ///   This constructor creates a matching event object for the passed menuEntryEvent
      /// </summary>
      /// <param name = "menuEntryEvent"> event menu entry for which we create this event </param>
      /// <param name = "ctlIdx"> current ctl idx </param>
      internal Event(MenuEntryEvent menuEntryEvent, int ctlIdx)
         : this()
      {
         // set the event type according to menu type
         if (menuEntryEvent.menuType() == GuiMenuEntry.MenuType.INTERNAL_EVENT)
         {
            setType(ConstInterface.EVENT_TYPE_INTERNAL);
            setInternal(menuEntryEvent.InternalEvent);
         }
         else if (menuEntryEvent.menuType() == GuiMenuEntry.MenuType.SYSTEM_EVENT)
         {
            setType(ConstInterface.EVENT_TYPE_SYSTEM);
            setKeyboardItem(menuEntryEvent.KbdEvent);
         }
         else
         {
            //menuEntryEvent.menuType() == MenuType.MENU_TYPE_USER_EVENT
            Task mainProgTask = MGDataCollection.Instance.GetMainProgByCtlIdx(ctlIdx);
            UserEvt = mainProgTask.getUserEvent(_userEvtIdx);
            setType(ConstInterface.EVENT_TYPE_USER);
            UserEvtTaskTag = mainProgTask.getTaskTag();
            _userEvtIdx = menuEntryEvent.UserEvtIdx - 1;
         }
      }

      /// <summary>
      ///   Create an Event for the passed os menu entry
      /// </summary>
      /// <param name = "osCommand">- the selected os command menu entry </param>
      internal Event(MenuEntryOSCommand osCommand)
         : this()
      {
         setType(ConstInterface.EVENT_TYPE_MENU_OS);
         setOsCommandText(osCommand.OsCommand);
         setOsCommandWait(osCommand.Wait);
         setOsCommandShow(osCommand.Show);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="aType"></param>
      protected Event(char aType)
      {
         setType(aType);
      }

      internal ForceExit ForceExit
      {
         get { return _forceExit; }
      }

      internal String DotNetEventName { get; set; } //name of the .NET event
      internal Type DotNetType { get; set; } //type of the .NET event (exists only for .NET type events
      internal Field DotNetField { get; set; }

      /// <summary>
      ///   Need part input String to relevant for the class data.
      ///   index of end <event ...> tag
      /// </summary>
      /// <param name = "xmlParser"></param>
      /// <param name = "taskRef"></param>
      internal void fillData(XmlParser xmlParser, Task taskRef)
      {
         string userValue;
         int endContext = xmlParser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, xmlParser.getCurrIndex());
         if (endContext != -1 && endContext < xmlParser.getXMLdata().Length)
         {
            //last position of its tag
            String tag = xmlParser.getXMLsubstring(endContext);
            xmlParser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_EVENT) + ConstInterface.MG_TAG_EVENT.Length);

            List<string> tokensVector = XmlParser.getTokens(xmlParser.getXMLsubstring(endContext),
                                                            XMLConstants.XML_ATTR_DELIM);
            initElements(tokensVector, taskRef, out userValue);

            xmlParser.setCurrIndex(endContext + XMLConstants.TAG_CLOSE.Length); //to delete ">" too

            InitInnerObjects(xmlParser);


            if (userValue != null)
               InitUserData(taskRef, userValue);

         }
         else
            Logger.Instance.WriteExceptionToLog("in Event.FillData() out of string bounds");
      }

      private void InitInnerObjects(XmlParser xmlParser)
      {
         string nextTag = xmlParser.getNextTag();
         switch (nextTag)
         {
            case XMLConstants.MG_TAG_TASKDEFINITIONID_ENTRY:
               string xmlBuffer = xmlParser.ReadToEndOfCurrentElement();
               InitTaskDefinitionId(xmlBuffer);

               int endContext = xmlParser.getXMLdata().IndexOf(ConstInterface.MG_TAG_EVENT + XMLConstants.TAG_CLOSE, xmlParser.getCurrIndex());
               xmlParser.setCurrIndex(endContext + (ConstInterface.MG_TAG_EVENT + XMLConstants.TAG_CLOSE).Length);
               break;
            default:
               break;
         }

      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every foundelement</param>
      /// <param name = "taskRef"></param>
      private void initElements(List<String> tokensVector, Task taskRef, out string userValue)
      {
         Modifiers modifier = Modifiers.MODIFIER_NONE;
         int keyCode = -1;
         String dotNetType = null;
         int assemblyId = 0;
         userValue = null;

         for (int j = 0;
              j < tokensVector.Count;
              j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case ConstInterface.MG_ATTR_EVENTTYPE:
                  _type = valueStr[0];
                  break;
               // System|Internal|User|Timer|Expression|Recompute|Dotnet
               case ConstInterface.MG_ATTR_MODIFIER:
                  modifier = (Modifiers)valueStr[0];
                  break;
               // Alt|Ctrl|Shift|None
               case ConstInterface.MG_ATTR_INTERNALEVENT:
                  InternalEvent = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_KEYCODE:
                  keyCode = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_SECONDS:
                  _seconds = XmlParser.getInt(valueStr);
                  break;
               case XMLConstants.MG_ATTR_EXP:
                  // for expression events - the expression number
                  Exp = taskRef.getExpById(XmlParser.getInt(valueStr));
                  break;
               case ConstInterface.MG_ATTR_FORCE_EXIT:
                  _forceExit = (ForceExit)valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_DESC:
                  _userEvtDesc = XmlParser.unescape(valueStr);
                  break;
               case ConstInterface.MG_ATTR_PUBLIC:
                  PublicName = XmlParser.unescape(valueStr);
                  break;
               case ConstInterface.MG_ATTR_PARAMS:
                  _parameters = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_PAR_ATTRS:
                  _paramAttrs = valueStr;
                  break;
               case ConstInterface.MG_ATTR_DOTNET_NAME:
                  DotNetEventName = valueStr;
                  break;
               case ConstInterface.MG_ATTR_DOTNET_VEE:
                  DotNetField = (Field)taskRef.getFieldByValueStr(valueStr);
                  break;
               case XMLConstants.MG_ATTR_DOTNET_TYPE:
                  dotNetType = XmlParser.unescape(valueStr);
                  break;
               case XMLConstants.MG_ATTR_DOTNET_ASSEMBLY_ID:
                  assemblyId = XmlParser.getInt(valueStr);
                  break;
               case XMLConstants.MG_ATTR_VALUE:
                  String paramStr = XmlParser.unescape(valueStr);
                  _paramVals = new String[_parameters];
                  //put the paramVals in place
                  parseParamVal(paramStr);
                  break;
               case ConstInterface.MG_ATTR_NULLS:
                  _paramNulls = valueStr;
                  break;
               case ConstInterface.MG_ATTR_USER:
                  userValue = valueStr;
                  break;
               //USER event
               case XMLConstants.MG_ATTR_NAME:
                  setUserDefinedFuncName(valueStr);
                  break;
               case ConstInterface.MG_TAG_USR_DEF_FUC_RET_EXP_ID:
                  _returnExp = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_TAG_USR_DEF_FUC_RET_EXP_ATTR:
                  break;
               default:
                  Logger.Instance.WriteExceptionToLog(
                     string.Format("There is no such tag in Event class. Insert case to Event.initElements for {0}", attribute));
                  break;
            }
         }
         if (_type == ConstInterface.EVENT_TYPE_SYSTEM)
            kbdItm = new KeyboardItem(keyCode, modifier);
         if (dotNetType != null)
            DotNetType = ReflectionServices.GetType(assemblyId, dotNetType);
         else if (DotNetField != null)
            DotNetType = DotNetField.DNType;
      }

      /// <summary>
      /// Perform the initialization code from the "user" attribute string
      /// </summary>
      /// <param name="valueStr"></param>
      /// <param name="taskRef"></param>
      private void InitUserData(Task taskRef, string userValue)
      {
         int comma = userValue.IndexOf(",");
         if (comma > -1)
         {
            String ueTaskId = userValue.Substring(0, comma);
            if(taskRef != null)
               ueTaskId = taskRef.TaskService.GetEventTaskId(taskRef, ueTaskId, this);

            Task ueTask = (Task)MGDataCollection.Instance.GetTaskByID(ueTaskId);

            int ueIdx = XmlParser.getInt(userValue.Substring(comma + 1));

            if (ueTask != null)
            {
               UserEvt = ueTask.getUserEvent(ueIdx);
               if (UserEvt == null)
                  throw new ApplicationException("in Event.fillData(): user event not found. task id = " +
                                                 ueTaskId + ", user event index = " + ueIdx);
               UserEvtTaskTag = ueTaskId;
               _userEvtIdx = ueIdx;
            }
            //the task hasn't been found, it not parsed, yet
            else
            {
               UserEvtTaskTag = ueTaskId;
               _userEvtIdx = ueIdx;
               UserEvt = null; //can not be initialized on this step
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="xmlBuffer"></param>
      void InitTaskDefinitionId(string xmlBuffer)
      {
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
         this.ownerTaskDefinitionId = taskDefinitionId;
      }

      /// <summary>
      ///   find task for User Event if it was not found during parsing
      /// </summary>
      protected internal void findUserEvent()
      {
         if (_type == ConstInterface.EVENT_TYPE_USER && UserEvt == null)
         {
            Task ueTask = (Task)MGDataCollection.Instance.GetTaskByID(UserEvtTaskTag);
            if (ueTask != null)
               UserEvt = ueTask.getUserEvent(_userEvtIdx);
            if (UserEvt == null)
               throw new ApplicationException(
                  string.Format("in Event.findUserEvent(): user event not found. task id = {0} doesn't exist",
                                UserEvtTaskTag));
         }
      }

      /// <summary>
      ///   compare this event to another - returns true if and only if the events are identical
      /// </summary>
      protected internal bool equals(Event evt)
      {
         bool bEqual = false;

         if (evt == null)
            throw new NullReferenceException("Trying to compare an event with a null event");

         // compare references
         if (this == evt)
            bEqual = true;
         // do shallow comparision
         else if (_type == evt._type)
         {
            switch (_type)
            {
               case ConstInterface.EVENT_TYPE_SYSTEM:
                  bEqual = kbdItm.equals(evt.kbdItm);
                  break;

               case ConstInterface.EVENT_TYPE_INTERNAL:
                  bEqual = (InternalEvent == evt.InternalEvent);
                  break;

               case ConstInterface.EVENT_TYPE_TIMER:
                  bEqual = (_seconds == evt._seconds);
                  break;

               case ConstInterface.EVENT_TYPE_EXPRESSION:
               case ConstInterface.EVENT_TYPE_PUBLIC:
                  bEqual = (Exp == evt.Exp);
                  break;

               case ConstInterface.EVENT_TYPE_USER:
                  bEqual = (UserEvt == evt.UserEvt);
                  break;

               case ConstInterface.EVENT_TYPE_USER_FUNC:
                  if (_userDefinedFuncNameHashCode == evt.getUserDefinedFuncNameHashCode())
                  {
                     if (evt.getUserDefinedFuncName().Equals(_userDefinedFuncName,
                                                             StringComparison.InvariantCultureIgnoreCase))
                        bEqual = true;
                  }
                  break;

               case ConstInterface.EVENT_TYPE_DOTNET:
                  bEqual = (DotNetType == evt.DotNetType && String.Equals(DotNetEventName, evt.DotNetEventName)
                            && DotNetField == evt.DotNetField);
                  break;

               default:
                  bEqual = false;
                  break;
            }
         }

         // For internal events, compare the internal names
         if (_type == ConstInterface.EVENT_TYPE_PUBLIC || evt._type == ConstInterface.EVENT_TYPE_PUBLIC)
         {
            String name1 = null;
            String name2 = null;

            if (_type != ConstInterface.EVENT_TYPE_USER && evt._type != ConstInterface.EVENT_TYPE_USER)
               bEqual = false;
            else if (_type == ConstInterface.EVENT_TYPE_PUBLIC)
            {
               name1 = PublicName;
               name2 = evt.getUserEvent().PublicName;
            }
            else
            {
               name1 = evt.PublicName;
               name2 = getUserEvent().PublicName;
            }

            if (name1 == null || name2 == null)
               bEqual = false;
            else
               bEqual = name1.Equals(name2);
         }
         else if (_type == ConstInterface.EVENT_TYPE_INTERNAL && evt._type == ConstInterface.EVENT_TYPE_SYSTEM)
         {
            if (ClientManager.Instance.getCurrTask() != null &&
                ClientManager.Instance.EventsManager.getMatchingAction(ClientManager.Instance.getCurrTask(), evt.getKbdItm(), false) ==
                getInternalCode())
               bEqual = true;
         }
         else if (_type == ConstInterface.EVENT_TYPE_DOTNET)
         {
            if (ReflectionServices.IsAssignableFrom(DotNetType, evt.DotNetType))
            {
               //else if (evt.type == ConstInterface.EVENT_TYPE_INTERNAL)
               //{
               //   if (DotNetEventName.Equals("Click") && evt.getInternalCode() == InternalInterface.MG_ACT_WEB_CLICK)
               //      bEqual = true;
               //}
               if (evt._type == ConstInterface.EVENT_TYPE_DOTNET)
               {
                  bEqual = DotNetEventName.Equals(evt.DotNetEventName);
               }
            }
         }
         else if (_type == ConstInterface.EVENT_TYPE_USER && evt._type != ConstInterface.EVENT_TYPE_USER)
         {
            //before using the user event, check that it is valid
            findUserEvent();

            // if one event of the two is a user event and the other isn't then
            // compare the trigger of the user event to the non-user event
            bEqual = UserEvt.equals(evt);
         }

         return bEqual;
      }

      /// <summary>
      ///   set the internal name
      /// </summary>
      internal void setPublicName()
      {
         // For a runtime internal event - calculate the internal name.
         if (_type == ConstInterface.EVENT_TYPE_PUBLIC && Exp != null)
         {
            PublicName = Exp.evaluate(255).mgVal;
            if (PublicName != null)
               PublicName = StrUtil.rtrim(PublicName);
         }
      }

      /// <summary>
      ///   set an Internal event
      /// </summary>
      /// <param name = "code">the code of the internal event </param>
      protected internal void setInternal(int code)
      {
         _type = ConstInterface.EVENT_TYPE_INTERNAL;
         InternalEvent = code;

         // MG_ACT_HIT/MG_ACT_CTRL_HIT are caused by mouse click. In this case 
         // time of last user action for IDLE function should be re-evaluated.
         if (code == InternalInterface.MG_ACT_HIT || code == InternalInterface.MG_ACT_CTRL_HIT)
            ClientManager.Instance.LastActionTime = Misc.getSystemMilliseconds();
      }

      /// <summary>
      ///   set an Internal event
      /// </summary>
      /// <param name = "name">the code of the internal event </param>
      internal void setDotNet(string name)
      {
         _type = ConstInterface.EVENT_TYPE_DOTNET;
         DotNetEventName = name;
      }

      /// <summary>
      ///   set an Expression event
      /// </summary>
      /// <param name = "expRef">a reference to the expression object </param>
      internal void setExpression(Expression expRef)
      {
         _type = ConstInterface.EVENT_TYPE_EXPRESSION;
         Exp = expRef;
      }

      /// <summary>
      ///   get Expression of the event
      /// </summary>
      protected internal Expression getExpression()
      {
         return Exp;
      }

      /// <summary>
      ///   get UserEvent description
      /// </summary>
      private String getUsrEvntDesc()
      {
         return UserEvt != null
                   ? UserEvt._userEvtDesc
                   : _userEvtDesc;
      }

      /// <summary>
      ///   set a Timer event
      /// </summary>
      /// <param name = "sec">the number of seconds for this events
      /// </param>
      protected internal void setTimer(int sec)
      {
         _type = ConstInterface.EVENT_TYPE_TIMER;
         _seconds = sec;
      }

      /// <summary>
      ///   set a System event
      /// </summary>
      /// <param name = "keyboardItem">the keyboard item object </param>
      internal void setSystem(KeyboardItem keyboardItem)
      {
         _type = ConstInterface.EVENT_TYPE_SYSTEM;
         kbdItm = keyboardItem;
      }

      /// <summary>
      ///   set a System event
      /// </summary>
      /// <param name = "keyboardItem">the keyboard item object </param>
      internal void setKeyboardItem(KeyboardItem keyboardItem)
      {
         kbdItm = keyboardItem;
      }

      /// <summary>
      ///   get type returns System|Internal|User|Timer|Expression|Recompute
      /// </summary>
      protected internal char getType()
      {
         return _type;
      }

      /// <summary>
      ///   set type receives System|Internal|User|Timer|Expression|Recompute
      /// </summary>
      protected internal void setType(char aType)
      {
         _type = aType;
      }

      /// <summary>
      ///   get internal event code
      /// </summary>
      /// <returns> int is the internal event code </returns>
      protected internal int getInternalCode()
      {
         if (_type == ConstInterface.EVENT_TYPE_INTERNAL)
            return InternalEvent;
         return Int32.MinValue;
      }

      /// <summary>
      ///   get the keyboard item
      /// </summary>
      internal KeyboardItem getKbdItm()
      {
         if (_type == ConstInterface.EVENT_TYPE_SYSTEM)
            return kbdItm;
         return null;
      }

      /// <summary>
      ///   get the keyboard item
      /// </summary>
      internal KeyboardItem getKbdItmAlways()
      {
         return kbdItm;
      }

      /// <summary>
      ///   returns true for non data events or data events which their expression
      ///   evaluates to true
      /// </summary>
      protected internal bool dataEventIsTrue()
      {
         if (_type != ConstInterface.EVENT_TYPE_EXPRESSION &&
             !(_type == ConstInterface.EVENT_TYPE_USER && getUserEventType() == ConstInterface.EVENT_TYPE_EXPRESSION))
            return true;
         if (_type != ConstInterface.EVENT_TYPE_USER)
            return DisplayConvertor.toBoolean(Exp.evaluate(StorageAttribute.BOOLEAN, 0));
         return
            DisplayConvertor.toBoolean(UserEvt.getExpression().evaluate(StorageAttribute.BOOLEAN, 0));
      }

      /// <summary>
      ///   get number of seconds (for timer events)
      /// </summary>
      internal int getSeconds()
      {
         return _seconds;
      }

      /// <summary>
      ///   get the break level of Event not for Flow Monitoring
      /// </summary>
      protected internal virtual String getBrkLevel()
      {
         return getBrkLevel(false);
      }

      /// <summary>
      ///   get the break level of Event
      /// </summary>
      /// <param name = "flowMonitor">is it needed for Flow Monitoring usage </param>
      protected internal String getBrkLevel(bool flowMonitor)
      {
         String level = "";

         switch (_type)
         {
            case ConstInterface.EVENT_TYPE_INTERNAL:
               switch (InternalEvent)
               {
                  case InternalInterface.MG_ACT_TASK_PREFIX:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_TASK_PREFIX_FM
                                : ConstInterface.BRK_LEVEL_TASK_PREFIX;
                     break;

                  case InternalInterface.MG_ACT_TASK_SUFFIX:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_TASK_SUFFIX_FM
                                : ConstInterface.BRK_LEVEL_TASK_SUFFIX;
                     break;

                  case InternalInterface.MG_ACT_REC_PREFIX:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_REC_PREFIX_FM
                                : ConstInterface.BRK_LEVEL_REC_PREFIX;
                     break;

                  case InternalInterface.MG_ACT_REC_SUFFIX:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_REC_SUFFIX_FM
                                : ConstInterface.BRK_LEVEL_REC_SUFFIX;
                     break;

                  case InternalInterface.MG_ACT_CTRL_PREFIX:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_CTRL_PREFIX_FM
                                : ConstInterface.BRK_LEVEL_CTRL_PREFIX + '_';
                     break;

                  case InternalInterface.MG_ACT_CTRL_SUFFIX:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_CTRL_SUFFIX_FM
                                : ConstInterface.BRK_LEVEL_CTRL_SUFFIX + '_';
                     break;

                  case InternalInterface.MG_ACT_CTRL_VERIFICATION:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_CTRL_VERIFICATION_FM
                                : ConstInterface.BRK_LEVEL_CTRL_VERIFICATION + '_';
                     break;

                  case InternalInterface.MG_ACT_VARIABLE:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_VARIABLE_FM
                                : ConstInterface.BRK_LEVEL_VARIABLE + '_';
                     break;

                  default:
                     level = flowMonitor
                                ? ConstInterface.BRK_LEVEL_HANDLER_INTERNAL_FM
                                : ConstInterface.BRK_LEVEL_HANDLER_INTERNAL + '_';
                     break;
               }
               level += getInternalEvtDescription();
               break;

            case ConstInterface.EVENT_TYPE_SYSTEM:
               level = flowMonitor
                          ? ConstInterface.BRK_LEVEL_HANDLER_SYSTEM_FM
                          : ConstInterface.BRK_LEVEL_HANDLER_SYSTEM + '_';
               level += getKeyboardItemString();
               break;

            case ConstInterface.EVENT_TYPE_TIMER:
               level = flowMonitor
                          ? ConstInterface.BRK_LEVEL_HANDLER_TIMER_FM + ' ' + seconds2String()
                          : ConstInterface.BRK_LEVEL_HANDLER_TIMER + '_' + seconds2String();
               break;

            case ConstInterface.EVENT_TYPE_EXPRESSION:
               //exp cannot be equal to null
               level = flowMonitor
                          ? ConstInterface.BRK_LEVEL_HANDLER_EXPRESSION_FM
                          : ConstInterface.BRK_LEVEL_HANDLER_EXPRESSION + '_' + Exp.getId();
               break;

            case ConstInterface.EVENT_TYPE_USER:
               level = (flowMonitor
                           ? (ConstInterface.BRK_LEVEL_HANDLER_USER_FM + ": ")
                           : (ConstInterface.BRK_LEVEL_HANDLER_USER + '_')) +
                       (UserEvt != null
                           ? UserEvt._userEvtDesc
                           : _userEvtDesc);
               break;

            case ConstInterface.EVENT_TYPE_USER_FUNC:
               level = (flowMonitor
                           ? (ConstInterface.BRK_LEVEL_USER_FUNCTION_FM + ':')
                           : (ConstInterface.BRK_LEVEL_USER_FUNCTION + '_')) + _userDefinedFuncName;
               break;

            case ConstInterface.EVENT_TYPE_DOTNET:
               level = (flowMonitor
                           ? (ConstInterface.BRK_LEVEL_HANDLER_DOTNET_FM)
                           : (ConstInterface.BRK_LEVEL_HANDLER_DOTNET + '_')) + getDotnetEvtDescription();
               break;
         }
         return level;
      }

      /// <summary>
      ///   get description of the internal event
      /// </summary>
      /// <returns> description of the event </returns>
      private String getInternalEvtDescription()
      {
         String description;
         switch (InternalEvent)
         {
            case InternalInterface.MG_ACT_ABOUT:
               description = "About Magic";
               break;

            case InternalInterface.MG_ACT_ACTION_LIST:
               description = "Action List";
               break;

            case InternalInterface.MG_ACT_ADD_HYPERLINK:
               description = "";
               break;

            case InternalInterface.MG_ACT_ALIGN_LEFT:
               description = "Left";
               break;

            case InternalInterface.MG_ACT_ALIGN_RIGHT:
               description = "Right";
               break;

            case InternalInterface.MG_ACT_APPL_PROPERTIES:
               description = "";
               break;

            case InternalInterface.MG_ACT_APPLICATIONS:
               description = "Applications";
               break;

            case InternalInterface.MG_ACT_APPLICATIONS_LIST:
               description = "Applications List";
               break;

            case InternalInterface.MG_ACT_ATTACH_TO_TABLE:
               description = "Attach to Table";
               break;

            case InternalInterface.MG_ACT_AUTHORIZE:
               description = "Authorize";
               break;

            case InternalInterface.MG_ACT_EDT_BEGFLD:
               description = "Begin Field";
               break;

            case InternalInterface.MG_ACT_EDT_BEGFORM:
               description = "Begin Form";
               break;

            case InternalInterface.MG_ACT_EDT_BEGLINE:
               description = "Begin Line";
               break;

            case InternalInterface.MG_ACT_EDT_BEGNXTLINE:
               description = "Begin Next Line";
               break;

            case InternalInterface.MG_ACT_EDT_BEGPAGE:
               description = "Begin Page";
               break;

            case InternalInterface.MG_ACT_TBL_BEGLINE:
               description = "Begin Row";
               break;

            case InternalInterface.MG_ACT_TBL_BEGPAGE:
               description = "Begin Screen";
               break;

            case InternalInterface.MG_ACT_TBL_BEGTBL:
               description = "Begin table";
               break;

            case InternalInterface.MG_ACT_BOTTOM:
               description = "Bottom";
               break;

            case InternalInterface.MG_ACT_BULLET:
               description = "Bullet";
               break;

            case InternalInterface.MG_ACT_BUTTON:
               description = "Button Press";
               break;

            case InternalInterface.MG_ACT_CANCEL:
               description = "Cancel";
               break;

            case InternalInterface.MG_ACT_RT_QUIT:
               description = "Quit";
               break;

            case InternalInterface.MG_ACT_CENTER:
               description = "Center";
               break;

            case InternalInterface.MG_ACT_CHANGE_COLOR:
               description = "";
               break;

            case InternalInterface.MG_ACT_CHANGE_FONT:
               description = "Change Font";
               break;

            case InternalInterface.MG_ACT_CHECK_SYNTAX:
               description = "Check Syntax";
               break;

            case InternalInterface.MG_ACT_CHECK_TO_END:
               description = "Check to End";
               break;

            case InternalInterface.MG_ACT_CLEAR_TEMPLATE:
               description = "Clear Template";
               break;

            case InternalInterface.MG_ACT_CLEAR_VALUE:
               description = "Clear Value";
               break;

            case InternalInterface.MG_ACT_CLOSE:
               description = "Close";
               break;

            case InternalInterface.MG_ACT_ACT_CLOSE_APPL:
               description = "Close Application";
               break;

            case InternalInterface.MG_ACT_COLORS:
               description = "Colors";
               break;

            case InternalInterface.MG_ACT_COMMUNICATIONS:
               description = "Communications";
               break;

            case InternalInterface.MG_ACT_COMPONENTS:
               description = "Components";
               break;

            case InternalInterface.MG_ACT_CTRL_HIT:
               description = "Control Hit";
               break;

            case InternalInterface.MG_ACT_CONTROL_NAME_LIST:
               description = "Control Name List";
               break;

            case InternalInterface.MG_ACT_CLIP_COPY:
               description = "Copy";
               break;

            case InternalInterface.MG_ACT_COPY_LAYOUT:
               description = "Copy Layout";
               break;

            case InternalInterface.MG_ACT_COPY_SUBTREE:
               description = "Copy Subtree";
               break;

            case InternalInterface.MG_ACT_CRELINE:
               description = "Create Line";
               break;

            case InternalInterface.MG_ACT_CREATE_PARENT:
               description = "Create Parent";
               break;

            case InternalInterface.MG_ACT_RTO_CREATE:
               description = "Create Records";
               break;

            case InternalInterface.MG_ACT_CREATE_SUBTASK:
               description = "Create Subtask";
               break;

            case InternalInterface.MG_ACT_CROSS_REFERENCE:
               description = "Cross Reference";
               break;

            case InternalInterface.MG_ACT_CUT:
               description = "Cut";
               break;

            case InternalInterface.MG_ACT_DB_TABLES:
               description = "";
               break;

            case InternalInterface.MG_ACT_DBMS:
               description = "DBMS";
               break;

            case InternalInterface.MG_ACT_DDF_MAKE:
               description = "DDF Make";
               break;

            case InternalInterface.MG_ACT_DATABASES:
               description = "Databases";
               break;

            case InternalInterface.MG_ACT_DEFAULT_LAYOUT:
               description = "Default Layout";
               break;

            case InternalInterface.MG_ACT_DEFINE_EXPRESSION:
               description = "Define Expression";
               break;

            case InternalInterface.MG_ACT_EDT_DELCURCH:
               description = "Current Char";
               break;

            case InternalInterface.MG_ACT_EDT_DELPRVCH:
               description = "Previous Char";
               break;

            case InternalInterface.MG_ACT_DELETE_HYPERLINK:
               description = "Delete Hyperlink";
               break;

            case InternalInterface.MG_ACT_DELLINE:
               description = "Delete Line";
               break;

            case InternalInterface.MG_ACT_DELETE_SUBTREE:
               description = "Delete Subtree";
               break;

            case InternalInterface.MG_ACT_RT_COPYFLD:
               description = "Ditto";
               break;

            case InternalInterface.MG_ACT_DISPLAY_REFRESH:
               description = "Display Refresh";
               break;

            case InternalInterface.MG_ACT_EDIT_MAIN_FORM:
               description = "Edit Main Form";
               break;

            case InternalInterface.MG_ACT_EDT_ENDFLD:
               description = "End Field";
               break;

            case InternalInterface.MG_ACT_EDT_ENDFORM:
               description = "End Form";
               break;

            case InternalInterface.MG_ACT_EDT_ENDLINE:
               description = "End Line";
               break;

            case InternalInterface.MG_ACT_EDT_ENDPAGE:
               description = "End Page";
               break;

            case InternalInterface.MG_ACT_TBL_ENDLINE:
               description = "End Row";
               break;

            case InternalInterface.MG_ACT_TBL_ENDPAGE:
               description = "End Screen";
               break;

            case InternalInterface.MG_ACT_TBL_ENDTBL:
               description = "End Table";
               break;

            case InternalInterface.MG_ACT_ENVIRONMENT:
               description = "Environment";
               break;

            case InternalInterface.MG_ACT_EXECUTE_PROGRAM:
               description = "Execute Program";
               break;

            case InternalInterface.MG_ACT_EXECUTE_REPORT:
               description = "Execute Report";
               break;

            case InternalInterface.MG_ACT_EXIT:
               description = "Exit";
               break;

            case InternalInterface.MG_ACT_EXIT_SYSTEM:
               description = "Exit System";
               break;

            case InternalInterface.MG_ACT_EXPAND_TREE:
               description = "Expand Tree";
               break;

            case InternalInterface.MG_ACT_EXPORT_IMPORT:
               description = "Export/Import";
               break;

            case InternalInterface.MG_ACT_EXPRESSION_RULES:
               description = "Expression rules";
               break;

            case InternalInterface.MG_ACT_EXTERNAL_EDITOR:
               description = "External Editor";
               break;

            case InternalInterface.MG_ACT_FONTS:
               description = "";
               break;

            case InternalInterface.MG_ACT_EXT_EVENT:
               description = "External Event";
               break;

            case InternalInterface.MG_ACT_FORMS:
               description = "Forms";
               break;

            case InternalInterface.MG_ACT_FROM_VALUE:
               description = "From Value";
               break;

            case InternalInterface.MG_ACT_FUNCTION_LIST:
               description = "Function List";
               break;

            case InternalInterface.MG_ACT_GENERATE_FORM:
               description = "Generate Form";
               break;

            case InternalInterface.MG_ACT_GENERATE_PROGRAM:
               description = "Generate Program";
               break;

            case InternalInterface.MG_ACT_GO_TO_TOP:
               description = "Go to Top";
               break;

            case InternalInterface.MG_ACT_H_CENTER_OF_FORM:
               description = "";
               break;

            case InternalInterface.MG_ACT_HTML_STYLES:
               description = "HTML Styles";
               break;

            case InternalInterface.MG_ACT_HELP:
               description = "Help";
               break;

            case InternalInterface.MG_ACT_HELP_SCREENS:
               description = "Help Screens";
               break;

            case InternalInterface.MG_ACT_HORIZ_CENTER:
               description = "Horiz. Center";
               break;

            case InternalInterface.MG_ACT_I_O_FILES:
               description = "Tables";
               break;

            case InternalInterface.MG_ACT_INDENT:
               description = "Indent";
               break;

            case InternalInterface.MG_ACT_INSERT_OBJECT:
               description = "Insert Object";
               break;

            case InternalInterface.MG_ACT_JUMP_TO_ROW:
               description = "Jump to Row";
               break;

            case InternalInterface.MG_ACT_KEYBOARD_LIST:
               description = "Keyboard List";
               break;

            case InternalInterface.MG_ACT_KEYBOARD_MAPPING:
               description = "Keyboard Mapping";
               break;

            case InternalInterface.MG_ACT_LANGUAGES:
               description = "languages";
               break;

            case InternalInterface.MG_ACT_RTO_SEARCH:
               description = "Locate Next";
               break;

            case InternalInterface.MG_ACT_RTO_LOCATE:
               description = "Locate a Record";
               break;

            case InternalInterface.MG_ACT_LOGICALPNAMES:
               description = "";
               break;

            case InternalInterface.MG_ACT_LOGON:
               description = "Logon";
               break;

            case InternalInterface.MG_ACT_USING_HELP:
               description = "Help Topics";
               break;

            case InternalInterface.MG_ACT_EDT_MARKNXTCH:
               description = "Mark Next Char";
               break;

            case InternalInterface.MG_ACT_MARK_NEXT_LINE:
               description = "Mark Next Line MLE";
               break;

            case InternalInterface.MG_ACT_EDT_MARKPRVCH:
               description = "Mark Previous Char";
               break;

            case InternalInterface.MG_ACT_EDT_MARKPRVLINE:
               description = "Mark Previous Line MLE";
               break;

            case InternalInterface.MG_ACT_MARK_SUBTREE:
               description = "";
               break;

            case InternalInterface.MG_ACT_EDT_MARKTOBEG:
               description = "Mark To Beginning";
               break;

            case InternalInterface.MG_ACT_EDT_MARKTOEND:
               description = "Mark To End";
               break;

            case InternalInterface.MG_ACT_MAXIMUM_HEIGHT:
               description = "Maximum Height";
               break;

            case InternalInterface.MG_ACT_MAXIMUM_WIDTH:
               description = "Maximum Width";
               break;

            case InternalInterface.MG_ACT_MENU_BAR:
               description = "Menu Bar";
               break;

            case InternalInterface.MG_ACT_MENUS:
               description = "";
               break;

            case InternalInterface.MG_ACT_MINIMUM_HEIGHT:
               description = "Minimum Height";
               break;

            case InternalInterface.MG_ACT_MINIMUM_WIDTH:
               description = "Minimum Width";
               break;

            case InternalInterface.MG_ACT_RTO_MODIFY:
               description = "Modify Records";
               break;

            case InternalInterface.MG_ACT_MONITOR_DEBUGGER:
               description = "Monitor/Debugger";
               break;

            case InternalInterface.MG_ACT_MOVE_ENTRY:
               description = "Move Entry";
               break;

            case InternalInterface.MG_ACT_MOVE_SUBTREE:
               description = "Move Subtree";
               break;

            case InternalInterface.MG_ACT_NULL_SETTINGS:
               description = "NULL Settings";
               break;

            case InternalInterface.MG_ACT_EDT_NXTCHAR:
               description = "Next Char";
               break;

            case InternalInterface.MG_ACT_TBL_NXTFLD:
               description = "Next Field";
               break;

            case InternalInterface.MG_ACT_EDT_NXTLINE:
               description = "Next Line";
               break;

            case InternalInterface.MG_ACT_EDT_NXTPAGE:
               description = "Next Page";
               break;

            case InternalInterface.MG_ACT_TBL_NXTLINE:
               description = "Next Row";
               break;

            case InternalInterface.MG_ACT_TBL_NXTPAGE:
               description = "Next Screen";
               break;

            case InternalInterface.MG_ACT_EDT_NXTTAB:
               description = "Next Tab";
               break;

            case InternalInterface.MG_ACT_EDT_NXTWORD:
               description = "Next Word";
               break;

            case InternalInterface.MG_ACT_NORMAL:
               description = "Normal";
               break;

            case InternalInterface.MG_ACT_OK:
               description = "OK";
               break;

            case InternalInterface.MG_ACT_OLE2:
               description = "OLE2";
               break;

            case InternalInterface.MG_ACT_OPEN_APPLICATION:
               description = "Open Application";
               break;

            case InternalInterface.MG_ACT_OVERWRITE_ENTRY:
               description = "Overwrite Entry";
               break;

            case InternalInterface.MG_ACT_OVERWRITE_SUBTREE:
               description = "Overwrite Subtree";
               break;

            case InternalInterface.MG_ACT_PPD:
               description = "PPD";
               break;

            case InternalInterface.MG_ACT_PAGE_FOOTER:
               description = "Page Footer";
               break;

            case InternalInterface.MG_ACT_PAGE_HEADER:
               description = "Page Header";
               break;

            case InternalInterface.MG_ACT_CLIP_PASTE:
               description = "Paste";
               break;

            case InternalInterface.MG_ACT_PASTE_LINK:
               description = "Paste Link";
               break;

            case InternalInterface.MG_ACT_PASTE_SUBTREE:
               description = "Paste Subtree";
               break;

            case InternalInterface.MG_ACT_EDT_PRVCHAR:
               description = "Previous Char";
               break;

            case InternalInterface.MG_ACT_TBL_PRVFLD:
               description = "Previous Field";
               break;

            case InternalInterface.MG_ACT_EDT_PRVLINE:
               description = "Previous Line";
               break;

            case InternalInterface.MG_ACT_EDT_PRVPAGE:
               description = "Previous Page";
               break;

            case InternalInterface.MG_ACT_TBL_PRVLINE:
               description = "Previous Row";
               break;

            case InternalInterface.MG_ACT_TBL_PRVPAGE:
               description = "Previous Screen";
               break;

            case InternalInterface.MG_ACT_EDT_PRVTAB:
               description = "Previous Tab";
               break;

            case InternalInterface.MG_ACT_EDT_PRVWORD:
               description = "Previous Word";
               break;

            case InternalInterface.MG_ACT_PRINT_ATTRIBUTES:
               description = "Print Attributes";
               break;

            case InternalInterface.MG_ACT_PRINTER_SETUP:
               description = "Printer Setup";
               break;

            case InternalInterface.MG_ACT_PRINTERS:
               description = "Printers";
               break;

            case InternalInterface.MG_ACT_PROGRAMS:
               description = "Programs";
               break;

            case InternalInterface.MG_ACT_PROPERTIES:
               description = "Properties";
               break;

            case InternalInterface.MG_ACT_RTO_QUERY:
               description = "Query Records";
               break;

            case InternalInterface.MG_ACT_RTO_RANGE:
               description = "Range of Records";
               break;

            case InternalInterface.MG_ACT_RT_REFRESH_RECORD:
               description = "Record Flush";
               break;

            case InternalInterface.MG_ACT_REDIRECT_FILES:
               description = "Redirect Files";
               break;

            case InternalInterface.MG_ACT_REDRAW_LAYOUT:
               description = "";
               break;

            case InternalInterface.MG_ACT_REPEAT_ENTRY:
               description = "Repeat Entry";
               break;

            case InternalInterface.MG_ACT_REPEAT_SUBTREE:
               description = "Repeat Subtree";
               break;

            case InternalInterface.MG_ACT_REPORT_GENERATOR:
               description = "";
               break;

            case InternalInterface.MG_ACT_RESTORE_DEFAULTS:
               description = "Restore Defaults";
               break;

            case InternalInterface.MG_ACT_RETURN_TO_TREE:
               description = "";
               break;

            case InternalInterface.MG_ACT_RIGHT:
               description = "Right";
               break;

            case InternalInterface.MG_ACT_RIGHTS:
               description = "";
               break;

            case InternalInterface.MG_ACT_RIGHTS_LIST:
               description = "Rights List";
               break;

            case InternalInterface.MG_ACT_SQL_ASSIST:
               description = "SQL Assist";
               break;

            case InternalInterface.MG_ACT_SQL_COLUMNS:
               description = "SQL Columns";
               break;

            case InternalInterface.MG_ACT_SQL_COMMAND:
               description = "SQL Command";
               break;

            case InternalInterface.MG_ACT_SQL_FLIP_DESC:
               description = "SQL Flip Desc.";
               break;

            case InternalInterface.MG_ACT_SQL_KEYWORDS:
               description = "SQL Keywords";
               break;

            case InternalInterface.MG_ACT_SQL_OPERATORS:
               description = "SQL Operators";
               break;

            case InternalInterface.MG_ACT_SQL_TABLES:
               description = "SQL Tables";
               break;

            case InternalInterface.MG_ACT_SQL_WHERE_CLAUSE:
               description = "";
               break;

            case InternalInterface.MG_ACT_RT_REFRESH_SCREEN:
               description = "Screen Refresh";
               break;

            case InternalInterface.MG_ACT_SECRET_NAMES:
               description = "Secret Names";
               break;

            case InternalInterface.MG_ACT_SELECT:
               description = "Select";
               break;

            case InternalInterface.MG_ACT_EDT_MARKALL:
               description = "Select All";
               break;

            case InternalInterface.MG_ACT_SERVERS:
               description = "";
               break;

            case InternalInterface.MG_ACT_SERVICES:
               description = "";
               break;

            case InternalInterface.MG_ACT_RT_EDT_NULL:
               description = "Set to NULL";
               break;

            case InternalInterface.MG_ACT_SHELL_TO_OS:
               description = "Shell to OS";
               break;

            case InternalInterface.MG_ACT_SHOW_EXPRESSION:
               description = "Show Expression";
               break;

            case InternalInterface.MG_ACT_SHOW_FULL_LAYOUT:
               description = "";
               break;

            case InternalInterface.MG_ACT_SHRINK_TREE:
               description = "";
               break;

            case InternalInterface.MG_ACT_SORT:
               description = "Sort";
               break;

            case InternalInterface.MG_ACT_SORT_RECORDS:
               description = "Sort Records";
               break;

            case InternalInterface.MG_ACT_TABLE_LOCATE:
               description = "Table Locate";
               break;

            case InternalInterface.MG_ACT_TABLE_LOCATE_NEXT:
               description = "Table Locate Next";
               break;

            case InternalInterface.MG_ACT_TABLES:
               description = "";
               break;

            case InternalInterface.MG_ACT_TASK_CONTROL:
               description = "";
               break;

            case InternalInterface.MG_ACT_TASK_EVENTS:
               description = "";
               break;

            case InternalInterface.MG_ACT_TO_VALUE:
               description = "To Value";
               break;

            case InternalInterface.MG_ACT_TOOLKIT_RUNTIME:
               description = "Toolkit/Runtime";
               break;

            case InternalInterface.MG_ACT_TOP:
               description = "Top";
               break;

            case InternalInterface.MG_ACT_TYPES:
               description = "";
               break;

            case InternalInterface.MG_ACT_EDT_UNDO:
               description = "Undo Editing";
               break;

            case InternalInterface.MG_ACT_UNINDENT:
               description = "Unindent";
               break;

            case InternalInterface.MG_ACT_UPDATE_LINK:
               description = "";
               break;

            case InternalInterface.MG_ACT_USER_ACTION_1:
            case InternalInterface.MG_ACT_USER_ACTION_2:
            case InternalInterface.MG_ACT_USER_ACTION_3:
            case InternalInterface.MG_ACT_USER_ACTION_4:
            case InternalInterface.MG_ACT_USER_ACTION_5:
            case InternalInterface.MG_ACT_USER_ACTION_6:
            case InternalInterface.MG_ACT_USER_ACTION_7:
            case InternalInterface.MG_ACT_USER_ACTION_8:
            case InternalInterface.MG_ACT_USER_ACTION_9:
            case InternalInterface.MG_ACT_USER_ACTION_10:
            case InternalInterface.MG_ACT_USER_ACTION_11:
            case InternalInterface.MG_ACT_USER_ACTION_12:
            case InternalInterface.MG_ACT_USER_ACTION_13:
            case InternalInterface.MG_ACT_USER_ACTION_14:
            case InternalInterface.MG_ACT_USER_ACTION_15:
            case InternalInterface.MG_ACT_USER_ACTION_16:
            case InternalInterface.MG_ACT_USER_ACTION_17:
            case InternalInterface.MG_ACT_USER_ACTION_18:
            case InternalInterface.MG_ACT_USER_ACTION_19:
            case InternalInterface.MG_ACT_USER_ACTION_20:
               description = "User Action " + (InternalEvent - InternalInterface.MG_ACT_USER_ACTION_1 + 1);
               break;

            case InternalInterface.MG_ACT_USER_GROUPS:
               description = "";
               break;

            case InternalInterface.MG_ACT_USER_IDS:
               description = "User IDs";
               break;

            case InternalInterface.MG_ACT_V_CENTER_OF_FORM:
               description = "Center of Form";
               break;

            case InternalInterface.MG_ACT_VERTICAL_CENTER:
               description = "Vertical Center";
               break;

            case InternalInterface.MG_ACT_VIEW_BY_KEY:
               description = "View by Key";
               break;

            case InternalInterface.MG_ACT_RT_REFRESH_VIEW:
               description = "View Refresh";
               break;

            case InternalInterface.MG_ACT_VIRTUAL_VARIABLES:
               description = "Variables";
               break;

            case InternalInterface.MG_ACT_VISUAL_CONNECTION:
               description = "Visual Connection";
               break;

            case InternalInterface.MG_ACT_WIDE:
               description = "Wide";
               break;

            case InternalInterface.MG_ACT_HIT:
               description = "Window Hit";
               break;

            case InternalInterface.MG_ACT_WINMOVE:
               description = "Window Reposition";
               break;

            case InternalInterface.MG_ACT_WINSIZE:
               description = "Window Resize";
               break;

            case InternalInterface.MG_ACT_ZOOM:
               description = "Zoom";
               break;

            case InternalInterface.MG_ACT_LOCKING_DETAILS:
               description = "Locking Details";
               break;

            case InternalInterface.MG_ACT_LOCKING_ABORT:
               description = "Locking Abort";
               break;

            case InternalInterface.MG_ACT_LOCKING_RETRY:
               description = "Locking Retry";
               break;

            case InternalInterface.MG_ACT_CHECK_OBJECT_LIST:
               description = "Check Object List";
               break;

            case InternalInterface.MG_ACT_USERS_LIST:
               description = "Users List";
               break;

            case InternalInterface.MG_ACT_MODELS:
               description = "Models";
               break;

            case InternalInterface.MG_ACT_WEB_CLICK:
               description = "Click";
               break;

            case InternalInterface.MG_ACT_WEB_ON_DBLICK:
               description = "DblClick";
               break;

            case InternalInterface.MG_ACT_WEB_MOUSE_OVER:
               description = "Mouse Over";
               break;

            case InternalInterface.MG_ACT_WEB_MOUSE_OUT:
               description = "Mouse Out";
               break;

            case InternalInterface.MG_ACT_BROWSER_ESC:
               description = "Esc";
               break;

            case InternalInterface.MG_ACT_ROLLBACK:
               description = "Rollback";
               break;

            case InternalInterface.MG_ACT_TREE_COLLAPSE:
               description = "Tree Collapse";
               break;

            case InternalInterface.MG_ACT_TREE_EXPAND:
               description = "Tree Expand";
               break;

            case InternalInterface.MG_ACT_TREE_CRE_SON:
               description = "Create Child";
               break;

            case InternalInterface.MG_ACT_TREE_RENAME_RT:
               description = "Edit Node";
               break;

            case InternalInterface.MG_ACT_TREE_MOVETO_PARENT:
               description = "Move To Parent";
               break;

            case InternalInterface.MG_ACT_TREE_MOVETO_FIRSTCHILD:
               description = "To First Child";
               break;

            case InternalInterface.MG_ACT_TREE_MOVETO_PREVSIBLING:
               description = "To Prev Sibling";
               break;

            case InternalInterface.MG_ACT_TREE_MOVETO_NEXTSIBLING:
               description = "To next sibling";
               break;

            case InternalInterface.MG_ACT_EMPTY_DATAVIEW:
               description = "Empty Dataview";
               break;

            case InternalInterface.MG_ACT_CTRL_MODIFY:
               description = "Control Modify";
               break;

            case InternalInterface.MG_ACT_PRINT_DATA:
               description = "Print Data";
               break;

            case InternalInterface.MG_ACT_POST_REFRESH_BY_PARENT:
               description = "Post Refresh by Parent";
               break;

            case InternalInterface.MG_ACT_SWITCH_TO_OFFLINE:
               description = "Switch To Offline";
               break;

            case InternalInterface.MG_ACT_UNAVAILABLE_SERVER:
               description = "Unavailable Server";
               break;

            case InternalInterface.MG_ACT_CONTEXT_MENU:
               description = "Context Menu";
               break;

            default:
               description = "";
               break;
         }
         return description;
      }

      /// <summary>
      ///   get description of the .net event
      /// </summary>
      /// <returns>description of .net event</returns>
      private String getDotnetEvtDescription()
      {
         String description;

         if (DotNetField != null)
            description = DotNetField.getVarName();
         else if (DotNetType != null)
            description = DotNetType.ToString();
         else
            description = "";

         return description + "." + DotNetEventName;
      }

      /// <summary>
      ///   get type of User Event
      /// </summary>
      /// <returns> type of user event </returns>
      internal char getUserEventType()
      {
         if (UserEvt == null)
            return ConstInterface.EVENT_TYPE_NOTINITED;

         return UserEvt.getType();
      }

      /// <summary>
      ///   get seconds of the user event
      /// </summary>
      internal int getSecondsOfUserEvent()
      {
         return UserEvt.getSeconds();
      }

      /// <summary>
      ///   get the user event object
      /// </summary>
      /// <returns> the user event object </returns>
      internal Event getUserEvent()
      {
         return UserEvt;
      }

      /// <summary>
      ///   get keyboard item ( for system event ) in form of string
      ///   for system events ONLY
      /// </summary>
      private String getKeyboardItemString()
      {
         KeyboardItem kbi = getKbdItm();
         if (kbi == null)
            return "";
         return kbi.ToString();
      }

      /// <summary>
      ///   insert the seconds to the picture "HH:MM:SS" for STORAGE_ATTR_TIME
      /// </summary>
      /// <returns> formated string of seconds
      /// </returns>
      private String seconds2String()
      {
         int[] time = new[] { 0, 0, 0 }; //  hours, minutes, seconds;
         String buffer = "";

         time[0] = _seconds / 3600;
         time[1] = (_seconds - time[0] * 3600) / 60;
         time[2] = _seconds % 60;
         for (int i = 0;
              i < time.Length;
              i++)
         {
            if (time[i] == 0)
               buffer += "00";
            else if (time[i] < 10)
               buffer += ("0" + time[i]);
            else
               buffer += time[i];
            //don't insert delimiter after the last case
            if (i < time.Length - 1)
               buffer += ":";
         }
         return buffer;
      }

      /// <summary>
      ///   Get the value of the parameter
      /// </summary>
      /// <param name = "idx">- the index of the parameter ( 0 = first param) </param>
      /// <returns> the value in a string </returns>
      protected internal String getParamVal(int idx)
      {
         return _paramVals[idx];
      }

      /// <summary>
      ///   parse the parameters values from the value string
      /// </summary>
      /// <param name = "valueStr">index of the param </param>
      /// <returns> the parameter value </returns>
      private int parseParamVal(String valueStr)
      {
         int startOfs = 0, endOfs = 0, nLen, j;
         String sLen, sValue = null;

         //find the value location
         for (j = 0;
              j < _parameters;
              j++)
         {
            switch ((StorageAttribute)_paramAttrs[j])
            {
               case StorageAttribute.SKIP:
                  sValue = "";
                  break;

               case StorageAttribute.ALPHA:
               case StorageAttribute.BLOB_VECTOR:
               case StorageAttribute.BLOB:
               case StorageAttribute.MEMO:
               case StorageAttribute.UNICODE:
                  endOfs = startOfs + 4;
                  sLen = valueStr.Substring(startOfs, (endOfs) - (startOfs));
                  nLen = Convert.ToInt32(sLen, 16);
                  startOfs = endOfs;

                  //QCR 1699 decode the parameter from base64 if needed
                  if (ClientManager.Instance.getEnvironment().GetDebugLevel() > 1 ||
                      ((StorageAttribute)_paramAttrs[j]) == StorageAttribute.ALPHA ||
                      ((StorageAttribute)_paramAttrs[j]) == StorageAttribute.UNICODE ||
                      ((StorageAttribute)_paramAttrs[j]) == StorageAttribute.MEMO)
                  {
                     if ((nLen & 0x8000) > 0)
                     //parse spanned record encoded to Hex
                     {
                        //compute the len of the parameter data
                        nLen = (nLen & 0x7FFF);
                        nLen *= 2;

                        //parse the String
                        StringBuilder res = new StringBuilder();
                        endOfs += RecordUtils.getSpannedField(valueStr, nLen, startOfs, (StorageAttribute)_paramAttrs[j], res, true);
                        valueStr = res.ToString();
                     }
                     //non spanned params
                     else
                     {
                        endOfs = startOfs + nLen;
                        sValue = valueStr.Substring(startOfs, (endOfs) - (startOfs));
                     }
                  }
                  else
                  {
                     if ((nLen & 0x8000) > 0)
                     //parse spanned record encoded to Hex
                     {
                        //compute the len of the parameter data
                        nLen = (nLen & 0x7FFF);
                        nLen = (nLen + 2) / 3 * 4;

                        //parse the String
                        StringBuilder res = new StringBuilder();
                        endOfs += RecordUtils.getSpannedField(valueStr, nLen, startOfs, (StorageAttribute)_paramAttrs[j], res, false);
                        sValue = res.ToString();
                     }
                     else
                     {
                        endOfs = startOfs + (nLen + 2) / 3 * 4;
                        sValue = Base64.decode(valueStr.Substring(startOfs, (endOfs) - (startOfs)));
                     }
                  }

                  break;

               case StorageAttribute.NUMERIC:
               case StorageAttribute.DATE:
               case StorageAttribute.TIME:
                  //QCR 1699 decode the parameter from base64 if needed
                  if (ClientManager.Instance.getEnvironment().GetDebugLevel() > 1)
                  {
                     endOfs = startOfs + ClientManager.Instance.getEnvironment().GetSignificantNumSize() * 2;
                     sValue = valueStr.Substring(startOfs, (endOfs) - (startOfs));
                  }
                  else
                  {
                     endOfs = startOfs + (((ClientManager.Instance.getEnvironment().GetSignificantNumSize() + 2) / 3) * 4);
                     sValue = Base64.decodeToHex(valueStr.Substring(startOfs, (endOfs) - (startOfs)));
                  }
                  break;

               case StorageAttribute.BOOLEAN:
                  endOfs = startOfs + 1;
                  sValue = valueStr.Substring(startOfs, (endOfs) - (startOfs));
                  break;
            }
            _paramVals[j] = sValue;
            startOfs = endOfs;
         }
         return endOfs;
      }

      /// <summary>
      ///   set Argument Values , without setting of the paramAttrs
      /// </summary>
      internal void setParamVals(String[] paramVals_)
      {
         _paramVals = paramVals_;
         _paramAttrs = _paramNulls = "";
         _parameters = 0;

         if (_paramVals != null)
         {
            _parameters = _paramVals.Length;
            for (int i = 0;
                 i < _parameters;
                 i++)
            {
               _paramAttrs += StorageAttribute.ALPHA; //temporary type of the data
               _paramNulls += "0";
            }
         }
      }

      /// <summary>
      ///   Try to evaluate value to the same type as the field type
      /// </summary>
      /// <param name = "paramNum">number of the parameter </param>
      /// <param name = "fld">- field with it's type, to enforce its type to the parameter value </param>
      /// <returns> true - if setting of the type has over without problems and now the parameter and the fields have the same type,
      ///   false - the setting impossible.
      /// </returns>
      protected internal bool setParamType(int paramNum, Field fld)
      {
         StorageAttribute currType;
         String currValue;
         StorageAttribute newType = fld.getType();
         PIC pic = null;
         bool sameType = false;

         if (_paramVals != null && getParamNum() >= paramNum)
         {
            currType = (StorageAttribute)_paramAttrs[paramNum];

            sameType = (currType == newType);
            if (!sameType)
            {
               //try to set the new type to the parameter
               char[] charArr = null;
               currValue = _paramVals[paramNum];

               switch (newType)
               {
                  case StorageAttribute.ALPHA:
                  case StorageAttribute.BLOB:
                  case StorageAttribute.BLOB_VECTOR:
                  case StorageAttribute.MEMO:
                  case StorageAttribute.UNICODE:
                     break;

                  case StorageAttribute.NUMERIC:
                  case StorageAttribute.DATE:
                  case StorageAttribute.TIME:
                     if (currType == StorageAttribute.ALPHA)
                     {
                        //from alpha to numeric
                        try
                        {
                           pic = PIC.buildPicture(newType, currValue, fld.getTask().getCompIdx(), true);
                           _paramVals[paramNum] = DisplayConvertor.Instance.disp2mg(currValue, "", pic,
                                                                                             fld.getTask().getCompIdx(),
                                                                                             BlobType.CONTENT_TYPE_UNKNOWN);
                           sameType = true;
                        }
                        catch (Exception)
                        {
                           // canNotEvaluateAsNumeric
                           sameType = false;
                           Logger.Instance.WriteExceptionToLog(string.Format("Event.setParamType() cannot transfer {0} to Numeric with picture {1}", currValue, (pic != null
                                                                                                                                                              ? pic.getFormat()
                                                                                                                                                              : "null")));
                        }
                     }
                     break;

                  case StorageAttribute.BOOLEAN:
                     if (currType == StorageAttribute.ALPHA)
                     {
                        //from alpha to logical
                        currValue = currValue.Trim();

                        if (currValue.Equals("1") || currValue.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                           _paramVals[paramNum] = "1";
                        else
                           _paramVals[paramNum] = "0";
                        sameType = true;
                     }
                     break;
               }

               if (sameType)
               {
                  charArr = _paramAttrs.ToCharArray();
                  charArr[paramNum] = (char)newType;
                  _paramAttrs = new String(charArr);
               }
            } // not the same type
         } //there is no such parameter

         return sameType;
      }

      /// <summary>
      ///   Get the number of params
      /// </summary>
      /// <returns> number of params </returns>
      protected internal int getParamNum()
      {
         return _parameters;
      }

      /// <summary>
      ///   Get the null flag of the param
      /// </summary>
      /// <param name = "idx">the param index </param>
      /// <returns> null flag </returns>
      protected internal bool getParamNull(int idx)
      {
         if ('0' == _paramNulls[idx])
            return false;
         else
            return true;
      }

      /// <summary>
      ///   Get the attribute of the param
      /// </summary>
      /// <param name = "idx">the param index </param>
      /// <returns> attribute char </returns>
      protected internal char getParamAttr(int idx)
      {
         return _paramAttrs[idx];
      }

      /// <summary>
      ///   Appends the event's description to the given StringBuilder.
      /// </summary>
      /// <param name = "buffer">The buffer to which the description should be appended.</param>
      internal void AppendDescription(StringBuilder buffer)
      {
         switch (_type)
         {
            case ConstInterface.EVENT_TYPE_INTERNAL:
               buffer.Append(getInternalEvtDescription());
               break;

            case ConstInterface.EVENT_TYPE_SYSTEM:
               buffer.Append(getKeyboardItemString());
               break;

            case ConstInterface.EVENT_TYPE_USER:
               buffer.Append(getUsrEvntDesc());
               break;

            case ConstInterface.EVENT_TYPE_USER_FUNC:
               buffer.Append(_userDefinedFuncName);
               break;

            case ConstInterface.EVENT_TYPE_DOTNET:
               buffer.Append(" .NET ").Append(DotNetEventName);
               break;

            default:
               buffer.Append("unknown event type ");
               break;
         }
      }

      /// <summary>
      ///   returns the user defined function's name
      /// </summary>
      internal void setUserDefinedFuncName(String name)
      {
         _userDefinedFuncName = name;
         string functionNameUpper = _userDefinedFuncName.ToUpper();
         _userDefinedFuncNameHashCode = NUM_TYPE.hash_str_new(Encoding.Default.GetBytes(functionNameUpper));
      }

      /// <summary>
      ///   returns the user defined function's name
      /// </summary>
      protected internal String getUserDefinedFuncName()
      {
         return _userDefinedFuncName;
      }

      /// <summary>
      ///   returns the user defined function's name's hash value
      /// </summary>
      protected internal long getUserDefinedFuncNameHashCode()
      {
         return _userDefinedFuncNameHashCode;
      }

      /// <summary>
      ///   returns the user defined function's return expression
      /// </summary>
      internal int getUserDefinedFuncRetExp()
      {
         return _returnExp;
      }

      internal String getOsCommandText()
      {
         return _osCommandText;
      }

      internal void setOsCommandText(String osCommandText)
      {
         _osCommandText = osCommandText;
      }

      internal bool getOsCommandWait()
      {
         return _osCommandWait;
      }

      private void setOsCommandWait(bool wait)
      {
         _osCommandWait = wait;
      }

      internal CallOsShow getOsCommandShow()
      {
         return _osCommandShow;
      }

      private void setOsCommandShow(CallOsShow osShow)
      {
         _osCommandShow = osShow;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      protected void setTimestamp()
      {
         _timestamp = _lastTimestamp++;
      }
      internal long getTimestamp()
      {
         return _timestamp;
      }

      /// <summary>
      ///   return true if this is dotnet event of the field of the filed's type
      /// </summary>
      /// <param name = "field"></param>
      /// <param name = "dnType"></param>
      /// <returns></returns>
      internal bool isMatchingDNEvent(Field field, Type dnType)
      {
         if (_type == ConstInterface.EVENT_TYPE_DOTNET)
         {
            if (DotNetField != null)
               return DotNetField == field;
            else
               return ReflectionServices.IsAssignableFrom(DotNetType, dnType);
         }
         else
            return false;
      }

      public override string ToString()
      {
         return String.Format("{{Event: {0}}}", getBrkLevel());
      }
   }
}
