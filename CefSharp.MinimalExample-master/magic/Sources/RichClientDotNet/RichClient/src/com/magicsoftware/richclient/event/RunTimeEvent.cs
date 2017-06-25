using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using Field = com.magicsoftware.richclient.data.Field;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.unipaas.dotnet;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.events
{
   /// <summary>
   ///   this class represents the runtime events
   /// </summary>
   internal sealed class RunTimeEvent : Event, IComparable
   {
      // ATTENTION !
      // any change to the member variables must be followed by a change to the CTOR that
      // gets one argument of the type 'Event' and one argument of the type 'RunTimeEvent'.

      private readonly List<MgControlBase> _controlsList; //list of controls
      private readonly int _direction;
      private readonly int _displayLine = Int32.MinValue;
      private readonly int _menuComp;
      private readonly int _menuUid;

      private EventSubType _eventSubType = EventSubType.Normal;
                                       // if CANCEL_IS_QUIT the event is part of Quit process.
      private int[] _actEnableList;
      private ArgumentsList _argList;
      private MgControl _ctrl; // the control on which the event was triggered
      private Object _dotNetObject; //object key of a control
      private Field _eventFld; // For variable events - the field on which the control was triggered
      private bool _fromServer; // if TRUE, the event was created on the server and sent to the client
      private bool _guiTriggeredEvent; //if true, the event was triggered by GUI level (includes timer events)
      private ImeParam _imeParam;
      private bool _immediate;
      private bool _isIdleTimer;
      private bool _isRealRefresh = true; // used only in MG_ACT_VIEW_REFRESH events to indicate a refresh invoked Specifically by the user
      private int _mgdId; // mgdata id that sent timer event

      // Used only if 'task' is a main prg. A reference to the task which
      // caused the main prg to become active in the runtime.
      private Task _mprgCreator;

      private Priority _priority = Priority.HIGH; //priority of event for events queue

      private bool _produceClick; // if TRUE, the event should produce click
      private bool _reversibleExit = true;

      private int _selectionEnd;
      private int _selectionStart;
      private bool _sendAll; //this flag is set for sending all records
      private Task _task; // a reference to the task
      private String _taskTag; // the task id on which the event was triggered
      private String _val;

      internal bool IsSubTree {get; set;}
      internal bool IsFormFocus { get; set; }

      internal bool ActivatedFromMDIFrame { get; private set; }
      internal char PrgFlow { get; private set; }
      internal bool CopyGlobalParams { get; private set; }
      internal List<String> MainProgVars { get; private set; }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "taskRef">a reference to the task</param>
      internal RunTimeEvent(Task taskRef)
         : base()
      {
         init(taskRef);
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "taskRef">reference to task</param>
      /// <param name = "ctrlRef">reference to control</param>
      internal RunTimeEvent(Task taskRef, MgControl ctrlRef)
         : this(ctrlRef != null ? (Task)ctrlRef.getForm().getTask() : taskRef)
      {
         Control = ctrlRef;
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "ctrlRef">a reference to the control </param>
      internal RunTimeEvent(MgControl ctrlRef)
         :this(null, ctrlRef)
      {
         if (ctrlRef != null && getType() == Char.MinValue) // not initialized yet
         {
            // on the first time, if a TRIGGER property exists, parse the event and add it to the control
            Property prop = ctrlRef.getProp(PropInterface.PROP_TYPE_TRIGGER);
            if (prop != null)
            {
               var xmlParser = new XmlParser(prop.getValue());
               fillData(xmlParser, (Task)ctrlRef.getForm().getTask());
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="ctrlRef"></param>
      /// <param name="guiTriggeredEvent"></param>
      internal RunTimeEvent(MgControl ctrlRef, bool guiTriggeredEvent)
         : this(ctrlRef)
      {
         _guiTriggeredEvent = guiTriggeredEvent;
         if (guiTriggeredEvent)
            //Events that are triggered by GUI level, must be executed after all
            //other events, thus thay have low priority;
            _priority = Priority.LOW;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="ctrlRef"></param>
      /// <param name="guiTriggeredEvent"></param>
      internal RunTimeEvent(MgControl ctrlRef, bool guiTriggeredEvent, bool ignoreSpecifiedControl)
         : this(ctrlRef, guiTriggeredEvent)
      {
         IgnoreSpecifiedControl = ignoreSpecifiedControl;
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "taskref">reference to task</param>
      /// <param name = "guiTriggeredEvent"></param>
      internal RunTimeEvent(Task taskref, bool guiTriggeredEvent)
         : this(taskref)
      {
         _guiTriggeredEvent = guiTriggeredEvent;
         if (guiTriggeredEvent)
            //Events that are triggered by GUI level, must be executed after all
            //other events, thus they have low priority;
            _priority = Priority.LOW;
      }

#if PocketPC
      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "guiTriggeredEvent"></param>
      internal RunTimeEvent(bool guiTriggeredEvent)
         : base()
      {
         _guiTriggeredEvent = guiTriggeredEvent;
         if (guiTriggeredEvent)
            //Events that are triggered by GUI level, must be executed after all
            //other events, thus they have low priority;
            _priority = Priority.LOW;
      }
#endif

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "fldRef">a reference to the field</param>
      internal RunTimeEvent(Field fldRef)
         : base()
      {
         _eventFld = fldRef;
         init(fldRef != null
                 ? (Task)fldRef.getTask()
                 : null);
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "ctrlRef">a reference to the control on which the event occured </param>
      /// <param name = "line">the line of the table in which the control is located </param>
      internal RunTimeEvent(MgControl ctrlRef, int line)
         : this(ctrlRef)
      {
         _displayLine = line;
      }

      internal RunTimeEvent(MgControl ctrlRef, int line, bool guiTriggeredEvent)
         : this(ctrlRef, guiTriggeredEvent)
      {
         _displayLine = line;
      }

      internal RunTimeEvent(MgControl ctrlRef, List<MgControlBase> controlsList, bool guiTriggeredEvent) :
                             this(ctrlRef, guiTriggeredEvent)
      {
         _controlsList = controlsList;
      }

	  /// <summary> CTOR</summary>
	  /// <param name="ctrlRef">a reference to the control on which the event occurred</param>
	  /// <param name="direction">the direction of the sort we are now going to sort by</param>
	  /// <param name="line">the line is kept in this CTOR to distinguish 
	  ///                    between the direction CTOR and the line CTOR
	  /// </param>
	  internal RunTimeEvent(MgControl ctrlRef, int direction, int line)
		  : this(ctrlRef, true)
	  {
		  _direction = direction;
	  }

     internal RunTimeEvent(MgControl ctrlRef, String columnHeader, int x, int y, int width, int height)
        :this(ctrlRef, true)
     {
     }

      /// <summary>
      ///   CTOR - creates a new run time event by copying the member variables of a given
      ///   event and the member variables of a given run time event
      /// </summary>
      /// <param name = "evt">a reference to the event to be used </param>
      /// <param name = "rtEvt">a reference to the run time event to be used </param>
      internal RunTimeEvent(Event evt, RunTimeEvent rtEvt)
         : base(evt)
      {
         _taskTag = rtEvt._taskTag;
         _task = rtEvt._task;
         Control = rtEvt.Control;
         _eventFld = rtEvt._eventFld;
         _mgdId = rtEvt._mgdId;
         _displayLine = rtEvt._displayLine;
         _reversibleExit = rtEvt._reversibleExit;
         _argList = rtEvt._argList;
         _immediate = rtEvt._immediate;
         _mprgCreator = rtEvt._mprgCreator;
         _priority = rtEvt._priority;
         _guiTriggeredEvent = rtEvt._guiTriggeredEvent;
         _isIdleTimer = rtEvt._isIdleTimer;
         _val = rtEvt._val;
         _selectionStart = rtEvt._selectionStart;
         _selectionEnd = rtEvt._selectionEnd;
         _controlsList = rtEvt._controlsList;
         _direction = rtEvt._direction;
         _dotNetObject = rtEvt._dotNetObject;
         DotNetArgs = rtEvt.DotNetArgs;
         LastFocusedVal = rtEvt.LastFocusedVal;
         _imeParam = rtEvt._imeParam;
      }

      /// <summary>
      ///   This constructor creates a matching event object for the passed menuEntryEvent
      /// </summary>
      /// <param name = "menuEntryEvent">event menu entry for which we create this event</param>
      /// <param name = "currentTask">current task from which the menu entry was activated</param>
      /// <param name = "control"></param>
      internal RunTimeEvent(MenuEntryEvent menuEntryEvent, Task currentTask, MgControl control, int ctlIdx)
         : base(menuEntryEvent, ctlIdx)
      {
         _task = currentTask;
         _taskTag = currentTask.getTaskTag();
         Control = control;
      }

      /// <summary>
      ///   Create an Event for the passed program menu entry
      /// </summary>
      /// <param name = "menuEntryProgram">the selected program menu entry</param>
      /// <param name = "currentTask">the task from which the menu was activated</param>
      /// <param name = "activatedFromMDIFrame"></param>
      internal RunTimeEvent(MenuEntryProgram menuEntryProgram, Task currentTask, bool activatedFromMDIFrame)
         : base(ConstInterface.EVENT_TYPE_MENU_PROGRAM)
      {
         PrgFlow = menuEntryProgram.Flow;
         PublicName = menuEntryProgram.PublicName;
         PrgDescription = menuEntryProgram.Description;
         CopyGlobalParams = menuEntryProgram.CopyGlobalParameters;
         MainProgVars = menuEntryProgram.MainProgVars;

         ActivatedFromMDIFrame = activatedFromMDIFrame;

         _task = currentTask;
         _menuUid = menuEntryProgram.menuUid();
         _menuComp = menuEntryProgram.getParentMgMenu().CtlIdx;
      }

      /// <summary>
      ///   Create an Event for the passed os menu entry
      /// </summary>
      /// <param name = "osCommand">the selected os command menu entry</param>
      /// <param name = "currentTask">the task from which the menu was activated</param>
      internal RunTimeEvent(MenuEntryOSCommand osCommand, Task currentTask)
         : base(osCommand)
      {
         _task = currentTask;
      }

      internal MgControl Control
      {
         get { return _ctrl; }
         set
         {
            if (_ctrl != null && _ctrl.IsDotNetControl())
            {
               DotNetType = null;
               DotNetField = null;
               _eventFld = null;
            }
            _ctrl = value;
            if (_ctrl != null && _ctrl.IsDotNetControl())
            {
               _eventFld = DotNetField = (Field)_ctrl.DNObjectReferenceField;

               DotNetType = _ctrl.DNType;
            }
         }
      }

      /// <summary>
      /// Should the control set on the event be ignored. if true, when processing the event we should 
      /// use the current focused control instead of the one set on the event.
      /// </summary>
      internal bool IgnoreSpecifiedControl { get; private set; }

      internal Object DotNetObject
      {
         get { return _dotNetObject; }
         set
         {
            _dotNetObject = value;
            DotNetType = ReflectionServices.GetType(_dotNetObject);
         }
      }

      internal Object[] DotNetArgs { get; set; }
      internal LastFocusedVal LastFocusedVal { get; set; } //value of last edited control
      internal List<MgControlBase> ControlsList
      {
         get { return _controlsList; }
      }

      internal int Direction
      {
         get { return _direction; }
      }

      #region IComparable Members

      /// <summary>
      ///   implementing comparator, the events are compared by priority
      /// </summary>
      public int CompareTo(object obj)
      {
         RunTimeEvent otherEvent = (RunTimeEvent)obj;
         Priority otherPriority = otherEvent._priority;
         int result = otherPriority - _priority;
         if (result == 0)
            result = (int) (_timestamp - otherEvent._timestamp);
         return result;
      }

      #endregion

      /// <summary>
      ///   returns a replica of this runtime event
      /// </summary>
      internal RunTimeEvent replicate()
      {
         RunTimeEvent newRtEvt = (RunTimeEvent) MemberwiseClone();
         newRtEvt.setTimestamp();
         return newRtEvt;
      }

      /// <summary>
      ///   a helper method that does some common initialization for some of the constructors
      /// </summary>
      private void init(Task taskRef)
      {
         _task = taskRef ?? MGDataCollection.Instance.getCurrMGData().getFirstTask();
         _taskTag = _task == null ? "" : _task.getTaskTag();
         _mprgCreator = null;
      }

      /// <summary>
      ///   converts parameters to argument list
      /// </summary>
      internal void convertParamsToArgs()
      {
         _argList = new ArgumentsList();
         _argList.buildListFromParams(_parameters, _paramAttrs, _paramVals, _paramNulls);
         _parameters = 0;
         _paramAttrs = null;
         _paramVals = null;
         _paramNulls = null;
      }

      /// <summary>
      ///   get a reference to the task
      /// </summary>
      internal Task getTask()
      {
         findUserEvent();
         return _task;
      }

      /// <summary>
      ///   set the task
      /// </summary>
      /// <param name = "aTask">- reference to a task</param>
      internal void setTask(Task taskRef)
      {
         _task = taskRef;

         if (_task != null)
            _taskTag = _task.getTaskTag();
         else
            _taskTag = null;
      }

      /// <summary>
      ///   set a Timer event
      /// </summary>
      /// <param name = "seconds">the number of seconds for this events</param>
      /// <param name = "numberWnd">the mgdata ID for the event</param>
      internal void setTimer(int sec, int mgdID, bool isIdle)
      {
         setTimer(sec);
         _mgdId = mgdID;
         _isIdleTimer = isIdle;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgdID"></param>
      /// <param name="closeActCode"></param>
      /// <returns></returns>
      internal void setClose(int mgdID, int closeActCode)
      {
         setInternal(closeActCode);
         _mgdId = mgdID;
         _guiTriggeredEvent = true;
      }

      /// <summary>
      ///   get the line in table of control in a table
      /// </summary>
      /// <returns> the line number</returns>
      internal int getDisplayLine()
      {
         return _displayLine;
      }

      /// <summary>
      ///   get number of window (for timer events)
      /// </summary>
      internal int getMgdID()
      {
         return _mgdId;
      }

      /// <summary>
      ///   get the break level of Event
      /// </summary>
      protected internal override String getBrkLevel()
      {
         String level = base.getBrkLevel();

         if (_type == ConstInterface.EVENT_TYPE_INTERNAL)
         {
            switch (InternalEvent)
            {
               case InternalInterface.MG_ACT_CTRL_PREFIX:
               case InternalInterface.MG_ACT_CTRL_SUFFIX:
               case InternalInterface.MG_ACT_CTRL_VERIFICATION:
                  level += Control.Name;
                  break;

               case InternalInterface.MG_ACT_VARIABLE:
                  level += _eventFld.getVarName();
                  break;
            }
         }
         return level;
      }

      /// <summary>
      ///   set a new control for the runtime event
      /// </summary>
      /// <param name = "newCtrl">a reference to the new control </param>
      internal void setCtrl(MgControl newCtrl)
      {
         _ctrl = newCtrl;
      }

      /// <summary>
      ///   set the reversible exit flag to false
      /// </summary>
      internal void setNonReversibleExit()
      {
         _reversibleExit = false;
      }

      /// <summary>
      ///   returns the value of the reversible exit flag
      /// </summary>
      internal bool reversibleExit()
      {
         return _reversibleExit;
      }

      /// <summary>
      ///   build XML Event string for Server
      /// </summary>
      internal void buildXML(StringBuilder message)
      {
         /*
         Event:<event eventtype=粘|I|T|E|U|N・modifier=尿|C|S|N・
         keycode=馬umber・internalevent=馬umber・seconds=馬umber・
         exp=馬umber・user=杯ask_id,number・force_exit=燃|C|N・>
         */

         message.Append("\n   " + XMLConstants.TAG_OPEN + ConstInterface.MG_TAG_EVENT);

         if (_type != Char.MinValue)
            message.Append(" " + ConstInterface.MG_ATTR_EVENTTYPE + "=\"" + _type + "\"");
         if (_type == ConstInterface.EVENT_TYPE_SYSTEM && kbdItm != null)
         {
            Modifiers modifier = kbdItm.getModifier();
            if (modifier != Modifiers.MODIFIER_NONE)
               message.Append(" " + ConstInterface.MG_ATTR_MODIFIER + "=\"" + modifier + "\"");
            message.Append(" " + ConstInterface.MG_ATTR_KEYCODE + "=\"" + kbdItm.getKeyCode() + "\"");
         }
         if (InternalEvent != Int32.MinValue)
            message.Append(" " + ConstInterface.MG_ATTR_INTERNALEVENT + "=\"" + InternalEvent + "\"");
         if (_seconds != Int32.MinValue)
            message.Append(" " + ConstInterface.MG_ATTR_SECONDS + "=\"" + _seconds + "\"");
         if (Exp != null)
            message.Append(" " + XMLConstants.MG_ATTR_EXP + "=\"" + Exp.getId() + "\"");
         if (_forceExit != ForceExit.None)
            message.Append(" " + XMLConstants.MG_ATTR_EXP + "=\"" + (char)_forceExit + "\"");
         if (UserEvt != null)
            message.Append(" " + ConstInterface.MG_ATTR_USER + "=\"" + UserEvtTaskTag + "," + _userEvtIdx + "\"");

         message.Append(XMLConstants.TAG_TERM);
      }

      /// <summary>
      ///   get the argList
      /// </summary>
      /// <returns> the argList</returns>
      internal ArgumentsList getArgList()
      {
         return _argList;
      }

      /// <summary>
      ///   set the argList
      /// </summary>
      /// <param name = "aArgList"></param>
      internal bool setArgList(ArgumentsList aArgList)
      {
         bool bRc;
         bRc = (null == _argList);
         _argList = aArgList;
         return bRc;
      }

      /// <summary>
      ///   set a value to the immediate flag
      /// </summary>
      /// <param name = "val">the value to use</param>
      internal void setImmediate(bool val)
      {
         _immediate = val;
      }

      /// <summary>
      ///   returns the value of the immediate flag
      /// </summary>
      internal bool isImmediate()
      {
         return _immediate;
      }

      /// <summary>
      ///   set the reference to the task which is "responsible" for a main program to become active.
      ///   For example: if task A calls task B and B's ctlIdx is different than A's, then B's main
      ///   program will be inserted into the task hirarchy thus B is "responsible" for the main prg.
      /// </summary>
      /// <param name = "src">the "responsible" task.</param>
      internal void setMainPrgCreator(Task src)
      {
         _mprgCreator = src;
      }

      /// <summary>
      ///   get the task which is "responsible" for a main program's execution
      /// </summary>
      /// <returns> the "responsible" task.</returns>
      internal Task getMainPrgCreator()
      {
         return _mprgCreator;
      }

      /// <summary>
      ///   sets the isRealRefresh flag to a given value
      /// </summary>
      internal void setIsRealRefresh(bool val)
      {
         _isRealRefresh = val;
      }

      /// <summary>
      ///   returns the type of a refresh event true is real refresh e.i. one invoked directlly by the user
      /// </summary>
      internal bool getRefreshType()
      {
         return _isRealRefresh;
      }

      /// <summary>
      ///   mark the event as an event which was created by the server
      /// </summary>
      internal void setFromServer()
      {
         _fromServer = true;
      }

      /// <summary>
      ///   mark the event as an event which was not created by the server
      /// </summary>
      internal void resetFromServer()
      {
         _fromServer = false;
      }

      /// <returns> an indication wheather the server created this event
      /// </returns>
      internal bool isFromServer()
      {
         return _fromServer;
      }

      /// <returns> an indication wheather the event is a part of quit process
      /// </returns>
      internal bool isQuit()
      {
         return _eventSubType == EventSubType.CancelIsQuit;
      }

      /// <summary>
      ///   Indicates that we do not want the MG_ACT_CANCEL to perform rollback.
      /// </summary>
      /// <returns></returns>
      internal bool RollbackInCancel()
      {
         return _eventSubType != EventSubType.CancelWithNoRollback;
      }

      /// <summary>
      ///  while exit the task due error (can be on any error in database in non interactive)
      /// <returns></returns>
      internal bool ExitDueToError()
      {
         return _eventSubType == EventSubType.ExitDueToError;
      }


      /// <summary>
      ///   Indicates that we do not want the MG_ACT_CANCEL to perform rollback.
      /// </summary>
      /// <returns></returns>
      internal bool RtViewRefreshUseCurrentRow()
      {
         return _eventSubType == EventSubType.RtRefreshViewUseCurrentRow;
      }


      /// <summary>
      ///   sets sub event type (use for cancel & rt_view_refresh action)
      /// </summary>
      internal void SetEventSubType(EventSubType eventSubType)
      {
         _eventSubType = eventSubType;
      }

      /// <summary>
      ///   return the Field whose change triggered this event
      /// </summary>
      /// <returns>reference to the field that triggered the event</returns>
      internal Field getFld()
      {
         return _eventFld;
      }


      /// <summary>
      ///   Gui Event - event that were added by Gui Layer, like click, timer, ect
      ///   For all this event the priority is low
      /// </summary>
      /// <returns> true for GUI Events </returns>
      internal bool isGuiTriggeredEvent()
      {
         return _guiTriggeredEvent;
      }

      /// <summary>
      /// </summary>
      /// <returns> priority </returns>
      internal Priority getPriority()
      {
         return _priority;
      }

      /// <summary>
      ///   sets priority
      /// </summary>
      /// <param name = "priority"> </param>
      internal void setPriority(Priority priority)
      {
         _priority = priority;
      }

      internal bool isIdleTimer()
      {
         return _isIdleTimer;
      }

      internal String getValue()
      {
         return _val;
      }

      internal void setValue(String val)
      {
         _val = val;
      }

      internal int getMenuUid()
      {
         return _menuUid;
      }

      internal int getMenuComp()
      {
         return _menuComp;
      }

      internal int getStartSelection()
      {
         return _selectionStart;
      }

      internal int getEndSelection()
      {
         return _selectionEnd;
      }

      /// <summary>
      ///   sets setEditParms
      /// </summary>
      /// <param name = "start">start selection</param>
      /// <param name = "end">end selection</param>
      /// <param name = "text">the text being written</param>
      internal void setEditParms(int start, int end, String text)
      {
         _selectionStart = start;
         _selectionEnd = end;
         setValue(text);
         return;
      }

      internal void setImeParam(ImeParam im)
      {
         _imeParam = im;
      }

      internal ImeParam getImeParam()
      {
         return _imeParam;
      }

      /// <summary>
      ///   returns true for produceClick flag
      /// </summary>
      /// <returns></returns>
      internal bool isProduceClick()
      {
         return _produceClick;
      }

      /// <summary>
      ///   set produce click
      /// </summary>
      /// <param name = "produceClick"></param>
      internal void setProduceClick(bool produceClick)
      {
         _produceClick = produceClick;
      }

      /// <summary>
      ///   set the action list to enable/disable
      /// </summary>
      /// <param name = "actList"></param>
      internal void setActEnableList(int[] actList)
      {
         _actEnableList = actList;
      }

      /// <summary>
      ///   get the action list to enable/disable
      /// </summary>
      /// <returns></returns>
      internal int[] getActEnableList()
      {
         return _actEnableList;
      }

      /// <summary>
      ///   return send all flag
      /// </summary>
      /// <returns></returns>
      internal bool isSendAll()
      {
         return _sendAll;
      }

      /// <summary>
      ///   set sendall flag
      /// </summary>
      /// <param name = "sendAll"> </param>
      internal void setSendAll(bool sendAll)
      {
         _sendAll = sendAll;
      }

      /// <summary>
      ///   set dot net parameters as arguments for the event
      /// </summary>
      /// <param name = "parameters"></param>
      internal void setDotNetArgs(object[] parameters)
      {
         ExpressionEvaluator.ExpVal[] argsList = new ExpressionEvaluator.ExpVal[parameters.Length];
         for (int i = 0; i < parameters.Length; i++)
         {
            int key = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);
            String blobString = BlobType.createDotNetBlobPrefix(key);
            DNManager.getInstance().DNObjectsCollection.Update(key, parameters[i]);
            argsList[i] = new ExpressionEvaluator.ExpVal(StorageAttribute.DOTNET,
                                                         parameters[i] == null, blobString);
         }
         //create the string for the blob
         ArgumentsList args = new ArgumentsList(argsList);
         setArgList(args);
      }

      /// <summary>
      ///   remove DNObjectsCollection entries made by dot net parameters for the event
      /// </summary>
      internal void removeDotNetArgs()
      {
         // free the object table entry
         for (int i = 0; i < _argList.getSize(); i++)
         {
            string val = _argList.getArg(i).getValue(StorageAttribute.DOTNET, 0);
            DNManager.getInstance().DNObjectsCollection.Remove(BlobType.getKey(val));
         }
      }

      /// <summary>
      ///   Check if this event is blocked by the currently active window. If the
      ///   current window is modal, and the event defined in one of its ancestors,
      ///   the event should not be executed.
      /// </summary>
      /// <param name = "activeWindowData">The MGData object describing the currently active window.</param>
      /// <returns>
      ///   The method returns true if the currently active window is modal is a 
      ///   descendant of the event's task. Otherwise it returns false.
      /// </returns>
      internal bool isBlockedByModalWindow(MGData activeWindowData)
      {
         if (activeWindowData.IsModal)
         {
            // Check that the event's task is an ancestor of the active window's task.
            Task eventTask = getTask();
            Task baseTask = activeWindowData.getTask(0);
            if (eventTask != baseTask && baseTask.isDescendentOf(eventTask))
               return true;
         }
         return false;
      }

      public override string ToString()
      {
         return String.Format("{{RTEvent {0} ({1}), type {2}{3}}}", getBrkLevel(), InternalEvent, _type, (_task == null ? "" : " on " + _task));
      }
   }
}
