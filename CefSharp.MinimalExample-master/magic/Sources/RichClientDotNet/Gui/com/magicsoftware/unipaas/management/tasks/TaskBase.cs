using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.events;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management.tasks
{
   /// <summary>
   /// task properties (+ few access methods) required and relevant for the GUI namespace.
   /// </summary>
   public abstract class TaskBase : ITask, PropParentInterface
   {
      protected const int MAIN_PRG_PARENT_ID = 32768;

      #region initiatedFromServer

      public String ApplicationGuid { get; private set; }
      public int ProgramIsn { get; private set; }
      public int TaskIsn { get; private set; }

      public Int64 ContextID { get; protected set; }
      public bool IsSubForm { get; set; } //indicates that the current task is a subform's task

      protected int _ctlIdx { get; private set; }
      protected int _compIdx { get; private set; }
      protected bool _isPrg { get; private set; }
      protected bool _isMainPrg { get; private set; }
      public bool IsInteractive { get; set; }
      protected String _taskTag { get; private set; }
      protected bool _openWin { get; private set; }
      bool _allowEvents       { get; set; }
      protected MgFormBase Form { get; set; }
      protected PropTable _propTab { get; private set; }
      protected Helps _helpTab { get; private set; }
      protected char _nullArithmetic { get; private set; }
      public String IconFileName { get; private set; }

      // OL:contents passed completely inside a response
      // RC:contents loaded from cache.
      //    In case of Disabled Cache, it may passed inside a response ?
      protected byte[] _menusContent { get; private set; } 
      public bool IsParallel { get; private set; }

      #endregion

      #region initiatedByClient

      public DataViewBase DataView { get; protected set; }
      public IActionManager ActionManager { get; protected set; }
      public TaskBase StudioParentTask { get; set; } //parent task of the subtask by toolkit tree.
      protected IFlowMonitorQueue _flowMonitor;
      private char _lastMoveInRowDirection = Constants.MOVE_DIRECTION_NONE; // if TRUE, the task is a MoveinRow operation
      private String _saveStatusText; // if we got a message to display on the status bar and the task was not initialized yet (which means
      private int _keyboardMappingState = unchecked((int)0xFFFF0000);
      protected char _refreshType = Constants.TASK_REFRESH_TABLE;
      protected MgControlBase _lastParkedCtrl;
      private String _brkLevel; // the runtime level (Record  Suffix, TaskBase Prefix, etc.)
      private int _brkLevelIndex = -1;
      private String _mainLevel; // the runtime mainlevel
      private char _level = (char)(0); // 'C' = in control, 'R' = in record (out of control), 'T' = in task (out of record)
      protected bool _enteredRecLevel; // TRUE only if the task was ever moved to record level.
      private MgControlBase _currVerifyCtrl;
      protected Field _currParkedFld;
      private int _systemContextMenu = 0;
      private bool shouldResumeSubformLayout = true;

      //This flag represents the current editing control.
      //It is reset to null when we are in between setting the focus and once we set the focus to control then we set this flag to current focused control.
      public MgControlBase CurrentEditingControl;

      internal bool ShouldResumeSubformLayout
      {
         get { return shouldResumeSubformLayout; }
         set { shouldResumeSubformLayout = value; }
      } 

      public bool IsSubtask { get; set; }
      public bool DataViewWasRetrieved {get; set;} // indicates that the data view was retrieved, i.e. it is possible to evaluate expressions

      /// <summary>
      /// indicates whether the task prefix was performed for this task
      /// </summary>
      public bool TaskPrefixExecuted { get; set; }

      #endregion

      /// <summary>
      /// 
      /// </summary>
      protected TaskBase()
      {
         _nullArithmetic = Constants.NULL_ARITH_USE_DEF;
         _propTab = new PropTable(this);
      }

      /// <summary>
      ///   get task id
      /// </summary>
      public String getTaskTag()
      {
         return _taskTag;
      }

      /// <summary>
      ///   return the ctl idx
      /// </summary>
      public int getCtlIdx()
      {
         return _ctlIdx;
      }

      /// <summary>
      ///   returns true if this task is a main program of an internal or external component
      /// </summary>
      public bool isMainProg()
      {
         return _isMainPrg;
      }

      /// <summary>
      ///   return the topmost mgform of the subform
      /// </summary>
      /// <returns></returns>
      public MgFormBase getTopMostForm()
      {
         MgFormBase form = null;

         if (Form != null)
            form = Form.getTopMostForm();
         //else
         // Assert should not be here because we have different conditions for RC & OL. 
         //Other then Main Program, task's form should not be null.
         //Debug.Assert(isMainProg());

         return form;
      }

      /// <summary>
      ///   returns the requested control
      /// </summary>
      public MgControlBase getCtrl(int ctrlIdx)
      {
         return Form.getCtrl(ctrlIdx);
      }

      /// <summary>
      /// reset menus content
      /// </summary>
      /// <returns></returns>
      public void resetMenusContent()
      {
         _menusContent = null;
      }

      /// <summary>
      /// sets the current task mode
      /// </summary>
      /// <param name="val">new task mode</param>
      public virtual void setMode(char val)
      {
         bool checkPaste = false;
         if (getMode() != val)
         {
            setProp(PropInterface.PROP_TYPE_TASK_MODE, val.ToString());

            //todo paste enable/disable
            checkPaste = true;

            if (_flowMonitor != null)
               _flowMonitor.addTaskCngMode(ContextID, val);
         }

         // check if paste event should be enable/disabled due to task mode change.
         if (checkPaste)
            ActionManager.checkPasteEnable(getLastParkedCtrl());

      }

      public void SetActiveHighlightRowState(bool state)
      {
         if ( Form != null)
         {
            Form.SetActiveHighlightRowState(state);
         }

      }

      /// <summary>
      ///   get the task mode
      /// </summary>
      public char getMode()
      {
         char taskMode = Constants.TASK_MODE_MODIFY;
         Property prop = getProp(PropInterface.PROP_TYPE_TASK_MODE);
         if (prop != null)
            taskMode = prop.getValue()[0];
         return taskMode;
      }

      /// <summary>
      ///   setter for the saveStatusText - saves the text to be written to the status bar once the task is initialized
      /// </summary>
      /// <param name = "statusText"></param>
      public void setSaveStatusText(String statusText)
      {
         _saveStatusText = statusText;
      }

      /// <summary>
      ///   getter for the saveStatusText
      /// </summary>
      /// <returns> the saveStatusText</returns>
      public String getSaveStatusText()
      {
         return _saveStatusText;
      }

      /// <summary>
      ///   reset refresh type
      /// </summary>
      public void resetRefreshType()
      {
         _refreshType = Constants.TASK_REFRESH_NONE;
      }

      /// <summary>
      ///   get refresh type
      /// </summary>
      /// <returns></returns>
      public char getRefreshType()
      {
         return _refreshType;
      }

      /// <summary>
      ///   set the refresh type for the next RefreshDisplay() call
      /// </summary>
      /// <param name = "refreshType">the new refresh type</param>
      public void SetRefreshType(char refreshType)
      {
         switch (refreshType)
         {
            case Constants.TASK_REFRESH_FORM:
            case Constants.TASK_REFRESH_TABLE:
            case Constants.TASK_REFRESH_CURR_REC:
            case Constants.TASK_REFRESH_TREE_AND_FORM:
               _refreshType = refreshType;
               break;

            default:
               break;
         }
      }

      /// <summary>
      ///   get last parked control
      /// </summary>
      public MgControlBase getLastParkedCtrl()
      {
         return _lastParkedCtrl;
      }

      /// <summary> get the open window
      /// </summary>
      public bool isOpenWin()
      {
         return _openWin;
      }

      /// <summary>
      ///   if the property exists returns its boolean value otherwise returns the default value
      /// </summary>
      /// <param name = "propId">id of the property</param>
      /// <param name = "defaultRetVal">the value to return in case the property does not exist</param>
      public bool checkProp(int propId, bool defaultRetVal)
      {
         Property prop = getProp(propId);
         if (prop != null)
            return prop.getValueBoolean();
         return defaultRetVal;
      }

      /// <summary>
      ///   returns the clicked control on the task
      /// </summary>
      public MgControlBase getClickedControl()
      {
         MgControlBase clickedCtrl = Manager.GetCurrentRuntimeContext().CurrentClickedCtrl;

         if (clickedCtrl != null && clickedCtrl.getForm().getTask() != this)
            clickedCtrl = null;

         return clickedCtrl;
      }

      /// <returns> keyboardMappingState
      /// </returns>
      public int getKeyboardMappingState()
      {
         return (_keyboardMappingState);
      }

      /// <summary>
      ///   returns true if state enabled
      /// </summary>
      /// <param name = "state"></param>
      /// <returns></returns>
      public bool isStateEnabled(int state)
      {
         return ((state & _keyboardMappingState) != 0);
      }

      /// <summary>
      ///   Sets the state on/off
      /// </summary>
      /// <param name = "state"></param>
      /// <param name = "on"></param>
      public void setKeyboardMappingState(int state, bool on)
      {
         _keyboardMappingState = on
                                   ? mkInt(hiShrt(_keyboardMappingState) & ~state, loShrt(_keyboardMappingState) | state)
                                   : mkInt(hiShrt(_keyboardMappingState) | state, loShrt(_keyboardMappingState) & ~state);
      }

      /// <param name = "n"></param>
      /// <returns> higher half of the integer</returns>
      public static int hiShrt(int n)
      {
         return (((n) & unchecked((int)0xffff0000)) >> 16);
      }

      /// <param name = "n"></param>
      /// <returns> low half of the integer</returns>
      public static int loShrt(int n)
      {
         return ((n) & 0x0000ffff);
      }

      /// <param name = "n1"></param>
      /// <param name = "n2"></param>
      /// <returns> unites low halves of 2 integers   </returns>
      public static int mkInt(int n1, int n2)
      {
         return (((n1) << 16) | ((n2)));
      }

      /// <summary>
      ///   get help item
      /// </summary>
      /// <param name = "idx">is the index of the item</param>
      public MagicHelp getHelpItem(int idx)
      {
         if (_helpTab != null)
            return _helpTab.getHelp(idx);
         return null;
      }

      /// <summary>
      ///   set a property of the task
      /// </summary>
      /// <param name = "propId">the id of the property</param>
      /// <param name = "val">the value of the property</param>
      public void setProp(int propId, String val)
      {
         _propTab.setProp(propId, val, this, 'T');
      }

      /// <summary>
      ///   set a property of the task
      /// </summary>
      /// <param name = "propId">the id of the property</param>
      /// <param name = "val">the value of the property</param>
      public void setProp(int propId, char val)
      {
         bool checkPaste = false;
         if (propId == PropInterface.PROP_TYPE_TASK_MODE && getMode() != val)
         {
            //todo paste enable/disable
            checkPaste = true;

            if (_flowMonitor != null)
               _flowMonitor.addTaskCngMode(ContextID, val);
         }

         setProp(propId, "" + val);

         // check if paste event should be enable/disabled due to task mode change.
         if (checkPaste)
            ActionManager.checkPasteEnable(getLastParkedCtrl());
      }

      /// <summary>
      /// </summary>
      /// <param name = "moveInRowDirection"></param>
      public void setLastMoveInRowDirection(char moveInRowDirection)
      {
         _lastMoveInRowDirection = moveInRowDirection;
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      public char getLastMoveInRowDirection()
      {
         return _lastMoveInRowDirection;
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      public bool isMoveInRow()
      {
         return (_lastMoveInRowDirection != Constants.MOVE_DIRECTION_NONE);
      }

      /// <summary>
      ///   set the runtime level of the task (Record Suffix, TaskBase Prefix, etc.)
      /// </summary>
      public void setBrkLevel(String cBrkLevel, int NewBrkLevelIndex)
      {
         _brkLevel = cBrkLevel;
         _brkLevelIndex = NewBrkLevelIndex;

         setMainLevel(cBrkLevel); // Update the MainLevel according to the new Level
      }

      /// <summary>
      ///   returns the runtime level of the task (Record Suffix, TaskBase Prefix, etc.)
      /// </summary>
      public String getBrkLevel()
      {
         return _brkLevel;
      }

      /// <summary>
      ///   return the id of the current executing handler
      /// </summary>
      /// <returns> the int id of the current executing handler</returns>
      public int getBrkLevelIndex()
      {
         return _brkLevelIndex;
      }

      /// <summary>
      ///   set the runtime mainlevel of the task
      /// </summary>
      protected internal void setMainLevel(String cBrkLevel)
      {
         if (cBrkLevel != null && cBrkLevel.Length > 0)
         {
            char level = cBrkLevel[0];
            /* then MAINLEVEL is updated only if the actual Level is a TASK or RECORD ( or none ) level */
            if (level == Constants.TASK_LEVEL_NONE || level == Constants.TASK_LEVEL_TASK ||
                level == Constants.TASK_LEVEL_RECORD)
               _mainLevel = cBrkLevel;
         }
      }

      /// <summary>
      ///   get the runtime mainlevel of the task
      /// </summary>
      public String getMainLevel()
      {
         return _mainLevel;
      }

      /// <summary>
      ///   set the level of the task
      /// </summary>
      /// <param name = "cLevel">'C' = in control, 'R' = in record (out of control), 'T' = in task (out of record)      /// </param>
      public void setLevel(char cLevel)
      {
         _level = cLevel;

         if (_level == Constants.TASK_LEVEL_RECORD)
            _enteredRecLevel = true;
      }

      /// <summary>
      ///   return the level of the task: 'C' = in control, 'R' = in record (out of control), 'T' = in task (out of record)
      /// </summary>
      public char getLevel()
      {
         return _level;
      }
      /// <summary>
      ///   set the DataSynced property of task
      /// </summary>
      public virtual void setDataSynced(bool synced)
      {
      }

      /// <summary>
      ///   returns scheme for null arithmetic
      /// </summary>
      public char getNullArithmetic()
      {
         return _nullArithmetic;
      }

      /// <summary>
      ///   returns System Context Menu
      /// </summary>
      public int getSystemContextMenu()
      {
         return _systemContextMenu;
      }

      /// <summary>
      /// get the currVerifyCtrl
      /// </summary>
      /// <returns></returns>
      public MgControlBase getCurrVerifyCtrl()
      {
         return _currVerifyCtrl;
      }

      /// <summary>
      /// set the currVerifyCtrl
      /// </summary>
      /// <param name="aCtrl"></param>
      public void setCurrVerifyCtrl(MgControlBase aCtrl)
      {
         if (aCtrl == null || aCtrl.getField() != null)
            _currVerifyCtrl = aCtrl;
      }

      /// <summary>
      ///   set the last parked control in this task
      /// </summary>
      /// <param name = "ctrl">last focused control</param>
      public virtual void setLastParkedCtrl(MgControlBase ctrl)
      {
         _lastParkedCtrl = ctrl;
         _currParkedFld = (_lastParkedCtrl != null
                            ? _lastParkedCtrl.getField()
                            : null);
         CurrentEditingControl = ctrl;
      }

      /// <summary>
      ///   get current field of the task
      /// </summary>
      public Field getCurrField()
      {
         return _currParkedFld;
      }

      /// <summary>
      ///   set current field of the task
      /// </summary>
      public void setCurrField(Field currField)
      {
         _currParkedFld = currField;
      }

      /// <summary>
      ///   returns the index of the field of the current parked control or -1 if there is no such field
      /// </summary>
      public int getCurrFieldIdx()
      {
         if (_currParkedFld != null)
            return _currParkedFld.getId();
         return -1;
      }

      public FieldDef getFieldByValueStr(String valueStr)
      {
         int parent;
         int vee;
         return getFieldByValueStr(valueStr, out parent, out vee);
      }

      /// <summary>
      /// get the field by 'valueStr'
      /// </summary>
      /// <param name="valueStr"></param>
      /// <returns></returns>
      public FieldDef getFieldByValueStr(String valueStr, out int parent, out int vee)
      {
         parent = vee = 0;
         FieldDef tempField = null;
         int comma = valueStr.IndexOf(",");
         if (comma > 0)
         {
            int parentId = Int32.Parse(valueStr.Substring(0, comma));
            int fldIdx = Int32.Parse(valueStr.Substring(comma + 1));
            tempField = (parentId != 0
                           ? getField(parentId, fldIdx)
                           : getField(fldIdx));

            parent = parentId;
            vee = fldIdx;
         }
         return tempField;
      }

      /// <summary>
      /// get the field from a 1-based main prog argument
      /// </summary>
      /// <param name="valueStr"></param>
      /// <returns></returns>
      public FieldDef getMainProgFieldByValueStr(String valueStr)
      {
         // string is 1-based - need to subtract 1 to get to 0-based value
         int fldIdx = Int32.Parse(valueStr) - 1;
         return getField(fldIdx);
      }

      /// <summary>
      ///   get a field by its idx
      /// </summary>
      /// <param name = "fldId">id of the field </param>
      public virtual FieldDef getField(int fldId)
      {
         return DataView.getField(fldId);
      }    

      /// <summary>
      /// initialize the current task's form.
      /// </summary>
      public virtual ITask InitForm()
      {
         TaskBase nonInteractiveTask = null;

         if (Form != null)
         {
            Form.InInitForm = true;
            Form.init();

            if (Form.getSubFormCtrl() != null)
            {
               Commands.addAsync(CommandType.SUSPEND_LAYOUT, Form.getSubFormCtrl().getParent(), true);
               //QCR #449743, show subform after all controls are displayed
               Commands.addAsync(CommandType.PROP_SET_VISIBLE, Form.getSubFormCtrl(), 0, false, !Form.IsFirstRefreshOfProps());
            }

            SetRefreshType(Form.hasTree()
                               ? Constants.TASK_REFRESH_TREE_AND_FORM
                               : Constants.TASK_REFRESH_FORM);

            Form.InitFormBeforeRefreshDisplay();
            RefreshDisplay();

            Form.arrangeZorder();
            Form.fixTableLocation();
            Form.createPlacementLayout();

            if (!IsInteractive && !_isMainPrg)
               nonInteractiveTask = this;

#if !PocketPC
            if (!IsSubForm)
            {
               String iconFileNameTrans;
               iconFileNameTrans = IconFileName != null ? Events.Translate(IconFileName) : "@Mgxpa";
               //After #984822, Gui.low expects the local file path
               if (!iconFileNameTrans.StartsWith("@"))
                  iconFileNameTrans = Events.GetLocalFileName(iconFileNameTrans, this);
               Commands.addAsync(CommandType.PROP_SET_ICON_FILE_NAME, Form, 0, iconFileNameTrans, 0);
            }
#endif

            if (Form.getSubFormCtrl() == null)
            {
               // calculation the height of the form should include the menu and toolbar height.
               // this must be executed after menu and toolbar create, and before the layout executed
               Property heightProperty = Form.GetComputedProperty(PropInterface.PROP_TYPE_HEIGHT);
               if (heightProperty != null)
                  heightProperty.RefreshDisplay(true);
            }
            else
            {
               // subform
               if (CanResumeSubformLayout())
                  ResumeSubformLayout(false);
            }

            if (Form.getContainerCtrl() != null)
               Commands.addAsync(CommandType.RESUME_LAYOUT, Form.getContainerCtrl(), false);

            Commands.beginInvoke();
            Form.InInitForm = false;
         }

         return nonInteractiveTask;
      }

      /// <summary>
      /// check if we can resume subform layout during init form  (check if data was retrieved)
      /// </summary>
      /// <returns></returns>
      virtual protected bool CanResumeSubformLayout()
      {
         return true;
      }

      /// <summary>
      /// resume subform layout
      /// </summary>
      /// <param name="applyFormUserState"> true if should apply form's user state</param>
      protected void ResumeSubformLayout(bool applyFormUserState)
      {
         if (Form != null)
         {
            MgControlBase subformControl = Form.getSubFormCtrl();
            if (subformControl != null && shouldResumeSubformLayout)
            {
               Commands.addAsync(CommandType.RESUME_LAYOUT, subformControl, false);
               Commands.addAsync(CommandType.PROP_SET_VISIBLE, subformControl, 0,
                                 subformControl.isVisible(), !Form.IsFirstRefreshOfProps());
               Commands.addAsync(CommandType.RESUME_LAYOUT, subformControl.getParent(), true);
               shouldResumeSubformLayout = false; //make sure we do this only once
               if (applyFormUserState)
                  Manager.ApplyformUserState(Form);

            }
         }
      }

      /// <summary>
      ///   returns the name of the control which was last parked
      /// </summary>
      /// <param name = "depth">the generation</param>
      internal String GetLastParkedCtrlName(int depth)
      {
         var ancestor = (TaskBase)GetTaskAncestor(depth);

         if (ancestor == null || ancestor._lastParkedCtrl == null)
            return "";
         return ancestor._lastParkedCtrl.Name;
      }

      #region PropParentInterface Members

      /// <summary>
      ///   get the form of the task
      /// </summary>
      /// <returns> Form is the form of the task</returns>
      public MgFormBase getForm()
      {
         return Form;
      }

      /// <summary>
      /// Set task's form.
      /// </summary>
      /// <returns> Form is the form of the task</returns>
      public MgFormBase SetForm(MgFormBase value)
      {
         return Form = value;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="propId"></param>
      /// <returns></returns>
      public bool checkIfExistProp(int propId)
      {
         bool exist = false;

         if (_propTab != null)
         {
            Property prop = _propTab.getPropById(propId);
            exist = prop != null;
         }
         return exist;
      }

      /// <summary>
      ///   get a task property
      /// </summary>
      /// <param name = "propId">Id of the requested property
      /// </param>
      public virtual Property getProp(int propId)
      {

         Property prop = null;

         if (_propTab != null)
         {
            prop = _propTab.getPropById(propId);

            // if the property doesn't exist then create a new property and give it the default value
            // in addition add this property to the properties table
            if (prop == null)
            {
               prop = PropDefaults.getDefaultProp(propId, GuiConstants.PARENT_TYPE_TASK, this);
               if (prop != null)
               {
                  prop.StudioValue = prop.getOrgValue();
                  _propTab.addProp(prop, false);
               }
            }
         }
         return prop;

      }

      /// <summary>
      ///   return the component idx
      /// </summary>
      public int getCompIdx()
      {
         return _compIdx;
      }

      /// <summary>
      ///   return true if this is first refresh
      /// </summary>
      /// <returns></returns>
      public bool IsFirstRefreshOfProps()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Evaluates the expression corresponding to the passed ID.
      /// </summary>
      /// <param name="expId">Exp ID</param>
      /// <param name="resType">expected return type</param>
      /// <param name="length">Expected len - is not used, it must be here because of computability with Rich Client</param>
      /// <param name="contentTypeUnicode">should the result be unicode</param>
      /// <param name="resCellType"></param>
      /// <param name="alwaysEvaluate">for form properties and some control properties we always evaluate expressions</param>
      /// <param name="wasEvaluated">indicates whether the expression was evaluated</param>
      /// <returns>evaluated value</returns>
      public String EvaluateExpression(int expId, StorageAttribute resType, int length, bool contentTypeUnicode,
                                                StorageAttribute resCellType, bool alwaysEvaluate, out bool wasEvaluated)
      {
          String result = null;
          wasEvaluated = false;
          if (alwaysEvaluate || DataViewWasRetrieved)
          {
              result = CalculateExpression(expId, resType, length, contentTypeUnicode, resCellType);
              wasEvaluated = true;
          }
          return result;
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
      public abstract string CalculateExpression(int expId, StorageAttribute resType, int length, bool contentTypeUnicode, StorageAttribute resCellType);

      #endregion

      #region abstract and virtual methods

      /// <summary>
      /// creates a mgform object
      /// </summary>
      /// <returns></returns>
      public abstract MgFormBase ConstructMgForm(ref bool alreadySetParentForm);

      /// <summary>
      ///   returns true if the task is started
      /// </summary>
      public virtual bool isStarted()
      {
         return true;
      }

      /// <summary>
      ///   tells whether the task is aborting
      /// </summary>
      public virtual bool isAborting()
      {
         return false;
      }

      /// <summary>
      ///   get the ancestor of the TaskBase
      /// </summary>
      /// <param name = "generation">of the ancestor task</param>
      public abstract ITask GetTaskAncestor(int generation);

      /// <summary>
      /// get the context task
      /// </summary>
      /// <returns></returns>
      public abstract ITask GetContextTask();

      /// <summary>
      /// Returns task depth
      /// </summary>
      /// <returns></returns>
      public abstract int GetTaskDepth();

      /// <summary>Synchronize non-Gui[RTE(MgCore)] after creating a .NET control.</summary>
      /// <param name="ctrlIdx">in: the .net control</param>
      public abstract void OnDNControlCreate(int ctrlIdx);

      /// <summary>
      /// return events-related details about a .net control on a form
      /// </summary>
      /// <param name="ctrlIdx">in: the .net control</param>
      /// <param name="dnObjKey">out: object key of the variable attached to the control</param>
      /// <param name="allTasksDNEventsNames">out: comma-delimited string of events that are handled in the current tasks or its ancestors, up to the main program</param>
      public abstract void GetDNControlEventsDetails(int ctrlIdx, out int dnObjKey, out List<String> allTasksDNEventsNames);

      /// <summary> refresh the display, paint the controls and their properties again </summary>
      public abstract void RefreshDisplay();

      /// <summary> get the value of a field. </summary>
      /// <param name="fieldDef"></param>
      /// <param name="value"></param>
      /// <param name="isNull"></param>
      public abstract void getFieldValue(FieldDef fieldDef, ref String value, ref bool isNull);

      /// <summary> get the Isn of PosElement of PosCache. </summary>
      public abstract int GetDVControlPosition();

      /// <summary> get the display value of a field. </summary>
      /// <param name="fieldDef"></param>
      /// <param name="value"></param>
      /// <param name="isNull"></param>
      public abstract void getFieldDisplayValue(FieldDef fieldDef, ref String value, ref bool isNull);

      /// <summary> updates the field's value and starts recompute. </summary>
      /// <param name="fieldDef"></param>
      /// <param name="value"></param>
      /// <param name="isNull"></param>
      public abstract void UpdateFieldValueAndStartRecompute(FieldDef fieldDef, String value, bool isNull);

      /// <summary> /// This function returns true if field in null else false.</summary>
      /// <param name="fieldDef"></param>
      /// <returns>true if field is null else false</returns>
      public abstract bool IsFieldNull(FieldDef fieldDef);

      /// <summary>
      /// return menus content (passed completely inside a response, i.e. not thru the cache)
      /// </summary>
      /// <returns></returns>
      public virtual void setMenusContent(byte[] content)
      {
         _menusContent = content;
      }

      /// <summary>
      /// return menus content (passed completely inside a response, i.e. not thru the cache)
      /// </summary>
      /// <returns></returns>
      public virtual byte[] getMenusContent()
      {
         return _menusContent;
      }

      /// <summary>
      /// compose and return a url for the menus content
      /// </summary>
      /// <returns></returns>
      public virtual String getMenusFileURL()
      {
         String menusFileURL = null;
         if (_menusContent != null)
            menusFileURL = String.Format("nocache_{0}_Menus{1}.xml", _menusContent.GetHashCode(), ContextID);
         return menusFileURL;
      }

      /// <summary></summary>
      /// <returns>true iff menus were attached to the program (in the server side)</returns>
      public virtual bool menusAttached()
      {
         return getMenusContent() != null;
      }
      /// <summary>get a field from the task.travel by parents (i.e. NOT by triggering tasks).</summary>
      /// <param name = "parent">generation number (0 is current task)</param>
      /// <param name = "fldIdx">the field index within its task</param>
      /// <returns> the id of the field described by parent + idx</returns>
      public virtual FieldDef getField(int parent, int fldIdx)
      {
         switch (parent)
         {
            case 0:
               return getField(fldIdx);

            case MAIN_PRG_PARENT_ID:
               // find the main program using the component id (ctl idx)
               TaskBase compMainProg = (TaskBase)Manager.MGDataTable.GetMainProgByCtlIdx(ContextID, _ctlIdx);
               return compMainProg.getField(fldIdx);

            default:
               parent--;
               if (StudioParentTask != null)
                  return StudioParentTask.getField(parent, fldIdx);
               return null;
         }
      }

      #endregion

      #region methods for parse

      /// <summary>
      ///   get the task attributes
      /// </summary>
      protected virtual void fillAttributes()
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            string tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(XMLConstants.MG_TAG_TASK) + XMLConstants.MG_TAG_TASK.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               string attribute = (tokensVector[j]);
               string valueStr = (tokensVector[j + 1]);
               if (!setAttribute(attribute, valueStr) && Events.ShouldLog(Logger.LogLevels.Development))
                  Events.WriteDevToLog(string.Format("In TaskBase.fillAttributes(): Unprocessed(!) attribute: '{0}' = '{1}'", attribute, valueStr));
            }

            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1); // to delete ">" too
         }
      }

      /// <summary>
      ///   get the task attributes
      /// </summary>
      protected virtual bool setAttribute(string attribute, string valueStr)
      {
         bool isTagProcessed = true;

         switch (attribute)
         {
            case XMLConstants.MG_ATTR_APPL_GUID:
               ApplicationGuid = valueStr;
               break;

            case XMLConstants.MG_ATTR_PROGRAM_ISN:
               ProgramIsn = Int32.Parse(valueStr);
               break;

            case XMLConstants.MG_ATTR_TASK_ISN:
               TaskIsn = Int32.Parse(valueStr);
               break;

            case XMLConstants.MG_ATTR_TOOLKIT_PARENT_TASK:
               if (Int32.Parse(valueStr) != 0)
                  StudioParentTask = (TaskBase)Manager.MGDataTable.GetTaskByID(valueStr);

               IsSubtask = true;
               break;

            case XMLConstants.MG_ATTR_TASKID:
               setTaskId(valueStr);
               if (Events.ShouldLog(Logger.LogLevels.Development))
                  Events.WriteDevToLog("TASK: " + _taskTag);
               break;

            case XMLConstants.MG_ATTR_CTL_IDX:
               setCtlAndCompIdx(valueStr);
               break;

            case XMLConstants.MG_ATTR_MAINPRG:
               setMainPrg(valueStr);
               break;

            case XMLConstants.MG_ATTR_NULL_ARITHMETIC:
               setNullArithmetic(valueStr);
               break;

            case XMLConstants.MG_ATTR_INTERACTIVE:
               setIsInteracive(valueStr);
               break;
               
            case XMLConstants.MG_ATTR_OPEN_WIN:
               setOpenWin(valueStr);
               break;

            case XMLConstants.MG_ATTR_ALLOW_EVENTS:
               SetAllowEvents(valueStr);
               break;

            case XMLConstants.MG_ATTR_ISPRG:
               setIsPrg(valueStr);
               break;

            case XMLConstants.MG_ATTR_ICON_FILE_NAME:
               IconFileName = valueStr;
               break;

            case XMLConstants.MG_ATTR_SYS_CONTEXT_MENU:
               _systemContextMenu = Int32.Parse(valueStr);
               break;

            case XMLConstants.MG_ATTR_PARALLEL:
               IsParallel = XmlParser.getBoolean(valueStr);
               break;

            case XMLConstants.MG_ATTR_SORT_BY_RECENTLY_USED:
#if !PocketPC
               Manager.MenuManager.WindowList.SetSortByRecentlyUsed(XmlParser.getBoolean(valueStr));
#endif
               break;

            default:
               isTagProcessed = false;
               break;
         }

         return isTagProcessed;
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">possible tag name , name of object, which need be allocated</param>
      /// <param name="parentForm">the form of the task that called the current task (note: NOT triggering task)</param>
      protected virtual bool initInnerObjects(String foundTagName, MgFormBase parentForm)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         switch (foundTagName)
         {
            case XMLConstants.MG_TAG_HELPTABLE:
               if (_helpTab == null)
                  _helpTab = new Helps();

               if (Events.ShouldLog(Logger.LogLevels.Development))
                  Events.WriteDevToLog(string.Format("{0} ...", foundTagName));

               _helpTab.fillData();
               break;

            case XMLConstants.MG_TAG_DVHEADER:
               if (Events.ShouldLog(Logger.LogLevels.Development))
                  Events.WriteDevToLog(string.Format("{0} ...", foundTagName));

               DataView.fillHeaderData();
               break;

            case XMLConstants.MG_TAG_PROP:
               _propTab.fillData(this, 'T');
               break;

            case XMLConstants.MG_TAG_FORM:
               Form = FormInitData(parentForm);
               break;

            case XMLConstants.MG_TAG_ASSEMBLIES:
               int start = parser.getCurrIndex();
               int endContext = parser.getXMLdata().IndexOf('/' + XMLConstants.MG_TAG_ASSEMBLIES,
                                                                    parser.getCurrIndex());
               parser.setCurrIndex(endContext);
               parser.setCurrIndex2EndOfTag();
               String assemblyData = parser.getXMLdata().Substring(start, parser.getCurrIndex() - start);
               //read assemblies data using sax parser
               try
               {
                  var mgSAXParser = new MgSAXParser(new AssembliesSaxHandler());
                  mgSAXParser.Parse(assemblyData);
               }
               catch (Exception ex)
               {
                  Events.WriteExceptionToLog(ex);
               }
               break;

            case XMLConstants.MG_ATTR_MENU_CONTENT:
               // set current index after tag name end
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.MG_ATTR_MENU_CONTENT, parser.getCurrIndex()) +
                                      XMLConstants.MG_ATTR_MENU_CONTENT.Length + 1);
               endContext = parser.getXMLdata().IndexOf("</" + XMLConstants.MG_ATTR_MENU_CONTENT, parser.getCurrIndex());
               _menusContent = Encoding.Unicode.GetBytes(parser.getXMLsubstring(endContext));

               Manager.MenuManager.getApplicationMenus(this);
               parser.setCurrIndex(endContext);
               parser.setCurrIndex2EndOfTag();
               break;

            default:
               return false;
         }

         return true;
      }

      /// <summary>
      /// init data for form and return the form
      /// </summary>
      /// <param name="foundTagName"></param>
      /// <param name="parentForm"></param>
      /// <returns></returns>
      public MgFormBase FormInitData(MgFormBase parentForm)
      {
         bool alreadySetParentForm = false;

         if (Form == null)
            Form = ConstructMgForm(ref alreadySetParentForm);

         if (!alreadySetParentForm)
            Form.ParentForm = parentForm;

         if (Events.ShouldLog(Logger.LogLevels.Development))
            Events.WriteDevToLog(XMLConstants.MG_TAG_FORM);

         Form.fillData(this);
        
         return Form;
      }

      /// <summary>
      /// returns true if this task should block activation of its acsestors
      /// </summary>
      public virtual bool IsBlockingBatch
      {
         get
         {
            return !_isMainPrg && isOpenWin() && !_allowEvents && !IsInteractive;
         }
      }
      /// <summary>
      ///   set task id
      /// </summary>
      public void setTaskId(String valueStr)
      {
         _taskTag = valueStr;
      }

      /// <summary>
      ///   set ctlIdx & compIdx
      /// </summary>
      private void setCtlAndCompIdx(String valueStr)
      {
         int i = valueStr.IndexOf(",");
         if (i > -1)
         {
            _ctlIdx = Int32.Parse(valueStr.Substring(0, i));
            _compIdx = Int32.Parse(valueStr.Substring(i + 1));
         }
      }

      /// <summary>
      /// Get ancestor task containing form 
      /// </summary>
      /// <returns>TaskBase</returns>
      public TaskBase GetAncestorTaskContainingForm()
      {
         TaskBase parentTask = (TaskBase)GetTaskAncestor(1);
         while (parentTask != null && parentTask.getForm() == null)
         {
            parentTask = (TaskBase)parentTask.GetTaskAncestor(1);
         }

         return parentTask;
      }

      /// <summary>
      /// set isMainPrg
      /// </summary>
      /// <param name="valueStr"></param>
      private void setMainPrg(String valueStr)
      {
         _isMainPrg = XmlParser.getBoolean(valueStr);
      }

      /// <summary>
      /// set nullArithmetic
      /// </summary>
      /// <param name="valueStr"></param>
      private void setNullArithmetic(String valueStr)
      {
         _nullArithmetic = valueStr[0];
      }

      /// <summary>
      /// set isInteractive
      /// </summary>
      /// <param name="valueStr"></param>
      private void setIsInteracive(String valueStr)
      {
         IsInteractive = XmlParser.getBoolean(valueStr);
      }

      /// <summary>
      /// set openWin
      /// </summary>
      /// <param name="valueStr"></param>
      private void setOpenWin(String valueStr)
      {
         _openWin = XmlParser.getBoolean(valueStr);
      }

      /// <summary>
      /// set openWin
      /// </summary>
      /// <param name="valueStr"></param>
      public void SetOpenWin(bool valuebool)
      {
         _openWin = valuebool;
      }

      /// <summary>
      /// set AllowEvents
      /// </summary>
      /// <param name="valueStr"></param>
      public void SetAllowEvents(String valueStr)
      {
         _allowEvents = XmlParser.getBoolean(valueStr);
      }


      /// <summary>
      /// set isPrg
      /// </summary>
      /// <param name="valueStr"></param>
      private void setIsPrg(String valueStr)
      {
         _isPrg = XmlParser.getBoolean(valueStr);
      }

      /// <summary>
      /// </summary>
      /// <param name="propId"></param>
      /// <returns></returns>
      public virtual bool ShouldEvaluatePropertyLocally(int propId)
      {
         return false;
      }

      #endregion
   }
}
