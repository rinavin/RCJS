using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using com.magicsoftware.support;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using Gui.com.magicsoftware.unipaas.management.gui;
using util.com.magicsoftware.util;
using System.Web.Script.Serialization;

#if !PocketPC
using RuntimeDesigner.Serialization;
using Gui.com.magicsoftware.unipaas.util;
#endif

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   ///   data for <form>...</form>
   /// </summary>
   public abstract class MgFormBase : GuiMgForm, PropParentInterface
   {
      private readonly List<MgControlBase> _controlsInheritingContext;
      private MgPointF _fontSize = new MgPointF(0, 0);
      private readonly Hashtable _toolbarGroupsCount; // <int index, ToolbarInfo>
      private bool _allowedSubformRecompute = true; //if we re allowed to recompute subforms
      private WindowType _concreteWindowType;
      private MgControlBase _containerCtrl;
      protected int _destTblRow = Int32.MinValue;
      private bool _firstRefreshOfProps = true;
      protected int _firstTableTabOrder; //tab order of first child of table

      private MgControlBase _frameFormCtrl;
      private List<GuiMgControl> _guiTableChildren;
      private int _horizontalFactor = -1;
      protected int LastDvRowCreated = -1;
      Dictionary<MgControlBase, Dictionary<string, object>> designerInfoDictionary = null;

      protected bool _inRefreshDisp;
      protected bool _inRestore;

      protected Hashtable _instatiatedMenus; // menus that belong to this form according to menu style(pulldown \ context)

      protected int _lastRowSent = -1; //last row sent ro gui thread
      protected MgTreeBase _mgTree;
      private int _prevFontId = -1;

      private int _prevSelIndex = -1;
      protected int _prevTopIndex = -1;
      protected PropTable _propTab;

      protected int _rowsInPage = 1; // TODO: was Integer.MIN_VALUE;
      private bool _shouldCreateToolbar; //if to create toolbar
      private bool _shouldShowPullDownMenu; //if show menu 
      protected MgStatusBar _statusBar;
      protected internal MgControlBase _subFormCtrl;
      private List<MgControlBase> _tableChildren; //Can't use List<T>
      private List<MgControlBase> _tableColumns;
      protected internal MgControlBase _tableMgControl;
#if !PocketPC
      public DVControlManager DVControlManager { get; internal set; }
#endif
      internal bool InInitForm { get; set; }

      protected int _tableItemsCount; // number of rows in the table
      protected bool _tableRefreshed;
      protected internal TaskBase _task; // the task to which the current form belongs
      protected internal bool _topIndexUpdated; //true if a top index was updated
      protected internal bool _transferingData; //rows are requested for a GUI thread
      protected internal MgControlBase _treeMgControl;
      private String _userStateId = "";
      private int _verticalFactor = -1;

      protected internal MgControlBase _wideControl; //for the wide form & parent form : save the MgControl of the wide
      private MgControlBase _wideParentControl; //for the wide form : save the MgControl of the parent

      //Form representing the internal help window.Here the internal form object is kept on the form object itself.
      //Property 'IsHelpWindow' is used to differentiate between the help form and non help form.
      private MgFormBase _internalHelpWindow;

      private const int _internalHelpFormNoBorder = 3;
      const string delimiter = ", ";

      public bool IsHelpWindow { get; private set; } // the form is help form\window or not.

      public ControlTable CtrlTab { get; private set; }

      public bool FormRefreshed { get; set; } //prevents refresh of DisplayList for the corresponding controls and currRecCompute(RC)
                                              //until the form is refreshed in the first time 
      public bool FormRefreshedOnceAfterFetchingDataView { get; set; }

      public bool RefreshRepeatableAllowed { get; private set; } //true, if we can paint repeatable controls

      public MgArrayList Rows { get; private set; }

      public ControlsData ScreenControlsData { get; set; } = new ControlsData();

      public int ModalFormsCount { get; private set; } //number of modal child forms of the frame window

      // Returns true if an error generated from managed side
      public bool ErrorOccured { get; set; }

      public int FormIsn { get; set; }


      public Dictionary<MgControlBase, Dictionary<string, object>> DesignerInfoDictionary
      {
         get { return designerInfoDictionary; }
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      public MgFormBase()
      {
         _propTab = new PropTable(this);
         CtrlTab = new ControlTable();
         Rows = new MgArrayList();
         _toolbarGroupsCount = new Hashtable();
         _controlsInheritingContext = new List<MgControlBase>();
         _instatiatedMenus = new Hashtable();
         _concreteWindowType = 0;
         ParentForm = null;
         IsHelpWindow = false;
      }

      public virtual void RefreshTableUI()
      {

      }
      internal String Name { get; set; }

      public bool Opened { get; set; }

      private int displayLine;
      public int DisplayLine {
         get
         {
            return displayLine;
         }
         set
         {
            displayLine = value;
         }
      }

      public bool IsFrameSet { get; set; }
      public bool isLegalForm { get; set; }

      internal int PBImagesNumber { get; set; }

      public int LastClickedMenuUid { get; set; } //on topmost form save the last MenuId that was press. 

      internal bool ignoreFirstRefreshTable { get; set; }

      internal bool ShouldShowPullDownMenu
      {
         get
         {
            return _shouldShowPullDownMenu;
         }
      }

      protected List<MgControlBase> TableChildren
      {
         get
         {
            if (_tableChildren == null)
               buildTableChildren();
            return _tableChildren;
         }

         set
         {
            _tableChildren = value;
         }
      }

      /// <summary>
      /// Returns the internal window form.
      /// </summary>
      /// <returns></returns>
      public MgFormBase GetInternalHelpWindowForm()
      {
         return _internalHelpWindow;
      }

      public String UserStateId
      {
         get
         {
            return _userStateId;
         }
      }

      public bool AllowedSubformRecompute
      {
         get
         {
            return _allowedSubformRecompute;
         }
         set
         {
            _allowedSubformRecompute = value;
         }
      }

      public bool ShouldCreateToolbar
      {
         get
         {
            return _shouldCreateToolbar;
         }
      }

      /// <summary>
      ///   returns true for mdi frame or sdi window
      /// </summary>
      public bool IsMDIOrSDIFrame
      {
         get
         {
            return isMDIOrSDI(ConcreteWindowType);
         }
      }

      public bool IsMDIFrame
      {
         get
         {
            return (ConcreteWindowType == WindowType.MdiFrame);
         }
      }

      public bool IsSDIFrame
      {
         get
         {
            return (ConcreteWindowType == WindowType.Sdi);
         }
      }

      public bool IsChildWindow
      {
         get
         {
            return (ConcreteWindowType == WindowType.ChildWindow);
         }
      }

      /// <summary>
      /// valid window types for WindowList.
      /// </summary>
      public bool IsValidWindowTypeForWidowList
      {
         get
         {
            return (IsMDIChild || IsSDIFrame || ConcreteWindowType == WindowType.Floating || ConcreteWindowType == WindowType.Tool);
         }
      }

      public WindowType ConcreteWindowType
      {
         get
         {
            if (_concreteWindowType == 0)
            {
               var windowTypeValue = (WindowType)(getProp(PropInterface.PROP_TYPE_WINDOW_TYPE).getValueInt());

               if (windowTypeValue == WindowType.ChildWindow)
               {
                  //if there is no parent for a child window, it should behave as Default type.
                  if (ParentForm == null)
                     windowTypeValue = WindowType.Default;
                  else if (ParentForm.IsMDIFrame)
                     windowTypeValue = WindowType.MdiChild;
               }

               if (windowTypeValue == WindowType.Default || windowTypeValue == WindowType.FitToMdi)
                  windowTypeValue = GetConcreteWindowTypeForMDIChild(windowTypeValue == WindowType.FitToMdi);

               // #989854 & 728875. If FrameForm is SDI and called is FIT_TO_MDI then make it floating.
               RuntimeContextBase rteCtx = Manager.GetCurrentRuntimeContext();
               if (windowTypeValue == WindowType.FitToMdi && rteCtx.FrameForm != null && rteCtx.FrameForm.IsSDIFrame)
                  windowTypeValue = WindowType.Floating;

               //Topic : 60 (WEBCLNT version 1.5 for WIN) Modal window - change of behavior and QCR #802495
               //Currently, any window that is called from a Modal window is opened as Modal window, regardless of it's type. 
               //This will be changed in RC only, and the called window type will be as defined in the called form 
               //(even if called from Modal window). This will solve problems when using the non-interactive RC tasks, 
               //in which the called ownerTask must be Modal.

               //QCR #937102 - if non interactive ownerTask opens mdi child it becomes modal.
               //Ref QCR #802495 - Direct children of non interactive tasks  will be opened using ShowDialog(modal behavior) to prevent events on their parents
               //Since mdi child can not implement modal behavior it is replaced with modal on the fly.
               if (isMDIChild(windowTypeValue))
               {
                  if (ShouldBehaveAsModal())
                     windowTypeValue = WindowType.Modal;
               }

               _concreteWindowType = windowTypeValue;
            }
            return _concreteWindowType;
         }
         set
         {
            _concreteWindowType = value;
         }
      }

      /// <summary>
      ///   returns true for mdi child and fit to mdi
      /// </summary>
      public bool IsMDIChild
      {
         get
         {
            return isMDIChild(ConcreteWindowType);
         }
      }

      /// <summary> Returns true for a FitToMdi window. </summary>
      public bool IsFitToMdi
      {
         get
         {
            return (ConcreteWindowType == WindowType.FitToMdi);
         }
      }

      /// <summary>
      /// Returns true for Modal.
      /// </summary>
      public bool IsModal
      {
         get
         {
            return (ConcreteWindowType == WindowType.Modal);
         }
      }

      /// <summary>
      /// Returns true for Floating, Tool or Modal.
      /// </summary>
      public bool IsFloatingToolOrModal
      {
         get
         {
            return (ConcreteWindowType == WindowType.Floating ||
                    ConcreteWindowType == WindowType.Tool ||
                    ConcreteWindowType == WindowType.Modal);
         }
      }

      /// <summary>the form of the task that called the current task (note: NOT triggering task).
      /// If this form (form of the task that called the current task) is not visible like in case 
      /// for non-interactive task with OpenWindow=No, the ParentForm will be the immediate previous
      /// visible form.
      /// This means that ParentForm's task and Task's ParentTask can be different.
      /// </summary>
      public MgFormBase ParentForm { get; set; }

      /// <summary>
      /// When closing the current form, we need to activate another form. Actually framework does it, but it activates 
      /// a different form which is not appropriate for Magic --- refer Defects #71348 and #129179.
      /// Until Defect #129937, ParentForm was activated on closing the current form. But, it is wrong to do so.
      /// Why? --- Refer detailed comment in "Fix Description" of Defect #129937.
      /// So, a separate FormToBoActivatedOnClosingCurrentForm is maintained.
      /// </summary>
      public MgFormBase FormToBoActivatedOnClosingCurrentForm { get; set; }

      /// <summary> update the count of modal child forms on the frame window </summary>
      /// <param name = "mgFormBase">current modal form</param>
      /// <param name = "increase"></param>
      public void UpdateModalFormsCount(MgFormBase mgFormBase, bool increase)
      {
         Debug.Assert(isMDIOrSDI(ConcreteWindowType));
         if (increase)
            ModalFormsCount++;
         else
            ModalFormsCount--;

         Events.SetModal(mgFormBase, increase);
      }

      #region PropParentInterface Members

      /// <summary>
      /// 
      /// </summary>
      /// <param name="expId"></param>
      /// <param name="resType"></param>
      /// <param name="length"></param>
      /// <param name="contentTypeUnicode"></param>
      /// <param name="resCellType"></param>
      /// <param name="alwaysEvaluate"></param>
      /// <param name="wasEvaluated"></param>
      /// <returns></returns>
      public String EvaluateExpression(int expId, StorageAttribute resType, int length, bool contentTypeUnicode,
                                       StorageAttribute resCellType, bool alwaysEvaluate, out bool wasEvaluated)
      {
         return _task.EvaluateExpression(expId, resType, length, contentTypeUnicode, resCellType, true, out wasEvaluated);
      }


      /// <summary>
      ///   This method enables a specific action on all the menu objects of the form and its children's
      ///   menus.
      /// </summary>
      /// <param name = "action">action whose state has changed</param>
      /// <param name = "enable">new state of the action</param>
      public void EnableActionMenu(int action, bool enable)
      {
         MgMenu mgMenu = null;
         MenuStyle menuStyle;
         ICollection menuStyles = _instatiatedMenus.Keys;
         IEnumerator menuStylesEnumerator = menuStyles.GetEnumerator();

         while (menuStylesEnumerator.MoveNext())
         {
            menuStyle = (MenuStyle)menuStylesEnumerator.Current;
            mgMenu = (MgMenu)_instatiatedMenus[menuStyle];
            mgMenu.enableInternalEvent(this, action, enable, null);
         }
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
      ///   return a property of the form by its Id
      /// </summary>
      /// <param name = "propId">the Id of the property</param>
      public Property getProp(int propId)
      {
         Property prop = null;
         Debug.Assert(propId != PropInterface.PROP_TYPE_STARTUP_POSITION, "Please use GetStartupPosition()");

         if (_propTab != null)
         {
            prop = _propTab.getPropById(propId);

            // if the property doesn't exist then create a new property and give it the default value
            // in addition add this property to the properties table
            if (prop == null)
            {
               prop = PropDefaults.getDefaultProp(propId, GuiConstants.PARENT_TYPE_FORM, this);
               if (prop != null)
                  _propTab.addProp(prop, false);
            }
         }
         return prop;
      }

      /// <summary>
      /// get a property of the form that already was computed by its Id
      /// This method does not create a property if it isn't exist.
      /// May we need to create a property and use its default value.
      /// In Phase 2 in RefreshProperties method we'll create and compute properties, 
      /// perhaps we need create all properties not only those that are different from default.
      /// </summary>
      /// <param name = "propId">the Id of the property</param>
      public Property GetComputedProperty(int propId)
      {
         Property prop = null;

         if (_propTab != null)
            prop = _propTab.getPropById(propId);
         return prop;
      }

      /// <summary>
      /// checks if property has expression
      /// </summary>
      /// <param name="propId"></param>
      /// <returns></returns>
      public bool PropertyHasExpression(int propId)
      {
         Property prop = _propTab.getPropById(propId);
         return (prop != null && prop.isExpression());
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      public int getCompIdx()
      {
         return (getTask() == null
                    ? 0
                    : getTask().getCompIdx());
      }

      /// <summary>
      ///   return true if this is first refresh
      /// </summary>
      /// <returns></returns>
      public bool IsFirstRefreshOfProps()
      {
         return _firstRefreshOfProps;
      }

      /// <summary>
      ///   for PropParentInterface
      /// </summary>
      public MgFormBase getForm()
      {
         return this;
      }

      #endregion

      /// <summary>
      ///   parse the form structure
      /// </summary>
      /// <param name = "taskRef">reference to the ownerTask of this form</param>
      public virtual void fillData(TaskBase taskRef)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (_task == null && taskRef != null)
            _task = taskRef;
         while (initInnerObjects(parser.getNextTag()))
         {
         }

         // inherit from the system menus if no menus is defined for the form
         inheritSystemMenus();
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name="foundTagName"></param>
      /// <returns></returns>
      protected virtual bool initInnerObjects(String foundTagName)
      {
         if (foundTagName == null)
            return false;

         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (MgFormBase.IsFormTag(foundTagName))
            fillName(foundTagName);
         else if (foundTagName.Equals(XMLConstants.MG_TAG_PROP))
            _propTab.fillData(this, 'F'); // the parent Object of the Property is the Form
         else if (foundTagName.Equals(XMLConstants.MG_TAG_CONTROL))
         {
            CtrlTab.fillData(this);
            initContextPropForControls();
            if (!HasTable())
               RefreshRepeatableAllowed = true;
         }
         else if (MgFormBase.IsEndFormTag(foundTagName))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
            return false;
         return true;
      }

      /// <summary>
      ///   This method performs a loop on all the forms controls and sets the context menu property 
      ///   by invoking the get method.
      ///   The get is done in order to create a context menu property for all the controls on the form. 
      ///   This way any control which does not have this property set, will still have a property which 
      ///   we will be able to refresh in order to set on it the forms context menu.
      /// </summary>
      public void initContextPropForControls()
      {
         int i = 0;
         MgControlBase ctrl;
         for (i = 0; i < CtrlTab.getSize(); i++)
         {
            ctrl = CtrlTab.getCtrl(i);
            ctrl.getProp(PropInterface.PROP_TYPE_CONTEXT_MENU);
         }
      }

      /// <summary>
      ///   initialization.
      /// </summary>
      internal void init()
      {
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            MgControlBase ctrl = CtrlTab.getCtrl(i);
            // initializes the subform controls
            ctrl.Init();
         }

         // build the controls tabbing order
         buildTabbingOrder();

         InitStatusBar(true);

         // build lists of linked controls on their parents
         buildLinkedControlsLists();

         // build lists of sibling controls
         buildSiblingList();

         // build list of columns and non columns
         buildTableColumnsList();

         createForm();

         _task.setKeyboardMappingState(Constants.ACT_STT_TBL_SCREEN_MODE, isScreenMode());
         _task.setKeyboardMappingState(Constants.ACT_STT_TBL_LEFT_TO_RIGHT, !getProp(PropInterface.PROP_TYPE_HEBREW).getValueBoolean());

         // if we tried to write to the status bar before the task was initialized, then now is the time to display the message!
         if (_task.getSaveStatusText() != null)
            Manager.WriteToMessagePane(_task, _task.getSaveStatusText(), false);

#if !PocketPC
         HandleRuntimeDesignerData();
#endif
      }

      /// <summary>
      /// handle data from runtime designer
      /// </summary>
      private void HandleRuntimeDesignerData()
      {
         List<ControlItem> designerInfoList = RuntimeDesignerSerializer.DeSerializeFromFile(GetControlsPersistencyFileName());

         if (designerInfoList != null)
         {
            designerInfoDictionary = BuildDesignerInfoDictionary(designerInfoList);
            SetDesignerInfoOnMgControls();
            SetDesignerInfoOnControls();

            UpdateHiddenControlsList();

            // if needed, update the tabbing order
            if (isAutomaticTabbingOrder() && CoordinatesChangedByRuntimeDesigner())
            {
               RebuildTabIndex();
               UpdateTabbingOrderFromRuntimeDesigner();
            }
         }
      }
      /// <summary>
      /// Did the runtime designer change any coordinates of any control?
      /// </summary>
      /// <returns></returns>
      bool CoordinatesChangedByRuntimeDesigner()
      {
         foreach (Dictionary<string, object> dict in designerInfoDictionary.Values)
         {
            foreach (var item in dict.Keys)
            {
               if (item.Equals(Constants.WinPropLeft) || item.Equals(Constants.WinPropTop))
                  return true;
            }
         }

         return false;
      }

      /// <summary>Initialize a standard status bar with the message pane. </summary>
      protected virtual bool InitStatusBar(bool createAllPanes)
      {
         bool shouldCreateStatusbar = false;

#if !PocketPC

         //initialize a standard status bar if the form is an SDI
         shouldCreateStatusbar = (IsMDIOrSDIFrame && !isSubForm() && getProp(PropInterface.PROP_TYPE_DISPLAY_STATUS_BAR).getValueBoolean());

         if (shouldCreateStatusbar)
         {
            var num = new NUM_TYPE();

            //Create status bar object.
            _statusBar = new MgStatusBar(_task);

            //Add to controls table.
            CtrlTab.addControl(_statusBar);

            //Get index of status bar object.
            int statusBarIdx = getControlIdx(_statusBar);

            //Message pane is common to RC and RTE, add it in base class itself.
            _statusBar.AddPane(MgControlType.CTRL_TYPE_SB_LABEL, Constants.SB_MSG_PANE_LAYER, Constants.SB_MSG_PANE_WIDTH, false);

            if (createAllPanes)
            {
               //Add Taskmode pane
               _statusBar.AddPane(MgControlType.CTRL_TYPE_SB_LABEL, GuiConstants.SB_TASKMODE_PANE_LAYER, GuiConstants.SB_TASKMODE_PANE_WIDTH, true);

               //Add Zoom pane
               _statusBar.AddPane(MgControlType.CTRL_TYPE_SB_LABEL, GuiConstants.SB_ZOOMOPTION_PANE_LAYER, GuiConstants.SB_ZOOMOPTION_PANE_WIDTH, true);

               //Add wide pane
               _statusBar.AddPane(MgControlType.CTRL_TYPE_SB_LABEL, GuiConstants.SB_WIDEMODE_PANE_LAYER, GuiConstants.SB_WIDEMODE_PANE_WIDTH, true);

               //Add insert pane
               _statusBar.AddPane(MgControlType.CTRL_TYPE_SB_LABEL, GuiConstants.SB_INSMODE_PANE_LAYER, GuiConstants.SB_INSMODE_PANE_WIDTH, true);
            }
         }
#endif

         return shouldCreateStatusbar;
      }

      /// <summary>
      ///   Fill name member of the class
      /// </summary>
      /// <returns> end Index of the<form ...> subtag</returns>
      internal void fillName(string formTag)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         List<String> tokensVector;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(formTag) + formTag.Length);

            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            String attribute, valueStr;
            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               attribute = (tokensVector[j]);
               valueStr = (tokensVector[j + 1]);

               switch (attribute)
               {
                  case XMLConstants.MG_ATTR_NAME:
                     Name = XmlParser.unescape(valueStr);
                     if (Events.ShouldLog(Logger.LogLevels.Gui))
                        Events.WriteGuiToLog(String.Format("Parsing form \"{0}\"", Name));
                     break;

                  case XMLConstants.MG_ATTR_IS_FRAMESET:
                     IsFrameSet = XmlParser.getBoolean(valueStr);
                     break;

                  case XMLConstants.MG_ATTR_IS_LIGAL_RC_FORM:
                     isLegalForm = XmlParser.getBoolean(valueStr);
                     break;

                  case XMLConstants.MG_ATTR_PB_IMAGES_NUMBER:
                     PBImagesNumber = XmlParser.getInt(valueStr);
                     break;
                  case XMLConstants.MG_ATTR_FORM_ISN:
                     FormIsn = XmlParser.getInt(valueStr);
                     break;

                  case XMLConstants.MG_ATTR_USERSTATE_ID:
                     _userStateId = XmlParser.unescape(valueStr);
                     break;

                  default:
                     Events.WriteExceptionToLog(string.Format("Unhandled attribute '{0}'.", attribute));
                     break;
               }
            }
            parser.setCurrIndex(++endContext); // to delete ">" too
            return;
         }
         Events.WriteExceptionToLog("in Form.FillName() out of bounds");
      }

      /// <summary>
      ///   for each control build the list of all the controls that are linked to it
      /// </summary>
      private void buildLinkedControlsLists()
      {
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            MgControlBase ctrl = CtrlTab.getCtrl(i);
            MgControlBase parent = ctrl.getLinkedParent(false);
            if (parent != null)
               parent.linkCtrl(ctrl);
         }

         if (_statusBar != null)
            _statusBar.sortLinkedControls();
      }

      /// <summary>
      ///   build list of all controls that are in the same data of the parameter mgConatol relevant only for the
      ///   RadioButton
      /// </summary>
      /// <returns></returns>
      private void buildSiblingList()
      {
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            MgControlBase control = CtrlTab.getCtrl(i);
            if (control.Type == MgControlType.CTRL_TYPE_RADIO)
            {
               if (control.getField() != null)
               {
                  var sibling = new List<MgControlBase>();
                  int myIdField = control.getField().getId();

                  for (int j = 0; j < CtrlTab.getSize(); j++)
                  {
                     MgControlBase currCtrl = CtrlTab.getCtrl(j);
                     if (currCtrl.Type == MgControlType.CTRL_TYPE_RADIO)
                     {
                        FieldDef field = currCtrl.getField();
                        if (control != currCtrl && field != null)
                        {
                           if (myIdField == field.getId())
                              sibling.Add(currCtrl);
                        }
                     }
                  }

                  if (sibling.Count > 0)
                     control.setSiblingVec(sibling);
               }
            }
         }
      }

      /// <summary>
      /// </summary>
      internal void arrangeZorder()
      {
         //create array list from the _ctrlTab
         var ctrlArrayList = new List<MgControlBase>();

         for (int i = 0; i < CtrlTab.getSize(); i++)
            ctrlArrayList.Add(CtrlTab.getCtrl(i));

         arrangeZorderControls(ctrlArrayList);
      }

      /// <summary>
      ///   arrange zorder for controls. recursive function.
      /// </summary>
      /// <param name = "controls"></param>
      private void arrangeZorderControls(List<MgControlBase> controls)
      {
         MgControlBase lowerControl = null;

         for (int i = 0; i < controls.Count; i++)
         {
            MgControlBase ctrl = controls[i];
            if (lowerControl != null)
            {
               // if there is no parent need to move above the lower control
               if (ctrl.getParent() == lowerControl.getParent())
               {
                  Commands.addAsync(CommandType.MOVE_ABOVE, ctrl);
                  lowerControl = ctrl;
               }
            }
            else
               lowerControl = ctrl;

            if (ctrl.getLinkedControls().Count > 0)
               arrangeZorderControls(ctrl.getLinkedControls());
         }
      }

      /// <summary>
      ///   build list of all columns controls
      /// </summary>
      private void buildTableColumnsList()
      {
         if (_tableMgControl != null)
         {
            _tableColumns = new List<MgControlBase>();

            for (int i = 0; i < CtrlTab.getSize(); i++)
            {
               MgControlBase control = CtrlTab.getCtrl(i);
               if (control.Type == MgControlType.CTRL_TYPE_COLUMN)
                  _tableColumns.Add(control);
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public virtual void InitFormBeforeRefreshDisplay()
      {
      }

      /// <summary>
      /// Checks if an internal Fit to MDI form needs to be created to hold the Main Program controls
      /// </summary>
      /// <returns></returns>
      private bool ShouldCreateInternalFormForMDI()
      {
         return (IsMDIFrame && (GetControlsCountExcludingStatusBar() > 0));
      }

      /// <summary>
      ///   Creates the window defined by this form and its child controls
      /// </summary>
      protected virtual void createForm()
      {
         RuntimeContextBase runtimeContext = Manager.GetCurrentRuntimeContext();
         if (IsMDIFrame)
            runtimeContext.FrameForm = this;

         if (!isSubForm())
         {
            Object parent = FindParent(runtimeContext);

            JSBridge.Instance.OpenForm(getProp(PropInterface.PROP_TYPE_FORM_NAME).getValue());

            // parent can be null.
            Commands.addAsync(CommandType.CREATE_FORM, parent, this, ConcreteWindowType, Name, false, ShouldCreateInternalFormForMDI(), _task.IsBlockingBatch);
         }
         else
         {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string inputControls = serializer.Serialize(GetListOfInputControls());

            JSBridge.Instance.OpenSubForm(this.getSubFormCtrl().getName(), ParentForm.getTask().getTaskTag(), getProp(PropInterface.PROP_TYPE_FORM_NAME).getValue(), _task.getTaskTag(), inputControls);
         }

         // create the controls
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            MgControlBase ctrl = CtrlTab.getCtrl(i);
            ctrl.createControl(false);
         }

         orderSplitterContainerChildren();
         InitMenuInfo();

         // default create one row in table control. DON'T set items count during form open.
         // firstTableRefresh() will create the actual rows, and refresh the table.
         if (HasTable())
            InitTableControl();

         Commands.beginInvoke();
      }

      protected virtual Object FindParent(RuntimeContextBase runtimeContext)
      {
         Object parent = GetParentForm();

         if (IsMDIChild || parent == null)
            parent = runtimeContext.FrameForm;
         return parent;
      }

      /// <summary>
      ///   order the splitter container children. The child that is docked to fill is added first, then
      ///   the splitter control and finally the child that is docked either to the top or to the left
      /// </summary>
      private void orderSplitterContainerChildren()
      {
         MgControlBase ctrl = null;
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            ctrl = CtrlTab.getCtrl(i);
            if (ctrl.isFrameSet())
               Commands.addAsync(CommandType.ORDER_MG_SPLITTER_CONTAINER_CHILDREN, ctrl);
         }
      }

      /// <summary>
      ///   save information about the menu visible
      ///   save information about the toolbar visible
      /// </summary>
      /// <returns></returns>
      public void InitMenuInfo()
      {
         _shouldShowPullDownMenu = false;
         _shouldCreateToolbar = false;
         /* in MenuEntry, we need to know of the menu\toolbar need to be display*/
         //the show\hide of the menu\toolbar is compute and not recompute.

         // if the parent of the TaskBase is Modal\floating\tool, then his child will be also the same window type.
         if (IsMDIOrSDIFrame)
         {
            // check if we need to display the menu
            if (getProp(PropInterface.PROP_TYPE_DISPLAY_MENU) != null)
               _shouldShowPullDownMenu = getProp(PropInterface.PROP_TYPE_DISPLAY_MENU).getValueBoolean();

            // check if we need to display the toolbar
            if (getProp(PropInterface.PROP_TYPE_DISPLAY_TOOLBAR) != null)
               _shouldCreateToolbar = getProp(PropInterface.PROP_TYPE_DISPLAY_TOOLBAR).getValueBoolean();
         }
      }

      /// <summary>
      /// returns true for mdi child and fit to mdi 
      /// </summary>
      /// <param name="windowType"></param>
      /// <returns></returns>
      internal static bool isMDIChild(WindowType windowType)
      {
         return (windowType == WindowType.MdiChild || windowType == WindowType.FitToMdi);
      }

      /// <summary>
      /// returns true for mdi or sdi
      /// </summary>
      /// <param name="windowType"></param>
      /// <returns></returns>
      internal static bool isMDIOrSDI(WindowType windowType)
      {
         return (windowType == WindowType.Sdi || windowType == WindowType.MdiFrame);
      }

      /// <summary>
      ///   check if the MgMenu is used by this form as a context menu
      /// </summary>
      /// <param name = "mgMenu"></param>
      /// <returns></returns>
      public bool isUsedAsContextMenu(MgMenu mgMenu)
      {
         bool isUsed = false;

         //check if this menu is used in the context menu form
         MgMenu mgMenuContext = getMgMenu(MenuStyle.MENU_STYLE_CONTEXT);
         if (mgMenuContext == mgMenu)
            isUsed = true;

         //check if this menu is used in one of the controls on this form
         if (!isUsed)
         {
            MgControlBase ctrl = null;
            for (int i = 0; i < CtrlTab.getSize() && !isUsed; i++)
            {
               ctrl = CtrlTab.getCtrl(i);
               if (ctrl.getContextMenu(false) == mgMenu)
                  isUsed = true;
            }
         }
         return isUsed;
      }

      /// <summary>
      ///   check if the MgMenu is used by this form as a pulldown menu
      /// </summary>
      /// <param name = "mgMenu"></param>
      /// <returns></returns>
      public bool isUsedAsPulldownMenu(MgMenu mgMenu)
      {
         return getPulldownMenu() == mgMenu;
      }

      /// <summary>
      /// </summary>
      /// <returns> the attached pulldown menu</returns>
      public MgMenu getPulldownMenu()
      {
         return (MgMenu)_instatiatedMenus[MenuStyle.MENU_STYLE_PULLDOWN];
      }

      /// <summary>
      ///   return true if form has automatic tabbing order
      /// </summary>
      /// <returns></returns>
      public bool isAutomaticTabbingOrder()
      {
         return (TabbingOrderType)getProp(PropInterface.PROP_TYPE_TABBING_ORDER).getValueInt() ==
                TabbingOrderType.Automatically;
      }

      /// <summary>
      ///   true if we can refresh repeatable controls
      ///   QCR #434649, we prevent painting table's control untill shell is opened and we know how many rows to paint
      /// </summary>
      /// <returns></returns>
      public bool isRefreshRepeatableAllowed()
      {
         return RefreshRepeatableAllowed;
      }

      /// <summary>
      ///   returns true if the form is screen mode
      /// </summary>
      public bool isScreenMode()
      {
         return !isLineMode();
      }

      /// <summary>
      ///   returns true if the form is line mode
      ///   if we have a tree or a table on the form
      /// </summary>
      public bool isLineMode()
      {
         return (getMainControl() != null);
      }

      /// <summary>
      ///   return true if task has table or tree
      /// </summary>
      /// <returns></returns>
      protected internal bool hasTableOrTree()
      {
         return (_tableMgControl != null || _treeMgControl != null);
      }

      /// <summary>
      ///   return true if form should be opened as modal window
      /// </summary>
      /// <returns></returns>
      protected internal bool isDialog()
      {
#if PocketPC
         // On Mobile, mixing modal and modeless forms may result in bad behavior - forms may be hidden/closed
         // when they shouldn't. Workaround: always make the forms modal. Since the forms always cover the
         // entire desktop area, and there is no way to get to the forms behind the current form, this change 
         // shouldn't cause problems.
         return true;
#else
         bool isDialog = false;

         //Check for 'isDialog' only for non help form.
         if (!IsHelpWindow)
         {
            WindowType windowType = ConcreteWindowType;
            if (windowType == WindowType.Modal || windowType == WindowType.ApplicationModal)
               isDialog = true;
            else if (ShouldBehaveAsModal())
               isDialog = true;

         }
         return isDialog;
#endif
      }


      /// <summary>
      /// Should this form behave as Modal although its WindowType != Modal? 
      /// </summary>
      /// <returns></returns>
      protected internal virtual bool ShouldBehaveAsModal()
      {
         return false;
      }

      /// <summary>
      ///   returns true if this form is subform
      /// </summary>
      /// <returns> boolean</returns>
      public bool isSubForm()
      {
         return (_subFormCtrl != null);
      }

      /// <summary>
      ///   return true if task has table
      /// </summary>
      /// <returns></returns>
      public bool HasTable()
      {
         return (_tableMgControl != null);
      }

      /// <returns> if there is Tab control on the form</returns>
      public bool hasTabControl()
      {
         bool hasTabCtrl = false;
         MgControlBase ctrl;

         for (int i = 0; i < CtrlTab.getSize() && !hasTabCtrl; i++)
         {
            ctrl = CtrlTab.getCtrl(i);
            if (ctrl.isTabControl())
               hasTabCtrl = true;
         }

         return hasTabCtrl;
      }

      /// <summary>
      ///   return true if task has tree
      /// </summary>
      /// <returns></returns>
      public bool hasTree()
      {
         return (_treeMgControl != null);
      }

      /// <summary>
      ///   return the menu of the form according to style
      /// </summary>
      /// <returns></returns>
      public MgMenu getMgMenu(MenuStyle style)
      {
         return (MgMenu)(_instatiatedMenus[style]);
      }

      /// <summary>
      ///   return the topmost mgform of the subform
      /// </summary>
      /// <returns></returns>
      public MgFormBase getTopMostForm()
      {
         MgFormBase topMostForm = this;

         //The form of the sub form control
         while (topMostForm.isSubForm() && topMostForm.ParentForm != null)
            topMostForm = topMostForm.ParentForm;

         return topMostForm;
      }

      /// <summary>
      ///   return the topmost frame form for menu refresh.
      /// </summary>
      /// <returns></returns>
      public virtual MgFormBase getTopMostFrameFormForMenuRefresh()
      {
         return getTopMostFrameForm();
      }

      /// <summary>
      ///   return the topmost mgform of the subform
      /// </summary>
      /// <returns></returns>
      public MgFormBase getTopMostFrameForm()
      {
         MgFormBase topMostForm = this;

         while (topMostForm != null && !topMostForm.IsMDIOrSDIFrame)
         {
            topMostForm = topMostForm.ParentForm != null
                          ? topMostForm.ParentForm
                          : null;
         }

         return topMostForm;
      }

      /// <returns> the subFormCtrl
      /// </returns>
      public MgControlBase getSubFormCtrl()
      {
         return _subFormCtrl;
      }

      /// <summary>
      ///   return object that is used for form mapping in control's map
      /// </summary>
      /// <returns></returns>
      public Object getMapObject()
      {
         return _subFormCtrl ?? (Object)this;

      }
      /// <summary>Returns the requested control</summary>
      /// <param name = "ctrlIdx">the index of the requested control</param>
      public MgControlBase getCtrl(int ctrlIdx)
      {
         return CtrlTab.getCtrl(ctrlIdx);
      }

      /// <summary>
      ///   Return the control with a given name
      /// </summary>
      /// <param name = "ctrlName">the of the requested control</param>
      /// <returns> a reference to the control with the given name or null if does not exist</returns>
      public MgControlBase GetCtrl(String ctrlName)
      {
         return CtrlTab.getCtrl(ctrlName);
      }

      /// <summary>
      /// Get the items list of choice controls.
      /// </summary>
      /// <param name="mgControl"></param>
      /// <returns></returns>
      public string GetChoiceControlItemList(MgControlBase mgControl)
      {
         Debug.Assert(mgControl.isChoiceControl());

         ArrayList items = new ArrayList();
         Field fld = (Field)mgControl.getField();
         StorageAttribute storageAttribute = fld.getCellsType();

         if (storageAttribute != StorageAttribute.DATE && storageAttribute != StorageAttribute.TIME)
         {
            if (mgControl.isRadio())
            {
               foreach (MgControlBase radioControl in ((Field)fld).GetRadioCtrls())
                  items.AddRange(radioControl.GetItemsRange());
            }
            else
               items.AddRange(mgControl.GetItemsRange());

            return ConvertArrayListToString(items, storageAttribute == StorageAttribute.NUMERIC);
         }

         return string.Empty;
      }

      /// <summary>
      /// Get the display list of choice controls.
      /// </summary>
      /// <param name="mgControl"></param>
      /// <returns></returns>
      public string GetChoiceControlDisplayList(MgControlBase mgControl)
      {
         Debug.Assert(mgControl.isChoiceControl());

         ArrayList items = new ArrayList();
         Field fld = (Field)mgControl.getField();
         StorageAttribute storageAttribute = fld.getCellsType();

         if (storageAttribute != StorageAttribute.DATE && storageAttribute != StorageAttribute.TIME)
         {
            if (mgControl.isRadio())
            {
               foreach (MgControlBase radioControl in ((Field)fld).GetRadioCtrls())
                  items.AddRange(radioControl.GetDisplayRange());
            }
            else
               items.AddRange(mgControl.GetDisplayRange());

            return ConvertArrayListToString(items, false);
         }

         return string.Empty;
      }

      /// <summary>
      /// Returns the comma concatenated string from array list.
      /// </summary>
      /// <param name="items"></param>
      /// <param name="isItemNumType"></param>
      /// <returns></returns>
      private string ConvertArrayListToString(ArrayList items, bool isItemNumType)
      {
         var from = new[] { "\\", "-", "," };
         var to = new[] { "\\\\", "\\-", "\\," };
         var stringBuilder = new StringBuilder();
         NUM_TYPE numType;

         for (int index = 0; index < items.Count; index++)
         {
            if (isItemNumType)
            {
               numType = new NUM_TYPE((string)items[index]);
               items[index] = numType.to_double().ToString();
            }

            stringBuilder.Append(StrUtil.searchAndReplace((string)items[index], from, to));

            if (index < items.Count - 1)
               stringBuilder.Append(delimiter);
         }

         return stringBuilder.ToString();
      }

      /// <returns> the frameFormCtrl</returns>
      public MgControlBase getFrameFormCtrl()
      {
         return _frameFormCtrl;
      }

      /// <summary>
      ///   return control's column
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <returns></returns>
      public MgControlBase getControlColumn(MgControlBase ctrl)
      {
         MgControlBase column = null;
         if ((ctrl.IsRepeatable || (ctrl.IsTableHeaderChild)) && _tableColumns != null)
            column = _tableColumns[ctrl.getLayer() - 1];
         return column;
      }

      /// <returns> the container control</returns>
      public MgControlBase getContainerCtrl()
      {
         return _containerCtrl;
      }

      /// <summary>
      ///   get all column on the form
      /// </summary>
      internal List<MgControlBase> getColumnControls()
      {
         List<MgControlBase> columnControlsList = null;

         if (CtrlTab != null)
         {
            for (int i = 0; i < CtrlTab.getSize(); i++)
            {
               MgControlBase ctrl = CtrlTab.getCtrl(i);
               if (ctrl.Type == MgControlType.CTRL_TYPE_COLUMN)
               {
                  if (columnControlsList == null)
                     columnControlsList = new List<MgControlBase>();
                  columnControlsList.Add(ctrl);
               }
            }
         }
         return columnControlsList;
      }

      /// <summary>
      ///   return the task of the form
      /// </summary>
      public TaskBase getTask()
      {
         return _task;
      }

      /// <summary> /// This function returns the index of the help object attached to the form/// </summary>
      /// <returns>Help index</returns>
      public int getHelpIndex()
      {
         Property prop = null;
         int hlpIndex = -1;

         if (_propTab != null)
         {
            prop = _propTab.getPropById(PropInterface.PROP_TYPE_HELP_SCR);
            if (prop != null)
               hlpIndex = Convert.ToInt32(prop.getValue());
         }
         return hlpIndex;
      }

      /// <summary>
      ///   translate uom Rectangle to pixels
      /// </summary>
      /// <param name = "rect"></param>
      internal MgRectangle uom2pixRect(MgRectangle rect)
      {
         return new MgRectangle(uom2pix(rect.x, true), uom2pix(rect.y, false), uom2pix(rect.width, true),
                                uom2pix(rect.height, false));
      }

      /// <summary>
      ///   convert size from uom 2 pixels and from pixels to uom
      /// </summary>
      /// <param name = "isXaxis:">if true then the result relates to the X-axis, otherwise, it relates to the Y-axis</param>
      /// <returns></returns>
      private float prepareUOMConversion(bool isXaxis)
      {
         MgPointF size = null;
         float factor;
         int currFontId = getProp(PropInterface.PROP_TYPE_FONT).getValueInt();

         // initialize the factor values on first call
         if (isXaxis)
         {
            if (_horizontalFactor == -1)
               _horizontalFactor = getProp(PropInterface.PROP_TYPE_HOR_FAC).getValueInt();
         }
         else if (_verticalFactor == -1)
            _verticalFactor = getProp(PropInterface.PROP_TYPE_VER_FAC).getValueInt();

         WinUom uomMode = (WinUom)getProp(PropInterface.PROP_TYPE_UOM).getValueInt();

         // set the object on which the calculation is to be done.
         object obj = this;
         if (isSubForm())
            obj = _subFormCtrl;

         switch (uomMode)
         {
            case WinUom.Dlg:
               // Since calling guiUtils.getFontMetrics() may severely affect the performance then we save the Font
               // Metrics results and call it again only if the font changes
               if (currFontId != _prevFontId || _horizontalFactor <= 0 || _verticalFactor <= 0)
               {
                  MgFont mgFont = Manager.GetFontsTable().getFont(currFontId);

                  // get the form font size
                  _fontSize = Commands.getFontMetrics(mgFont, obj);

                  _prevFontId = currFontId;

                  //fixed bug #988296, while the hor\ver factor are zero, take the default size.(as we doing for online).
                  if (isXaxis)
                  {
                     if (_horizontalFactor <= 0)
                        _horizontalFactor = (int)_fontSize.x;
                  }
                  else if (_verticalFactor <= 0)
                     _verticalFactor = (int)_fontSize.y;
               }

               size = _fontSize;

               break;

            case WinUom.Mm:
            case WinUom.Inch:
               size = new MgPointF(Commands.getResolution(obj));
               break;

            case WinUom.Pix:
               return ((float)Commands.getResolution(obj).x / 96);

            default:
               throw new ApplicationException("prepareUOMConversion() --- unsupported uom.");
         }

         if (isXaxis)
         {
            factor = (float)size.x / _horizontalFactor;
         }
         else
         {
            factor = (float)size.y / _verticalFactor;
         }


         if (uomMode == WinUom.Mm)
            factor /= (float)2.54;

         return factor;
      }

      /// <summary>
      ///   converts from uom to pixels
      /// </summary>
      /// <param name = "uomVal">the measurement size in uom terms</param>
      /// <param name = "isXaxis">if true then the result relates to the X-axis, otherwise, it relates to the Y-axis</param>
      /// <returns> the uomVal converted to pixels</returns>
      protected internal int uom2pix(long uomVal, bool isXaxis)
      {
         int result = 0;

         // calculate the result of the conversion from uom to pixels
         if (uomVal != 0)
         {
            float factor = prepareUOMConversion(isXaxis);
            result = (int)(uomVal * factor + 0.5);
         }
         return result;
      }

      /// <summary>
      /// converts from double uom to pixels
      /// </summary>
      /// <param name="uomVal"></param>
      /// <param name="isXaxis"></param>
      /// <returns></returns>
      protected internal double uom2pix(double uomVal, bool isXaxis)
      {
         double result = 0;

         // calculate the result of the conversion from uom to pixels
         if (uomVal != 0)
            result = (uomVal * prepareUOMConversion(isXaxis));

         return result;
      }

      /// <summary>
      ///   converts from pixels to uom
      /// </summary>
      /// <param name = "pixVal">the measurement size in pixels terms</param>
      /// <param name = "isXaxis">if true then the result relates to the X-axis, otherwise, it relates to the Y-axis</param>
      /// <returns> the uomVal converted to pixels</returns>
      public int pix2uom(long pixVal, bool isXaxis)
      {
         int result = 0;

         //calculate the result of the conversion from pixels to uom  
         if (pixVal != 0)
         {
            float factor = prepareUOMConversion(isXaxis);
            factor = pixVal / factor;
            result = (int)(factor + (factor > 0
                                      ? 0.5
                                      : -0.5));
         }
         return result;
      }

      /// <summary>
      ///  converts from double pixels to uom
      /// </summary>
      /// <param name="pixVal"></param>
      /// <param name="isXaxis"></param>
      /// <returns></returns>
      public double pix2uom(double pixVal, bool isXaxis)
      {
         double result = 0;
         //calculate the result of the conversion from pixels to uom  
         if (pixVal != 0)
            result = pixVal / (double)prepareUOMConversion(isXaxis);

         return result;
      }

      /// <summary>
      ///   after open the form need to move it according to the startup position property on the form
      ///   PROP_TYPE_STARTUP_POSITION
      /// </summary>
      protected internal void startupPosition()
      {
         MgRectangle relativeRect = null;
         MgFormBase mgFormParent = null;
         MgPoint centerPoint = null;
         bool addCommandType = false;
         bool needConvertToPix = true;

         if (!isSubForm() && !IsFitToMdi)
         {
            var windowPosition = GetStartupPosition();
            centerPoint = new MgPoint(0, 0);
            relativeRect = new MgRectangle(0, 0, 0, 0);
            MgFormBase parentForm = null;

            switch (windowPosition)
            {
               case WindowPosition.CenteredToMagic: //center to MDI
               case WindowPosition.CenteredToParent:
                  RuntimeContextBase currentRuntimeContext = Manager.GetCurrentRuntimeContext();
                  if (windowPosition == WindowPosition.CenteredToMagic)
                     mgFormParent = currentRuntimeContext.FrameForm;
                  else // WindowPosition.WIN_POS_CENTERED_TO_PARENT
                  {
                     mgFormParent = ParentForm;
                     if (IsMDIChild)
                     {
                        parentForm = ParentForm;
                        if (ParentForm.getSubFormCtrl() != null)
                           parentForm = ParentForm.getTopMostForm();

                        // For MDI child, if the topMostForm is neither MDIChild nor MDIFrame then return MDIFrame
                        if (!(parentForm.IsChildWindow || parentForm.IsMDIChild || parentForm.IsMDIFrame)) //RC only
                           mgFormParent = currentRuntimeContext.FrameForm;
                     }
                  }

                  if (mgFormParent != null)
                  {
                     centerPoint = GetCenterPoint(mgFormParent);
                     needConvertToPix = false;
                     addCommandType = true;
                  }
                  break;
               case WindowPosition.Customized:
                  // The x&y of the user define in the prop sheet (according to the parent)
                  // the server doesn't send the x,y when inherited
                  centerPoint.x = getProp(PropInterface.PROP_TYPE_LEFT).getValueInt();
                  centerPoint.y = getProp(PropInterface.PROP_TYPE_TOP).getValueInt();
                  addCommandType = true;
                  break;
               case WindowPosition.DefaultBounds:
               case WindowPosition.DefaultLocation:
                  // Do nothing, OS is define the point
                  break;
               case WindowPosition.CenteredToDesktop:
                  MgFormBase relativeForm = null;
                  if (IsMDIChild)
                     relativeForm = Manager.GetCurrentRuntimeContext().FrameForm;
                  else
                     relativeForm = ParentForm;

                  Commands.getDesktopBounds(relativeRect, relativeForm);

                  // when MdiChild position in MDIFrame
                  // If child window position in parent window
                  // When floating, modal, tool or sdi converting to client co-ordinates not required
                  centerPoint = GetCenterPoint(relativeForm, relativeRect);
                  needConvertToPix = false;
                  addCommandType = true;
                  break;
            }
         }

         if (addCommandType)
         {
            if (needConvertToPix)
            {
               centerPoint.x = uom2pix(centerPoint.x, true);
               centerPoint.y = uom2pix(centerPoint.y, false);
            }

            Commands.addAsync(CommandType.PROP_SET_BOUNDS, this, 0, centerPoint.x, centerPoint.y,
                              GuiConstants.DEFAULT_VALUE_INT, GuiConstants.DEFAULT_VALUE_INT, false, false);
         }
      }

      /// <summary>
      ///   return Point ,center to the giving relativeRect
      /// </summary>
      private MgPoint GetCenterPoint(MgFormBase parentForm, MgRectangle relativeRect)
      {
         int x, y;
         var formRect = new MgRectangle(0, 0, 0, 0);

         // get bounds for the current form
         Commands.getBounds(this, formRect);

         // calculate the point
         x = ((relativeRect.width - formRect.width) / 2) + relativeRect.x;
         y = ((relativeRect.height - formRect.height) / 2) + relativeRect.y;

         var pt = new MgPoint(x, y);

         if (IsMDIChild || IsChildWindow)
         {
            Object parent = null;
            if (parentForm != null)
            {
               parent = parentForm;
               if (parentForm.isSubForm())
                  parent = parentForm.getSubFormCtrl();
               else if (parentForm.getContainerCtrl() != null)
                  parent = parentForm.getContainerCtrl();
            }

            Commands.PointToClient(parent, pt);

            //fix negative coords
            pt.x = Math.Max(pt.x, 0);
            pt.y = Math.Max(pt.y, 0);
         }
         else
         {
            // Defect# 117245: For floating/modal windows, check if point is inside the bounds of one of the monitors.
            // If so, use that point. If it is not contained in any of the monitors, then get parent form monitor.
            // Get LeftTop of parent form monitor and use it as reference location.
            bool isPointInMonitor = Commands.IsPointInMonitor(pt);

            if (!isPointInMonitor)
            {
               pt = Commands.GetLeftTopLocationFormMonitor(parentForm);
            }
         }

         return pt;
      }

      /// <summary>
      ///   Calculate Center point.
      ///   All co-ordinates will be converted to screen co-ordinates and then 
      ///   they will be converted to client co-ordinates according to their parent.
      /// </summary>
      private MgPoint GetCenterPoint(MgFormBase mgFormParent)
      {
         MgFormBase relativeForm = mgFormParent;
         MgRectangle relativeRect = new MgRectangle(0, 0, 0, 0);
         MgPoint centerPoint = new MgPoint(0, 0);
         RuntimeContextBase currentRuntimeContext = Manager.GetCurrentRuntimeContext();

         if (mgFormParent.getSubFormCtrl() != null ||
             mgFormParent.getContainerCtrl() != null)
         {
            // If Parent form is sub-form or form in frameset then get relativeRect of 
            // appropriate control
            MgControlBase ctrl = (mgFormParent.getSubFormCtrl() != null) ? mgFormParent.getSubFormCtrl()
                                                                         : mgFormParent.getContainerCtrl();
            Commands.getBoundsRelativeTo(ctrl, 0, relativeRect, null);
         }
         else
         {
            Commands.getBounds(mgFormParent, relativeRect);

            // When Parent is SDI/Floating/Modal/Tool then the co-ordinates are already screen
            // co-ordinates. When form has a subform/container control then
            // co-ordinates returned by getBoundsRelativeTo are screen co-ordinates.
            MgPoint pt = new MgPoint(relativeRect.x, relativeRect.y);
            switch (mgFormParent.ConcreteWindowType)
            {
               case WindowType.MdiFrame:
                  relativeRect = Commands.GetMDIClientBounds();
                  pt = new MgPoint(relativeRect.x, relativeRect.y);
                  Commands.PointToScreen(currentRuntimeContext.FrameForm, pt);
                  break;

               case WindowType.MdiChild:
               case WindowType.FitToMdi:
                  // If calling window is MDIChild then pointToScreen of MDIClient is to be called
                  Commands.PointToScreen(currentRuntimeContext.FrameForm, pt);
                  break;

               case WindowType.ChildWindow:
                  // If parent form is child window then call PointToScreen of its parent
                  Object parentObj = mgFormParent.GetParentForm();
                  Commands.PointToScreen(parentObj, pt);
                  break;
            }
            relativeRect.x = pt.x;
            relativeRect.y = pt.y;
         }

         if (IsMDIChild)
            relativeForm = currentRuntimeContext.FrameForm;
         else
            relativeForm = ParentForm;

         centerPoint = GetCenterPoint(relativeForm, relativeRect);

         return centerPoint;
      }

      /// <returns> get all ctrls of 'type'</returns>
      internal List<MgControlBase> getCtrls(MgControlType type)
      {
         MgControlBase curr = null;
         var ctrls = new List<MgControlBase>();

         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            curr = CtrlTab.getCtrl(i);
            if (curr.Type == type)
               ctrls.Add(curr);
         }

         return ctrls;
      }

      /// <summary>
      ///   get MgTreeBase
      /// </summary>
      /// <returns></returns>
      public MgTreeBase getMgTree()
      {
         return _mgTree;
      }

      /// <summary>
      ///   returns the statusbar object
      /// </summary>
      /// <returns></returns>
      public MgStatusBar GetStatusBar(bool fromAncestor)
      {
         MgStatusBar mgStatusBar = _statusBar;

         if ((mgStatusBar == null || !mgStatusBar.isVisible()) && (isSubForm() || fromAncestor))
         {
            TaskBase ancestorTask = null;
            MgFormBase ancestorForm = null;
            if (IsMDIChild)
            {
               // In case MDIFrameForm is not created yet then don't show the message, it will be saved and be displayed later when form is created.
               Int64 contextID = getTask().ContextID;
               if (Events.GetRuntimeContext(contextID).FrameForm != null)
               {
                  ancestorTask = Events.GetRuntimeContext(contextID).FrameForm.getTask();
                  if (ancestorTask != null)
                     ancestorForm = ancestorTask.getForm();
               }
            }
            else
               ancestorForm = ParentForm; //If it's not MDI Child , search status bar using ParentForm.

            if (ancestorForm != null)
               mgStatusBar = ancestorForm.GetStatusBar(fromAncestor);
            else
               mgStatusBar = GetStatusBarFromContext();
         }

         return mgStatusBar;
      }

      /// <summary>
      ///   get table control
      /// </summary>
      /// <returns></returns>
      public MgControlBase getTableCtrl()
      {
         return _tableMgControl;
      }

      /// <summary>
      ///   return tree or table control of the form
      /// </summary>
      /// <returns></returns>
      protected MgControlBase getMainControl()
      {
         MgControlBase mainControl = _tableMgControl;
         // controlForSelect can be table control or tree control
         if (mainControl == null)
            mainControl = _treeMgControl;
         return mainControl;
      }

      /// <summary>
      ///   return tree control
      /// </summary>
      /// <returns></returns>
      public MgControlBase getTreeCtrl()
      {
         return _treeMgControl;
      }

      /// <summary>
      ///   Return the control (which is not the frame form) with a given name
      /// </summary>
      /// <param name = "ctrlName">the of the requested control</param>
      /// <param name="ctrlType"></param>
      /// <returns> a reference to the control with the given name or null if does not exist</returns>
      public MgControlBase getCtrlByName(String ctrlName, MgControlType ctrlType)
      {
         return CtrlTab.getCtrlByName(ctrlName, ctrlType);
      }

      /// <summary>
      ///   This method returns the context menu number of the form.
      ///   In case the form does not have a context menu defined, the system context menu is returned
      /// </summary>
      /// <returns></returns>
      internal int getContextMenuNumber()
      {
         int contextMenu = (GetComputedProperty(PropInterface.PROP_TYPE_CONTEXT_MENU) != null
                            ? GetComputedProperty(PropInterface.PROP_TYPE_CONTEXT_MENU).GetComputedValueInteger()
                            : 0);

         if (contextMenu == 0)
         {
            if (isSubForm())
            {
               if (ParentForm != null)
                  contextMenu = ParentForm.getContextMenuNumber();
            }
         }
         // if value is zero, we need to use the system menu definition
         if (contextMenu == 0)
            contextMenu = getSystemContextMenu();
         return contextMenu;
      }

      /// <summary>
      ///   return the control's context menu
      /// </summary>
      /// <param name = "createIfNotExist">This decided if Context menu is to be created or not.</param>
      /// <returns>matching mgMenu</returns>
      public MgMenu getContextMenu(bool createIfNotExist)
      {
         MgMenu mgMenu = null;

         int contextMenuNum = getContextMenuNumber();
         if (contextMenuNum > 0)
            mgMenu = Manager.GetMenu(getTask().ContextID, getTask().getCtlIdx(), contextMenuNum, MenuStyle.MENU_STYLE_CONTEXT, this, createIfNotExist);

         return mgMenu;
      }

      /// <summary>
      ///   return the application-level context menu index
      /// </summary>
      /// <returns>matching menu index</returns>
      internal int getSystemContextMenu()
      {
         TaskBase mainProg = (TaskBase)Manager.MGDataTable.GetMainProgByCtlIdx(getTask().ContextID, getTask().getCtlIdx());
         return mainProg.getSystemContextMenu();
      }


      /// <summary>
      ///   if row is not created yet creates row
      /// </summary>
      /// <param name = "idx"></param>
      protected internal void checkAndCreateRow(int idx)
      {
#if !PocketPC
         if (DVControlManager != null && LastDvRowCreated != idx)
         {
            //If in form initialization, do not create row in dv control.
            if (InInitForm)
               return;
            DVControlManager.checkAndCreateRow(idx);
            LastDvRowCreated = idx;
         }
         else
#endif
         {
            if (_tableMgControl != null && !isRowCreated(idx))
               createRow(idx);
         }
      }

      /// <summary>
      ///   marks row created
      /// </summary>
      /// <param name = "idx"></param>
      private void createRow(int idx)
      {
         Commands.addAsync(CommandType.CREATE_TABLE_ROW, _tableMgControl, 0, idx);
         if (Rows.Count <= idx)
            Rows.SetSize(idx + 1);
         Rows[idx] = new Row(true, true);
      }

      /// <summary>
      ///   validate row
      /// </summary>
      /// <param name = "idx"></param>
      internal void validateRow(int idx)
      {
         if (_tableMgControl != null)
         {
            var row = (Row)Rows[idx];
            if (!row.Validated)
            {
               Commands.addAsync(CommandType.VALIDATE_TABLE_ROW, _tableMgControl, 0, idx);
               row.Validated = true;
            }
         }
      }

      /// <summary>(public)
      /// check if specified row is valid row in page
      /// </summary>
      /// <param name="idx"></param>
      public bool IsValidRow(int idx)
      {
         bool validRow = false;
         if (_tableMgControl != null && (idx >= 0 && idx < Rows.Count))
         {
            var row = (Row)Rows[idx];
            validRow = (row != null ? row.Validated : false);
         }
         return validRow;
      }

      /// <summary>
      /// put record data on the screen
      /// </summary>
      public void refreshRow(int rowIdx, bool repeatableOnly)
      {
#if !PocketPC
         if (hasTableOrTree() || DVControlManager != null)
#else
         if (hasTableOrTree())
#endif
         {
            checkAndCreateRow(rowIdx);
            DisplayLine = rowIdx;
         }
         refreshControls(repeatableOnly);
      }

      /// <summary>
      ///   refresh the form properties
      /// </summary>
      public void refreshProps()
      {
         if (_firstRefreshOfProps)
         {
            getProp(PropInterface.PROP_TYPE_TITLE_BAR);
            getProp(PropInterface.PROP_TYPE_SYSTEM_MENU);
            getProp(PropInterface.PROP_TYPE_MINBOX);
            getProp(PropInterface.PROP_TYPE_MAXBOX);
            getProp(PropInterface.PROP_TYPE_BORDER_STYLE);

            if (isSubForm())
               getProp(PropInterface.PROP_TYPE_WALLPAPER);
            //getProp(PropInterface.PROP_TYPE_WINDOW_TYPE);
         }
         _propTab.RefreshDisplay(false, false);

         if (_firstRefreshOfProps)
            _firstRefreshOfProps = false;
      }

      public class AnControl
      {
         public string Value { get; set; }
         public int ControlIsn { get; set; }
      }

      public class AnRow
      {
         public List<AnControl> controls = new List<AnControl>();
         private int line;
         public int Line
         {
            get { return line; }
            set { line = value; }
         }
         public void AddControl(MgControlBase ctrl)
         {
            if (ctrl.IsRepeatable)
            {
               AnControl anControl = new AnControl() { Value = ctrl.Value, ControlIsn = ctrl.ControlIsn };
               controls.Add(anControl);
            }
         }
      }

      /// <summary>
      ///   refresh the controls of the current row
      /// </summary>
      /// <param name = "repeatableOnly">should be true in order to refresh only the controls that belong to a table</param>
      public void refreshControls(bool repeatableOnly)
      {
         int i;
         MgControlBase ctrl;

         // record does not exist
         if (DisplayLine == Int32.MinValue && !IsMDIFrame)
            return;

         // refresh the table first
         if (_tableMgControl != null && !repeatableOnly)
            _tableMgControl.RefreshDisplay();

         // refresh all COLUMNS (refresh for each control base on the refresh of the column (e.g:column width)
         // so we must refresh the column first
         if (_tableColumns != null && !repeatableOnly)
         {
            for (i = 0; i < _tableColumns.Count; i++)
            {
               MgControlBase mgCtrl = _tableColumns[i];
               mgCtrl.RefreshDisplay();
            }
         }

         checkAndCreateRow(DisplayLine);

         // QCR #709563 Must refresh frameset's subfroms before we refresh frameset itself.
         // During refresh of the frameset nested subforms with RefreshWhenHidden might be brought to server
         // but their subform's controls have not been set with correct bounds. This causes illegal placement
         // and incorrect final location of controls on nested subforms
         var frameSets = new Stack<MgControlBase>();
         var tabAndGroupControls = new Stack<MgControlBase>();
         AnRow anrow = new AnRow() { Line = displayLine };
         // refresh all but table controls
         for (i = 0; i < CtrlTab.getSize(); i++)
         {
            ctrl = CtrlTab.getCtrl(i);

            if ((ctrl.isTabControl() || ctrl.isGroup()) && !repeatableOnly)
            {
               tabAndGroupControls.Push(ctrl);
               Commands.addAsync(CommandType.SUSPEND_LAYOUT, ctrl);
            }

            if (ctrl.Type == MgControlType.CTRL_TYPE_FRAME_SET)
            {
               frameSets.Push(ctrl);
               continue;
            }

            if (ctrl.Type != MgControlType.CTRL_TYPE_TABLE && ctrl.Type != MgControlType.CTRL_TYPE_COLUMN)
            {
               if (ctrl.IsRepeatable || !repeatableOnly)
                  ctrl.RefreshDisplay();
            }
            else if (ctrl.Type == MgControlType.CTRL_TYPE_TABLE && repeatableOnly)
               ctrl.RefreshDisplay(repeatableOnly);
            if (ctrl.IsRepeatable)
               anrow.AddControl(ctrl);
         }

         while (frameSets.Count > 0)
            frameSets.Pop().RefreshDisplay();

         while (tabAndGroupControls.Count > 0)
            Commands.addAsync(CommandType.RESUME_LAYOUT, tabAndGroupControls.Pop());

         validateRow(DisplayLine);
         //((Row)Rows[displayLine]).Anrow = anrow;



         if (!FormRefreshedOnceAfterFetchingDataView && _tableMgControl != null && _task.DataViewWasRetrieved)
         {
            FormRefreshedOnceAfterFetchingDataView = true;
            Commands.addAsync(CommandType.SET_SHOULD_APPLY_PLACEMENT_TO_HIDDEN_COLUMNS, _tableMgControl);
         }

      }

      /// <summary>
      ///   returns true if Row is created on GUI level
      /// </summary>
      /// <param name = "idx"></param>
      /// <returns></returns>
      protected internal bool isRowCreated(int idx)
      {
         if (Rows.Count <= idx || idx < 0)
            return false;
         var row = (Row)Rows[idx];
         if (row == null)
            return false;
         return row.Created;
      }

      /// <summary>
      ///   marks this row as not created
      /// </summary>
      /// <param name = "idx"></param>
      protected internal void markRowNOTCreated(int idx)
      {
         Commands.addAsync(CommandType.UNDO_CREATE_TABLE_ROW, _tableMgControl, 0, idx);

         if (Rows.Count <= idx || idx < 0)
            return;
         Rows[idx] = new Row(false, false);
      }


      /// <summary>
      ///   Mark current row as selected row
      /// </summary>
      public void SelectRow()
      {
         SelectRow(false);
      }

      /// <summary>
      /// Mark current row as selected row
      /// </summary>
      /// <param name = "sendAlways">send selection command depending on previous value</param>
      public virtual void SelectRow(bool sendAlways)
      {
         SelectRow(DisplayLine, sendAlways);
      }

      /// <summary>
      ///   Mark a row as selected row
      /// </summary>
      /// <param name = "rowIdx">row to select</param>
      /// <param name = "sendAlways">send selection command depending on previous value</param>
      public virtual void SelectRow(int rowIdx, bool sendAlways)
      {
         // controlForSelect can be table control or tree control
         MgControlBase mainControl = getMainControl();
         if (mainControl != null && RefreshRepeatableAllowed)
         {
            int index = rowIdx;
            if (_task.DataView.isEmptyDataview())
               index = GuiConstants.NO_ROW_SELECTED;
            if (mainControl.isTreeControl() && _mgTree != null)
               //make sure that all ancestors of the node r expandes
               _mgTree.expandAncestors(index);
            if (sendAlways || index != _prevSelIndex)
            // for tree control we always send selection, since sometimes we need to undo tree defaul selection
            {
               _prevSelIndex = index;
               Commands.addAsync(CommandType.SET_SELECTION_INDEX, mainControl, 0, index);
            }
         }
      }

      /// <summary>
      /// Mark/unmark row for multi marking
      /// </summary>
      /// <param name="rowIdx">indentifers the row</param>
      /// <param name="isMarked">mark/unmark</param>
      public void MarkRow(int rowIdx, bool isMarked)
      {
         Commands.addAsync(CommandType.SET_MARKED_ITEM_STATE, getTableCtrl(), rowIdx, isMarked);
      }

      /// <summary>
      ///   true if this is mdi child with startup position centered to client
      /// </summary>
      /// <returns></returns>
      internal bool MdliChildCenteredToParent()
      {
         if (ConcreteWindowType == WindowType.MdiChild)
         {
            if (GetStartupPosition() == WindowPosition.CenteredToParent)
               return true;
         }

         return false;
      }

      /// <summary>
      ///   This method returns the number of tools for the passed tool group
      /// </summary>
      /// <param name = "groupNumber">number of the tool group (0-based)</param>
      /// <returns></returns>
      internal int getToolbarGroupCount(int groupNumber)
      {
         ToolbarInfo toolbarInfo = getToolBarInfoByGroupNumber(groupNumber, false);
         return (toolbarInfo != null
                 ? toolbarInfo.getCount()
                 : 0);
      }

      /// <summary>
      ///   return the toolbar info for group number, if not exist create it
      /// </summary>
      /// <param name = "groupNumber"></param>
      /// <param name = "createIfNotExist">TODO</param>
      /// <returns></returns>
      private ToolbarInfo getToolBarInfoByGroupNumber(int groupNumber, bool createIfNotExist)
      {
         var toolbarInfo = (ToolbarInfo)_toolbarGroupsCount[groupNumber];
         if (toolbarInfo != null && !createIfNotExist)
         {
            if (toolbarInfo.getCount() == 0 && toolbarInfo.getMenuEntrySeperator() == null)
               return null;
         }
         if (toolbarInfo == null && createIfNotExist)
         {
            toolbarInfo = new ToolbarInfo();
            _toolbarGroupsCount[groupNumber] = toolbarInfo;
         }

         return toolbarInfo;
      }

      /// <summary>
      /// get the rows in a page in table
      /// </summary>
      /// <returns> int is the table size</returns>
      public int getRowsInPage()
      {
         return _rowsInPage;
      }

      /// <summary>
      /// set the rows in a page in table
      /// </summary>
      /// <param name = "size">the table size</param>
      public void setRowsInPage(int size)
      {
         _rowsInPage = size;
      }

      /// <summary>
      ///   gets number of columns in the table
      /// </summary>
      /// <returns></returns>
      internal int getColumnsCount()
      {
         int columnsCount = 0;

         if (CtrlTab != null)
         {
            for (int i = 0; i < CtrlTab.getSize(); i++)
            {
               MgControlBase control = CtrlTab.getCtrl(i);
               if (control.Type == MgControlType.CTRL_TYPE_COLUMN)
                  columnsCount++;
            }
         }
         return columnsCount;
      }

      /// <summary>
      ///   clear
      /// </summary>
      internal void toolbarGroupsCountClear()
      {
         _toolbarGroupsCount.Clear();
      }

      /// <summary>
      ///   This method adds a tools count for the passed tool group
      /// </summary>
      /// <param name = "groupNumber">number of the tool group (0-based)</param>
      /// <param name = "count">the new count of tool on the group</param>
      internal void setToolbarGroupCount(int groupNumber, int count)
      {
         ToolbarInfo toolbarInfo = getToolBarInfoByGroupNumber(groupNumber, true);
         toolbarInfo.setCount(count);
      }

      /// <summary>
      ///   returns the value of the "in refresh display" flag
      /// </summary>
      public bool inRefreshDisplay()
      {
         return _inRefreshDisp;
      }

      /// <summary>
      ///   remove the wide control from the _ctrlTab of the form
      /// </summary>
      private void removeWideControl()
      {
         if (_wideControl != null)
         {
            CtrlTab.Remove(_wideControl);
            Commands.addAsync(CommandType.DISPOSE_OBJECT, _wideControl);
            _wideControl = null;
         }
      }

      /// <summary>
      ///   return true if the wide is open
      /// </summary>
      /// <returns></returns>
      public bool wideIsOpen()
      {
         return (getTopMostForm()._wideControl != null);
      }

      /// <summary>
      ///   get wide control
      /// </summary>
      /// <returns></returns>
      public MgControlBase getWideControl()
      {
         return _wideControl;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public MgControlBase getWideParentControl()
      {
         return _wideParentControl;
      }

      /// <summary>
      ///   Execute the layout and all its children
      /// </summary>
      public void executeLayout()
      {
         Commands.addAsync(CommandType.EXECUTE_LAYOUT, (Object)this, true);
      }

      /// <summary>
      ///   decrease one from the count on the groupNumber
      /// </summary>
      /// <param name="groupNumber"></param>
      /// <param name="removeSeperat"></param>
      internal void removeToolFromGroupCount(int groupNumber, bool removeSeperat)
      {
         if (getToolbarGroupCount(groupNumber) > 0)
         {
            int newCount = Math.Max(getToolbarGroupCount(groupNumber) - 1, 0);
            setToolbarGroupCount(groupNumber, newCount);

            if (removeSeperat && newCount <= 1)
            {
               // when the group is empty(has only separator) need to remove the separator from the prev group
               // Only when it is the last group and not in the middle
               int prevGroup = getPrevGroup(groupNumber);
               //QCR#712355:Menu separator was not displayed properly as groupNumber was passed to
               //needToRemoveSeperatorFromPrevGroup() insteade of prevGroup.
               bool removeSeperatorFromPrevGroup = needToRemoveSeperatorFromPrevGroup(prevGroup);
               if (removeSeperatorFromPrevGroup)
                  removeSepFromGroup(prevGroup);
               else
               {
                  bool removeSeperatorFromCurrGroup = needToRemoveSeperatorFromCurrGroup(groupNumber);
                  if (removeSeperatorFromCurrGroup)
                     removeSepFromGroup(groupNumber);
               }
            }
            else
            {
               // if there is not longer any tool on the toolbar need to delete the toolbar
               // FYI: just hide the toolbar to actually deleted (it remove from the control map)
               if (getToolItemCount() == 0)
               {
                  var mgMenu = (MgMenu)_instatiatedMenus[MenuStyle.MENU_STYLE_PULLDOWN];
                  if (mgMenu != null)
                     mgMenu.deleteToolBar(this);
               }
            }
         }
         else
            Debug.Assert(false);
      }

      /// <param name = "currentGroup">
      /// </param>
      /// <returns></returns>
      private bool needToRemoveSeperatorFromPrevGroup(int currentGroup)
      {
         bool needToRemoveSeperator = false;
         int prevGroup = getPrevGroup(currentGroup);
         int currentGroupCount = getToolbarGroupCount(currentGroup);
         MenuEntry menuEntrySep = getToolbarGroupMenuEntrySep(currentGroup);

         bool itIsLastGroup = itIsLastGroupInToolbar(currentGroup);

         //if it is last group need to remove the sep from the last group
         if (!itIsLastGroup)
         {
            //remove the sep if we the second group and up, and we don't have any item 
            if (prevGroup >= 0)
            {
               if ((currentGroupCount == 0 || (currentGroupCount == 1 && menuEntrySep != null)))
                  needToRemoveSeperator = true;
            }
            else
            {
               //it is the first group
               if (currentGroupCount == 0 && getGroupCount() > 1 && menuEntrySep != null)
                  needToRemoveSeperator = true;
            }
         }
         return needToRemoveSeperator;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="groupNumber"></param>
      /// <returns></returns>
      private void removeSepFromGroup(int groupNumber)
      {
         //remove the separator from the prev group
         MenuEntry menuEntrySep = getToolbarGroupMenuEntrySep(groupNumber);

         if (menuEntrySep != null)
         {
            menuEntrySep.deleteMenuEntryTool(this, false, true);
            setToolbarGroupMenuEntrySep(groupNumber, null);
         }
      }

      /// <summary>
      ///   This method adds a menuEntry for seperator
      /// </summary>
      /// <param name = "groupNumber">number of the tool group (0-based)</param>
      /// <param name = "menuEntrySep">the new menuEntrySep of tool on the group</param>
      internal void setToolbarGroupMenuEntrySep(int groupNumber, MenuEntry menuEntrySep)
      {
         ToolbarInfo toolbarInfo = getToolBarInfoByGroupNumber(groupNumber, true);
         toolbarInfo.setMenuEntrySeperator(menuEntrySep);
      }

      /// <summary>
      ///   check if the sep need to be remove from the current group
      /// </summary>
      /// <param name = "currentGroup"></param>
      /// <returns></returns>
      private bool needToRemoveSeperatorFromCurrGroup(int currentGroup)
      {
         bool needToRemoveSeperator = false;
         bool itIsLastGroup = itIsLastGroupInToolbar(currentGroup);
         if (itIsLastGroup)
            needToRemoveSeperator = true;
         else
         {
            int currentGroupCount = getToolbarGroupCount(currentGroup);
            MenuEntry menuEntrySep = getToolbarGroupMenuEntrySep(currentGroup);
            if (getGroupCount() > 1 && currentGroupCount <= 1 && menuEntrySep != null)
               needToRemoveSeperator = true;
         }

         return needToRemoveSeperator;
      }

      /// <summary>
      ///   return TRUE if this group is the last group in the toolbar
      /// </summary>
      /// <param name = "groupNumber"></param>
      /// <returns></returns>
      internal bool itIsLastGroupInToolbar(int groupNumber)
      {
         //get the ToolbarInfo of the last group count
         ToolbarInfo toolbarInfoLastGroup = getLastGroup();

         //get the ToolbarInfo of the groupNumber
         ToolbarInfo toolbarInfoGroupNumber = getToolBarInfoByGroupNumber(groupNumber, false);

         bool itIsTheLastGroup = ReferenceEquals(toolbarInfoLastGroup, toolbarInfoGroupNumber);
         return itIsTheLastGroup;
      }

      /// <summary>
      ///   return the toolbar info for group number, if not exist create it
      /// </summary>
      /// <returns></returns>
      private ToolbarInfo getLastGroup()
      {
         IEnumerator toolbarGroupsEnumerator = _toolbarGroupsCount.GetEnumerator();
         ToolbarInfo toolbarInfo = null;
         ToolbarInfo toolbarInfoLastGorup = null;
         int lastImageGroup = -1;

         while (toolbarGroupsEnumerator.MoveNext())
         {
            Object obj = toolbarGroupsEnumerator.Current;
            toolbarInfo = (ToolbarInfo)((DictionaryEntry)obj).Value;
            MenuEntry MenuEntrySep = toolbarInfo.getMenuEntrySeperator();
            if (MenuEntrySep != null)
            {
               int currImageGroup = MenuEntrySep.ImageGroup;
               if (currImageGroup > lastImageGroup)
               {
                  lastImageGroup = currImageGroup;
                  toolbarInfoLastGorup = toolbarInfo;
               }
            }
         }

         return toolbarInfoLastGorup;
      }

      /// <summary>
      ///   This method get a menuEntry for separator
      /// </summary>
      /// <param name = "groupNumber">number of the tool group (0-based)</param>
      internal MenuEntry getToolbarGroupMenuEntrySep(int groupNumber)
      {
         ToolbarInfo toolbarInfo = getToolBarInfoByGroupNumber(groupNumber, false);
         return (toolbarInfo != null
                 ? toolbarInfo.getMenuEntrySeperator()
                 : null);
      }

      /// <summary>
      ///   return the number of the group
      /// </summary>
      /// <returns></returns>
      internal int getGroupCount()
      {
         int groupCount = 0;

         IEnumerator toolbarGroupsEnumerator = _toolbarGroupsCount.GetEnumerator();
         ToolbarInfo toolbarInfo = null;

         while (toolbarGroupsEnumerator.MoveNext())
         {
            Object obj = toolbarGroupsEnumerator.Current;
            toolbarInfo = (ToolbarInfo)((DictionaryEntry)obj).Value;
            if (toolbarInfo != null && toolbarInfo.getCount() > 0)
               groupCount++;
         }

         return groupCount;
      }

      /// <summary>
      /// Table control is always first on the form
      /// When table is a child of tab, it is created before tab's layers(TabPages) are created.
      /// Computing Tab's children coordinates depends on the TabPages being created
      /// So, we must fix table's location after Tab is created
      /// QCR #289966
      /// </summary>
      internal void fixTableLocation()
      {
         if (HasTable())
         {
            MgControlBase parent = _tableMgControl.getParent() as MgControlBase;
            if (parent != null && parent.isTabControl())
            {
               _tableMgControl.getProp(PropInterface.PROP_TYPE_TOP).RefreshDisplay(true);
               _tableMgControl.getProp(PropInterface.PROP_TYPE_LEFT).RefreshDisplay(true);
            }
         }
      }

      /// <summary>
      /// this form may have ActiveRowHightlightState
      /// </summary>
      public virtual bool SupportActiveRowHightlightState
      {
         get
         {
            return HasTable();
         }

      }



      public void SetActiveHighlightRowState(bool state)
      {
         if (SupportActiveRowHightlightState)
            Commands.addAsync(CommandType.PROP_SET_ACTIVE_ROW_HIGHLIGHT_STATE, _tableMgControl, state);

      }
      /// <summary>
      ///   create placement layout on the form
      /// </summary>
      internal void createPlacementLayout()
      {
         if (IsMDIFrame && GetControlsCountExcludingStatusBar() == 0)
            return;
         int? runtimeDesignerXDiff = null, runtimeDesignerYDiff = null;

         // get coordinates that were defined on the form originally,
         // i.e. before expression executed
         MgRectangle rect = Property.getOrgRect(this);
         if (_subFormCtrl == null)
            Commands.addAsync(CommandType.CREATE_PLACEMENT_LAYOUT, this, 0, rect.x, rect.y, rect.width, rect.height,
                              false, false, null, null);
         else
         {
            //For frame set form we have fill layout
            if (!IsFrameSet)
            {
               var autoFit = (AutoFit)_subFormCtrl.getProp(PropInterface.PROP_TYPE_AUTO_FIT).getValueInt();
               if (autoFit == AutoFit.None)
                  //if we replace subform with AUTO_FIT_NONE we do placement relatively to subform size
                  //if regular case it will just prevent placement
                  rect = _subFormCtrl.getRect();

               //fix values to prevend additional placement if runtime designer changed widht/height of the subform
               if (_subFormCtrl.getForm() != null)
               {
                  bool containsWidth, containsHeight;
                  _subFormCtrl.RuntimeDesignerContainsSize(out containsWidth, out containsHeight);
                  if (containsWidth)
                  {
                     if (autoFit == AutoFit.None)
                        runtimeDesignerXDiff = 0; /* prevent placement in this case*/
                     else
                     {
                        int subformWidth = _subFormCtrl.getProp(PropInterface.PROP_TYPE_WIDTH).CalcWidthValue(_subFormCtrl);
                        int formWidth = rect.width;
                        runtimeDesignerXDiff = subformWidth - formWidth;
                     }
                  }
                  if (containsHeight)
                  {
                     if (autoFit == AutoFit.None)
                        runtimeDesignerYDiff = 0; /* prevent placement in this case*/
                     else
                     {
                        int subformHeight = _subFormCtrl.getProp(PropInterface.PROP_TYPE_HEIGHT).CalcWidthValue(_subFormCtrl);
                        int formHeight = rect.height;
                        runtimeDesignerYDiff = subformHeight - formHeight;
                     }
                  }

               }

               //TODO
               Commands.addAsync(CommandType.CREATE_PLACEMENT_LAYOUT, _subFormCtrl, 0, rect.x, rect.y, rect.width,
                                 rect.height, false, false, runtimeDesignerXDiff, runtimeDesignerYDiff);
            }
         }
      }


      /// <summary>
      ///   return the prev group index if exist
      /// </summary>
      /// <param name = "currGroup"></param>
      /// <returns></returns>
      private int getPrevGroup(int currGroup)
      {
         // create a separator for the previous group
         int prevGroup = currGroup - 1;

         while (prevGroup >= 0)
         {
            if (getToolBarInfoByGroupNumber(prevGroup, false) != null)
               break;
            else
               prevGroup--;
         }

         return prevGroup;
      }

      /// <summary>
      ///   return the count of the tool item on the toolbar (not include the separators for the groups)
      /// </summary>
      /// <returns></returns>
      internal int getToolItemCount()
      {
         int toolItemCount = 0;

         IEnumerator toolbarGroupsEnumerator = _toolbarGroupsCount.GetEnumerator();
         ToolbarInfo toolbarInfo = null;

         while (toolbarGroupsEnumerator.MoveNext())
         {
            Object obj = toolbarGroupsEnumerator.Current;
            toolbarInfo = (ToolbarInfo)((DictionaryEntry)obj).Value;
            toolItemCount += (toolbarInfo != null
                              ? toolbarInfo.getCount()
                              : 0);
         }
         return toolItemCount;
      }

      /// <summary>
      ///   This method puts the system menus on the form, in case the form does not have the
      ///   menus properties (pulldown \ context) defined.
      /// </summary>
      private void inheritSystemMenus()
      {
         Property prop = null;
         int contextMenu = 0;
         var num = new NUM_TYPE();

         prop = getProp(PropInterface.PROP_TYPE_CONTEXT_MENU);
         if (prop == null || !prop.isExpression())
         {
            if (prop != null)
               contextMenu = prop.getValueInt();
            // if value is zero, we need to use the system menu definition
            if (contextMenu == 0)
            {
               contextMenu = getSystemContextMenu();
               if (contextMenu > 0)
               {
                  num.NUM_4_LONG(contextMenu);
                  setProp(PropInterface.PROP_TYPE_CONTEXT_MENU, num.toXMLrecord());
               }
            }
         }
      }

      /// <summary>
      ///   sets the property
      /// </summary>
      /// <param name = "propId"></param>
      /// <param name = "val"></param>
      protected void setProp(int propId, String val)
      {
         if (_propTab == null)
            _propTab = new PropTable(this);

         _propTab.setProp(propId, val, this, GuiConstants.PARENT_TYPE_FORM);
      }

      public int getControlIdx(MgControlBase ctrl)
      {
         return CtrlTab.getControlIdx(ctrl, true);
      }

      /// <summary>
      ///   Add an MgMenu object to the list of the form's menus.
      ///   A form can have an MgMenu which is assigned to it directly (since the menu is set as the form's
      ///   pulldown menu, for example). It also has the context menus of its children - a context menu of 
      ///   a control is created under the form and saved in the form's list, and then assigned to the control
      ///   (this to allow re-usability of menus which appear more than once).
      /// </summary>
      /// <param name = "mgMenu">menu to be added to the list</param>
      internal void addMgMenuToList(MgMenu mgMenu, MenuStyle menuStyle)
      {
         _instatiatedMenus[menuStyle] = mgMenu;
      }

      /// <summary>
      ///   create separator on a group
      /// </summary>
      internal void createSepOnGroup(MgMenu mgMenu, int groupNumber)
      {
         var menuEntry = new MenuEntry(GuiMenuEntry.MenuType.SEPARATOR, mgMenu);
         menuEntry.setVisible(true, true, true, null, null);
         setToolbarGroupMenuEntrySep(groupNumber, menuEntry);
         menuEntry.ImageGroup = groupNumber;
         // create the matching tool
         menuEntry.createMenuEntryTool(this, false);
      }

      /// <summary>
      ///   This method creates a new tool group for the passed group index, and places a separator in its end.
      ///   returns true if the tool group was created now, false if it already existed.
      /// </summary>
      /// <param name = "toolGroup"></param>
      internal bool createToolGroup(MgMenu mgMenu, int toolGroup)
      {
         bool needToCreateAtTheEnd = false;
         ToolbarInfo toolbarInfo = getToolBarInfoByGroupNumber(toolGroup, false);
         //      Integer toolbarGroupCount = getToolbarGroupCount(toolGroup);
         if (toolbarInfo == null || toolbarInfo.getCount() == 0)
         {
            // add the new toolGroup to the hashmap
            //toolbarGroupsCount.put(toolGroup, 0);
            setToolbarGroupCount(toolGroup, 0);

            // create a separator for the previous group
            int prevGroup = getPrevGroup(toolGroup);

            if (prevGroup >= 0)
            {
               //when there is already sep on the prev group, create the setp on the current group
               if (getToolbarGroupMenuEntrySep(prevGroup) != null)
                  needToCreateAtTheEnd = true;
               else
                  createSepOnGroup(mgMenu, prevGroup);
            }
            else if (getToolbarGroupMenuEntrySep(toolGroup) == null)
            {
               //even if single toolgroup is present, add separator at the end, same as online
               needToCreateAtTheEnd = true;
            }
         }
         return needToCreateAtTheEnd;
      }

      /// <summary>
      ///   Loop on all form's controls and refresh the contex menu.
      /// </summary>
      internal void refreshContextMenuForControls()
      {
         int i = 0;
         MgControlBase ctrl;
         Property prop = null;

         for (i = 0; i < CtrlTab.getSize(); i++)
         {
            ctrl = CtrlTab.getCtrl(i);
            prop = ctrl.getProp(PropInterface.PROP_TYPE_CONTEXT_MENU);
            if (prop != null)
               prop.RefreshDisplay(true);
         }
      }

      /// <summary>
      ///   gets record from gui level
      /// </summary>
      virtual public int getTopIndexFromGUI()
      {
         Debug.Assert(Misc.IsWorkThread());

         int topDisplayLine = 0;
         if (hasTableOrTree())
            topDisplayLine = Commands.getTopIndex(getMainControl());
         return topDisplayLine;
      }

      /// <summary>
      ///   set tree control
      /// </summary>
      /// <param name = "control"></param>
      internal void setTreeCtrl(MgControlBase control)
      {
         _treeMgControl = control;
      }

      /// <param name = "inheritingControl"></param>
      internal void addControlToInheritingContextControls(MgControlBase inheritingControl)
      {
         if (!(inheritingControl is MgStatusBar) && !_controlsInheritingContext.Contains(inheritingControl))
            _controlsInheritingContext.Add(inheritingControl);
      }

      /// <summary>
      ///   compute column width
      ///   needed to work like in online in uom2pix calculations
      /// </summary>
      /// <param name = "layer">columns layer</param>
      /// <returns> column width in pixels</returns>
      internal int computeColumnWidth(int layer)
      {
         int width = 0, currentWidth = 0;
         for (int i = 0; i < layer; i++)
         {
            MgControlBase columnCtrl = _tableColumns[i];
            int colWidth = columnCtrl.getProp(PropInterface.PROP_TYPE_WIDTH).getValueInt();
            width += colWidth;
            if (columnCtrl.getLayer() == layer)
               currentWidth = colWidth;
         }
         int tableLeft = _tableMgControl.getProp(PropInterface.PROP_TYPE_LEFT).getValueInt();
         int pixWidth = uom2pix(tableLeft + width, true) - uom2pix(tableLeft + width - currentWidth, true);
         return pixWidth;
      }

      /// <summary>
      ///   get horizontal factor
      /// </summary>
      /// <returns></returns>
      internal int getHorizontalFactor()
      {
         if (_horizontalFactor == -1)
            _horizontalFactor = getProp(PropInterface.PROP_TYPE_HOR_FAC).getValueInt();
         return _horizontalFactor;
      }

      /// <summary>
      ///   This method returns the pulldown menu number of the form.
      ///   In case the form does not have a pulldown menu defined, the system pulldown menu is returned
      /// </summary>
      /// <returns></returns>
      public int getPulldownMenuNumber()
      {

         int pulldownMenu = (GetComputedProperty(PropInterface.PROP_TYPE_PULLDOWN_MENU) != null
                            ? GetComputedProperty(PropInterface.PROP_TYPE_PULLDOWN_MENU).GetComputedValueInteger()
                            : 0);

         return pulldownMenu;
      }

      /// <summary>
      ///   This method returns the pulldown menu number of the form.
      ///   In case the form does not have a pulldown menu defined, the system pulldown menu is returned
      /// </summary>
      /// <returns></returns>
      public void setPulldownMenuNumber(int idx, bool refresh)
      {
         var num = new NUM_TYPE();

         num.NUM_4_LONG(idx);
         String numString = num.toXMLrecord();

         setProp(PropInterface.PROP_TYPE_PULLDOWN_MENU, numString);

         if (refresh)
         {
            getProp(PropInterface.PROP_TYPE_PULLDOWN_MENU).RefreshDisplay(true, Int32.MinValue, false);
            if (idx == 0)
               toolbarGroupsCountClear();
         }
      }

      /// <summary>
      /// </summary>
      /// <returns> table items count</returns>
      internal int getTableItemsCount()
      {
         return _tableItemsCount;
      }

      /// <summary>
      ///   get vertical factor
      /// </summary>
      /// <returns></returns>
      internal int getVerticalFactor()
      {
         if (_verticalFactor == -1)
            _verticalFactor = getProp(PropInterface.PROP_TYPE_VER_FAC).getValueInt();
         return _verticalFactor;
      }

      /// <param name = "propVal"></param>
      internal void refreshContextMenuOnLinkedControls(int propVal)
      {
         var num = new NUM_TYPE();

         if (_controlsInheritingContext != null && _controlsInheritingContext.Count > 0)
         {
            num.NUM_4_LONG(propVal);
            String numString = num.toXMLrecord();
            MgControlBase ctrl = null;
            int i = 0;
            for (i = 0; i < _controlsInheritingContext.Count; i++)
            {
               ctrl = _controlsInheritingContext[i];
               ctrl.setProp(PropInterface.PROP_TYPE_CONTEXT_MENU, numString);
               ctrl.getProp(PropInterface.PROP_TYPE_CONTEXT_MENU).RefreshDisplay(true);
            }
         }
      }

      /// <param name = "inheritingControl"></param>
      internal void removeControlFromInheritingContextControls(MgControlBase inheritingControl)
      {
         if (_controlsInheritingContext != null && _controlsInheritingContext.Contains(inheritingControl))
            _controlsInheritingContext.Remove(inheritingControl);
      }

      /// <summary>
      ///   remove references to the controls of this form
      /// </summary>
      public virtual void removeRefsToCtrls()
      {
         int i;

         for (i = 0; i < CtrlTab.getSize(); i++)
            CtrlTab.getCtrl(i).removeRefFromField();
      }

      /// <summary>
      ///   set mgTree
      /// </summary>
      /// <param name = "mgTree"></param>
      public void setMgTree(MgTreeBase mgTree)
      {
         _mgTree = mgTree;
      }

      /// <summary>
      ///   build list of table children
      /// </summary>
      internal void buildTableChildren()
      {
         if (_tableChildren == null)
         {
            _tableChildren = new List<MgControlBase>();

            if (CtrlTab != null)
            {
               int minTabOrder = int.MaxValue;
               bool automaticTabbingOrder = isAutomaticTabbingOrder();

               for (int i = 0; i < CtrlTab.getSize(); i++)
               {
                  MgControlBase control = CtrlTab.getCtrl(i);

                  if (control.IsRepeatable || control.IsTableHeaderChild)
                  {
                     _tableChildren.Add(control);

                     if (automaticTabbingOrder)
                     {
                        Property prop = control.getProp(PropInterface.PROP_TYPE_TAB_ORDER);
                        if (prop != null)
                        {
                           int tabOrder = prop.getValueInt();
                           if (tabOrder < minTabOrder)
                              minTabOrder = tabOrder;
                        }
                     }
                  }
               }

               if (minTabOrder != int.MaxValue && automaticTabbingOrder)
                  _firstTableTabOrder = minTabOrder;
            }
         }
      }

      /// <summary>
      ///   compute and send to Gui level original column position : 
      ///   x coordinate of were column starts (in RTL - distance between table's right corner and columns right corner
      /// </summary>
      /// <param name = "columnIdx"></param>
      internal void setColumnOrgPos(int columnIdx)
      {
         int pos = 0;
         if (_tableMgControl.getProp(PropInterface.PROP_TYPE_HEBREW).getValueBoolean())
         {
            int logSize = 0;
            for (int i = _tableColumns.Count - 1; i >= 0; i--)
            {
               MgControlBase mgCtrl = _tableColumns[i];
               int width = mgCtrl.getOrgWidth();
               if (i > columnIdx)
                  pos += width;
               logSize += width;
            }
            int displayWidth = Math.Max(logSize, _tableMgControl.getOrgWidth());
            pos = displayWidth - pos;
         }
         else
         {
            for (int i = 0; i < columnIdx; i++)
            {
               MgControlBase mgCtrl = _tableColumns[i];
               pos += mgCtrl.getOrgWidth();
            }
         }
         pos = uom2pix(pos, true);
         Commands.addAsync(CommandType.SET_COLUMN_START_POS, _tableColumns[columnIdx], 0, pos);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal List<GuiMgControl> getGuiTableChildren()
      {
         if (_guiTableChildren == null)
         {
            _guiTableChildren = new List<GuiMgControl>();

            List<MgControlBase> children = TableChildren;
            foreach (MgControlBase child in children)
               _guiTableChildren.Add(child);
         }
         return _guiTableChildren;
      }

      /// <summary>
      ///   Sets the table control related to this form
      /// </summary>
      /// <param name = "tableCtrl">the tableCtrl to set</param>
      internal void setTableCtrl(MgControlBase tableCtrl)
      {
         _tableMgControl = tableCtrl;
      }

      /// <param name = "value">the subFormCtrl to set</param>
      internal void setFrameFormCtrl(MgControlBase value)
      {
         Debug.Assert(_frameFormCtrl == null && value != null);
         _frameFormCtrl = value;
      }

      /// <param name = "value">control to set</param>
      internal void setContainerCtrl(MgControlBase value)
      {
         Debug.Assert(_containerCtrl == null && value != null);
         _containerCtrl = value;
      }

      /// <returns></returns>
      public int getCtrlCount()
      {
         return CtrlTab.getSize();
      }

      /// <summary>
      /// Gets the size of the controls excluding controls related to StatusBar
      /// </summary>
      /// <returns></returns>
      public int GetControlsCountExcludingStatusBar()
      {
         return CtrlTab.GetControls(x => { return (!x.IsStatusBar() && !x.IsStatusPane()); }).Count;
      }

      /// <summary>
      ///   copy text property from fromControl to toControl and ,match cursor pos and selection we can't work with
      ///   the GuiInteractive because we need to need to the command type, refresh the text and then do focus on it
      /// </summary>
      /// <param name = "fromControl"></param>
      /// <param name = "toControl"></param>
      private void matchTextData(MgControlBase fromControl, MgControlBase toControl)
      {
         // 1. Copy the text from fromControl TO toControl.
         toControl.copyValFrom(fromControl);

         //2. Set ModifiedByUser flag (needed for saving data in the field and ACT_CANCEL confirm dialog)
         toControl.ModifiedByUser = fromControl.ModifiedByUser;

         // 3. set the caret to be on the same char when it was on wideParentControl,
         // and select the same text that was select)
         MgPoint selection = Manager.SelectionGet(fromControl);
         Manager.SetMark(toControl, selection.x, selection.y);
      }

      /// <summary>
      ///   Refresh form display if it has a property which is an expression
      /// </summary>
      public void refreshPropsOnExpression()
      {
         int i;
         int size = (_propTab == null
                     ? 0
                     : _propTab.getSize());
         Property prop;
         bool refresh = false;

         for (i = 0; i < size && !refresh; i++)
         {
            prop = _propTab.getProp(i);

            if (prop.isExpression())
            {
               refresh = true;
               refreshProps();
            }
         }
      }

      /// <summary> Sets DcValId on a Control. </summary>
      /// <param name="ditIdx">0-based index identifying a control.</param>
      /// <param name="refreshControl"> indicates whether to refresh the control or not. </param>
      public void setDCValIdOnControl(int ditIdx, int dcValId, bool refreshControl)
      {
         MgControlBase mgControlBase = getCtrl(ditIdx);
         mgControlBase.setDcValId(dcValId);
         if (refreshControl)
            mgControlBase.RefreshDisplay(false);
      }

      /// <summary>
      ///   init the wide info.
      /// </summary>
      /// <param name = "parentControl"></param>
      internal void initWideinfo(MgControlBase parentControl)
      {
         //save data about the parent mg control
         _wideParentControl = parentControl;
      }

      /// <summary>
      ///   get the first menu that is enable that belong to the keybordItem
      /// </summary>
      /// <param name = "kbItm"></param>
      /// <param name = "menuStyle"></param>
      /// <returns></returns>
      public MenuEntry getMenuEntrybyAccessKey(KeyboardItem kbItm, MenuStyle menuStyle)
      {
         MenuEntry menuEntry = null;

         var mgMenu = (MgMenu)_instatiatedMenus[menuStyle];
         if (mgMenu != null)
         {
            List<MenuEntry> menuEntries = mgMenu.getMenuEntriesWithAccessKey(kbItm);
            if (menuEntries != null && menuEntries.Count > 0)
               for (int i = 0; i < menuEntries.Count; i++)
               {
                  MenuEntry currMenuEntry = menuEntries[i];
                  if (currMenuEntry.getEnabled())
                  {
                     menuEntry = currMenuEntry;
                     break;
                  }
               }
         }

         return menuEntry;
      }

      /// <summary>
      ///   get rectangle of the wide to be open (in UOM)
      /// </summary>
      /// <returns> MgRectangle in of the wide related to the desktop in UOM
      ///   in magic :edt_mle.cpp \ pop_drop()
      /// </returns>
      internal MgRectangle getWideBounds()
      {
         int dataLen = _wideParentControl.getPIC().getSize();
         var wideRect = new MgRectangle(0, 0, 0, 0);
         var rect = new MgRectangle(0, 0, 0, 0);
         var parentRect = new MgRectangle(0, 0, 0, 0);
         int x, y, dx, dy;
         Object parentObject = this;

         // 1. get the bounds of the caller control
         Object reletiveTo = this;
         if (_wideParentControl.getForm().isSubForm())
            reletiveTo = _wideParentControl.getForm().getSubFormCtrl();
         else if (IsFrameSet)
            parentObject = reletiveTo = getContainerCtrl();

         Commands.getBoundsRelativeTo(_wideParentControl, _wideParentControl.getDisplayLine(true), rect, reletiveTo);

         // 2. get the bounds of the form  
         if (_wideParentControl.getForm().isSubForm())
            parentObject = _wideParentControl.getForm().getSubFormCtrl();

         Commands.getClientBounds(parentObject, parentRect, false);

         //Get the font of the parent control
         MgPointF parentCtrlFontSize = new MgPointF(0, 0);
         int fontId = _wideParentControl.getProp(PropInterface.PROP_TYPE_FONT).getValueInt();
         MgFont mgFont = Manager.GetFontsTable().getFont(fontId);

         // get the parent control font size
         parentCtrlFontSize = Commands.getFontMetrics(mgFont, this);

         // 3. Compute width & height of popup window       
         // check the characters to be display on one line, max 60 chars and min 5 chars
         int Characters = Math.Max(5, (int)Math.Min(60, (parentRect.width - 2) / parentCtrlFontSize.x));
         dx = (int)(Characters * parentCtrlFontSize.x);

         // 4. Compute horizontal position of child 
         x = rect.x - 5 - 1;
         if (x + dx > parentRect.width)
            x = Math.Max(0, parentRect.width - dx - 10);

         x = Math.Max(x, 0);

         dy = (dataLen / Characters) + 1;
         dy = (int)((dy * parentCtrlFontSize.y) + 6 + 2 + 2); // add 2 for large font in english window (ehud ?? )

         // 5. Compute vertical position of child
         if (rect.y + dy > parentRect.height)
         {
            // If the Wide window cannot be accomodated below the parent control
            // within the parent window, show it above the parent control
            y = Math.Max(0, rect.y - dy);
         }
         else
            y = rect.y + rect.height;

         y = Math.Max(y, 0);

         // 6. If the height of wide window is bigger than the parent's height
         //    the wide window will be clipped in its bottom.  It happens in URL dialog.         
         if (y == 0 && parentRect.height < dy)
            dy -= (int)((dy - parentRect.height + parentCtrlFontSize.y - 1) / parentCtrlFontSize.y * parentCtrlFontSize.y);

         //7. calculate the rect into UOM : according to the form of the wide
         wideRect.x = _wideParentControl.getForm().pix2uom(x, true);
         wideRect.y = _wideParentControl.getForm().pix2uom(y, false);
         wideRect.width = _wideParentControl.getForm().pix2uom(dx, true);
         wideRect.height = _wideParentControl.getForm().pix2uom(dy, false);


         return (wideRect);
      }

      /// <summary>
      ///   create the wide control, according to the send rect wide
      /// </summary>
      /// <param name = "rectWide"></param>
      internal void createWideControl(MgRectangle rectWide)
      {
         int parentControlIdx = getWideParentControlIndex(_wideParentControl);
         _wideControl = ConstructMgControl(MgControlType.CTRL_TYPE_TEXT, _wideParentControl.getForm().getTask(), parentControlIdx);
         CtrlTab.addControl(_wideControl);
         _wideControl.setWideProperties(_wideParentControl.getForm(), _wideParentControl, rectWide);
      }

      /// <summary>
      ///   get the parent control index of the wide control
      /// </summary>
      /// <param name = "wideParentControl"></param>
      /// <returns></returns>
      private int getWideParentControlIndex(MgControlBase wideParentControl)
      {
         int parentControlIdx = -1;
         MgControlBase parentMgControl = null;
         // while we on frame set and it and the wideParentControl isn't subform, it mean the control is on Frame Form
         // we need to create the control under the FrameFormControl
         if (IsFrameSet && !wideParentControl.getForm().isSubForm())
         {
            parentMgControl = getContainerCtrl();
            parentControlIdx = parentMgControl.getDitIdx();
         }

         return parentControlIdx;
      }

      /// <summary>
      /// Close internal Help.
      /// </summary>
      public void CloseInternalHelp()
      {
         //Closing the internal help window.
         if (_internalHelpWindow != null)
         {
            Commands.addAsync(CommandType.CLOSE_FORM, _internalHelpWindow);
            Commands.beginInvoke();
            _internalHelpWindow = null;
         }
      }
      /// <summary>
      /// show internal help in a self created form.
      /// </summary>
      /// <param name="hlpObject">Help object containing help details</param>
      public void ShowInternalHelp(MagicHelp hlpObject)
      {
         _internalHelpWindow = null;
         WindowType helpWindowType = WindowType.Default;
         var num = new NUM_TYPE();

         //Get help object.
         InternalHelp internalHelpObj = (InternalHelp)hlpObject;

         //Constructing help window.
         bool alreadySetParentForm = false;
         _internalHelpWindow = getTask().ConstructMgForm(ref alreadySetParentForm);
         _internalHelpWindow.IsHelpWindow = true;
         _internalHelpWindow.Opened = true;
         _internalHelpWindow.ParentForm = getTask().getTopMostForm();

         //Creating edit box.
         MgControlBase txtCtrl = _internalHelpWindow.ConstructMgControl(MgControlType.CTRL_TYPE_TEXT, _internalHelpWindow, -1);

         //Add control to parent form control table.
         _internalHelpWindow.CtrlTab.addControl(txtCtrl);

         //Setting the properties of internal help window.
         num.NUM_4_LONG(internalHelpObj.FactorX);
         _internalHelpWindow.setProp(PropInterface.PROP_TYPE_HOR_FAC, num.toXMLrecord());
         num.NUM_4_LONG(internalHelpObj.FactorY);
         _internalHelpWindow.setProp(PropInterface.PROP_TYPE_VER_FAC, num.toXMLrecord());
         num.NUM_4_LONG(internalHelpObj.FontTableIndex);
         _internalHelpWindow.setProp(PropInterface.PROP_TYPE_FONT, num.toXMLrecord());
         num.NUM_4_LONG((int)WindowType.Default);
         _internalHelpWindow.setProp(PropInterface.PROP_TYPE_WINDOW_TYPE, num.toXMLrecord());

         //Instead of Deriving Window type from parent window,  Set window type for Internal help as Floating.
         //so that there should not be any focussing issues for help window if the parent window is MDI frame. 
         helpWindowType = WindowType.Floating;

         //If MDI is enabled then set the parent window accordingly.
         var parentForm = (IsMDIChild
                              ? Manager.GetCurrentRuntimeContext().FrameForm
                              : getTask().getTopMostForm());

         //GUI commands for creating the internal help window.
         Commands.addAsync(CommandType.CREATE_FORM, parentForm, _internalHelpWindow, helpWindowType, internalHelpObj.Name, true, false, false);

         //Before setting the bounds, change the startup position to customized.
         Commands.addAsync(CommandType.PROP_SET_STARTUP_POSITION, _internalHelpWindow, 0, WindowPosition.Customized);
         Commands.addAsync(CommandType.PROP_SET_BOUNDS, _internalHelpWindow, 0, _internalHelpWindow.uom2pix(internalHelpObj.FrameX, true), _internalHelpWindow.uom2pix(internalHelpObj.FrameY, false), _internalHelpWindow.uom2pix(internalHelpObj.SizedX, true), _internalHelpWindow.uom2pix(internalHelpObj.SizedY, false), false, false);

         Commands.addAsync(CommandType.PROP_SET_TEXT, _internalHelpWindow, 0, internalHelpObj.Name, 0);
         // #932179: Set titlebar
         Commands.addAsync(CommandType.PROP_SET_TITLE_BAR, (object)_internalHelpWindow, internalHelpObj.TitleBar == 1 ? true : false);
         //Set the system menu.
         Commands.addAsync(CommandType.PROP_SET_SYSTEM_MENU, (object)_internalHelpWindow, internalHelpObj.SystemMenu == 1 ? true : false);

         Commands.addAsync(CommandType.PROP_SET_MAXBOX, (object)_internalHelpWindow, false);
         Commands.addAsync(CommandType.PROP_SET_MINBOX, (object)_internalHelpWindow, false);

         Commands.addAsync(CommandType.PROP_SET_ICON_FILE_NAME, _internalHelpWindow, 0, "@Mgxpa", 0);

         //Open the form.
         Commands.addAsync(CommandType.INITIAL_FORM_LAYOUT, (object)_internalHelpWindow, false, internalHelpObj.Name);
         Commands.addAsync(CommandType.SHOW_FORM, (object)_internalHelpWindow, false, true, internalHelpObj.Name);

         //Creating edit control for showing help text.We are creating an edit control so that we can show large help strings with scrolling
         //and also we can set the focus on the edit control, so that user can copy\paste the help text.
         //The edit control will NOT be logical control (forceWindowControl = true) since there is only one control in help window, there is no
         //point in creating logical control.
         Commands.addAsync(CommandType.CREATE_EDIT, (object)_internalHelpWindow, txtCtrl, 0, 0, null, null, 0, false, true,
                                                                                                    0, null, 0, null, true, DockingStyle.FILL);
         Commands.addAsync(CommandType.PROP_SET_BOUNDS, txtCtrl, 0, 0, 0, _internalHelpWindow.uom2pix(internalHelpObj.SizedX, true), _internalHelpWindow.uom2pix(internalHelpObj.SizedY, false), false, false);
         Commands.addAsync(CommandType.PROP_SET_READ_ONLY, (object)txtCtrl, true);
         Commands.addAsync(CommandType.PROP_SET_VISIBLE, (object)txtCtrl, true);
         Commands.addAsync(CommandType.PROP_SET_MULTILINE, (object)txtCtrl, true);

         //Get the font info.
         MgFont hlpTxtFont = Manager.GetFontsTable().getFont(internalHelpObj.FontTableIndex);
         Commands.addAsync(CommandType.PROP_SET_FONT, (object)txtCtrl, 0, hlpTxtFont, internalHelpObj.FontTableIndex);

         Commands.addAsync(CommandType.SET_FOCUS, (object)txtCtrl);
         Commands.addAsync(CommandType.PROP_SET_TEXT, (object)txtCtrl, 0, internalHelpObj.val, 0);

         Commands.beginInvoke();
      }

      /// <summary>Writes the string or sets the image on the pane with the specified index.</summary>
      /// <param name="paneIdx">Index of the pane to be updated</param>
      /// <param name="strInfo">Message</param>
      /// <param name="soundBeep">Sound beep or not</param>
      public virtual void UpdateStatusBar(int paneIdx, string strInfo, bool soundBeep)
      {
#if !PocketPC
         MgStatusBar statusBar = null;
         MgFormBase form = getForm();

         if (form != null)
            statusBar = form.GetStatusBar(true);

         if (statusBar != null)
         {
            statusBar.UpdatePaneContent(paneIdx, Manager.GetMessage(strInfo));
            if (soundBeep)
               Manager.Beep();
         }
         else
            Events.WriteDevToLog("UpdateStatusBar invoked while there's no status bar in the current form.");
#endif
      }

      /// <summary>
      /// Writes the tool tip to the specified pane of the status bar.
      /// </summary>
      /// <param name="paneIdx">Pane Idx</param>
      /// <param name="strToolTipText">Tool tip text</param>
      public void UpdateStatusPaneTooltip(int paneIdx, string strToolTipText)
      {
         MgStatusBar statusBar = null;
         MgFormBase form = getForm();

         if (form != null)
            statusBar = form.GetStatusBar(true);

         if (statusBar != null)
            statusBar.UpdatePaneToolTip(paneIdx, strToolTipText);
      }

      /// <summary> Suspend form layout </summary>
      public void SuspendLayout()
      {
         Commands.addAsync(CommandType.SUSPEND_LAYOUT, getTopMostForm());
      }

      /// <summary> Resume form layout </summary>
      public void ResumeLayout()
      {
         Commands.addAsync(CommandType.RESUME_LAYOUT, getTopMostForm());
      }

      /// <summary>
      ///   find tab control for performing CTRL+TAB && CTRL+SHIFT+TAB actions
      ///   the code original is in RT::skip_to_tab_control
      /// </summary>
      /// <param name = "ctrl">current control</param>
      /// <returns></returns>
      public MgControlBase getTabControl(MgControlBase ctrl)
      {
         Object obj = null;
         MgControlBase result = null;

         obj = ctrl;
         while (obj is MgControlBase)
         {
            var currCtrl = (MgControlBase)obj;
            if (currCtrl.isTabControl())
            {
               result = currCtrl;
               break;
            }
            obj = currCtrl.getParent();
         }

         // if ctrl is not tab and not tab's child - find first tab in the form
         if (result == null)
         {
            for (int i = 0; i < CtrlTab.getSize(); i++)
            {
               obj = CtrlTab.getCtrl(i);
               if (((MgControlBase)obj).isTabControl())
               {
                  result = (MgControlBase)obj;
                  break;
               }
            }
         }

         return result;
      }

      /// <returns> if there is browser control on the form</returns>
      internal bool hasBrowserControl()
      {
         bool hasBrowserCtrl = false;
         MgControlBase ctrl;

         for (int i = 0; i < CtrlTab.getSize() && !hasBrowserCtrl; i++)
         {
            ctrl = CtrlTab.getCtrl(i);
            if (ctrl.isBrowserControl())
               hasBrowserCtrl = true;
         }

         return hasBrowserCtrl;
      }

      /// <summary> Returns the startup position </summary>
      /// <returns>WindowPosition - startup position</returns>
      protected internal WindowPosition GetStartupPosition()
      {
         WindowPosition startupPos = WindowPosition.Customized;

         if (_propTab != null)
         {
            Property prop = _propTab.getPropById(PropInterface.PROP_TYPE_STARTUP_POSITION);
            if (prop != null)
               startupPos = (WindowPosition)prop.getValueInt();

            //for mdichild and WIN_POS_CENTERED_TO_PARENT it is handled in startupPosition()
            switch (startupPos)
            {
               case WindowPosition.DefaultBounds:
                  if (ConcreteWindowType == WindowType.ChildWindow)
                     startupPos = WindowPosition.Customized;
                  break;

               case WindowPosition.CenteredToMagic:
                  if (Manager.GetCurrentRuntimeContext().FrameForm == null)
                     startupPos = WindowPosition.CenteredToParent;
                  if (ParentForm == null)
                     startupPos = WindowPosition.CenteredToDesktop;
                  break;

               case WindowPosition.CenteredToParent:
                  if (ParentForm == null)
                     startupPos = WindowPosition.CenteredToDesktop;
                  break;
            }
         }

         return startupPos;
      }

      /// <summary> Applies the placement to the specified child form, if the 
      /// current form's dimensions has changed. </summary>
      /// <param name="childForm"></param>
      internal void ApplyChildWindowPlacement(MgFormBase childForm)
      {
         if (childForm.IsChildWindow)
         {
            MgRectangle orgRectangle = Property.getOrgRect(this);
            MgRectangle currentRectangle = new MgRectangle();
            Object parentObj = this;

            if (this.isSubForm())
               parentObj = this.getSubFormCtrl();

            Commands.getClientBounds(parentObj, currentRectangle, true);

            MgPoint sizeDifference = currentRectangle.GetSizeDifferenceFrom(orgRectangle);

            if (sizeDifference.x != 0 || sizeDifference.y != 0)
               Commands.addAsync(CommandType.APPLY_CHILD_WINDOW_PLACEMENT, childForm, 0, sizeDifference.x, sizeDifference.y);
         }
      }

      /// <summary>
      /// Set the toolbar
      /// </summary>
      public void SetToolBar()
      {
         Debug.Assert(IsMDIFrame);
         if (ShouldCreateToolbar && _toolbarGroupsCount.Count > 0)
         {
            MgMenu pullDownMenu = getPulldownMenu();
            if (pullDownMenu != null) // Pulldown menu can be null, when Display Toolbar = YES.
            {
               Commands.addAsync(CommandType.SET_TOOLBAR, this, pullDownMenu.createAndGetToolbar(this));
               Commands.beginInvoke();
            }
         }
      }


      public Dictionary<string, string> GetListOfInputControls()
      {
         Dictionary<string, string> names = new Dictionary<string, string>();
         foreach (var control in CtrlTab.GetControls(c => IsInputType(c.Type)))
         {           
            names.Add(control.UniqueWebId, control.IsRepeatable? "1":"0");
         }
         return names;
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

      #region abstract and virtual methods

      /// <summary>
      /// constructs an object of MgControl
      /// </summary>
      /// <param name="addToMap"></param>
      /// <returns></returns>
      public abstract MgControlBase ConstructMgControl();

      /// <summary>
      /// constructs an object of MgControl
      /// </summary>
      /// <param name="type"></param>
      /// <param name="task"></param>
      /// <param name="parentControl"></param>
      /// <returns></returns>
      public abstract MgControlBase ConstructMgControl(MgControlType type, TaskBase task, int parentControl);


      /// <summary>
      /// constructs an object of MgControl
      /// </summary>
      /// <param name="type"></param>
      /// <param name="parentMgForm">Parent form</param>
      /// <param name="parentControl">Parent control IDX</param>
      /// <returns>mg control</returns>
      protected abstract MgControlBase ConstructMgControl(MgControlType type, MgFormBase parentMgForm, int parentControlIdx);

      /// <summary>
      ///   build the controls tabbing order of this form
      /// </summary>
      protected virtual void buildTabbingOrder()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      protected virtual void UpdateTabbingOrderFromRuntimeDesigner()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      protected virtual void UpdateHiddenControlsList()
      {
      }

      /// <summary>
      ///   close the wide and return the focus to the caller control FI : we can't call this from the ShellLisiner
      ///   !!!, (Due this method is send command type(text,focus, select text...) to the guiCommandQueue, the task
      ///   is close and the runnable is ger to the implements methode(Se text) on object that isn't exist any more
      ///   (it was closed) and the data MUST update the caller control.
      /// </summary>
      public virtual void closeWide()
      {
         if (wideIsOpen())
         {
            //1. Copy the text to the parent control
            matchTextData(_wideControl, _wideParentControl);
            _wideControl.InControl = false;
            _wideControl.ClipBoardDataExists = false;
            Manager.SetFocus(_wideParentControl, -1);

            //2. restore the the field parent control       
            _wideControl.removeRefFromField();
            _wideParentControl.setField(_wideControl.getField());
            _wideParentControl.setControlToFocus();

            //3. return the focus to the parent control
            _wideParentControl.getForm().getTask().setLastParkedCtrl(_wideParentControl);

            //4. remove wide control from the form  
            removeWideControl();

            if (_wideParentControl.isTreeControl())
               Manager.EventsManager.addInternalEvent(_wideParentControl, DisplayLine,
                                                      InternalInterface.MG_ACT_TREE_RENAME_RT);

            //7. execute the commands
            Commands.beginInvoke();
         }
      }

      /// <summary>
      ///   refresh table first time we enter the ownerTask
      ///   get rows number form gui level
      /// </summary>
      public virtual void firstTableRefresh()
      {
         if (_tableMgControl != null)
         {
            _rowsInPage = Commands.getRowsInPage(_tableMgControl);
            RefreshRepeatableAllowed = true;
         }
      }

      /// <summary>
      ///   create a wide form & control
      /// </summary>
      /// <param name = "parentControl">: the control that call the wide</param>
      protected internal virtual void openWide(MgControlBase parentControl)
      {
         //1. init data for the wide          
         initWideinfo(parentControl);

         //2. calculate the wide window rect
         MgRectangle rectWide = getWideBounds();

         //3. create the wide control
         createWideControl(rectWide);

         //4. set the focus to the wide control                  
         parentControl.getForm().getTask().setLastParkedCtrl(_wideControl);

         //5. create the form and the wide control 
         _wideControl.createControl(true);

         //6. refresh wide control properties & wide form properties
         _wideControl.RefreshDisplay();

         //7. set text for the wide control from the parent control
         // we can't call wideParentControl.getValue(), because when user press chars and then wide action, the
         // data isn't saved into the field yet so we work on control
         matchTextData(_wideParentControl, _wideControl);
         Manager.SetFocus(_wideControl, -1);
         _wideControl.InControl = true;

         //8. set the wide control to be top
         Commands.addAsync(CommandType.MOVE_ABOVE, _wideControl);

         //9. execute the commands type
         Commands.beginInvoke();
      }

      /// <summary>
      ///   update table item's count according to parameter size
      /// </summary>
      /// <param name = "size">new size</param>
      /// <param name = "removeAll">remove all elements form the table including first and last dummy elements</param>
      public virtual void SetTableItemsCount(int size, bool removeAll)
      {
         InitTableControl(size, removeAll);
      }

      /// <summary>
      /// inits the table control
      /// </summary>
      protected virtual void InitTableControl()
      {
         InitTableControl(1, false);
      }

      /// <summary>
      ///   update table item's count according to parameter size
      /// </summary>
      /// <param name = "size">new size</param>
      /// <param name = "removeAll">remove all elements form the table including first and last dummy elements</param>
      protected virtual void InitTableControl(int size, bool removeAll)
      {
         if (_tableMgControl != null)
         {
            Commands.addAsync(CommandType.SET_TABLE_ITEMS_COUNT, _tableMgControl, 0, size);
            _tableItemsCount = size;
            Rows.SetSize(size);
            // if (prevSelIndex >= size || prevTopIndex >= size)
            // {
            _prevTopIndex = -2;
            _prevSelIndex = -2;
            // }
            // set repeatable controls arrays to the size
            UpdateTableChildrenArraysSize(size);
         }
      }

      /// <summary>
      /// update arrays of table children controls
      /// </summary>
      /// <param name="size"></param>
      public void UpdateTableChildrenArraysSize(int size)
      {
         List<MgControlBase> children = TableChildren;
         if (children != null)
         {
            for (int i = 0; i < children.Count; i++)
            {
               MgControlBase control = children[i];
               control.updateArrays(size);
            }
         }
         // update the size now, so previous row colors array will be created
         if (_tableMgControl != null)
            _tableMgControl.updateArrays(size);
      }

      /// <summary>
      /// saves the userstate of the form
      /// </summary>
      public void SaveUserState()
      {
         // save form's state
         if (!FormUserState.GetInstance().IsDisabled)
            FormUserState.GetInstance().Save(this);
      }

      /// <summary>
      /// Get the appropriate window type for a Default window type.
      /// </summary>
      /// <param name="windowType"></param>
      /// <returns></returns>
      public virtual WindowType GetConcreteWindowTypeForMDIChild(bool isFitToMdi)
      {
         if (isFitToMdi)
            return WindowType.FitToMdi;
         else
            return WindowType.Default;
      }

      /// <summary>
      /// For online we need to pit 'ACT_HIT' when the form was closed from system menu
      /// if the task has only subform control on it.
      /// For RC always return false
      /// </summary>
      /// <returns></returns>
      public virtual bool ShouldPutActOnFormClose()
      {
         return false;
      }

      /// <summary>
      /// Gets the statusbar for current context. Only for OL.
      /// </summary>
      /// <returns></returns>
      public virtual MgStatusBar GetStatusBarFromContext()
      {
         return null;
      }

      /// <summary>
      /// refresh property with expression
      /// </summary>
      /// <param name="propId"></param>
      public void RefreshPropertyByExpression(int propId)
      {
         if (PropertyHasExpression(propId))
            getProp(propId).RefreshDisplay(true);
      }

      /// <summary>
      /// Returns the parent form.
      /// </summary>
      /// <returns></returns>
      protected virtual Object GetParentForm()
      {
         MgControlBase parentControl = null;
         MgFormBase parentForm = ParentForm;

         if (parentForm != null)
         {
            // for a subform the parent control must be the subform control
            if (parentForm.isSubForm())
               parentControl = parentForm.getSubFormCtrl();
            // for a form frame the parent control must be its container.
            // it is important for a child window task that is opened from a form frame parent task.
            else if (parentForm.getContainerCtrl() != null)
               parentControl = parentForm.getContainerCtrl();
         }

         return (parentControl != null
                        ? (Object)parentControl
                        : (Object)parentForm);
      }

      #endregion

      #region Nested type: Row



      /// <summary>
      ///   class describing row
      /// </summary>
      /// <author>rinav</author>
      public class Row
      {
         public bool Created { get; private set; } //is row created on gui level
         public bool Validated { get; set; } //is row Validated         
         public ControlsData ControlsData { get; set; } = new ControlsData();

         protected Row()
         {
            Validated = true;
         }

         internal Row(bool created, bool validated)
         {
            Created = created;
            Validated = validated;
         }
      }
      #endregion

      public static bool IsFormTag(string tagName)
      {
         bool isFormTag = (tagName.Equals(XMLConstants.MG_TAG_FORM) ||
                            tagName.Equals(XMLConstants.MG_TAG_FORM_PROPERTIES));
         return isFormTag;
      }
      public static bool IsEndFormTag(string tagName)
      {
         bool isFormTag = tagName.Equals('/' + XMLConstants.MG_TAG_FORM) ||
                  tagName.Equals('/' + XMLConstants.MG_TAG_FORM_PROPERTIES);

         return isFormTag;
      }

      /// <summary>
      /// The method iterates through the form's controls and checks for each of the controls, whether it
      /// has the specified property in the property list and, if so, it creates a Property Refresh Rule using
      /// the specified factory and assigns it to the property's RefreshRule property.
      /// </summary>
      /// <param name="propertyId">The id of the property to which the rule applies.</param>
      /// <param name="rule">The rule to assign.</param>
      public void SetPropertyRefreshRule(int propertyId, IPropertyRefreshRule rule)
      {
         for (int i = 0, controlCnt = CtrlTab.getSize(); i < controlCnt; i++)
         {
            MgControlBase control = CtrlTab.getCtrl(i);
            Property property = control.getProp(propertyId);
            if (property != null)
               property.RefreshRule = rule;
         }
      }

      public override string ToString()
      {
         return "{" + GetType().Name + ": Id=" + _userStateId + "}";
      }

      /// <summary>
      /// create the dictionary of objects (form and controls) and the info for the designer 
      /// </summary>
      /// <returns></returns>
      internal Dictionary<object, ControlDesignerInfo> BuildStudioValuesDictionaryForForm()
      {
         Dictionary<object, ControlDesignerInfo> dict = new Dictionary<object, ControlDesignerInfo>();

         // add form info
         ControlDesignerInfo formInfo = new ControlDesignerInfo();
         formInfo.Id = 0;
         formInfo.Properties = BuildFormDesignerProperties();
         dict.Add(this, formInfo);
         //add controls
         BuildStudioValuesDictionaryForForm(dict);

         return dict;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dict"></param>
      private void BuildStudioValuesDictionaryForForm(Dictionary<object, ControlDesignerInfo> dict)
      {
         // add controls info
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            MgControlBase ctrl = CtrlTab.getCtrl(i);
            dict.Add(ctrl, ctrl.BuildStudioValuesDictionary());

            if (ctrl.isSubform())
            {
               MgFormBase sunformForm = ctrl.GetSubformMgForm();
               //subform can be type = none
               if (sunformForm != null)
                  sunformForm.BuildStudioValuesDictionaryForForm(dict);
            }
         }
      }

      /// <summary>
      /// build the properties dictionary for the runtime designer
      /// </summary>
      /// <returns></returns>
      private Dictionary<string, DesignerPropertyInfo> BuildFormDesignerProperties()
      {
         Dictionary<string, DesignerPropertyInfo> propsDict = new Dictionary<string, DesignerPropertyInfo>();

         propsDict.Add(Constants.ConfigurationFilePropertyName, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = GetControlsPersistencyFileName() });

         return propsDict;
      }

      /// <summary>
      /// get the runtime designer file name
      /// </summary>
      /// <returns></returns>
      public string GetControlsPersistencyFileName()
      {
         String controlsPersistencyPath = EnvControlsPersistencyPath.GetInstance().GetFullControlsPersistencyFileName(this);

         return controlsPersistencyPath;

      }

#if !PocketPC
      /// <summary>
      /// set the runtime designer info in MgControl properties
      /// </summary>
      private void SetDesignerInfoOnMgControls()
      {
         foreach (var item in designerInfoDictionary)
         {
            item.Key.SetDesignerPropertiesValues(item.Value);
         }
      }

      /// <summary>
      /// set the runtime designer info on the real controls, in the GUI thread
      /// </summary>
      private void SetDesignerInfoOnControls()
      {
         Commands.addAsync(CommandType.SET_DESIGNER_VALUES, designerInfoDictionary);
      }

      /// <summary>
      /// create the designer values dictionaries from the unserialized lists
      /// </summary>
      /// <param name="designerInfoList"></param>
      private Dictionary<MgControlBase, Dictionary<string, object>> BuildDesignerInfoDictionary(List<ControlItem> designerInfoList)
      {
         Dictionary<MgControlBase, Dictionary<string, object>> designerInfoDictionary = new Dictionary<MgControlBase, Dictionary<string, object>>();

         // for each control
         foreach (ControlItem item in designerInfoList)
         {
            // find the MgControl
            MgControlBase control = CtrlTab.GetControlByIsn(item.Isn);

            if (control == null)
               continue;

            if (!control.Type.ToString().Equals(item.ControlType))
            {
               Events.WriteDevToLog("Configuration file isn't match to the form, please clean it");
               return new Dictionary<MgControlBase, Dictionary<string, object>>(); ;
            }

            Dictionary<string, object> props = new Dictionary<string, object>();

            // build the properties dictionary
            foreach (PropertyItem propItem in item.Properties)
            {
               props.Add(propItem.Key, propItem.GetValue());
               if (propItem is TabOrderOffsetPropertyItem)
                  props.Add(propItem.Key + Constants.TabOrderPropertyTermination, ((TabOrderOffsetPropertyItem)propItem).GetOffsetValue());
            }

            if (!designerInfoDictionary.ContainsKey(control))
               designerInfoDictionary.Add(control, props);
         }

         return designerInfoDictionary;
      }

      /// <summary>
      /// rebuild the tab order and set the new values on controls
      /// </summary>
      private void RebuildTabIndex()
      {
         MgControlBase frameFormControl = getFrameFormCtrl();

         // get list of tabable controls
         // if the frame form control not exist get all controls       
         // if the frame form control exist get all controls except controls under frame form
         IList<MgControlBase> l = CtrlTab.GetControls(x => { return IncludeControlInTabbingOrder(x, frameFormControl == null); });
         // new list, to be sorted for new tab order
         List<MgControlBase> list = new List<MgControlBase>(l);
         list.Sort(new TabOrderComparer(this));

         // frame form exist 
         if (frameFormControl != null)
         {
            // get index of frame form control
            int frameFormControlIndex = GetFrameFormControlIndex(getFrameFormCtrl().ControlIsn, list);

            if (frameFormControlIndex > 0)
            {
               // get list of controls that EXIST under frame form control
               IList<MgControlBase> l2 = CtrlTab.GetControls(x => { return OnlyFrameFormChild(x); });
               // new list, to be sorted for new tab order
               List<MgControlBase> listonlyFrameFormChild = new List<MgControlBase>(l2);
               listonlyFrameFormChild.Sort(new TabOrderComparer(this));
               // marge the chidren of the frame form to be afther container control in tabe order
               list.InsertRange(frameFormControlIndex, listonlyFrameFormChild);
            }
         }

         // set the new tab-order values on the controls
         for (int i = 0; i < list.Count; i++)
         {
            list[i].getProp(PropInterface.PROP_TYPE_TAB_ORDER).SetValue(i + 1);
         }

      }


      /// <summary>
      /// return frame form display index on list (1..n)
      /// return 0 if the frame isn't exist
      /// </summary>
      /// <param name="frameFormControlIsn"></param>
      /// <param name="list  "></param>
      /// <returns></returns>
      private static int GetFrameFormControlIndex(long frameFormControlIsn, List<MgControlBase> list)
      {
         int frameFormControlIndex = 0;

         for (int i = 0; i < list.Count; i++)
         {
            if (list[i].ControlIsn == frameFormControlIsn)
               frameFormControlIndex = i + 1;
         }

         return frameFormControlIndex;
      }

      bool IsControlChildOfFrameForm(MgControlBase control)
      {
         bool isControlChildOfFrameForm = false;

         MgControlBase parent = control.getParent() as MgControlBase;
         if (parent != null && parent.isContainerControl())
            isControlChildOfFrameForm = true;

         return isControlChildOfFrameForm;
      }

      /// <summary>
      /// should the control be included in the recalculation of the tab order
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected bool IncludeControlInTabbingOrder(MgControlBase control, bool includeControlChildOfFrameForm)
      {
         if (!control.PropertyExists(PropInterface.PROP_TYPE_TAB_ORDER))
            return false;

         // if the frame does not allow parking
         if (_frameFormCtrl != null && _frameFormCtrl.PropertyExists(PropInterface.PROP_TYPE_ALLOW_PARKING) &&
            _frameFormCtrl.GetComputedBooleanProperty(PropInterface.PROP_TYPE_ALLOW_PARKING, true) == false)
            return false;

         if (!includeControlChildOfFrameForm)
         {
            if (IsControlChildOfFrameForm(control))
               return false;
         }

         return true;
      }

      /// <summary>
      /// should the control be included in the recalculation of the tab order
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected bool OnlyFrameFormChild(MgControlBase control)
      {
         if (!control.PropertyExists(PropInterface.PROP_TYPE_TAB_ORDER))
            return false;

         // if the frame does not allow parking
         if (_frameFormCtrl != null && _frameFormCtrl.PropertyExists(PropInterface.PROP_TYPE_ALLOW_PARKING) &&
            _frameFormCtrl.GetComputedBooleanProperty(PropInterface.PROP_TYPE_ALLOW_PARKING, true) == false)
            return false;

         if (IsControlChildOfFrameForm(control))
            return true;

         return false;
      }

      /// <summary>
      /// force all MgControls to update the font used on the controls
      /// </summary>
      public void UpdateFontValues()
      {
         MgControlBase control;
         Property prop;

         // update fonts on controls
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            control = CtrlTab.getCtrl(i);
            if (control.PropertyExists(PropInterface.PROP_TYPE_FONT))
            {
               prop = control.getProp(PropInterface.PROP_TYPE_FONT);
               prop.RefreshDisplay(true);
            }

            // update fonts on controls on subform
            if (control.Type == MgControlType.CTRL_TYPE_SUBFORM)
            {
               MgFormBase subform = control.GetSubformMgForm();
               subform.UpdateFontValues();
            }
         }

         // update fonts on  table
         if (HasTable())
            Commands.addAsync(CommandType.RECALCULATE_TABLE_FONTS, _tableMgControl);

         // update fonts on this form
         prop = getProp(PropInterface.PROP_TYPE_FONT);
         prop.RefreshDisplay(true);

         Commands.beginInvoke();
      }

      /// <summary>
      /// force all MgControls to update the colors used on the controls
      /// </summary>
      public void UpdateColorValues()
      {
         MgControlBase control;
         Property prop;

         // update colors on controls
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            control = CtrlTab.getCtrl(i);
            foreach (int propId in PropInterface.ColorProperties)
            {
               if (control.PropertyExists(propId))
               {
                  prop = control.getProp(propId);
                  prop.RefreshDisplay(true);
               }
            }

            if (control.Type == MgControlType.CTRL_TYPE_SUBFORM)
            {
               // update colors on controls on subform
               MgFormBase subform = control.GetSubformMgForm();
               subform.UpdateColorValues();
            }
         }

         // update colors on  table
         if (HasTable())
            Commands.addAsync(CommandType.RECALCULATE_TABLE_COLORS, _tableMgControl);

         // update colors on this form
         prop = getProp(PropInterface.PROP_TYPE_COLOR);
         prop.RefreshDisplay(true);

         Commands.beginInvoke();
      }

#endif

   }

   public class ControlMetaData
   {
      public Dictionary<string, string> Properties { get; set; } = new Dictionary<string,string>();
      public String Type { get; set; }
   }

   //TODO: move to MgForm class
   public class ControlsData
   {
      public Dictionary<string, string> ControlsValues { get; private set; } = new Dictionary<string, string>();

      public Dictionary<string, ControlMetaData> ControlsMetaData { get; set; } = new Dictionary<string, ControlMetaData>();


      public bool IsEmpty()
      {
         return ControlsValues.Count == 0 && ControlsMetaData.Count == 0;
      }

      public void ClearForSerialization()
      {
         //if (ControlsValues.Count == 0)
         //   ControlsValues = null;
         //if (ControlsProperties.Count == 0)
         //   ControlsProperties = null;
         
      }
   }

   
}
