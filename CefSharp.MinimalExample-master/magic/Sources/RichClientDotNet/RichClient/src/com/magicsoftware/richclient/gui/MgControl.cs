using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using DataView = com.magicsoftware.richclient.data.DataView;
using Record = com.magicsoftware.richclient.data.Record;
using Field = com.magicsoftware.richclient.data.Field;
using com.magicsoftware.richclient.rt;
using util.com.magicsoftware.util;

#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace com.magicsoftware.richclient.gui
{
   /// <summary>
   ///   data for <control> ...</control> tag
   /// </summary>
   /// <author>  ehudm
   /// 
   /// </author>
   internal class MgControl : MgControlBase, IComparable, PropParentInterface
   {
      private bool _focusedStopExecution; //used to remember stopExecution flag in the end of 
      private bool _inControlSuffix; //true, is control is in control suffix
      private Task _rtEvtTask;
      private MgControl _nextCtrl; // pointers to the prev/next ctrls in the
      private MgControl _prevCtrl;

      private String _subformTaskId; // the task id of the sub form task (if this
      private Task _subformTask; // reference to task of the sub form task
      internal bool SubformLoaded { get; private set; } //true, if the subform task was loaded already
      private List<BoundaryFactory> rangeBoundariesList;

      internal bool HasZoomHandler { set; private get; } //if TRUE, there is a zoom handler on this ctrl

      internal bool IsInteractiveUpdate; // true if this control was modified by user

      internal ArgumentsList ArgList { get; set; } // argument list built from the property
      private String refreshOnString; // Only for offline. Get it from XML and put it on the subform task when the subform task is opened

      internal IEnumerable<BoundaryFactory> RangeBoundaryFactories { get { return rangeBoundariesList; } }
      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal MgControl()
      {
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      internal MgControl(MgControlType type, Task task, int parentControl)
      : base(type, task.getForm(), parentControl)
      {
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      internal MgControl(MgControlType type, MgFormBase parentMgForm, int parentControlIdx)
         : base(type, parentMgForm, parentControlIdx)
      {
      }


      internal bool IgnoreDirectionWhileParking { get; set; } //ignore direction check while finding a parkable ctrl

      internal bool HasVerifyHandler { get; set; }

      #region IComparable Members

      public int CompareTo(object obj)
      {
         // if (!(obj is Control))
         // return 1;
         MgControl otherCtrl = (MgControl)obj;
         Property otherOrderProp = otherCtrl._propTab.getPropById(PropInterface.PROP_TYPE_TAB_ORDER);
         Property thisOrderProp = _propTab.getPropById(PropInterface.PROP_TYPE_TAB_ORDER);
         int thisOrder, otherOrder;

         if (thisOrderProp == null && otherOrderProp == null)
            return 0;
         if (thisOrderProp == null)
            return -1;
         if (otherOrderProp == null)
            return 1;

         thisOrder = thisOrderProp.getValueInt();
         otherOrder = otherOrderProp.getValueInt();

         if (thisOrder > otherOrder)
            return 1;
         if (thisOrder < otherOrder)
            return -1;
         if (getDitIdx() > otherCtrl.getDitIdx())
            return 1;
         else
            return -1;
      }

      #endregion

      public override int GetVarIndex()
      {
         return ((DataView)getForm().getTask().DataView).Dvcount + veeIndx;
      }

      /// <summary>
      /// Set arguments from the property
      /// </summary>
      /// <param name="mgForm"></param>
      /// <param name="ditIdx"></param>
      public override void fillData(MgFormBase mgForm, int ditIdx)
      {
         base.fillData(mgForm, ditIdx);
         ArgList = GetArgumentList();
      }

      /// <summary>
      ///   set the control attributes
      /// </summary>
      protected override bool SetAttribute(string attribute, string valueStr)
      {
         bool isTagProcessed = base.SetAttribute(attribute, valueStr);
         if (!isTagProcessed)
         {
            switch (attribute)
            {
               case ConstInterface.MG_ATTR_SUBFORM_TASK:
                  _subformTaskId = valueStr;
                  break;
               case ConstInterface.MG_ATTR_REFRESHON:
                  refreshOnString = valueStr.Trim();
                  break;
               default:
                  isTagProcessed = false;
                  Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
                  break;
            }
         }
         return isTagProcessed;
      }

      protected override bool initInnerObjects(string foundTagName)
      {
         if (foundTagName == XMLConstants.MG_TAG_RANGES)
         {
            return ParseRangeBoundaries();
         }
         return base.initInnerObjects(foundTagName);
      }

      private bool ParseRangeBoundaries()
      {
         string rangesSegment = ClientManager.Instance.RuntimeCtx.Parser.ReadToEndOfCurrentElement();
         var handler = new BoundarySaxHandler();
         MgSAXParser.Parse(rangesSegment, handler);
         rangeBoundariesList = new List<BoundaryFactory>(handler.BoundaryFactories);
         return true;
      }

      /// <summary>
      ///   Get select mode property
      /// </summary>
      internal char GetSelectMode()
      {
         char SelectMode = Constants.SELPRG_MODE_BEFORE;
         //save the select mode
         String smTmp = getProp(PropInterface.PROP_TYPE_SELECT_MODE).getValue();
         if (smTmp != null)
            SelectMode = smTmp[0];

         return SelectMode;
      }

      /// <summary>
      ///   return a url of the help for this control
      /// </summary>
      internal String getHelpUrl()
      {
         if (_propTab == null)
            return null;
         Property helpProp = _propTab.getPropById(PropInterface.PROP_TYPE_HELP_SCR);
         if (helpProp == null)
            return null;
         return helpProp.getValue();
      }

      /// <summary>
      /// build Argument list from the property
      /// </summary>
      private ArgumentsList GetArgumentList()
      {
         ArgumentsList argList = null;

         if (_propTab == null)
            return argList;
         Property ArgListProp = _propTab.getPropById(PropInterface.PROP_TYPE_PARAMETERS);
         if (ArgListProp == null)
            return argList;

         String parms = ArgListProp.getValue();
         if (parms != null)
         {
            argList = new ArgumentsList();
            argList.fillList(parms, (Task)getForm().getTask());
            if (refreshOnString != null)
               argList.RefreshOnString = refreshOnString;
         }

         return argList;
      }

      /// <summary>
      ///   set the next control
      /// </summary>
      internal void setNextCtrl(MgControl aCtrl)
      {
         _nextCtrl = aCtrl;
      }

      /// <summary>
      ///   set the prev control
      /// </summary>
      internal void setPrevCtrl(MgControl aCtrl)
      {
         _prevCtrl = aCtrl;
      }

      /// <summary>
      ///   return true if this control is descendant of the parentForm
      /// </summary>
      /// <param name = "parentForm"></param>
      /// <returns></returns>
      internal bool isDescendent(MgForm parentForm)
      {
         bool isDescendent = false;
         MgForm checkCurrentForm = (MgForm)getForm();

         Debug.Assert(parentForm != null);

         while (!isDescendent && checkCurrentForm != null)
         {
            if (parentForm == checkCurrentForm)
               isDescendent = true;
            else
            {
               Task currTask = (Task)checkCurrentForm.getTask();
               if (currTask.getParent() != null)
                  checkCurrentForm = (MgForm)currTask.getParent().getForm();
               else
                  break;
            }
         }
         return isDescendent;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal bool allowedParkRelatedToDirection()
      {
         bool allowed = false;
         Direction checkForDirection = Direction.FORE;
         int allowDirection = getProp(PropInterface.PROP_TYPE_ALLOWED_DIRECTION).getValueInt();
         MgControl lastparkedCtrl = GUIManager.getLastFocusedControl();
         MgForm form = (MgForm)getForm();

         if (lastparkedCtrl != null)
         {
            if (getForm().getTask() == lastparkedCtrl.getForm().getTask())
            {
               // If currCtrl and lastParkedCtrl on same form, direction is BACK if LParkedCtrl tab order is
               // greater than currCtrl's and LastMoveInRowDirection is not MOVE_DIRECTION_NEXT.
               if (form.ctrlTabOrderIdx(lastparkedCtrl) > form.ctrlTabOrderIdx(this)
                   && getForm().getTask().getLastMoveInRowDirection() != Constants.MOVE_DIRECTION_NEXT)
                  checkForDirection = Direction.BACK;

               // #797851 - task has moveToNextRecord and we have tabbed from last ctrl and move to first ctrl of 
               // next Rec. For this we have come from moveInView MOVE_DIR_DEGIN and must reset checkForDirection to FORE.
               if (checkForDirection == Direction.BACK &&
                   getForm().getTask().getLastMoveInRowDirection() == Constants.MOVE_DIRECTION_BEGIN)
                  checkForDirection = Direction.FORE;
            }
            else if (onDiffForm(lastparkedCtrl))
            {
               int lastparkedCtrlTabOrder = form.ctrlTabOrderIdx(lastparkedCtrl);

               // destination ctrl's form contains LP ctrl i.e destination ctrl (currCtrl) is always in the parent form
               // and lastparked ctrl is in one of it subforms. In such a case, we are moving from subform to parent form.
               if (lastparkedCtrlTabOrder > -1)
               {
                  if (lastparkedCtrlTabOrder > form.ctrlTabOrderIdx(this))
                     checkForDirection = Direction.BACK;
               }
               else
               // LP form contains the destination ctrl.
               // Lastparked ctrl's form contains current ctrl i.e Lastparked ctrl is always on the parent form
               // and current ctrl is in one of it subforms. In such a case, we are moving from parent form to subform.
               if (((MgForm)lastparkedCtrl.getForm()).ctrlTabOrderIdx(lastparkedCtrl) >
                   ((MgForm)lastparkedCtrl.getForm()).ctrlTabOrderIdx(this))
                  checkForDirection = Direction.BACK;

               // #756675 - we may have got FLOW_BACK dir on taborderidx, but we have done tab from last
               // ctrl of subform and move to first ctrl of parent.
               if (checkForDirection == Direction.BACK &&
                   lastparkedCtrl.getForm().getTask().getLastMoveInRowDirection() == Constants.MOVE_DIRECTION_NEXT)
                  checkForDirection = Direction.FORE;

               // #712538 - If we are moving from parent form to subform. we must check if direction satisfies with all
               // subformCtrl's allowdir property in path.
               List<MgControlBase> subformPathList = new List<MgControlBase>();
               ClientManager.Instance.EventsManager.buildSubformPath(subformPathList, lastparkedCtrl, this);

               // add current subformctrl onto pathlist
               // #981222 - Lastparked ctrl is always on the parent form i.e contains destination ctrl
               if (lastparkedCtrlTabOrder == -1)
               {
                  if (getForm().isSubForm())
                     subformPathList.Add((MgControl)form.getSubFormCtrl());
                  else
                  //fixed bug#:994239 if the form is frameform there is no subform.
                  if (lastparkedCtrl.getForm().getSubFormCtrl() != null)
                     subformPathList.Add((MgControl)((MgForm)lastparkedCtrl.getForm()).getSubFormCtrl());
               }

               bool allowedSubformCtrl = true;
               int allowDirectionSubform = (int)AllowedDirectionType.Both;

               foreach (MgControl subformCtrl in subformPathList)
               {
                  allowDirectionSubform = subformCtrl.getProp(PropInterface.PROP_TYPE_ALLOWED_DIRECTION).getValueInt();

                  if ((allowDirectionSubform == (int)AllowedDirectionType.Both) ||
                      ((checkForDirection == Direction.FORE) &&
                       (allowDirectionSubform == (int)AllowedDirectionType.Foreword)) ||
                      ((checkForDirection == Direction.BACK) &&
                       (allowDirectionSubform == (int)AllowedDirectionType.Backward)))
                     allowedSubformCtrl = true;
                  else
                  {
                     allowedSubformCtrl = false;
                     break;
                  }
               }

               // return if dirMatch failed.
               if (!allowedSubformCtrl)
                  return allowedSubformCtrl;
            }
         }

         //ALLOWED_DIRECTION_TYPE_BOTH = 1;
         //ALLOWED_DIRECTION_TYPE_FOREWORD = 2;
         //ALLOWED_DIRECTION_TYPE_BACKWARD = 3;

         if ((allowDirection == (int)AllowedDirectionType.Both) ||
             ((checkForDirection == Direction.FORE) && (allowDirection == (int)AllowedDirectionType.Foreword)) ||
             ((checkForDirection == Direction.BACK) && (allowDirection == (int)AllowedDirectionType.Backward)))
            allowed = true;

         return allowed;
      }

      /// <summary>
      ///   Refresh control display if it has a property which is an expression
      /// </summary>
      protected internal void refreshOnExpression()
      {
          int i;
          int size = (_propTab == null ? 0 : _propTab.getSize());
          Property prop;
          bool refresh = false;

          for (i = 0; i < size && !refresh; i++)
          {
              prop = _propTab.getProp(i);

              switch (prop.getID())
              {
                  case PropInterface.PROP_TYPE_DATA:
                      if (!prop.isExpression() && _field != null && _field.getTask() != getForm().getTask())
                      {
                          refresh = true;
                          break;
                      }
                      goto case PropInterface.PROP_TYPE_WIDTH;

                  case PropInterface.PROP_TYPE_WIDTH:
                  case PropInterface.PROP_TYPE_HEIGHT:
                  case PropInterface.PROP_TYPE_LEFT:
                  case PropInterface.PROP_TYPE_TOP:
                  case PropInterface.PROP_TYPE_COLOR:
                  case PropInterface.PROP_TYPE_FOCUS_COLOR:
                  case PropInterface.PROP_TYPE_GRADIENT_COLOR:
                  case PropInterface.PROP_TYPE_VISITED_COLOR:
                  case PropInterface.PROP_TYPE_HOVERING_COLOR:
                  case PropInterface.PROP_TYPE_FORMAT:
                  case PropInterface.PROP_TYPE_VISIBLE:
                  case PropInterface.PROP_TYPE_ENABLED:
                  case PropInterface.PROP_TYPE_RANGE:
                  case PropInterface.PROP_TYPE_TRANSLATOR:
                      if (prop.isExpression())
                          refresh = true;
                      break;

                  default:
                      break;
              }

              if (refresh)
                  RefreshDisplay();
          }
      }

      /// <summary>
      ///   get the next control
      /// </summary>
      /// <returns> the nextCtrl</returns>
      internal MgControl getNextCtrl()
      {
         return _nextCtrl;
      }

      /// <summary>
      /// </summary>
      internal MgControl getControlToFocus()
      {
         MgControl ctrl = this;
         if (getField() != null)
            ctrl = (MgControl)getField().ControlToFocus;

         return ctrl;
      }

      /// <summary>
      ///   get the prev control
      /// </summary>
      /// <returns> the prevCtrl</returns>
      internal MgControl getPrevCtrl()
      {
         return _prevCtrl;
      }

      /// <summary>
      ///   set this control as the current control and run its control prefix returns true if the control is
      ///   parkable and control prefix was executed (even if the perfix failed)
      /// </summary>
      internal bool invoke()
      {
         return invoke(false, true);
      }

      /// <summary>
      ///   set this control as the current control and run its control prefix returns true if the control is
      ///   parkable and control perfix was executed (even if the perfix failed)
      /// </summary>
      /// <param name = "moveByTab">was then this control was invoked by TAB movement?</param>
      /// <param name="checkSubFormTabIntoProperty"></param>
      protected internal bool invoke(bool moveByTab, bool checkSubFormTabIntoProperty)
      {
         bool invoked = false;

         // if the control is not visible, not enabled or not parkable skip this control
         if (isParkable(true, moveByTab, checkSubFormTabIntoProperty) &&
             (IgnoreDirectionWhileParking || allowedParkRelatedToDirection()))
         {
            ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_CTRL_PREFIX);
            invoked = true;
         }
         return invoked;
      }

      /// <summary>(internal)
      /// returns true if the control is parkable. Check enable, parkable and visibility
      /// </summary>
      /// <param name = "moveByTab">check parkability for TAB forward/backward operations</param>
      public override bool IsParkable(bool moveByTab)
      {
         return isParkable(true, moveByTab, true);
      }

      internal bool isParkable(bool checkEnabledAndVisible, bool moveByTab, bool checkSubFormTabIntoProperty)
      {
         bool result;

         result = isParkable(checkEnabledAndVisible, moveByTab);

         // if this ctrl is on a subform, check if the subform is parkable
         // if this ctrl on frame form, check if the frame form is parkable
         // this check might be recursive if the subforms\frame form are recursive.
         if (result && (Form.isSubForm() || Form.getFrameFormCtrl() != null))
         {
            MgControlBase containerControl = (Form.isSubForm()
                                                 ? Form.getSubFormCtrl()
                                                 : Form.getFrameFormCtrl());
            MgControl lastParkedCtrl = GUIManager.getLastFocusedControl();

            result = parkingIsAllowedOnSubform(containerControl);

            // If there is a switch between forms then only tabInto property must be checked of the container subform.
            if (moveByTab && result && checkSubFormTabIntoProperty && lastParkedCtrl != null &&
                getForm().getTask() != lastParkedCtrl.getForm().getTask())
               result = tabIntoIsAllowedOnSubform(containerControl);
         }

         return result;
      }

      /// <summary>
      ///   returns the subform task id of the control
      /// </summary>
      internal String getSubformTaskId()
      {
         return _subformTaskId;
      }

      /// <summary>
      ///   find the subform task according to its id and save it
      /// </summary>
      internal void initSubformTask()
      {
         if (isSubform())
            _subformTask = (Task)Manager.MGDataTable.GetTaskByID(_subformTaskId);
      }

      /// <summary>
      ///   Recursively check if the parking alowed on the subform.
      ///   Also, if this subform is inside a subform, check his parent subform as well.
      /// </summary>
      /// <param name = "subformCtrl">- The subform ctrl to check. can be frame form and also subform control</param>
      /// <returns></returns>
      private bool parkingIsAllowedOnSubform(MgControlBase subformCtrl)
      {
         bool result = subformCtrl.checkProp(PropInterface.PROP_TYPE_ALLOW_PARKING, true);

         if (result)
         {
            MgFormBase parentForm = subformCtrl.getForm();
            if (parentForm != null && parentForm.isSubForm())
               result = parkingIsAllowedOnSubform(parentForm.getSubFormCtrl());
         }

         return result;
      }

      /// <summary>
      ///   Recursively check if the tab into allowed on the subform. Also, if this subform is inside a subform,
      ///   check his parent subform as well.
      ///   subFormCtrl: can be frame form and also subform control
      /// </summary>
      /// <param name = "subFormCtrl">The subform ctrl to check.</param>
      /// <returns></returns>
      private bool tabIntoIsAllowedOnSubform(MgControlBase subFormCtrl)
      {
         bool result = subFormCtrl.checkProp(PropInterface.PROP_TYPE_TAB_IN, true);

         if (result)
         {
            MgFormBase parentForm = subFormCtrl.getForm();
            if (parentForm != null && parentForm.isSubForm())
               result = tabIntoIsAllowedOnSubform(parentForm.getSubFormCtrl());
         }

         return result;
      }

      /// <summary>
      ///   get the control's parentTable
      /// </summary>
      internal MgControl getParentTable()
      {
         return (MgControl)_parentTable;
      }

      /// <summary>
      ///   returns true if the control is modifiable (both on DB and Controll level) and the task mode is not query
      /// </summary>
      protected internal bool isModifiableCtrlDB()
      {
         bool dbModifable = true;
         if (_field != null)
            dbModifable = _field.DbModifiable;
         return (dbModifable && isModifiable());
      }

      /// <summary>
      ///   restore and display the old value of the control used when the control is not modifiable
      /// </summary>
      internal void restoreOldValue()
      {
         if (_field != null)
            ((Field)_field).updateDisplay();
         else
         {
            resetPrevVal();
            SetAndRefreshDisplayValue(Value, IsNull, false);
         }
      }

      /// <summary>
      ///   sets the event task
      /// </summary>
      internal void setRtEvtTask(Task curTask)
      {
         _rtEvtTask = curTask;
      }

      /// <summary>
      ///   returns the event task
      /// </summary>
      internal Task getRtEvtTask()
      {
         return _rtEvtTask;
      }

      /// <summary>
      ///   sets the subform task id of the control
      /// </summary>
      internal void setSubformTaskId(String taskId)
      {
         _subformTaskId = taskId;
      }

      /// <summary>
      ///   check if this control is part of data group
      /// </summary>
      /// <returns></returns>
      internal bool isPartOfDataGroup()
      {
         return (_siblingVec != null && _siblingVec.Count > 0);
      }

      /// <summary>
      ///   get the next sibling control
      /// </summary>
      /// <returns></returns>
      internal MgControl getNextSiblingControl()
      {
         MgControl nextSiblingCtrl = null;
         MgControl ctrl = null;
         int myIndex = Form.getControlIdx(this);

         for (int i = 0; i < _siblingVec.Count && nextSiblingCtrl == null; i++)
         {
            ctrl = (MgControl)_siblingVec[i];
            if (Form.getControlIdx(ctrl) > myIndex)
               nextSiblingCtrl = ctrl;
         }

         return nextSiblingCtrl;
      }

      /// <summary>
      ///   get the next sibling control
      /// </summary>
      /// <returns></returns>
      internal MgControl getPrevSiblingControl()
      {
         MgControl prevSiblingCtrl = null;
         MgControl ctrl = null;
         int myIndex = Form.getControlIdx(this);

         for (int i = _siblingVec.Count - 1; i >= 0 && prevSiblingCtrl == null; i--)
         {
            ctrl = (MgControl)_siblingVec[i];
            if (Form.getControlIdx(ctrl) < myIndex)
               prevSiblingCtrl = ctrl;
         }

         return prevSiblingCtrl;
      }

      /// <summary>
      ///   get the first menu that is enable that belong to the keybordItem on the context menus
      /// </summary>
      /// <param name = "kbItm"></param>
      internal MenuEntry getContextMenuEntrybyAccessKeyToControl(KeyboardItem kbItm)
      {
         MenuEntry menuEntry = null;

         MgMenu mgMenu = getContextMenu(false);

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
      /// resetDisplayToPrevVal : set the control to its previous value and display it. 
      /// also, put all the text into selection (like we first entered the control).
      /// </summary>
      internal void resetDisplayToPrevVal()
      {
         resetPrevVal(); // force update of the display
         RefreshDisplay();
         Manager.SetSelection(this, 0, -1, -1);
      }

      /// <summary>
      ///   enable/disable actions and states for Text control
      /// </summary>
      internal void enableTextAction(bool enable)
      {
         if (isTextControl() || isRichEditControl())
         {
            Task task = (Task)getForm().getTask();
            task.setKeyboardMappingState(Constants.ACT_STT_EDT_EDITING, enable);
            task.ActionManager.enableEditingActions(enable);

            if (isRichEditControl())
               task.ActionManager.enableRichEditActions(enable);

            if (isMultiline())
            {
               task.ActionManager.enableMLEActions(enable);

               if (enable)
               {
                  // richedit CR always allowed.
                  bool allowCR = getProp(PropInterface.PROP_TYPE_MULTILINE_ALLOW_CR).getValueBoolean() ||
                                 isRichEditControl();
                  task.ActionManager.enable(InternalInterface.MG_ACT_EDT_BEGNXTLINE, allowCR);
               }
            }

            task.ActionManager.enable(InternalInterface.MG_ACT_WIDE, enable && IsWideAllowed());

            task.ActionManager.enable(InternalInterface.MG_ACT_CLIP_PASTE, enable && IsPasteAllowed());
         }
      }

      /// <summary>
      ///   enable/disable actions and states for Text of Tree control
      /// </summary>
      internal void enableTreeEditingAction(bool enable)
      {
         if (isTreeControl())
         {
            Task task = (Task)getForm().getTask();
            task.setKeyboardMappingState(Constants.ACT_STT_TREE_EDITING, enable);
            task.ActionManager.enable(InternalInterface.MG_ACT_TREE_RENAME_RT_EXIT, enable);
            task.ActionManager.enableEditingActions(enable);
            task.ActionManager.enable(InternalInterface.MG_ACT_WIDE, enable && IsWideAllowed());
         }
      }

     

      /// <summary>
      ///   change selected item of tab control
      /// </summary>
      /// <param name = "direction"></param>
      internal void changeSelection(char direction)
      {
         int[] layers = getCurrentIndexOfChoice();
         int currLayer = layers[0];
         int maxLayer = getMaxDisplayItems();

         if (direction == Constants.MOVE_DIRECTION_NEXT)
         {
            currLayer++;
            if (currLayer >= maxLayer)
               currLayer = 0;
         }
         else if (direction == Constants.MOVE_DIRECTION_PREV)
         {
            currLayer--;
            if (currLayer < 0)
               currLayer = maxLayer - 1;
         }
         else
            Debug.Assert(false);
         String val = (currLayer).ToString();
         ClientManager.Instance.EventsManager.simulateSelection(this, val, 0, false);
      }

      /// <summary>
      ///   returns stopExecution flag in the end of handle focus on the control
      /// </summary>
      /// <returns></returns>
      internal bool isFocusedStopExecution()
      {
         return _focusedStopExecution;
      }

      /// <summary>
      ///   remember stopExecution flag in the end of handle focus on the control
      /// </summary>
      /// <param name = "focusedStopExecution"></param>
      internal void setFocusedStopExecution(bool focusedStopExecution)
      {
         _focusedStopExecution = focusedStopExecution;
      }

      /// <summary>
      ///   return tree value
      /// </summary>
      /// <param name = "displayLine"></param>
      /// <returns></returns>
      internal String getTreeValue(int displayLine)
      {
         int recIdx = ((MgForm)Form).displayLine2RecordIdx(displayLine);
         if (recIdx == (int)RecordIdx.NOT_FOUND)
            return null;
         String val = ((Field)_nodeIdField).getValueByRecIdx(recIdx);
         return val;
      }

      /// <summary>
      ///   return  value of parent
      /// </summary>
      /// <param name = "displayLine"></param>
      /// <returns></returns>
      internal String getTreeParentValue(int displayLine)
      {
         int recIdx = ((MgForm)Form).displayLine2RecordIdx(displayLine);
         if (recIdx == (int)RecordIdx.NOT_FOUND)
            return null;
         String val = ((Field)_nodeParentIdField).getValueByRecIdx(recIdx);
         return val;
      }

      /// <param name = "ctrl-">The ctrl to check. can be frame form and also subform control
      /// </param>
      /// <returns></returns>
      internal bool onTheSameSubFormControl(MgControl ctrl)
      {
         bool retValue = false;
         if (ctrl != null)
         {
            if (getForm().isSubForm() && ctrl.getForm().isSubForm())
            {
               if (getForm().getTask() == ctrl.getForm().getTask())
                  retValue = true;
            }
            else if (getForm().getFrameFormCtrl() != null && ctrl.getForm().getFrameFormCtrl() != null)
            {
               if (getForm().getTask() == ctrl.getForm().getTask())
                  retValue = true;
            }
         }
         return retValue;
      }

      /// <summary>
      ///   true if the ctrls are on different forms
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <returns></returns>
      internal bool onDiffForm(MgControl ctrl)
      {
         bool isSubform = (getForm().isSubForm() || ctrl.getForm().isSubForm());
         bool isFrameForm = (getForm().getFrameFormCtrl() != null || ctrl.getForm().getFrameFormCtrl() != null);
         bool isOnDiffForm = ((isSubform || isFrameForm) && !onTheSameSubFormControl(ctrl));

         return isOnDiffForm;
      }

      /// <summary>
      ///   true if control is in control suffix
      /// </summary>
      /// <returns></returns>
      internal bool isInControlSuffix()
      {
         return _inControlSuffix;
      }

      /// <summary>
      ///   set true if control is in control suffix
      /// </summary>
      /// <param name = "inControlSuffix"></param>
      internal void setInControlSuffix(bool inControlSuffix)
      {
         _inControlSuffix = inControlSuffix;
      }

      /// <summary>
      ///   return true if the control has select program
      /// </summary>
      internal bool HasSelectProgram()
      {
         bool HasSelectProgram = false;
         Property selectProgProp = getProp(PropInterface.PROP_TYPE_SELECT_PROGRAM);
         if (selectProgProp != null && selectProgProp.getValue() != null)
            HasSelectProgram = true;

         return HasSelectProgram;
      }

      /// <summary>
      ///   return true if the control use the handler ZOOM (for select program or Zoom handler)
      /// </summary>
      /// <returns></returns>
      internal bool useZoomHandler()
      {
         return (HasZoomHandler ||
                 getField() != null && (((Field)getField()).getHasZoomHandler() ||
                (HasSelectProgram() && GetSelectMode() != Constants.SELPRG_MODE_PROMPT)));
      }

      /// <summary>
      ///   returns the next control depending upon direction
      /// </summary>
      /// <param name = "direction"></param>
      /// <returns></returns>
      internal MgControl getNextCtrlOnDirection(Direction direction)
      {
         MgControl nextCtrlOnDir = null;

         if (direction == Direction.FORE)
            nextCtrlOnDir = _nextCtrl;
         else if (direction == Direction.BACK)
            nextCtrlOnDir = _prevCtrl;

         return nextCtrlOnDir;
      }

      /// <summary>
      ///   return TRUE if PROP_TYPE_PARK_ON_CLICK exists for the control type
      /// </summary>
      /// <returns></returns>
      internal bool propParkOnClickExists()
      {
         return _propTab.propExists(PropInterface.PROP_TYPE_PARK_ON_CLICK);
      }


      /// <summary>
      ///   Compares taborder of two controls based on combined taborder of all forms.
      ///   Outputs :
      ///   less than zero - ctrl1 is less than ctrl2.
      ///   Greater than zero - ctrl1 is greater than ctrl2.
      ///   equal to Zero - if equal or not on same taborder.
      /// </summary>
      /// <param name = "ctrl1"></param>
      /// <param name = "ctrl2"></param>
      /// <returns></returns>
      internal static int CompareTabOrder(MgControl ctrl1, MgControl ctrl2)
      {
         int result = 0;

         if (ctrl1 == ctrl2 || ctrl1.getTopMostForm() != ctrl2.getTopMostForm())
            return result;

         MgControl ctrl = ctrl1;
         result = 1; // assume ctrl1 > ctrl2

         // traverse till the end till we get ctrl2
         while (ctrl != null)
         {
            if (ctrl == ctrl2)
            {
               result = -1;
               break;
            }

            ctrl = ctrl.getNextCtrl();
         }

         return result;
      }

      /// <summary>
      ///   Update boolean property(ex:visible\enable) of control and it's children
      /// </summary>
      /// <param name = "propId">property id </param>
      /// <param name = "commandType">command type that belong to the prop id</param>
      /// <param name = "val"></param>
      /// <param name = "updateThis"></param>
      public override void updatePropertyLogicNesting(int propId, CommandType commandType, bool val, bool updateThis)
      {
         if (val && haveToCheckParentValue())
            val = isParentPropValue(propId);
         // if the subform must be invisible (CTRL_SUBFORM_NONE) we change its visibility to false
         if (val && isSubform() && (commandType == CommandType.PROP_SET_VISIBLE) && ShouldSetSubformInvisible())
               val = false;
         if (updateThis)
         {
            if (commandType == CommandType.PROP_SET_VISIBLE)
               Commands.addAsync(commandType, this, getDisplayLine(false), val, !IsFirstRefreshOfProps());
            else
               Commands.addAsync(commandType, this, getDisplayLine(false), val);
         }
        
      }

      /// <summary>
      /// If the subform must be invisible (for example CTRL_SUBFORM_NONE) returns true.
      /// </summary>
      /// <returns></returns>
      private bool ShouldSetSubformInvisible()
      {
         bool setInvisible = false;
         var subformType = (SubformType)getProp(PropInterface.PROP_TYPE_SUBFORM_TYPE).getValueInt();
         if (subformType == SubformType.None && getSubformTaskId() == null)
            setInvisible = true;
         return setInvisible;
      }

      /// <summary>
      ///   Updates visibility or enable property of subform controls
      /// </summary>
      /// <param name = "propId">Id of visibility or enable property</param>
      /// <param name = "commandType">type of the GUI command</param>
      /// <param name = "val">value for the property</param>
      protected override void updateSubformChildrenPropValue(int propId, CommandType commandType, bool val)
      {
         Debug.Assert(isSubform());

         if (!((Task)getForm().getTask()).AfterFirstRecordPrefix)
            return;

         var subformType = (SubformType)getProp(PropInterface.PROP_TYPE_SUBFORM_TYPE).getValueInt();
         if (_subformTaskId != null || subformType == SubformType.None)
         {
            if (_subformTask != null && _subformTask.isStarted())
            {
               SubformLoaded = true;
               if (commandType == CommandType.PROP_SET_VISIBLE && val && RefreshOnVisible)
               {
                  RefreshOnVisible = false;
                  ((Task)getForm().getTask()).SubformRefresh((Task)_subformTask, true);
               }

               MgFormBase subformForm = _subformTask.getForm();
               for (int i = 0; i < subformForm.getCtrlCount(); i++)
               {
                  MgControlBase child = subformForm.getCtrl(i);
                  //QCR #981555, We should not affect visibility of the TableColumns since it is managed by the table.Setting visibility 
                  //of all columns to false prevents table placement
                  if (child.isColumnControl()) 
                     continue;
                  bool childValue = child.checkProp(propId, true) & val;
                  if (childValue)
                      childValue = child.isParentPropValue(propId);

                  if (_subformTask.isStarted())
                     Commands.addAsync(commandType, child, child.getDisplayLine(false), childValue);
                  child.updateChildrenPropValue(propId, commandType, childValue);
               }
            }
         }
         else if (commandType == CommandType.PROP_SET_VISIBLE && val && !SubformLoaded)
         {
            SubformLoaded = true;
            ClientManager.Instance.EventsManager.handleInternalEvent(this, InternalInterface.MG_ACT_SUBFORM_OPEN);
         }
      }

      /// <summary>
      /// Set subform task id for a subform control
      /// </summary>
      public override void Init()
      {
         initSubformTask();
      }

      /// <summary>
      /// get the behaviour for the table control
      /// </summary>
      /// <returns></returns>
      protected override TableBehaviour GetTableBehaviour()
      {
         return TableBehaviour.UnlimitedItems;
      }

      /// <summary> returns true if the control is parkable </summary>
      /// <param name = "checkEnabledAndVisible">check enabled and visible properties too</param>
      /// <param name = "moveByTab">check parkability for TAB forward/backward operations</param>
      public override bool isParkable(bool checkEnabledAndVisible, bool moveByTab)
      {
         bool result;

         result = !isImageControl();

         if (result)
         {
            // In RC we need to evaluate expressions of following properties 
            // because in isParkable method we use already computed property values.
            if (moveByTab)
               checkProp(PropInterface.PROP_TYPE_TAB_IN, true);
            checkProp(PropInterface.PROP_TYPE_ENABLED, true);
            checkProp(PropInterface.PROP_TYPE_ALLOW_PARKING, true);
            checkProp(PropInterface.PROP_TYPE_VISIBLE, true);
            result = base.isParkable(checkEnabledAndVisible, moveByTab);
         }

         return result;
      }

      /// <summary>
      ///   return true if value of the field for parent record is null
      /// </summary>
      /// <param name = "displayLine"></param>
      /// <returns></returns>
      internal bool isTreeParentNull(int displayLine)
      {
          int recIdx = ((MgForm)Form).displayLine2RecordIdx(displayLine);
          if (recIdx == (int)RecordIdx.NOT_FOUND)
              return true;
          return ((Field)_nodeParentIdField).isNullByRecIdx(recIdx);
      }

      /// <summary>
      ///   return true if value of the field for record is null
      /// </summary>
      /// <param name = "displayLine"></param>
      /// <returns></returns>
      internal bool isTreeNull(int displayLine)
      {
          int recIdx = ((MgForm)Form).displayLine2RecordIdx(displayLine);
          if (recIdx == (int)RecordIdx.NOT_FOUND)
              return true;
          return ((Field)_nodeIdField).isNullByRecIdx(recIdx);
      }

      /// <summary>
      /// </summary>
      /// <param name = "recIdx"></param>
      /// <returns></returns>
      internal String getFieldXML(int recIdx)
      {
          if (recIdx < 0)
              return "";
          else
          {
             Record rec = ((DataView)getForm().getTask().DataView).getRecByIdx(recIdx);
             return rec.getFieldDataXML(_nodeIdField.getId(), false);
          }
      }

      /// <summary>
      ///   get xml from parent field
      /// </summary>
      /// <param name = "recIdx"></param>
      /// <returns></returns>
      internal String getParentFieldXML(int recIdx)
      {
          if (recIdx < 0)
              return "";
          Record rec = ((DataView)getForm().getTask().DataView).getRecByIdx(recIdx);
          return rec.getFieldDataXML(_nodeParentIdField.getId(), false);
      }

      /// <summary>
      ///   validate the value of the control and set it's value
      /// </summary>
      /// <param name = "NewValue">the value of the control</param>
      /// <param name = "updateCtrl">if true then the control will be updated upon successful check</param>
      /// <returns>returns if validation is successful</returns>
      internal bool validateAndSetValue(String NewValue, bool updateCtrl)
      {
         ValidationDetails vd;

         UpdateModifiedByUser(NewValue);

         // if the value of the control is null or was changed
         // check validation and update the control value
         if (ModifiedByUser)
         {
            string ctrlCurrValue = isRichEditControl()
                                   ? getRtfVal()
                                   : Value;

            try
            {
               vd = buildPicture(ctrlCurrValue, NewValue);
            }
            catch (NullReferenceException e)
            {
               if (getPIC() == null)
                  return true;
               throw e;
            }

            vd.evaluate();

            // the validation of the fields value was wrong
            if (vd.ValidationFailed)
            {
               ModifiedByUser = false;
               return false;
            }

            IsInteractiveUpdate = true;

            getForm().getTask().setDataSynced(false);

            // set the value (handle recompute, etc.)
            if (updateCtrl)
            {
               // QCR#441020:After validating display value, take the same ctrl's value for mg value conversion.
               // Using display value generated from MgValue, might have problem to convert back to MgVal using picture due to alignment (changed alignment ).
               // This problem occurs for numeric.
               // The fix should be applicable for numeric type only. Because date and time field with complex masking have problem on using ctrl's value.
               // Refer date qcrs: #291666, #307353, #434241 and for time, check default value when format is 'HH-MM-SS PMZ@'
               if (_field != null && _field.getType() == StorageAttribute.NUMERIC && (NewValue.Trim().Length > 0))
                  vd.setValue(NewValue);

               setValue(vd);
            }
         }
         // the value of control wasn't changed
         else
         {
            if (checkProp(PropInterface.PROP_TYPE_MUST_INPUT, false) && isModifiable())
            {
               // the value of control with must input property wasn't changed
               Manager.WriteToMessagePanebyMsgId(getForm().getTask(), MsgInterface.RT_STR_FLD_MUST_UPDATED, false);
               return false;
            }
         }

         return true;
      }

      /// <summary>
      ///   set the value received from the user input after validation and update the field connected to this
      ///   control (if exists)
      /// </summary>
      /// <param name = "vd">the validation details object</param>
      private void setValue(ValidationDetails vd)
      {
         if (IsDotNetControl())
            return;

         String newValue, mgVal;
         bool isNull = vd.getIsNull(); // is new inserted value equals to null ?

         newValue = vd.getDispValue();

         if (!isNull || !_field.hasNullDisplayValue())
            mgVal = getMgValue(newValue);
         // it's inner value -> translate it to the display
         else
            mgVal = (_field.getType() == StorageAttribute.BLOB_VECTOR
                        ? _field.getCellDefualtValue()
                        : _field.getDefaultValue()); // insert null display value
         if (mgVal != null)
         {

            if (_field != null && (!newValue.Equals(Value) || KeyStrokeOn || IsNull != isNull))
            {
               ((Field)_field).setValueAndStartRecompute(mgVal, isNull, true, true, false);
               
               // defect 127734 : value of the field might be changed already inside a control change handler
               // to avoid updating old value and overrunning the value fr0m the control change
               // get the value from the field. (need to be done for combo and list box, but it's too risky
               // and unlikely to happen, so the fix will be for check box only)
               if (isCheckBox())
                  mgVal = ((Field)_field).getValue(false);
            }
            // if field value is null & has null display value, take evaluated null display value.
            if (_field != null && isNull && _field.hasNullDisplayValue())
               mgVal = newValue;

            SetAndRefreshDisplayValue(mgVal, isNull, false);
            // QCR #438753. Prevent RP&RS cycle, when it's not needed.
            KeyStrokeOn = false;
         }
      }

      /// <summary> refresh the item list of radio button </summary>
      /// <param name="execComputeChoice"></param>
      protected override void refreshAndSetItemsListForRadioButton(int line, bool execComputeChoice)
      {
         base.refreshAndSetItemsListForRadioButton(line, execComputeChoice);

         // If the parking was on the radio control, we need to reset the focus because 
         // the old controls were disposed and the new ones are created.
         Task lastFocussedTask = ClientManager.Instance.getLastFocusedTask(((Task)this.getForm().getTask()).getMgdID());
         if (lastFocussedTask != null && this == lastFocussedTask.getLastParkedCtrl())
            Commands.addAsync(CommandType.SET_FOCUS, this);
      }

      /// <summary>
      ///   returns the subform task of this subform control
      /// </summary>
      internal Task getSubformTask()
      {
         return _subformTask;
      }

      /// <summary>
      /// returns MgForm of the subform control task
      /// </summary>
      public override MgFormBase GetSubformMgForm()
      {
         MgFormBase form = null;
         if (_subformTask != null)
            form = _subformTask.getForm();
         return form;
      }

      /// <summary>
      /// Check whether table being used with absolute scrollbar thumb.
      /// </summary>      
      public bool IsTableWithAbsoluteScrollbar()
      {
         bool tableWithAbsoluteScrollbar = false;
         Debug.Assert((Type == MgControlType.CTRL_TYPE_TABLE), "In MgControl.IsTableWithAbsoluteScrollbar(): Not a table control.");

         if (_propTab.propExists(PropInterface.PROP_TYPE_SCROLL_BAR_THUMB))
            tableWithAbsoluteScrollbar = (_propTab.getPropById(PropInterface.PROP_TYPE_SCROLL_BAR_THUMB)).getValueInt() == (int)ScrollBarThumbType.Absolute;

         return tableWithAbsoluteScrollbar;
      }
   }
}
