using System;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.unipaas.dotnet;
using System.Diagnostics;
#if !PocketPC
using System.Drawing;
using System.Threading;
using com.magicsoftware.util;
#else
using com.magicsoftware.win32;
using Monitor = com.magicsoftware.richclient.mobile.util.Monitor;
using com.magicsoftware.richclient;
using com.magicsoftware.util;
using System.Drawing;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> This is a default handler, it is implemented as singleton 
   /// Here will be implemented actions that are common for most GUI objects</summary>
   /// <author>  rinav </author>
   class DefaultHandler : HandlerBase
   {
      /// <summary>
      /// singleton
      /// </summary>
      private static DefaultHandler _instance;
      internal static DefaultHandler getInstance()
      {
         if (_instance == null)
            _instance = new DefaultHandler();
         return _instance;
      }

      private DefaultHandler()
      {
      }

      /// <summary> check if handle traverse for ctrl</summary>
      /// <param name="ctrl"></param>
      /// <returns></returns>
      private TraverseMode doTraverse(GuiMgControl ctrl, Keys keyCode, Control control, Modifiers modifiers)
      {
         TraverseMode doTravers = TraverseMode.NONE;

         if (ctrl != null)
         {
            if (keyCode == Keys.Tab)
               doTravers = TraverseMode.BETWEEN_CONTROLS;

            bool isArrowKey = keyCode == Keys.Down || keyCode == Keys.Up || keyCode == Keys.Right || keyCode == Keys.Left;
            switch (ctrl.Type)
            {
               case MgControlType.CTRL_TYPE_RADIO:
                  if (isArrowKey || keyCode == Keys.Tab || keyCode == Keys.Space)
                     doTravers = TraverseMode.WITHIN_CONTROL;
                  break;

               case MgControlType.CTRL_TYPE_TAB:
                  if (isArrowKey || keyCode == Keys.Home || keyCode == Keys.End ||
                     (keyCode == Keys.Tab &&
                     (modifiers == Modifiers.MODIFIER_CTRL || modifiers == Modifiers.MODIFIER_SHIFT_CTRL)))
                        doTravers = TraverseMode.WITHIN_CONTROL;
                  break;

               case MgControlType.CTRL_TYPE_COMBO:
                  if (isArrowKey || keyCode == Keys.PageUp || keyCode == Keys.PageDown
                     || keyCode == Keys.Home || keyCode == Keys.End)
                  {
                     if (((MgComboBox)control).DroppedDown && (keyCode == Keys.Home || keyCode == Keys.End))
                        doTravers = (modifiers == Modifiers.MODIFIER_CTRL) ? TraverseMode.WITHIN_CONTROL
                                                                           : TraverseMode.NONE;
                     else
                     {
                        //Fixed bug#:942596 (continue fix of bug#:800763)
                        // if (((MgComboBox)control).DroppedDown)
                        doTravers = (modifiers == Modifiers.MODIFIER_NONE) ? TraverseMode.WITHIN_CONTROL
                                                                           : TraverseMode.NONE;
                     }
                  }
                  break;

               case MgControlType.CTRL_TYPE_LIST:
                  if (keyCode == Keys.Down || keyCode == Keys.Up)
                     doTravers = TraverseMode.WITHIN_CONTROL;
                  break;
            }
         }
         return doTravers;
      }

      /// <summary>
      /// get the next sugessted Value</summary>
      /// <param name="event"></param>
      /// <returns></returns>
      private String getNextSuggestedValue(Control control, Keys KeyCode, Modifiers modifiers)
      {
         int suggestedValue = -1;
         if (control is RadioButton)
         {
            String val = GuiUtils.getValue(control);
            if (KeyCode == Keys.Space)
            {
               val = GuiUtils.GetRadioButtonIndex((RadioButton)control);
            }

            String maxOption = "" + control.Parent.Controls.Count;
            suggestedValue = getNextSuggestedValueForRadioButton(control, KeyCode, val, maxOption);
            GuiUtils.setSuggestedValueOfChoiceControlOnTagData(control.Parent, "" + suggestedValue);
         }
         else if (control is MgTabControl)
         {
            suggestedValue = getNextSuggestedValueForTabControl((MgTabControl)control, KeyCode, modifiers);
            GuiUtils.setSuggestedValueOfChoiceControlOnTagData(control, "" + suggestedValue);
         }
         else if (control is MgComboBox)
         {
            suggestedValue = getNextSuggestedValueForComboBox((MgComboBox)control, KeyCode);
            GuiUtils.setSuggestedValueOfChoiceControlOnTagData(control, "" + suggestedValue);
         }
         else if (control is ListBox)
         {
            suggestedValue = getNextSuggestedValueForListBox((ListBox)control, KeyCode);
            GuiUtils.setSuggestedValueOfChoiceControlOnTagData(control, "" + suggestedValue);
         }

         return "" + suggestedValue;
      }

      /// <summary> get the next list box Value</summary>
      /// <param name="event"></param>
      /// <returns></returns>
      private int getNextSuggestedValueForListBox(ListBox listBox, Keys KeyCode)
      {
         int suggestedValue = -1;
         String value = "";
         int valueInt = -1;
         int ItemsCount = listBox.Items.Count;

         if (ItemsCount > 0)
         {
            value = GuiUtils.getValue(listBox);
            valueInt = Int32.Parse(value);

            int offsetLines = 1;
            switch (KeyCode)
            {
               case Keys.Down:
               case Keys.Up:
                  offsetLines = 1;
                  break;
            }

            switch (KeyCode)
            {
               case Keys.Down:
                  suggestedValue = valueInt + offsetLines;
                  if (suggestedValue > ItemsCount - 1)
                     suggestedValue = ItemsCount - 1;
                  suggestedValue = Math.Max(suggestedValue, 0);
                  break;

               case Keys.Up:
                  suggestedValue = valueInt - offsetLines;
                  if (suggestedValue < 0)
                     suggestedValue = 0;
                  break;
            }
         }

         return suggestedValue;
      }

      /// <summary> get the next combo box Value</summary>
      /// <param name="event"></param>
      /// <returns></returns>
      private int getNextSuggestedValueForComboBox(MgComboBox comboBox, Keys KeyCode)
      {
         int suggestedValue = -1;
         int currentValue = -1;
         int ItemsCount = comboBox.Items.Count;

         if (ItemsCount > 0)
         {
            currentValue = comboBox.SelectedIndex;
            int visibleLines = ((TagData)comboBox.Tag).VisibleLines;

            int offsetLines = 1;
            switch (KeyCode)
            {
               case Keys.PageUp:
               case Keys.PageDown:
                  offsetLines = visibleLines;
                  break;

               case Keys.Down:
               case Keys.Right:
               case Keys.Up:
               case Keys.Left:
                  offsetLines = 1;
                  break;

               case Keys.Home:
               case Keys.End:
                  offsetLines = ItemsCount;
                  break;
            }

            switch (KeyCode)
            {
               case Keys.End:
               case Keys.PageDown:
               case Keys.Down:
               case Keys.Right:
                  suggestedValue = currentValue + offsetLines;
                  if (suggestedValue > ItemsCount - 1)
                     suggestedValue = ItemsCount - 1;

                  suggestedValue = Math.Max(suggestedValue, 0);
                  break;

               case Keys.Home:
               case Keys.PageUp:
               case Keys.Up:
               case Keys.Left:
                  suggestedValue = currentValue - offsetLines;
                  if (suggestedValue < 0)
                     suggestedValue = 0;
                  break;
            }
         }

         return suggestedValue;
      }

      /// <summary> return the suggested Value 0 - maxOption</summary>
      private int getNextSuggestedValueForRadioButton(Control control, Keys keyCode, String val, String maxOption)
      {
         int choiceCnt;
         int suggestedValueInt = -1;
         int valueInt = (val.Equals("") ? -1 : Int32.Parse(val));
         int maxOptionInt = (maxOption.Equals("") ? -1 : Int32.Parse(maxOption));
         
         // TODO: Hebrew
         bool isRTL = false;
#if !PocketPC         
         isRTL = control.RightToLeft == RightToLeft.Yes;
#endif

         if (valueInt == -1)
            suggestedValueInt = 0;
         else
         {
            switch (keyCode)
            {
               case Keys.Up:
                  suggestedValueInt = Math.Max(valueInt - 1, 0);
                  break;

               case Keys.Down:
                  suggestedValueInt = Math.Min(valueInt + 1, maxOptionInt - 1);
                  break;

               case Keys.Left:
                  if (!isRTL)
                     choiceCnt = getNextSuggestedValueForLeftKeybord(control, ref suggestedValueInt, valueInt);
                  else
                     choiceCnt = getNextSuggestedValueForRightKeybord(control, ref suggestedValueInt, valueInt);
                     
                  break;

               case Keys.Right:
                  if (!isRTL)
                     choiceCnt = getNextSuggestedValueForRightKeybord(control, ref suggestedValueInt, valueInt);
                  else
                     choiceCnt = getNextSuggestedValueForLeftKeybord(control, ref suggestedValueInt, valueInt);
                  break;

               case Keys.Space:
                  suggestedValueInt = valueInt;
                  break;
            }
         }

         return suggestedValueInt;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="suggestedValueInt"></param>
      /// <param name="radioInColumn"></param>
      /// <param name="valueInt"></param>
      /// <returns></returns>
      private static int getNextSuggestedValueForRightKeybord(Control control, ref int suggestedValueInt, int valueInt)
      {
         int choiceCnt = control.Parent.Controls.Count;
         int radioInColumn = ((MgRadioPanel)control.Parent).getCountOfButtonsInColumn(choiceCnt);
         if (valueInt + radioInColumn < choiceCnt)
            suggestedValueInt = valueInt + radioInColumn;
         else
            suggestedValueInt = valueInt;
         return choiceCnt;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="suggestedValueInt"></param>
      /// <param name="radioInColumn"></param>
      /// <param name="valueInt"></param>
      /// <returns></returns>
      private static int getNextSuggestedValueForLeftKeybord(Control control, ref int suggestedValueInt,  int valueInt)
      {
         int choiceCnt = control.Parent.Controls.Count;
         int radioInColumn = ((MgRadioPanel)control.Parent).getCountOfButtonsInColumn(choiceCnt);
         if (valueInt - radioInColumn >= 0)
            suggestedValueInt = valueInt - radioInColumn;
         else
            suggestedValueInt = valueInt;
         return choiceCnt;
      }

      /// <summary> get the next tab Value</summary>
      /// <param name="event"></param>
      /// <returns></returns>
      private int getNextSuggestedValueForTabControl(MgTabControl tabControl, Keys KeyCode, Modifiers modifiers)
      {
         int suggestedValue = -1;
         String value = "";
         int valueInt = -1;
         int tabCount = tabControl.TabCount;

         if (tabCount > 0)
         {
            if (KeyCode == Keys.Tab)
            {
               /* Ctrl+Tab is same as Down (or Right) arrow and
                * Ctrl+Shift+Tab is same as Up (or Left) arrow
                */
               if (modifiers == Modifiers.MODIFIER_CTRL)
                  KeyCode = Keys.Down;
               if (modifiers == Modifiers.MODIFIER_SHIFT_CTRL)
                  KeyCode = Keys.Up;
            }

            value = GuiUtils.getValue(tabControl);
            valueInt = Int32.Parse(value);

            switch (KeyCode)
            {
               case Keys.Down:
               case Keys.Right:
                  suggestedValue = valueInt + 1;
                  if (suggestedValue > tabControl.TabCount - 1)
                     suggestedValue = 0;
                  break;

               case Keys.Up:
               case Keys.Left:
                  suggestedValue = valueInt - 1;
                  if (suggestedValue < 0)
                     suggestedValue = tabControl.TabCount - 1;
                  break;

               case Keys.Home:
                  suggestedValue = 0;
                  break;

               case Keys.End:
                  suggestedValue = tabControl.TabCount - 1;
                  break;
            }
         }

         return suggestedValue;
      }

      /// <summary> get items maneger for
      /// table control
      /// table child control
      /// tree control
      /// tree child control</summary>
      /// <param name="mapData"></param>
      /// <returns></returns>
      private ContainerManager getContainerManager(MapData mapData)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         GuiMgControl ctrl = mapData.getControl();
         Object obj = controlsMap.object2Widget(ctrl, mapData.getIdx());
         if (obj is TableControl || obj is TreeView)
            return GuiUtils.getItemsManager((Control)obj);
         else if (obj is LogicalControl)
            return ((LogicalControl)obj).ContainerManager;
         return null;
      }

      /// <summary>return true if it is input keys</summary>
      /// <param name="sender"></param>
      /// <param name="evtArgs"></param>
      private void setInputKeys(Object sender, EventArgs evtArgs)
      {
#if !PocketPC //TODO: handle for CF
         PreviewKeyDownEventArgs previewKeyDownEventArgs = (PreviewKeyDownEventArgs)evtArgs;
         bool IsInputKey = previewKeyDownEventArgs.IsInputKey;

         if (previewKeyDownEventArgs.Control)
         {
            switch (previewKeyDownEventArgs.KeyCode)
            {
               //In .net, we do not receive Ctrl + E, Ctrl + J, Ctrl + L, Ctrl + R KeyDown 
               //events if the control is ReadOnly
               case Keys.E:
               case Keys.J:
               case Keys.L:
               case Keys.R:
                  IsInputKey = true;
                  break;

               //If there is MDI, framework doesn't raise KeyDown event for 
               //Ctrl+F4 and Ctrl+F6 unless IsInputKey is set to 'true'.
               //Magic needs it to execute the event handlers.
               case Keys.F4:
               case Keys.F6:
                  IsInputKey = true;
                  break;

            }
         }

         if (previewKeyDownEventArgs.KeyCode == Keys.Tab)
            IsInputKey = true;
         else if (sender is ButtonBase || sender is MgLinkLabel || sender is MgPanel)
         {
            if (previewKeyDownEventArgs.KeyCode == Keys.Down ||
                previewKeyDownEventArgs.KeyCode == Keys.Up   ||
                previewKeyDownEventArgs.KeyCode == Keys.Left ||
                previewKeyDownEventArgs.KeyCode == Keys.Right)
               IsInputKey = true;
         }

         // if enter should be added as event, set IsInputKey, otherwise, Enter will trigger click event on default button.
         if (previewKeyDownEventArgs.KeyCode == Keys.Enter && !(sender is MgLinkLabel) && Events.AddEnterAsKeyEvent())
            IsInputKey = true;

         previewKeyDownEventArgs.IsInputKey = IsInputKey;
#endif
      }

      /// <summary>handle event</summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="evtArgs"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs evtArgs)
      {
         handleEvent(type, sender, evtArgs, null);
      }

      /// <summary>
      /// return TRUE if this control support focus in right mouse down
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      private bool supportFocusOnRightMouseDown(Control control)
      {
         bool FourceFocusOnMouseDown = false;
         if (control is TreeView || control is ListBox || control is RadioButton || control is MgComboBox || control is TextBox)
            FourceFocusOnMouseDown = true;

         return FourceFocusOnMouseDown;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="eventObject">object on which the event has occured</param>
      /// <param name="evtArgs"></param>
      /// <param name="mapData"></param>
      internal void handleEvent(EventType type, Object eventObject, EventArgs evtArgs, MapData mapData)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         Control control = eventObject as Control;
         if (mapData == null)
            mapData = controlsMap.getMapData(control);
         if (mapData == null)
            return;
         bool leftClickWasPressed;
         bool rightClickWasPressed;
         bool focusProcessed = false;

         GuiMgControl mgControl = mapData.getControl();
         GuiMgForm guiMgForm = mapData.getForm();
         checkAndStopDelay(type);

         Object guiMgObject = mgControl;
         if (guiMgObject == null)
            guiMgObject = guiMgForm;
         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgObject));
         try
         {
            switch (type)
            {
               case EventType.MOUSE_MOVE:
               case EventType.NODE_MOUSE_HOVER:
#if !PocketPC
                  if (type == EventType.MOUSE_MOVE && GuiUtils.ShouldPerformBeginDrag(control, (MouseEventArgs)evtArgs))
                     GuiUtils.BeginDrag(control, (MouseEventArgs)evtArgs, mapData);
#endif
                  handleTooltip(control, mgControl, mapData);
                  handleMouseOverLeave(control, mapData);
                  break;

               case EventType.MOUSE_LEAVE:
                  if (control != null)
                     resetTooltip(control);
                  break;

               case EventType.MOUSE_WHEEL:
                  // reset the tooltip of the control
                  if (control != null)
                  {
                     resetTooltip(control);
#if !PocketPC
                     HandleMouseWheelEvent(control, (MouseEventArgs)evtArgs);
#endif
                  }

                  break;

               case EventType.PREVIEW_KEY_DOWN:
                  setInputKeys(eventObject, evtArgs);
                  break;

               case EventType.KEY_DOWN:
                  handleKeyDown(control, mgControl, guiMgForm, (KeyEventArgs)evtArgs);
                  return;

               case EventType.KEY_PRESS:
                  if (!(control is TextBoxBase))
                     handleKeyPress(control, mgControl, guiMgForm, (KeyPressEventArgs)evtArgs);
                  return;

               case EventType.GOT_FOCUS:
                  if (mgControl != null)
                  {
                     // we should only perform processFocus when focus comes as a result of a click,
                     // if focusIn comes as a result of control.setFocus() we should not exectute it
                     GuiUtils.saveFocusingControl(GuiUtils.FindForm(control), mapData);
                  }
                  break;

               case EventType.MOUSE_DOWN:
#if PocketPC
               // On mobile we don't get mouse move events. We want to know about mouse events
               // before the usual control processing, to reset the incremental locate buffer
               ClientManager.Instance.EventsManager.addGuiTriggeredEvent(InternalInterface.MG_ACT_CLICK);
#endif
                  GuiUtils.checkAndCloseTreeEditorOnClick(control);
                  Object[] dotNetArgs = null;
                  //fixed bug#:263511, Click event doesn't need to executed on pressing on right mouse click
                  bool SupportFocusOnRightMouseDown = supportFocusOnRightMouseDown((Control)eventObject);
                  leftClickWasPressed = (((MouseEventArgs)evtArgs).Button == MouseButtons.Left);
                  rightClickWasPressed = (((MouseEventArgs)evtArgs).Button == MouseButtons.Right);
                  bool onMultiMark = CalculateOnMultimark(control, mgControl);

                 // Save the SaveLastClickInfo for LEFT and RIGHT click 
                 if (leftClickWasPressed || rightClickWasPressed)
                 {
                    // save last click 
                    if (mgControl != null && !mgControl.isSubform())
                    {
                        //QCR #:916951.Checking whether the control is sub form or not.
                        //if it is a subform then we will not be executing 'saveLastClickInfo' as sub form control 
                        // should not be saved as last clicked control.This behaviour is same as of Online.
                        if (!mgControl.isSubform())
                        {
                            MouseEventArgs mouseEvtArgs = (MouseEventArgs)evtArgs;
                            GuiUtils.SaveLastClickInfo(mapData, (Control)eventObject, new Point(mouseEvtArgs.X, mouseEvtArgs.Y));
                        }
                    }
                 }

                 if (leftClickWasPressed || (SupportFocusOnRightMouseDown && rightClickWasPressed))
                  {
                     if (mgControl != null && !mgControl.isSubform())
                     {                  
#if !PocketPC
                        if (leftClickWasPressed)
                           GuiUtils.AssessDrag(control, (MouseEventArgs)evtArgs, mapData);
#endif

                        // For TabControl, processSelection sets/restores selection, whereas processFocus sets focus
                        // to nextCtrl if tab non parkable.
                        if (leftClickWasPressed && mgControl.isTabControl())
                        {
                           Control invokeOnCtrl = null;

                           // If we have got a panel, we should proccess selection on tabcontrol
                           if (control is Panel && ((TagData)(control.Tag)).ContainerTabControl != null)
                              invokeOnCtrl = ((TagData)(control.Tag)).ContainerTabControl;
                           else if (control is TabControl)
                              invokeOnCtrl = control;

                           Debug.Assert(invokeOnCtrl is MgTabControl);

                           String val = null;

#if !PocketPC
                           // We want to handle CV/CS of last parked ctrl, parkability of tabCtrl etc, then switch tab if there was no
                           // stopexecution. So, we have cancelled the selecting event when clicked or tabbed, and save
                           // the selectingIdx. This selectingIdx is used in mousedown to switch the tab.
                           // If SelectingIdx is not Int32.MinValue, it means that the click is on a different tab. 
                           // So, use the new value.
                           // If SelectingIdx is Int32.MinValue, it means that the click is on the same tab (or its panel).
                           // So, directly get the value from the control.
                           if (((TagData)invokeOnCtrl.Tag).SelectingIdx != Int32.MinValue)
                           {
                              val = ((TagData)invokeOnCtrl.Tag).SelectingIdx.ToString();
                              ((TagData)invokeOnCtrl.Tag).SelectingIdx = Int32.MinValue;
                           }
                           else
#endif
                              val = ((TabControl)invokeOnCtrl).SelectedIndex.ToString();

                           Events.OnSelection(val, mgControl, mapData.getIdx(), false);
                           focusProcessed = true;
                        }

                        if (!focusProcessed && (GuiUtils.isFocusingControl(GuiUtils.FindForm(control), mapData) || mgControl.isTreeControl() || mgControl.isImageControl() || mgControl.IsDotNetControl()))
                        {
                           Events.OnFocus(mgControl, mapData.getIdx(), leftClickWasPressed, onMultiMark);
                           focusProcessed = true;
                        }

                        if (focusProcessed)
                           GuiUtils.removeFocusingControl(GuiUtils.FindForm(control));

                        if (!mgControl.IsDotNetControl())
                           resetTooltip(control);
                        if (leftClickWasPressed)
                           dotNetArgs = mgControl.IsDotNetControl() ? new Object[] { eventObject, evtArgs } : null;
                     }
                  }

 #if !PocketPC
                  // For multi mark, when the click isn't on the table control.
                  // Only for edit control.
                  Modifiers modifiers = GuiUtils.getModifier(Control.ModifierKeys);
                  if (mgControl != null && modifiers == Modifiers.MODIFIER_CTRL && mgControl.isTextControl() && mgControl.IsRepeatable && (!(control is TableControl)))
                  {
                     onMultiMark = true;
                     Events.OnMultiMarkHit(mgControl, mapData.getIdx() + 1, modifiers);
                  }
#endif
                  Events.OnMouseDown(guiMgForm, mgControl, dotNetArgs, leftClickWasPressed, mapData.getIdx(), onMultiMark, GuiUtils.canMouseDownProduceClick(control, (MouseEventArgs)evtArgs));

                  // just for the 'close tasks on parent click'. if focuse not done above, we call process Focus here just for 
                  // handling the child tasks on handleFocuse. (in online the whole close process and focus is initiated by the mg_act_hit/control_hit
                  // but in rich client, we might get to control_hit or not. and when we get to it , its the last thing (after the focus)
                  // instead of the first, where the user can also block the focus with a handler.
                  // if we change the flow in rich (mg_act_conrol_hit before all) then we must put the close child tasks there.
                  // we will only do this for static controls (otherwise we need to send here a special flag in the event).
                  // also, call the focus here only when the closeTasksOnParentActivate flag is on in order to send less events otherwise.
                  if (mgControl != null && !mgControl.isSubform() && !focusProcessed && (mgControl.isStatic() || (rightClickWasPressed && !SupportFocusOnRightMouseDown)) && Events.CloseTasksOnParentActivate())
                     Events.OnFocus(mgControl, 0, false, onMultiMark);

                  break;

               case EventType.MOUSE_UP:
                  if (mgControl != null)
                  {
                     Events.OnMouseUp(mgControl, mapData.getIdx());
                     if (mgControl.isTreeControl() || mgControl.isTableControl()
                        || GuiUtils.isOwnerDrawControl(mgControl))
                     {
                        ContainerManager itemManager = getContainerManager(mapData);
                        leftClickWasPressed = (((MouseEventArgs)evtArgs).Button == MouseButtons.Left);
                        if (leftClickWasPressed && itemManager != null && itemManager.isDoubleClick(mapData))
                           Events.OnDblClick(mgControl, mapData.getIdx());
                     }
#if !PocketPC
                     GuiUtils.ResetDragInfo(control);
#endif
                  }
                  break;

               case EventType.CHECK_STATE_CHANGED:
               case EventType.CLICK:
               case EventType.SELECTED_INDEX_CHANGED:
                  if (mgControl != null)
                  {
                     if (type == EventType.SELECTED_INDEX_CHANGED)
                        GuiUtils.setSuggestedValueOfChoiceControlOnTagData(control, GuiUtils.getValue(control));
                     Events.OnSelection(GuiUtils.getValue(control), mgControl, mapData.getIdx(), ((TagData)(control.Tag)).OnMouseDown);
                     GuiUtils.SetOnClickOnTagData(control, false);
                  }
                  break;

#if !PocketPC
               case EventType.MOUSE_DBLCLICK:
                  leftClickWasPressed = ((MouseEventArgs)evtArgs).Button == MouseButtons.Left;
                  if (mgControl != null && !GuiUtils.isOwnerDrawControl(mgControl) && leftClickWasPressed)
                     Events.OnDblClick(mgControl, mapData.getIdx());
                  break;

               case EventType.DRAG_OVER:
                  if (GuiUtils.handleDragOver(control, mgControl, mapData.getIdx()))
                     ((DragEventArgs)evtArgs).Effect = DragDropEffects.Copy;
                  else
                     ((DragEventArgs)evtArgs).Effect = DragDropEffects.None;
                  break;

               case EventType.DRAG_DROP:
                  GuiUtils.handleDragDrop(control, evtArgs, mapData);
                  break;

               case EventType.GIVE_FEEDBACK:
                  GuiUtils.SetDragCursor((GiveFeedbackEventArgs)evtArgs);
                  break;
#endif

               case EventType.DISPOSED:
                  if (guiMgForm != null)
                  {
                     controlsMap.remove(guiMgForm);

                     if (control is Control)
                        GuiUtils.clearContextMenu(control);
                  }
                  else if (control != null)
                  {
                     // fixed bug #:414095(915499), when control is dispose we need to reset the menu so the
                     // control.dispose will not dispose the menu that connect to the control
                     // when the shell will be dispose the all menus will be dispose also.           
                     if (control is Control)
                        GuiUtils.clearContextMenu(control);

                     // remove events for .net controls.
                     if (((TagData)control.Tag).IsDotNetControl)
                        DNManager.getInstance().DNObjectEventsCollection.removeEvents(control);

                     // do not dispose Editors
                     if (!((TagData)control.Tag).IsEditor)
                     {
                        control.Tag = null;
                        controlsMap.remove(mgControl, mapData.getIdx());
                     }
                  }
                  break;

               case EventType.PRESS:
                  if (mgControl != null && !mgControl.isSubform())
                  {
                     // save control info and co-ords on which press event is fired
                     GuiUtils.SaveLastClickInfo(mapData, (Control)eventObject, new Point(0, 0));
                  }
                  
                  if ((mgControl != null) && (GuiUtils.isFocusingControl(GuiUtils.FindForm(control), mapData) ||
                      mgControl.isTreeControl() || mgControl.isImageControl()))
                  {
                     // move the focus
                     Events.OnFocus(mgControl, mapData.getIdx(), true, false);
                     GuiUtils.removeFocusingControl(GuiUtils.FindForm(control));
                  }

                  // raise press event
                  Events.OnPress(guiMgForm, mgControl, mapData.getIdx());
                  break;
            }
         }
         finally
         {
            contextIDGuard.Dispose();
         }
      }

      private static bool CalculateOnMultimark(Control control, GuiMgControl mgControl)
      {
         bool onMultiMark = false;
         if (mgControl != null && mgControl.IsRepeatable)
         {
            if (control is TableControl)
               onMultiMark = true;
         }
         
         return onMultiMark;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="mgControl"></param>
      /// <param name="form"></param>
      /// <param name="keyEventArgs"></param>
      private void handleKeyDown(Control control, GuiMgControl mgControl, GuiMgForm guiMgForm, KeyEventArgs keyEventArgs)
      {
         if (keyEventArgs.KeyCode == Keys.None)
            return;
         else if (KbdConvertor.isModifier(keyEventArgs.KeyCode))
            return;

         // check if we need to ignore keyDown events.
         // (for ignoring key down sent by GuiInteractive.onPostKeyEvent)
         bool ignoreKeyDown = control.Tag is TagData && ((TagData)control.Tag).IgnoreKeyDown;
         // should we ignore the key pressed ?
         if (ignoreKeyDown)
         {
            // modifier is ignored and the flag is unchaged
            // a non modifier only key also sets the flag to false:
            // next key down will not be ignored.
            ((TagData)control.Tag).IgnoreKeyDown = false;

            return;
         }

         Modifiers modifiers = GuiUtils.getModifier(keyEventArgs.Modifiers);

         // In .Net, if the child of a TabControl is in focus, keyDown event on 
         // this child is received twice: one for the TabControl and one for the child.
         // So, we need to ignore the keydown on the TabControl, if the childControl is in focus.
         // For Ctrl + Tab and Ctrl + Shift + Tab, key down event comes only once so we need to handle those keydown events.
         if (control is TabControl &&
               !(keyEventArgs.KeyCode == Keys.Tab && (modifiers == Modifiers.MODIFIER_CTRL || modifiers == Modifiers.MODIFIER_SHIFT_CTRL)))
         {
            if (!control.Focused)
               return;
         }

         // In case of tab we do not revieve any other event, process it here
         TraverseMode traversMode = doTraverse(mgControl, keyEventArgs.KeyCode, control, modifiers);
         if (traversMode != TraverseMode.NONE)
         {
            String suggestedValue = "-1";
            bool ComboIsDrowDown = false;

            if (traversMode == TraverseMode.WITHIN_CONTROL)
            {
               suggestedValue = getNextSuggestedValue(control, keyEventArgs.KeyCode, modifiers);
               ComboIsDrowDown = (control is MgComboBox ? ((MgComboBox)control).DroppedDown : false);
            }

            Events.OnKeyDown(null, mgControl, modifiers, keyEventArgs.KeyValue, suggestedValue, ComboIsDrowDown, keyEventArgs.Handled);
            keyEventArgs.Handled = true;
            suppressKeyPress(keyEventArgs, control);
            return;
         }

         // #281096. Magic should process TAB keys as well when it is received on a form.
         // The code has been derieved from SWT and in SWT for TAB key we had separate Event
         // named "Traverse" and hence we didn't handle TAB key in KeyDown.
         // Tab key is already handled in above block and we should process the same.
         if (!(keyEventArgs.KeyCode == Keys.F10 && modifiers == Modifiers.MODIFIER_SHIFT))
         {
            bool isActChar = false;
            String pressedChar = null;
            int start = 0, end = 0;

            // only for Text control ,there is a possibility for ACT_CHAR event
            if (mgControl != null && mgControl.IsDotNetControl())
            {   // for dot net controls - no specific handling
            }
            else if (control is TextBoxBase)
            {
               TextBoxBase textCtrl = (TextBoxBase)control;
               // is it a printable char (not sure it will fit DBCS).
               // Alt+shift or ctrl+shift will not produce act_char.
               if (modifiers == Modifiers.MODIFIER_NONE || modifiers == Modifiers.MODIFIER_SHIFT)
               {
                  // char keys (i.e. not control keys) will be handled in KEY_PRESS.
                  if (!isControlChar(keyEventArgs))
                     return;
               }

               // start/end position come from the widget (not the event).
               // not only for typing, also for copy and paste, cut and selection
               start = textCtrl.SelectionStart;
               end = textCtrl.SelectionStart + textCtrl.SelectionLength;
               keyEventArgs.Handled = true;

               if ((keyEventArgs.Modifiers & Keys.Alt) == 0)
               {
                  bool supressKeyPress = true;

                  // For Alt modifier, it must not be supressed, otherwise OS does not handle accelerators (#981390).
                  // QCR #913765 - All combination of modifiers with Alt must not be suppersed for languages like Polish to work 

                  // Since '+' and '-' keys are treated as control char [QCR#317367],
                  // handlers will be executed (if defined). But at the same time, key should not be suppressed.
                  // The '+' & '-' should be treated as as both as control key and char [ same as in unipaas 1.9]
                  //Defect 125502 : Previously this fix was made only for JPN env. Now, we are making it common for all environments.
                  if ((keyEventArgs.KeyCode == Keys.Add || keyEventArgs.KeyCode == Keys.Subtract) && !ShouldSupressNumpadPlus())
                      supressKeyPress = false;

                  if (supressKeyPress)
                     suppressKeyPress(keyEventArgs, control);
               }
            }
            else if (control is TreeView)
            {
               if ((keyEventArgs.Modifiers & Keys.Alt) == 0)
               {
                  keyEventArgs.Handled = true;
                  // allow keyPress when char is press in order to insert keyboard event with a char. (here below, the char would be empty)
                  if (isControlChar(keyEventArgs))
                     suppressKeyPress(keyEventArgs, control);
               }
            }
            else
            {
               //pressing F10, will not highlight menubar for any control. keeping the behavior same for other ctrls like text box.
               if (keyEventArgs.KeyCode == Keys.F10 && modifiers == Modifiers.MODIFIER_NONE)
                  keyEventArgs.Handled = true;

               if (keyEventArgs.KeyCode == Keys.F4 && modifiers == Modifiers.MODIFIER_ALT)
                  keyEventArgs.Handled = true;

               if (modifiers == Modifiers.MODIFIER_NONE || modifiers == Modifiers.MODIFIER_SHIFT)
               {
                  // for all other ctrls, char keys (i.e. not control keys) pressed , make isActChar true.
                  if (!isControlChar(keyEventArgs))
                  {
                     return;
                  }
               }
            }


            //fixed bug #721680:
            if (shouldSuppressKeyPress(keyEventArgs))
               suppressKeyPress(keyEventArgs, control);

            keyEventArgs.Handled = Events.OnKeyDown(guiMgForm, mgControl, modifiers, keyEventArgs.KeyValue, start, end, pressedChar, isActChar, "-1", keyEventArgs.Handled);
         }

#if !PocketPC && DEBUG
         // the user presses CTRL+SHIFT+J
         if (keyEventArgs.KeyCode == Keys.J && (keyEventArgs.Modifiers & Keys.Control) > 0 && (keyEventArgs.Modifiers & Keys.Shift) > 0)
         {
            // if the special console window can be shown, show it
            if (Manager.CanOpenInternalConsoleWindow)
            {
               Console.ConsoleWindow _instance = Console.ConsoleWindow.getInstance();
               if (_instance != null)
                  _instance.toggleVisibility();
            }
            else
               Events.ShowSessionStatisticsForm();
         }
#endif

         return;
      }

      /// <summary>
      /// This function decides whether we want to supressKeyPress for NumpadPlus.
      /// When the SpecialNumpadPlusGetCharacter flag is set to Y then we allow to type '+'.
      /// The flag is only relevant for Online
      /// </summary>
      /// <returns></returns>
      private static bool ShouldSupressNumpadPlus()
      {
          bool shouldSupress = false;

          if (!Manager.Environment.SpecialNumpadPlusChar)
              shouldSupress = true;

          return shouldSupress;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="mgControl"></param>
      /// <param name="form"></param>
      /// <param name="keyPressEventArgs"></param>
      private void handleKeyPress(Control control, GuiMgControl guiMgControl, GuiMgForm guiMgForm, KeyPressEventArgs keyPressEventArgs)
      {
         bool ignoreKeyPress = ((TagData)control.Tag).IgnoreKeyPress;
         // should we ignore the key pressed ?
         if (ignoreKeyPress)
         {
            ((TagData)control.Tag).IgnoreKeyPress = false;
            return;
         }

         int start = 0, end = 0;
         // skip control key
         if (Char.IsControl(keyPressEventArgs.KeyChar))
            return;

         // In .Net, if the child of a TabControl is in focus, keyDown event on 
         // this child is received twice: one for the TabControl and one for the child.
         // So, we need to ignore the keydown on the TabControl, if the childControl is in focus.
         if (control is TabControl && !control.Focused)
            return;

         String pressedChar = "" + keyPressEventArgs.KeyChar;

         // flag the isActChar to indicate this is MG_ACT_CHAR
         keyPressEventArgs.Handled = Events.OnKeyDown(guiMgForm, guiMgControl, Modifiers.MODIFIER_NONE, 0, start, end, pressedChar, true, "-1", keyPressEventArgs.Handled);

         // in case of tree control, we are only interested in the keyboard event, not in the potential effects of keyPress (like changing the selected tree node)
         if (control is TreeView)
            keyPressEventArgs.Handled = true;
      }

      /// <summary></summary>
      /// <param name="keyEventArgs"></param>
      internal static void suppressKeyPress(KeyEventArgs keyEventArgs, Control control)
      {
#if !PocketPC
         keyEventArgs.SuppressKeyPress = true;
#else
         // Remove messages from message queue, using PeekMessage. Same thing as done in Windows.
         com.magicsoftware.win32.NativeWindowCommon.Message msg = new com.magicsoftware.win32.NativeWindowCommon.Message();
         while (NativeWindowCommon.PeekMessage(out msg, control.Handle,
                                                       NativeWindowCommon.WM_CHAR,
                                                       NativeWindowCommon.WM_CHAR,
                                                       NativeWindowCommon.PM_REMOVE))
         { } // NULL loop
         while (NativeWindowCommon.PeekMessage(out msg, control.Handle,
                                                       NativeWindowCommon.WM_SYSCHAR,
                                                       NativeWindowCommon.WM_SYSCHAR,
                                                       NativeWindowCommon.PM_REMOVE))
         { } // NULL loop
         while (NativeWindowCommon.PeekMessage(out msg, control.Handle,
                                                       NativeWindowCommon.WM_IME_CHAR,
                                                       NativeWindowCommon.WM_IME_CHAR,
                                                       NativeWindowCommon.PM_REMOVE))
         { } // NULL loop
#endif
      }
       
      /// <summary>
      /// define each keys is ignore
      /// </summary>
      /// <param name="keyEventArgs"></param>
      /// <returns></returns>
      private static bool shouldSuppressKeyPress(KeyEventArgs keyEventArgs)
      {
         bool suppressKeyPress = false;
         //the alt + back, in menu is unexpected behavior
         suppressKeyPress = ((keyEventArgs.Modifiers & Keys.Alt) == Keys.Alt && keyEventArgs.KeyCode == Keys.Back);

         // Since '+' and '-' keys are treated as control char [QCR#317367],
         // handlers will be executed (if defined). But at the same time, key should not be suppressed.
         // The '+' & '-' should be treated as as both as control key and char [ same as in unipaas 1.9]
         //Defect 125502 : Previously this fix was made only for JPN env. Now, we are making it common for all environments.
         if (keyEventArgs.KeyCode == Keys.Add || keyEventArgs.KeyCode == Keys.Subtract)
            suppressKeyPress = ShouldSupressNumpadPlus();

         return suppressKeyPress;
      }

      /// <param name="keyEventArgs"></param>
      /// <returns>true iff the typed character is a control character</returns>
      private static bool isControlChar(KeyEventArgs keyEventArgs)
      {
         bool ret = true;

         /**
          *  PockectPC doen't support ToAscii(). But, even if ToAscii() is supported in
          *  Standard Framework, it has many problems:
          *  
          *  1. QCR #754580 & #783617 --- The problem is where the characters are produced
          *  as a result of 2 keys. The first is known as DeadKey and there is one more.
          *  Now when we get KeyDown event for the dead key and we call ToAscii(), the dead
          *  key information is removed. So, the second key is considered as a fresh key.
          *  
          *  2. The code in the following "#if false" considered 'INS', 'DEL', etc. keys
          *  as non-control keys. So, we didn't handle them in KeyDown. And, we never received
          *  KeyPress event for these keys. So, these keys were always handled by the CLR. As
          *  a result, System Event Handlers based on these keys didn't work.
          *  
          *  The bottomline is that we should not use ToAscii() even for Standard Framework.
          *  We will use the same code that we use for PockectPC --- may be temporary, may be 
          *  permanently :)
          */
#if false
         StringBuilder sb = new StringBuilder();
         byte[] keyState = new byte[256];
         user32.GetKeyboardState(keyState);
         if (user32.ToAscii((uint)keyEventArgs.KeyCode, 0, keyState, sb, 0) == 1)
         {
            char chr = (char)sb.ToString()[0];
            ret = Char.IsControl(chr);
         }
#endif

            //TODO: research further ASAP:
            // PockectPC does not have ToAscii. Until a better way is found, just go over the
            // key code and try to guess which are controls and which are real characters
            switch (keyEventArgs.KeyCode)
            {
               case Keys.Space:
               // regular numbers
               case Keys.D0:
               case Keys.D1:
               case Keys.D2:
               case Keys.D3:
               case Keys.D4:
               case Keys.D5:
               case Keys.D6:
               case Keys.D7:
               case Keys.D8:
               case Keys.D9:
               //regular letters
               case Keys.A:
               case Keys.B:
               case Keys.C:
               case Keys.D:
               case Keys.E:
               case Keys.F:
               case Keys.G:
               case Keys.H:
               case Keys.I:
               case Keys.J:
               case Keys.K:
               case Keys.L:
               case Keys.M:
               case Keys.N:
               case Keys.O:
               case Keys.P:
               case Keys.Q:
               case Keys.R:
               case Keys.S:
               case Keys.T:
               case Keys.U:
               case Keys.V:
               case Keys.W:
               case Keys.X:
               case Keys.Y:
               case Keys.Z:
               // numpad numbers
               case Keys.NumPad0:
               case Keys.NumPad1:
               case Keys.NumPad2:
               case Keys.NumPad3:
               case Keys.NumPad4:
               case Keys.NumPad5:
               case Keys.NumPad6:
               case Keys.NumPad7:
               case Keys.NumPad8:
               case Keys.NumPad9:
               // numpad math operators
               case Keys.Multiply:
               case Keys.Separator:
               case Keys.Decimal:
               case Keys.Divide:
               // Characters not included in System.Windows.Forms.Keys
               case (Keys)0x9c: // == oe
               case (Keys)0xa2: // == cent sign
               case (Keys)0xa3: // == pound
               case (Keys)0xa4: // == currency sign
               case (Keys)0xa5: // == yen
               case (Keys)0xa7: // == section sign
               case (Keys)0xac: // == euro
               case (Keys)0xb0: // == degrease
               case (Keys)0xb1: // == +-
               case (Keys)0xb5: // == greek mu
               case (Keys)0xb6: // == pilcrow sign
               case (Keys)0xba: // == ';'
               case (Keys)0xbb: // == '+'
               case (Keys)0xbc: // == ','
               case (Keys)0xbd: // == '-'
               case (Keys)0xbe: // == '.'
               case (Keys)0xbf: // == '/'
               case (Keys)0xc0: // == '`'
               case (Keys)0xdb: // == '['
               case (Keys)0xdc: // == '\'
               case (Keys)0xdd: // == ']'
               case (Keys)0xde: // == '''
               case (Keys)0xdf: // == beta
               // european characters part
               case (Keys)0xe0: // == 
               case (Keys)0xe1: // == 
               case (Keys)0xe2: // == 
               case (Keys)0xe3: // ==     
               case (Keys)0xe4: // ==     
               case (Keys)0xe6: // ==     
               case (Keys)0xe7: // ==     
               case (Keys)0xe8: // ==     
               case (Keys)0xe9: // ==     
               case (Keys)0xea: // ==     
               case (Keys)0xeb: // ==     
               case (Keys)0xec: // ==     
               case (Keys)0xed: // ==     
               case (Keys)0xee: // ==     
               case (Keys)0xef: // ==     
               case (Keys)0xf0: // ==     
               case (Keys)0xf1: // ==     
               case (Keys)0xf2: // ==     
               case (Keys)0xf3: // ==     
               case (Keys)0xf4: // ==     
               case (Keys)0xf5: // ==     
               case (Keys)0xf6: // ==     
               case (Keys)0xf8: // ==     
               case (Keys)0xf9: // ==     
               case (Keys)0xfa: // ==     
               case (Keys)0xfb: // ==     
               case (Keys)0xfc: // ==     
               case (Keys)0xfd: // ==     
               case (Keys)0xfe: // ==     
               // Value passed for all chars by Mobile hebrew keyboard
               case (Keys)0xff:
                  ret = false;
                  break;

#if PocketPC
            case (Keys)0x0c:
               if (UtilStrByteMode.isLocaleDefLangJPN())
                  ret = false;
               else
                  ret = true;
               break;
#endif
               default:
                  ret = true;
                  break;
         }

         return ret;
      }

      /// <summary>handle the tooltip to be display</summary>
      /// <param name="control"></param>
      /// <param name="ctrl"></param>
      /// <param name="mapData"></param>
      private void handleTooltip(Control control, GuiMgControl mgControl, MapData mapData)
      {
#if !PocketPC
         ControlsMap controlsMap = ControlsMap.getInstance();
         String tooltipStr = null;

         if (mgControl != null)
         {
            Object obj = controlsMap.object2Widget(mgControl, mapData.getIdx());
            if (obj is LogicalControl)
            {
               if (((LogicalControl)obj).Visible)
                  tooltipStr = ((LogicalControl)obj).Tooltip;
            }
            else if (obj is Control && ((Control)obj).Tag is TagData)
               tooltipStr = ((TagData)((Control)obj).Tag).Tooltip;
         }
         if (mgControl == null || !mgControl.IsDotNetControl())
            GuiUtils.setTooltip(control, tooltipStr);
#endif
      }

      /// <summary>reset the tooltip</summary>
      /// <param name="control"></param>
      private void resetTooltip(Control control)
      {
#if !PocketPC
         GuiUtils.setTooltip(control, "");
#endif
      }

      /// <summary>create mouse over and mouse leave events</summary>
      /// <param name="control"></param>
      /// <param name="mapData"></param>
      private void handleMouseOverLeave(Control control, MapData mapData)
      {
         Form form = GuiUtils.FindForm(control);
         TagData td = (TagData)form.Tag;
         MapData lastMapData = td.LastMouseOver;
         GuiMgControl lastControl = (lastMapData == null ? null : lastMapData.getControl());
         GuiMgControl newControl = mapData.getControl();
         if (lastControl != newControl)
         {
            if (lastControl != null)
               Events.OnMouseOut(lastControl);
            if (newControl != null)
               Events.OnMouseOver(newControl);
            td.LastMouseOver = mapData;
         }
      }

      /// <summary>if there is a delay in progress,  some events will stop it.</summary>
      /// <param name="type"></param>
      private void checkAndStopDelay(EventType type)
      {
         switch (type)
         {
            case EventType.MOUSE_DOWN:
            case EventType.CLOSED:
            case EventType.MOUSE_WHEEL:
            case EventType.MOUSE_DBLCLICK:
            //case EventType.RESIZE: //stops delay when window opens
            case EventType.KEY_DOWN:
            case EventType.CLICK:
            case EventType.PREVIEW_KEY_DOWN:
            //case EventType.ACTIVATED:  // Qcr #931421. Make it work like in online.
            case EventType.SPLITTER_MOVED:
            case EventType.SELECTED_INDEX_CHANGED:
            case EventType.CHECK_STATE_CHANGED:
            case EventType.MOVE:
            case EventType.COLUMN_CLICK:
            case EventType.MENU_OPENING:
            case EventType.REORDER_STARTED:
            case EventType.LINK_CLICKED:
               //todo minimize, maximize - there is no events for them on form, need to be implemented manually

               // activate delay object
               Object delayObject = Manager.GetDelayWait();
               Monitor.Enter(delayObject);
               try
               {
                  Monitor.Pulse(delayObject);
               }
               finally
               {
                  Monitor.Exit(delayObject);
               }
               break;
         }
      }

#if !PocketPC
      ///<summary>
      ///  Handle Mouse wheel event.
      ///</summary>
      ///<param name="control">control</param>
      ///<param name="evtArgs">event arguments</param>      
      private void HandleMouseWheelEvent(Control control, MouseEventArgs evtArgs)
      {
         Object guiMgObject = null;
         Control ctrlForMapData = control;

         if (control is MgRadioButton) // MapData for RadioButton should be retrieved from RadioPanel.
            ctrlForMapData = control.Parent;

         MapData mapData = ControlsMap.getInstance().getMapData(ctrlForMapData);
         if (mapData == null)
            return;

         guiMgObject = mapData.getControl();
         if (guiMgObject == null)
            guiMgObject = mapData.getForm();

         // Check the event args type. e.g  'DevExpress.Utils.OfficeMouseWheelEventArgs' cannot not cast to 'System.Windows.Forms.HandledMouseEventArgs'
         // handle event in case of HandledMouseEventArgs only
         if (evtArgs is HandledMouseEventArgs)
         {
            if (!((HandledMouseEventArgs)evtArgs).Handled)
            {
               bool isTableScroll = false;
               bool isPageScroll = false;
               Modifiers modifiers = GuiUtils.getModifier(Control.ModifierKeys);
               switch (modifiers)
               {
                  case Modifiers.MODIFIER_CTRL:
                  case Modifiers.MODIFIER_SHIFT_CTRL:
                  case Modifiers.MODIFIER_ALT_CTRL:
                     isTableScroll = true;
                     break;

                  case Modifiers.MODIFIER_SHIFT:
                  case Modifiers.MODIFIER_ALT_SHIFT:
                     isPageScroll = true;
                     break;
               }

               int nosOfRowsScrolled = (-evtArgs.Delta / 120) * SystemInformation.MouseWheelScrollLines;

               if (Events.OnScrollTable(guiMgObject, mapData.getIdx(), nosOfRowsScrolled, isPageScroll, isTableScroll, true))
                  ((HandledMouseEventArgs)evtArgs).Handled = true;
            }
         }
      }
#endif
   }
}
