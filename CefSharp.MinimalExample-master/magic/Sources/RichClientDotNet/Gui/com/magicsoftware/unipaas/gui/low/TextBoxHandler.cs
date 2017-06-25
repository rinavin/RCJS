using System;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using System.Drawing;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>Class implements handlers for the text controls, this class is a singleton</summary>
   /// <author>rinav</author>
   class TextBoxHandler : HandlerBase
   {
      private static TextBoxHandler _instance;
      internal static TextBoxHandler getInstance()
      {
         if (_instance == null)
            _instance = new TextBoxHandler();
         return _instance;
      }

      /// <summary> </summary>
      private TextBoxHandler()
      {
      }

      /// <summary>
      /// The handlers defined in this function are attached to control is all the cases,
      /// irrespective of other handlers are attached or not. 
      /// </summary>
      /// <param name="control">control to which handlers are attached.</param>
      internal void addCommonHandlers(Control control)
      {
         control.KeyDown += KeyDownHandler;
      }

      /// <summary>
      /// The handlers defined in this function are attached to the control only if the flag addDefaultHandlers
      /// is true.This is helpful in selectively  attaching the handlers to the control.
      /// </summary>
      /// <param name="control">control to which handlers are attached.</param>
      internal void addHandler(Control control)
      {
         control.MouseDown += MouseDownHandler;
         control.MouseUp += MouseUpHandler;
         control.MouseMove += MouseMoveHandler;
#if !PocketPC //?
         control.MouseEnter += MouseEnterHandler;
         control.MouseHover += MouseHoverHandler;
         control.MouseLeave += MouseLeaveHandler;
         control.MouseWheel += MouseWheelHandler;
         control.MouseDoubleClick += MouseDoubleClickHandler;
         control.PreviewKeyDown += PreviewKeyDownHandler;
         control.DragOver += DragOverHandler;
         control.DragDrop += DragDropHandler;
         control.GiveFeedback += GiveFeedBackHandler;
         ((MgTextBox)control).ImeEvent += ImeEventHandler;
         ((MgTextBox)control).CutEvent += CutHandler;
         ((MgTextBox)control).CopyEvent += CopyHandler;
         ((MgTextBox)control).PasteEvent += PasteHandler;
         ((MgTextBox)control).ClearEvent += ClearHandler;
         ((MgTextBox)control).UndoEvent += UndoHandler;
#endif
         control.KeyPress += KeyPressHandler;
         control.KeyUp += KeyUpHandler;
         control.GotFocus += GotFocusHandler;
         control.LostFocus += LostFocusHandler;
         control.Disposed += DisposedHandler;


         if (Manager.UtilImeJpn != null) // JPN: ZIMERead function
            control.TextChanged += StatusTextChangedHandler;
      }

      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {         
         ControlsMap controlsMap = ControlsMap.getInstance();
         UtilImeJpn utilImeJpn = Manager.UtilImeJpn;

         TextBox textCtrl = (TextBox)sender;
         int start;
         int end;

         MapData mapData = controlsMap.getMapData(textCtrl);
         if (mapData == null)
            return;

         GuiMgControl guiMgCtrl = mapData.getControl();
         GuiMgForm guiMgForm = mapData.getForm();

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgCtrl));
         if (Events.ShouldLog(Logger.LogLevels.Gui))
            Events.WriteGuiToLog("TextBoxHandler(\"" + mapData.getControl().getName(mapData.getIdx()) + "\"): " + type);

         try
         {
            switch (type)
            {
               case EventType.GOT_FOCUS:
                  // check the paste enable. check the clip content.
                  if (mapData != null)
                  {                  
                     GuiUtils.checkPasteEnable(mapData.getControl(), true);
                     GuiUtils.SetFocusColor(textCtrl);
                  }
                  break;

               case EventType.LOST_FOCUS:
                  // Always disable paste when exiting a text ctrl. (since we might be focusing on a diff type of
                  // ctrl).
                  if (mapData != null)
                  {
                     GuiUtils.disablePaste(mapData.getControl());
                     GuiUtils.ResetFocusColor(textCtrl);
                  }
                  break;

               case EventType.KEY_UP:
                  GuiUtils.enableDisableEvents(sender, mapData.getControl());
                  return;

               case EventType.KEY_DOWN:
                  KeyEventArgs keyEventArgs = (KeyEventArgs)e;
                  
                  if (ShouldBeHandledByTextBox(textCtrl, keyEventArgs ))
                  {
                     GuiUtils.checkAutoWide(mapData.getControl(), textCtrl, GuiUtils.getValue(textCtrl));
                     keyEventArgs.Handled = false;
                     return;
                  }
                  break;

               case EventType.IME_EVENT:
                  // (Korean) IME messages (WM_IME_COMPOSITION, etc.) are handled as pseudo-input 
                  // where action=MG_ACT_CHAR, text=" ".
                  // To distinguish with real " ", ImeParam im is attached to RuntimeEvent.
                  ImeEventArgs iea = (ImeEventArgs)e;
                  start = textCtrl.SelectionStart;
                  end = textCtrl.SelectionStart + textCtrl.SelectionLength;
                  Events.OnKeyDown(guiMgForm, guiMgCtrl, Modifiers.MODIFIER_NONE, 0, start, end, " ", iea.im, true, "-1", false, iea.Handled);
                  iea.Handled = true;
                  break;

               case EventType.KEY_PRESS:
                  KeyPressEventArgs keyPressEventArgs = (KeyPressEventArgs)e;
                  // skipp control key
                  if (Char.IsControl(keyPressEventArgs.KeyChar))
                     return;

                  start = textCtrl.SelectionStart;
                  end = textCtrl.SelectionStart + textCtrl.SelectionLength;
                  String pressedChar = "" + keyPressEventArgs.KeyChar;

                  // flag the isActChar to indicate this is MG_ACT_CHAR
                  Events.OnKeyDown(guiMgForm, guiMgCtrl, Modifiers.MODIFIER_NONE, 0, start, end, pressedChar, true, "-1", keyPressEventArgs.Handled);
                  keyPressEventArgs.Handled = true;
                  break;

               case EventType.MOUSE_UP:
                  GuiUtils.enableDisableEvents(sender, mapData.getControl());
                  break;

               case EventType.CUT:
                  Events.CutEvent(mapData.getControl());
                  return;
               case EventType.COPY:
                  Events.CopyEvent(mapData.getControl());
                  return;
               case EventType.PASTE:
                  Events.PasteEvent(mapData.getControl());
                  return;
               case EventType.CLEAR:
                  Events.ClearEvent(mapData.getControl());
                  return;
               case EventType.UNDO:
                  Events.UndoEvent(mapData.getControl());
                  return;



               case EventType.STATUS_TEXT_CHANGED:
                  // JPN: ZIMERead function
                  if (utilImeJpn != null && sender is MgTextBox && !utilImeJpn.IsEditingCompStr((Control)sender))
                     utilImeJpn.StrImeRead = ((MgTextBox)sender).GetCompositionString();
                  return;
            }
         }
         finally 
         {
            contextIDGuard.Dispose();
         }

         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }
       

      /// <summary>
      /// returns true if key will be handled by control itself, false - to be habdled by magic
      /// </summary>
      /// <param name="textBox"></param>
      /// <param name="keyEventArgs"></param>
      /// <returns></returns>
      bool ShouldBeHandledByTextBox(TextBox textBox, KeyEventArgs keyEventArgs)
      {
         // marking the text (next/prev char or beg/end text) we let the
         // system to take care of it.
         // why ? There is no way in windows to set the caret at the beginning of
         // a selected text. it works only on multi mark for some reason.
         // also posting a shift+key does not work well since we have no way of knowing
         // if the shift is already pressed or not.
         // *** ALL other keys will continue to handleEvent.
         // ** Control+Right,Control+Left are passed in order to handle NextWord/PrevWord events 
        
         if (keyEventArgs.Shift)
         {
            if (keyEventArgs.KeyCode == Keys.Left || keyEventArgs.KeyCode == Keys.Right)
               return true;

            if (textBox.Multiline && (keyEventArgs.KeyCode == Keys.Up || keyEventArgs.KeyCode == Keys.Down || keyEventArgs.KeyCode == Keys.Home || keyEventArgs.KeyCode == Keys.End))
               return true;
         }
         return false;
      }
   }
}
