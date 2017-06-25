using System;
using System.Windows.Forms;

using com.magicsoftware.controls;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// Event handler for Rich Edit Controls
   /// </summary>
   /// <author>  kaushals</author>
   class RichEditHandler : HandlerBase
   {
      private static RichEditHandler _instance;
      internal static RichEditHandler getInstance()
      {
         if (_instance == null)
            _instance = new RichEditHandler();
         return _instance;
      }

      /// <summary> </summary>
      private RichEditHandler()
      {
      }

      /// <summary> 
      /// adds events for text
      /// </summary>
      /// <param name="control"> </param>
      internal void addHandler(Control control)
      {
         control.MouseDown += MouseDownHandler;
         control.MouseUp += MouseUpHandler;
         control.MouseMove += MouseMoveHandler;
         control.MouseEnter += MouseEnterHandler;
         control.MouseHover += MouseHoverHandler;
         control.MouseLeave += MouseLeaveHandler;
         control.MouseWheel += MouseWheelHandler;
         control.MouseDoubleClick += MouseDoubleClickHandler;

         control.PreviewKeyDown += PreviewKeyDownHandler;
         control.KeyDown += KeyDownHandler;
         //TODO: check if KeyPress is required for RichEdit
         //control.KeyPress += KeyPressHandler;
         control.KeyUp += KeyUpHandler;
         control.KeyPress += KeyPressHandler;
         control.GotFocus += GotFocusHandler;
         control.LostFocus += LostFocusHandler;
         control.Disposed += DisposedHandler;

#if !PocketPC
         // 981903. Drop/Move sign is displayed on RTF when dragging from external source.
         // We don't support Drag & Drop on RTF control, hence we will set evtArgs.Effects = NONE.
         control.DragOver += DragOverHandler;
#endif
      }

      /* (non-Javadoc)
      * @see org.eclipse.swt.widgets.Handler#handleEvent(org.eclipse.swt.widgets.Event)
      */
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         RichTextBox richTextCtrl = (RichTextBox)sender;
         MapData mapData = controlsMap.getMapData(richTextCtrl);
         if (mapData == null)
            return;

         GuiMgControl ctrl = mapData.getControl();
         GuiMgForm guiMgForm = mapData.getForm();

         UtilImeJpn utilImeJpn = Manager.UtilImeJpn; // JPN: IME support

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(ctrl));
         try
         {
            switch (type)
            {
               case EventType.GOT_FOCUS:
                  // check the paste enable. check the clip content.
                  if (mapData != null)
                     GuiUtils.checkPasteEnable(mapData.getControl(), true);

                  // For RichEdit Ctrl, Set AcceptButton(i.e. DefaultButton) to null in order to allow enter key on RichEdit control.
                  if (sender is MgRichTextBox)
                  {
                     Form form = GuiUtils.FindForm(richTextCtrl);
                     form.AcceptButton = null;

                     if (((MgRichTextBox)sender).ReadOnly)
                        GuiUtils.restoreFocus(form);
                  }
                  break;

               case EventType.LOST_FOCUS:
                  // Always disable paste when exiting a text ctrl. (since we might be focusing on a diff type of
                  // ctrl).
                  if (mapData != null)
                     GuiUtils.disablePaste(mapData.getControl());
                  break;

               case EventType.KEY_UP:
                  // Korean
                  if (sender is MgRichTextBox && ((MgRichTextBox)sender).KoreanInterimSel >= 0)
                     return;

                  if (utilImeJpn != null)
                  {
                     if (utilImeJpn.IsEditingCompStr(richTextCtrl))  // JPN: IME support
                        return;

                     if (richTextCtrl is MgRichTextBox)              // JPN: ZIMERead function
                        utilImeJpn.StrImeRead = ((MgRichTextBox)richTextCtrl).GetCompositionString();
                  }

                  GuiUtils.enableDisableEvents(sender, mapData.getControl());
                  return;

               case EventType.KEY_DOWN:
                  // Korean
                  if (sender is MgRichTextBox && ((MgRichTextBox)sender).KoreanInterimSel >= 0)
                     return;

                  if (utilImeJpn != null && utilImeJpn.IsEditingCompStr(richTextCtrl)) // JPN: IME support
                     return;

                  KeyEventArgs keyEventArgs = (KeyEventArgs)e;
                  // marking the text (next/prev char or beg/end text) we let the
                  // system to take care of it.
                  // why ? There is no way in windows to set the caret at the beginning of
                  // a selected text. it works only on multi mark for some reason.
                  // also posting a shift+key does not work well since we have no way of knowing
                  // if the shift is already pressed or not.
                  // *** ALL other keys will continue to handleEvent.
                  if ((keyEventArgs.Shift && (keyEventArgs.KeyCode == Keys.Left || keyEventArgs.KeyCode == Keys.Right || keyEventArgs.KeyCode == Keys.Up || keyEventArgs.KeyCode == Keys.Down || keyEventArgs.KeyCode == Keys.Home || keyEventArgs.KeyCode == Keys.End)) ||
                      (keyEventArgs.Control && (keyEventArgs.KeyCode == Keys.Left || keyEventArgs.KeyCode == Keys.Right)))
                  {
                     keyEventArgs.Handled = false;
                     return;
                  }
                  break;

               case EventType.KEY_PRESS:

                  KeyPressEventArgs keyPressEventArgs = (KeyPressEventArgs)e;

                  bool IgnoreKeyPress = ((TagData)richTextCtrl.Tag).IgnoreKeyPress;
                  // should we ignore the key pressed ?
                  if (IgnoreKeyPress)
                  {
                     ((TagData)richTextCtrl.Tag).IgnoreKeyPress = false;

                     return;
                  }

                  // skipp control key
                  if (Char.IsControl(keyPressEventArgs.KeyChar))
                     return;

                  int start = richTextCtrl.SelectionStart;
                  int end = richTextCtrl.SelectionStart + richTextCtrl.SelectionLength;

                  String pressedChar = "" + keyPressEventArgs.KeyChar;

                  // flag the isActChar to indicate this is MG_ACT_CHAR
                  Events.OnKeyDown(guiMgForm, ctrl, Modifiers.MODIFIER_NONE, 0, start, end, pressedChar, true, "-1", keyPressEventArgs.Handled);

                  // keyPressEventArgs.Handled wii stay 'false' in order to let the system put the correct char.
                  // What will happen is 2 things : 1. processKeyDown will add 'MG_ACT_CHAR'. 2. The system will write the char.
                  // In the past, the 'ACT_CHAR' was using sendKeys in order to write the char, but it makes problems in multilanguage systems.
                  // So, in TextMaskEditor for rich , ACT_CHAR will do nothing, just pass there in order to rais the 'control modify'.
                  //keyPressEventArgs.Handled = true;
                  break;

               case EventType.MOUSE_UP:
                  GuiUtils.enableDisableEvents(sender, mapData.getControl());
                  break;
            }
         }
         finally
         {
            contextIDGuard.Dispose();
         }

         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }
   }
}
