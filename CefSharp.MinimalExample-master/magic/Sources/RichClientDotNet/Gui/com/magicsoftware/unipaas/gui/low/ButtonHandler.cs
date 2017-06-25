using System;
using System.Windows.Forms;
using com.magicsoftware.win32;
using com.magicsoftware.controls;
using System.Drawing;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>MgButton handler</summary>
   /// <author> rinav</author>
   internal class ButtonHandler : HandlerBase
   {
      private static ButtonHandler _instance;
      internal static ButtonHandler getInstance()
      {
         if (_instance == null)
            _instance = new ButtonHandler();
         return _instance;
      }

      /// <summary>
      /// 
      /// </summary>
      private ButtonHandler()
      {
      }

      /// <summary> adds events for control</summary>
      /// <param name="form"></param>
      internal void addHandler(Control control)
      {
         control.MouseMove += MouseMoveHandler;
#if !PocketPC //?
         control.MouseLeave += MouseLeaveHandler;
         control.MouseEnter += MouseEnterHandler;
         control.MouseWheel += MouseWheelHandler;
         control.PreviewKeyDown += PreviewKeyDownHandler;
         control.MouseDoubleClick += MouseDoubleClickHandler;
         control.DragOver += DragOverHandler;
         control.DragDrop += DragDropHandler;
         control.GiveFeedback += GiveFeedBackHandler;
#endif
         control.MouseDown += MouseDownHandler;
         control.GotFocus += GotFocusHandler;
         control.KeyDown += KeyDownHandler;
         control.KeyPress += KeyPressHandler;
         control.Disposed += DisposedHandler;

         // Mouseup is required for handling click. We can not use control.Click
         // as this event is received even while using keyboard.
         control.MouseUp += MouseUpHandler;

         if (control is MgCheckBox)
         {
            ((MgCheckBox)control).CheckStateChanged += CheckedStateChangedHandler;
         }
         if (control is MgButtonBase)
         {
            ((MgButtonBase)control).LostFocus += LostFocusHandler;
            control.Click += ClickHandler;
            control.Resize += ResizeHandler;
         }
      }

      /// <summary> </summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         Control ctrl = (Control)sender;
         MapData mapData = controlsMap.getMapData(ctrl);
         if (mapData == null)
            return;

         GuiMgControl guiMgControl = mapData.getControl();
         bool isButton = (ctrl is MgButtonBase);
         bool isImageButton = (ctrl is MgImageButton);
         bool isCheckBox = (ctrl is MgCheckBox);

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgControl));
         try
         {
            switch (type)
            {
               case EventType.MOUSE_LEAVE:
                  if (isImageButton && ((MgImageButton)ctrl).Supports6Images())
                  {
                     ((TagData)(ctrl).Tag).OnHovering = false;
                     GuiUtils.RefreshButtonImage(ctrl);
                  }
                  break;

               case EventType.MOUSE_ENTER:
                  if (isImageButton && ((MgImageButton)ctrl).Supports6Images())
                  {
                     ((TagData)(ctrl).Tag).OnHovering = true;
                     GuiUtils.RefreshButtonImage(ctrl);
                  }
                  break;

               case EventType.CLICK:
                  if (isButton)
                  {
                     bool isClick = false;

                     // if it is not click (onMouseDown) and lastparked ctrl is not equal to button, then
                     // it has come through accelerators and accelerators should be considered as click
                     if (((TagData)ctrl.Tag).OnMouseDown)
                        isClick = true;

                     String controlName = guiMgControl == null ? "" : guiMgControl.Name;
                     Events.SaveLastClickedCtrlName(guiMgControl, controlName);

                     Events.OnSelection(GuiUtils.getValue(ctrl), guiMgControl, mapData.getIdx(), isClick);
                     GuiUtils.SetOnClickOnTagData(ctrl, false);
                     GuiUtils.RefreshButtonImage(ctrl);
                  }
                  return;

               case EventType.MOUSE_DOWN:
                  bool mouseDown = (isButton ? ((MouseEventArgs)e).Button == MouseButtons.Left : true);
                  GuiUtils.SetOnClickOnTagData(ctrl, mouseDown);
                  if (isImageButton)
                     GuiUtils.RefreshButtonImage(ctrl);
                  if (isButton || isCheckBox)
                  {
                     MouseEventArgs mouseEvtArgs = (MouseEventArgs)e;
                     GuiUtils.SaveLastClickInfo(mapData, (Control)sender, new Point(mouseEvtArgs.X, mouseEvtArgs.Y));
#if !PocketPC
                     // In mouse down event, we initiate drag and since we are returning from here,
                     // we need to handle it here itself as it won't call defaulthandler.handleEvent.
                     if (((MouseEventArgs)e).Button == MouseButtons.Left)
                        GuiUtils.AssessDrag(ctrl, (MouseEventArgs)e, mapData);
#endif
                     return;
                  }
                  break;

               case EventType.KEY_PRESS:
                  /*For CheckBox and Button, Space bar key should be ignored as it is handled thr' selection event.*/
                  if (Char.IsWhiteSpace(((KeyPressEventArgs)e).KeyChar))
                     return;
                  break;
               case EventType.LOST_FOCUS:
                  if (isButton)
                  {
                     GuiUtils.SetOnClickOnTagData(ctrl, false);
                     GuiUtils.RefreshButtonImage(ctrl);

                     //fixed bug #:252654, .NET display the control and hot Track in spite of it's not,
                     //            when focus is lost update the UIIState by set UISF_HIDEFOCUS 
                     //            the same fixed was done in online for check box in
                     UpdateUIstate(ctrl, true);
                  }
                  return;

               case EventType.GOT_FOCUS:
                  GuiUtils.SetOnClickOnTagData(ctrl, false);
                  if (isButton)
                  {
                     GuiUtils.RefreshButtonImage(ctrl);

                     //fixed bug #:252654, .NET display the control and hot Track in spite of it's not,
                     //            when focus is got update the UIIState by clear the UISF_HIDEFOCUS 
                     //            the same fixed was done in online for check box in
                     UpdateUIstate(ctrl, false);
                  }
                  if (isButton || isCheckBox)
                     return;
                  else
                     break;

               case EventType.MOUSE_UP:
                  GuiUtils.SetOnClickOnTagData(ctrl, false);
                  if (isImageButton)
                     GuiUtils.RefreshButtonImage(ctrl);

#if !PocketPC
                  // Reset drag information, since we are returning from here.
                  GuiUtils.ResetDragInfo(ctrl);
#endif
                  return;

               case EventType.RESIZE:
                  if (isImageButton)
                     GuiUtils.RefreshButtonImage(ctrl);
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
      /// Update UIState of the control with NativeWindowCommon.UISF_HIDEFOCUS 
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="set"></param>
      private static void UpdateUIstate(Control ctrl, bool set)
      {
         int UIS_l = set ? NativeWindowCommon.UIS_SET : NativeWindowCommon.UIS_CLEAR;
         int wParam = NativeWindowCommon.MakeLong(UIS_l, NativeWindowCommon.UISF_HIDEFOCUS);
         NativeWindowCommon.SendMessage(ctrl.Handle, NativeWindowCommon.WM_UPDATEUISTATE, wParam, 0);
         ctrl.Invalidate(ctrl.ClientRectangle);
      }
   }
}