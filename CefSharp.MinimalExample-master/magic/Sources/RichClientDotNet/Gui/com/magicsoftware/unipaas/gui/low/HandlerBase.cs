using System;
using System.Windows.Forms;
using System.ComponentModel;
using com.magicsoftware.controls;
using com.magicsoftware.controls.utils;

#if PocketPC
using SplitterEventArgs = com.magicsoftware.mobilestubs.SplitterEventArgs;
using PreviewKeyDownEventArgs = com.magicsoftware.mobilestubs.PreviewKeyDownEventArgs;
using NodeLabelEditEventArgs = com.magicsoftware.mobilestubs.NodeLabelEditEventArgs;
using TreeNodeMouseHoverEventArgs = com.magicsoftware.mobilestubs.TreeNodeMouseHoverEventArgs;
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
using LinkLabelLinkClickedEventArgs = OpenNETCF.Windows.Forms.LinkLabel2LinkClickedEventArgs;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   public abstract class HandlerBase
   {
      internal enum EventType
      {
         NONE, MOUSE_DOWN, MOUSE_UP, CLOSING, CLOSED, DISPOSED, GOT_FOCUS, LOST_FOCUS,
         MOUSE_MOVE, MOUSE_ENTER, MOUSE_HOVER, MOUSE_LEAVE, MOUSE_WHEEL,
         MOUSE_DBLCLICK, RESIZE, SHOWN, KEY_DOWN, KEY_PRESS, KEY_UP, CLICK,
         PREVIEW_KEY_DOWN, ACTIVATED, MDI_CHILD_ACTIVATED, PAINT, SPLITTER_MOVING, SPLITTER_MOVED,
         DISPOSE_ITEM, PAINT_ITEM, DROP_DOWN_CLOSED, DROP_DOWN,
         ERASE_ITEM, AFTER_COLUMN_TRACK, SCROLL, LAYOUT,
         SELECTED_INDEX_CHANGED, CHECK_STATE_CHANGED, MOVE,
         STATUS_TEXT_CHANGED, BEFOR_EXPAND, BEFOR_COLLAPSE, LABEL_EDIT, BEFORE_SELECT, SELECT, COLUMN_CLICK,
         MENU_OPENING, MENU_ITEM_SELECTED, NODE_MOUSE_HOVER, REORDER_STARTED, REORDER_ENDED, LOAD,
         LINK_CLICKED, EXTERNAL_EVENT, ENABLED_CHANGED, SIZING, SELECTING, DRAG_DROP, DRAG_OVER,
         IME_EVENT, RESIZE_BEGIN, RESIZE_END, GIVE_FEEDBACK, BEFORE_REORDER, NCMOUSE_DOWN, PRESS, NCACTIVATE,
         COPY_DATA, HORIZONTAL_SCROLL_VISIBILITY_CHANGED, DEACTIVATED, CUT, COPY, PASTE, CLEAR, UNDO, COLUMN_FILTER_CLICK, CAN_REPOSITION, WMACTIVATE, MNEMONIC_KEY_PRESSED
      }

      internal void ImeEventHandler(object sender, ImeEventArgs e)
      {
         handleEvent(EventType.IME_EVENT, sender, e);
      }

      internal void LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         handleEvent(EventType.LINK_CLICKED, sender, e);
      }

      internal void BeforeReorderHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.BEFORE_REORDER, sender, e);
      }

      internal void StatusTextChangedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.STATUS_TEXT_CHANGED, sender, e);
      }

      internal void MoveHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.MOVE, sender, e);
      }

      internal void CheckedStateChangedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.CHECK_STATE_CHANGED, sender, e);
      }

      internal void SplitterMovedHandler(object sender, SplitterEventArgs e)
      {
         handleEvent(EventType.SPLITTER_MOVED, sender, e);
      }

      internal void SplitterMovingHandler(object sender, SplitterEventArgs e)
      {
         handleEvent(EventType.SPLITTER_MOVING, sender, e);
      }

      internal void PaintHandler(object sender, PaintEventArgs e)
      {
         handleEvent(EventType.PAINT, sender, e);
      }
      
      internal void ActivatedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.ACTIVATED, sender, e);
      }

      internal void DeActivatedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.DEACTIVATED, sender, e);
      }

      internal void MdiChildActivatedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.MDI_CHILD_ACTIVATED, sender, e);
      }

      internal void ClickHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.CLICK, sender, e);
      }

      internal void MouseDownHandler(Object sender, MouseEventArgs e)
      {
         handleEvent(EventType.MOUSE_DOWN, sender, e);
      }

      internal void MouseUpHandler(Object sender, MouseEventArgs e)
      {
         handleEvent(EventType.MOUSE_UP, sender, e);
      }

      internal void MouseMoveHandler(Object sender, MouseEventArgs e)
      {
         handleEvent(EventType.MOUSE_MOVE, sender, e);
      }

      internal void MouseEnterHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.MOUSE_ENTER, sender, e);
      }

      internal void MouseHoverHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.MOUSE_HOVER, sender, e);
      }

      internal void MouseLeaveHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.MOUSE_LEAVE, sender, e);
      }

      internal void MouseWheelHandler(Object sender, MouseEventArgs e)
      {
         handleEvent(EventType.MOUSE_WHEEL, sender, e);
      }

      internal void MouseDoubleClickHandler(Object sender, MouseEventArgs e)
      {
         handleEvent(EventType.MOUSE_DBLCLICK, sender, e);
      }

      internal void NCMouseDownHandler(Object sender, NCMouseEventArgs e)
      {
         handleEvent(EventType.NCMOUSE_DOWN, sender, e);
      }

      internal void CanRepositionHandler(Object sender, RepositionEventArgs e)
      {
         handleEvent(EventType.CAN_REPOSITION, sender, e);
      }


      internal void HorizontalScrollVisibilityChangedHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.HORIZONTAL_SCROLL_VISIBILITY_CHANGED, sender, e);
      }

      internal void NCActivateHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.NCACTIVATE, sender, e);
      }

      // This handler should be invoked by Mobile clients 
      // where press event is applicable
      internal void PressHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.PRESS, sender, e);
      }

      internal void ResizeHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.RESIZE, sender, e);
      }

      internal void ResizeBeginHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.RESIZE_BEGIN, sender, e);
      }

      internal void ResizeEndHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.RESIZE_END, sender, e);
      }

      internal void ClosingHandler(Object sender, CancelEventArgs e)
      {
         handleEvent(EventType.CLOSING, sender, e);
      }

      internal void ClosedHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.CLOSED, sender, e);
      }

      internal void DisposedHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.DISPOSED, sender, e);
      }

      internal void GotFocusHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.GOT_FOCUS, sender, e);
      }

      internal void CutHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.CUT, sender, e);
      }

      internal void CopyHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.COPY, sender, e);
      }

      internal void PasteHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.PASTE, sender, e);
      }

      internal void ClearHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.CLEAR, sender, e);
      }

      internal void UndoHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.UNDO, sender, e);
      }

      internal void ShownHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.SHOWN, sender, e);
      }

      internal void DropDownHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.DROP_DOWN, sender, e);
      }

      internal void DropDownClosedHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.DROP_DOWN_CLOSED, sender, e);
      }

      internal void LostFocusHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.LOST_FOCUS, sender, e);
      }

      internal void KeyDownHandler(Object sender, EventArgs e)
      {
         handleEvent(EventType.KEY_DOWN, sender, e);
      }

      internal void KeyPressHandler(Object sender, KeyPressEventArgs e)
      {
         handleEvent(EventType.KEY_PRESS, sender, e);
      }

      internal void KeyUpHandler(Object sender, KeyEventArgs e)
      {
         handleEvent(EventType.KEY_UP, sender, e);
      }

      internal void PreviewKeyDownHandler(object sender, PreviewKeyDownEventArgs e)
      {
         handleEvent(EventType.PREVIEW_KEY_DOWN, sender, e);
      }

      internal void DisposedItemHandler(object sender, TableItemDisposeArgs e)
      {
         handleEvent(EventType.DISPOSE_ITEM, sender, e);
      }

      internal void PaintItemHandler(object sender, TablePaintRowArgs e)
      {
         handleEvent(EventType.PAINT_ITEM, sender, e);
      }

      internal void AfterColumnTrackHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.AFTER_COLUMN_TRACK, sender, e);
      }

      internal void ScrollHandler(object sender, ScrollEventArgs e)
      {
         handleEvent(EventType.SCROLL, sender, e);
      }

      internal void SelectedIndexChangedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.SELECTED_INDEX_CHANGED, sender, e);
      }

      internal void MnemonicKeyPressedHandler(object sender, MnemonicKeyPressedEventArgs e)
      {
         handleEvent(EventType.MNEMONIC_KEY_PRESSED, sender, e);
      }

      internal void SelectingHandler(object sender, CancelEventArgs e)
      {
         handleEvent(EventType.SELECTING, sender, e);
      }

      internal void LayoutHandler(object sender, LayoutEventArgs e)
      {
         handleEvent(EventType.LAYOUT, sender, e);
      }

      internal void BeforeExpandHandler(object sender, TreeViewCancelEventArgs e)
      {
         handleEvent(EventType.BEFOR_EXPAND, sender, e);
      }

      internal void BeforeCollapseHandler(object sender, TreeViewCancelEventArgs e)
      {
         handleEvent(EventType.BEFOR_COLLAPSE, sender, e);
      }

      internal void TreeNodeBeforeSelect(object sender, TreeViewCancelEventArgs e)
      {
         handleEvent(EventType.BEFORE_SELECT, sender, e);
      }

      internal void LabelEditHandler(object sender, NodeLabelEditEventArgs e)
      {
          handleEvent(EventType.LABEL_EDIT, sender, e);
      }

      internal void MenuOpeningHandler(object sender, System.EventArgs e)
      {
         handleEvent(EventType.MENU_OPENING, sender, e);
      }

      internal void MenuClosedHandler(object sender, System.EventArgs e)
      {
         handleEvent(EventType.CLOSED, sender, e);
      }

      internal void MenuItemSelectedHandler(object sender, System.EventArgs e)
      {
         handleEvent(EventType.MENU_ITEM_SELECTED, sender, e);
      }

      internal void ColumnClick(object sender, EventArgs e)
      {
          handleEvent(EventType.COLUMN_CLICK, sender, e);
      }

      internal void ColumnFilterClick(object sender, EventArgs e)
      {
         handleEvent(EventType.COLUMN_FILTER_CLICK, sender, e);
      }

      internal void NodeMouseHoverHandler(object sender, TreeNodeMouseHoverEventArgs e)
      {
          handleEvent(EventType.NODE_MOUSE_HOVER, sender, e);
      }

      internal void ReorderHandler(object sender, TableReorderArgs e)
      {
         handleEvent(EventType.REORDER_STARTED, sender, e);
      }

      internal void ReorderEndedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.REORDER_ENDED, sender, e);
      }

      internal void LoadHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.LOAD, sender, e);
      }

      internal void ExternalEventHandler(object sender, ExternalEventArgs e)
      {
         handleEvent(EventType.EXTERNAL_EVENT, sender, e);
      }

      internal void EnabledChangedHandler(object sender, EventArgs e)
      {
         handleEvent(EventType.ENABLED_CHANGED, sender, e);
      }

      internal void SizingHandler(object sender, SizingEventArgs e)
      {
         handleEvent(EventType.SIZING, sender, e);
      }

      internal void WMActivateHandler(object sender, ActivateArgs e)
      {
         handleEvent(EventType.WMACTIVATE, sender, e);
      }

#if !PocketPC
      internal void CopyDataHandler(object sender, CopyDataEventArgs e)
      {
         handleEvent(EventType.COPY_DATA, sender, e);
      }

      internal void DragOverHandler(object sender, DragEventArgs e)
      {
         handleEvent(EventType.DRAG_OVER, sender, e);
      }

      internal void DragDropHandler(object sender, DragEventArgs e)
      {
         handleEvent(EventType.DRAG_DROP, sender, e);
      }

      internal void GiveFeedBackHandler (object sender, GiveFeedbackEventArgs e)
      {
         handleEvent(EventType.GIVE_FEEDBACK, sender, e);
      }
#endif

      abstract internal void handleEvent(EventType type, Object sender, EventArgs e);
   }
}
