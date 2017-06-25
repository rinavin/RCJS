using System;
using System.Windows.Forms;
using com.magicsoftware.controls;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> handler for combo
   /// 
   /// </summary>
   /// <author>  rinav</author>
   internal class ComboHandler : HandlerBase
   {
      private static ComboHandler _instance;
      internal static ComboHandler getInstance()
      {
         if (_instance == null)
            _instance = new ComboHandler();

         return _instance;
      }

      /// <summary>
      /// 
      /// </summary>
      private ComboHandler()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="comboBox"></param>
      internal void addHandler(MgComboBox comboBox)
      {
         comboBox.MouseMove += MouseMoveHandler;
#if !PocketPC
         comboBox.MouseEnter += MouseEnterHandler;
         comboBox.MouseHover += MouseHoverHandler;
         comboBox.MouseLeave += MouseLeaveHandler;
         comboBox.MouseWheel += MouseWheelHandler;
         comboBox.MouseDoubleClick += MouseDoubleClickHandler;
         comboBox.PreviewKeyDown += PreviewKeyDownHandler;
         comboBox.DragOver += DragOverHandler;
         comboBox.DragDrop += DragDropHandler;
         comboBox.GiveFeedback += GiveFeedBackHandler;
         comboBox.DropDownClosed += DropDownClosedHandler;
         comboBox.DropDown += DropDownHandler;
         comboBox.MouseDownOnDropDownList += MouseDownOnDropDownListHandler;   
#endif
         comboBox.MouseDown += MouseDownHandler;
         comboBox.MouseUp += MouseUpHandler;
         comboBox.Click += ClickHandler;
         comboBox.Disposed += DisposedHandler;
         comboBox.KeyDown += KeyDownHandler;
         comboBox.KeyPress += KeyPressHandler;
         comboBox.GotFocus += GotFocusHandler;
         comboBox.SelectedIndexChanged += SelectedIndexChangedHandler;
         comboBox.Resize += ResizeHandler;
      }

      /// <summary>
      /// a click was on the list box inside the combo box
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void MouseDownOnDropDownListHandler(object sender, EventArgs e)
      {
         ComboBox comboBox = (ComboBox)sender;
         TagData tag = ((TagData)comboBox.Tag);
         tag.ClickOnComboDropDownList = true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         MgComboBox comboBox = (MgComboBox)sender;
         MapData mapData = controlsMap.getMapData(comboBox);
         if (mapData == null)
            return;

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(mapData.getControl()));
         try
         {
            switch (type)
            {
               case EventType.RESIZE:
                  GuiMgControl mgControl = mapData.getControl();
                  if (mgControl.IsTableHeaderChild)
                     comboBox.Invalidate();
                  break;

               case EventType.MOUSE_DOWN:
                  ((TagData)comboBox.Tag).HandleOnDropDownClosed = false;

                  GuiUtils.saveFocusingControl(GuiUtils.FindForm(comboBox), mapData);
                  DefaultHandler.getInstance().handleEvent(type, sender, e);

                  // QCR #713073 Process drop down only for left mouse click. right click opens the context menu.
                  if (((MouseEventArgs)e).Button == MouseButtons.Left)
                  {
#if !PocketPC
                     // In mouse down event, we initiate drag and since we are returning from here,
                     // we need to handle it here itself as it won't call defaulthandler.handleEvent.
                     GuiUtils.AssessDrag((Control)sender, (MouseEventArgs)e, mapData);
#endif

                     // QCR #927727, this will prevent openning combo for non parkable conrols
                     Events.OnComboDroppingDown(mapData.getControl(), mapData.getIdx());
                  }
                  return;

               case EventType.DROP_DOWN:
                  // when the drop down is open reset the member
                  ((TagData)comboBox.Tag).ClickOnComboDropDownList = false;
                  break;

               case EventType.DROP_DOWN_CLOSED:
                  OnDropDownClosed(comboBox, e);
                  break;

               case EventType.KEY_DOWN:
                  if (OnkeyDown(sender, e))
                     return;
                  break;

               case EventType.SELECTED_INDEX_CHANGED:
                  ((TagData)comboBox.Tag).HandleOnDropDownClosed = false;
                  TagData tg = ((TagData)comboBox.Tag);
                  tg.IgnoreTwiceClickWhenToValueIs = GuiUtils.getValue(comboBox);
                  //fixed bug #:782615, when type char, we get click_index_change in spite of the index isn't changed.
                  //                    ignore when the value isn't real changed
                  //While creating combo control, if you try to select the option from combo using key, 
                  //ListControlOriginalValue is not initialized yet. So, return.
                  if (tg.ListControlOriginalValue == null || tg.ListControlOriginalValue.Equals(tg.IgnoreTwiceClickWhenToValueIs))
                     return;

                  //we must this fixed for bug #:768284, we get twice before the GuiCommandQueue.setselection(comboBox) was called.
                  //                                     so the value on tg.ComboBoxOriginalValue isn't updated yet.
                  if (tg.IgnoreTwiceClick)
                  {
                     if (tg.IgnoreTwiceClickWhenToValueIs == tg.IgnoreTwiceClickWhenFromValueIs)
                     {
                        tg.IgnoreTwiceClickWhenFromValueIs = tg.IgnoreTwiceClickWhenToValueIs;
                        tg.IgnoreTwiceClick = false;
                        return;
                     }
                     else
                        tg.IgnoreTwiceClickWhenFromValueIs = tg.IgnoreTwiceClickWhenToValueIs;
                  }
                  break;

               case EventType.PRESS:
                  // save current control as last focused control
                  if (!comboBox.Focused)
                     GuiUtils.saveFocusingControl(GuiUtils.FindForm(comboBox), mapData);
                  break;
            }
         }
         finally
         {
            contextIDGuard.Dispose();
         }

         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      /// <returns></returns>
      private bool OnkeyDown(Object sender, EventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         MgComboBox comboBox = (MgComboBox)sender;
         MapData mapData = controlsMap.getMapData(comboBox);
         bool ignoreEvent = false;
         bool isDropped = false;
         KeyEventArgs keyEventArgs = (KeyEventArgs)e;

         // Definition of the behavior (the same in on line)
         // ---------------------------------------------
         // When NOT Dropped :
         //                   page down or up : move to the next\prev record
         //                   up,down,left,right : move to the next\prev field
         //                   Home, End : select the first\end options in the popup (don't need to do nothing)
         // When Is Dropped :
         //                   page down or up : move to the next\prev page in the dropped
         //                   up,down,left,right : move to the next\prev option in the dropped
         //                   Home, End : select the first\end options in the popup(don't need to do nothing)

         TagData tg = ((TagData)comboBox.Tag);
         ((TagData)comboBox.Tag).HandleOnDropDownClosed = true;

         isDropped = comboBox.DroppedDown;

         //Fixed bug #:768284, unexpected behavior of .NET: when Dropped Down is open and press key (navigation\char)
         //                    and in the selection index change open message box\modal window,  we get selection index
         //                    change twice to the same control ignore the second time
         tg.IgnoreTwiceClick = false;
         if (isDropped)
         {
            tg.IgnoreTwiceClickWhenFromValueIs = GuiUtils.getValue(comboBox);
            tg.IgnoreTwiceClick = true;
         }

         switch (keyEventArgs.KeyCode)
         {
            case Keys.Left:
            case Keys.Right:
            case Keys.Down:
            case Keys.Up:
               //Check whether up/down arrow key is presses with ALT.
               if (eventForOpenClosePopup(keyEventArgs))
               {
                  //If combo is not dropped down, then open it, else will be closed.
                  if (!isDropped)
                  {
                     Events.OnComboDroppingDown(mapData.getControl(), mapData.getIdx());
                     keyEventArgs.Handled = true;
                  }
               }
               else
                  keyEventArgs.Handled = true;
               break;

            case Keys.PageUp:
            case Keys.PageDown:
            case Keys.Home:
            case Keys.End:
               keyEventArgs.Handled = true;
               break;

            case Keys.F4:
               // QCR #798995, f4 in windows opens combo and this is unwanted behaviour
               keyEventArgs.Handled = true;
               break;

            case Keys.Tab:
               //we need to handle the key.Tab so no need to update ignoreEvent
               if (comboBox.DroppedDown)
                  CloseComboDropDownForSpecialKeys(comboBox, e);
               break;

            case Keys.Escape:
               ignoreEvent = CloseComboDropDownForSpecialKeys(comboBox, e);
               break;

            case Keys.Enter:
               // in case of selection task, Enter on an open combo should only select. we don't want to exit the task.
               if (comboBox.DroppedDown)
               {
                  ((TagData)comboBox.Tag).IgnoreKeyDown = true;
                  ((TagData)comboBox.Tag).HandleOnDropDownClosed = false;
               }
               break;

            default:
               // ctrl + key, focuses on the value starting with this char on .net. But in online it doesn't.
               // Also, ctrl+M, might change query to modify and them focus on 'm'. We don't want that.
               if (keyEventArgs.Control)
                  DefaultHandler.suppressKeyPress(keyEventArgs, (Control)sender);

               break;
         }

         return ignoreEvent;
      }

      /// <summary>
      /// handle the esc\tab while the combo box is opened
      /// </summary>
      /// <param name="Combo"></param>
      /// <param name="event_Renamed"></param>
      /// <returns></returns>
      private bool CloseComboDropDownForSpecialKeys(MgComboBox Combo, EventArgs event_Renamed)
      {
         bool ignoreEvent = Combo.DroppedDown;

         if (Combo.DroppedDown)
         {
            //Fixed bug #:992432 (in online bug #:771131, fixed in guiCmbo.cpp v45)            
            //for DOTNet we need to set Combo.DroppedDown = false so the new value will
            // be set to the combo and not to the dropDownCombo         
            Combo.DroppedDown = false;

            ((TagData)Combo.Tag).IgnoreTwiceClickWhenFromValueIs = "";
            ((TagData)Combo.Tag).IgnoreTwiceClickWhenToValueIs = "";
         }
         return ignoreEvent;
      }

      /// <summary>
      /// handle the esc\tab while the combo box is opened
      /// </summary>
      /// <param name="Combo"></param>
      /// <param name="event_Renamed"></param>
      /// <returns></returns>
      private void OnDropDownClosed(MgComboBox Combo, EventArgs event_Renamed)
      {
         TagData tag = (TagData)Combo.Tag;

         // Fixed bug#:994737,912854  when combo box is closed by click on drop down we will get event SELECTED_INDEX_CHANGED with the correct value
         if (tag.ClickOnComboDropDownList)
         {
            tag.ClickOnComboDropDownList = false;
            return;
         }
   
         // fixed bug#:994737, when the user is use the mouse the selection need to be handle
         // by event SELECTED_INDEX_CHANGED and not DropDownClosed 
         if (tag.HandleOnDropDownClosed)
         {
            //get the org value that on the combo box

            tag.IgnoreTwiceClickWhenToValueIs = GuiUtils.getValue(Combo);
            //fixed bug #:939740,when the IgnoreTwiceClickWhenToValueIs or IgnoreTwiceClickWhenFromValueIs are null and the 
            // ListControlOriginalValue is equal to IgnoreTwiceClickWhenToValueIs we need to ignore the set select see the "case EventType.SELECTED_INDEX_CHANGED:"
            if (tag.IgnoreTwiceClickWhenToValueIs == null || tag.IgnoreTwiceClickWhenFromValueIs == null ||
                tag.ListControlOriginalValue.Equals(tag.IgnoreTwiceClickWhenToValueIs))
               return;

            String OrgValue = tag.ListControlOriginalValue;
            //set the value to the real combo box
            int OrgValueInt = Int32.Parse(OrgValue);
            GuiUtils.setSelect(Combo, OrgValueInt);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="keyEventArgs"></param>
      /// <returns></returns>
      private bool eventForOpenClosePopup(KeyEventArgs keyEventArgs)
      {
         return (keyEventArgs.Alt && (keyEventArgs.KeyCode == Keys.Up || keyEventArgs.KeyCode == Keys.Down));
      }

      /// <summary>
      /// event for oppenning combo
      /// </summary>
      /// <param name="keyEventArgs"></param>
      /// <returns></returns>
      private bool eventForOpenPopup(KeyEventArgs keyEventArgs)
      {
         return (keyEventArgs.Alt &&  keyEventArgs.KeyCode == Keys.Down);
      }

   }
}
