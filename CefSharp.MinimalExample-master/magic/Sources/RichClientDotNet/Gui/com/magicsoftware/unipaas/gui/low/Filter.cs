using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.util;
using com.magicsoftware.win32;


namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>  The class is responsible disabling/enabling UI ,
   /// when worker thread is busy handling previous requests
   /// </summary>
   internal class Filter //: IThreadRunnable
   {
      private const long SEQUENCE_DELTA = 300;
      private const int REPEAT_TIMEOUT = 10;

      private static readonly Keys[] _navigationKeys = {
                                                         Keys.Enter, Keys.Left, Keys.Right, Keys.Up, Keys.Down,
                                                         Keys.PageUp, Keys.PageDown, Keys.Home, Keys.End, Keys.Tab,
                                                         Keys.Escape
                                                       };

      private readonly Timer _timer;
      internal bool FormsEnabled { get; private set; }
      internal bool AllowKeyboardBuffering = false;
      internal bool AllowExtendedKeyboardBuffering = false;

      ArrayList keyStrokesBuffer = new ArrayList();

      /// <summary>
      /// 
      /// </summary>
      internal Filter()
      {
         Application.AddMessageFilter(new MessageFilter(this));
         _timer = new Timer();
         _timer.Interval = REPEAT_TIMEOUT;
         _timer.Tick += timer_Tick;
         _timer.Start();
         FormsEnabled = true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void timer_Tick(object sender, EventArgs e)
      {
         // TODO: MerlinRT. getting event time is wrong. We could have simply check if the queue is not empty
         long lastTime = Events.GetEventTime();
         if (lastTime == 0) // worker thread is free             
            enableForms(true);
         else
         {
            long delta = Misc.getSystemMilliseconds() - lastTime;
            if (delta > SEQUENCE_DELTA)
               // worker thread is busy for SEQUENCE_DELTA we need to disable user input and to display wait cursor
               enableForms(false); // disable Form
         }
      }

      /// <summary>
      ///   Enable/disable forms
      /// </summary>
      /// <param name = "value"></param>
      private void enableForms(bool value)
      {
         if (!value)
         {
            if (!Events.IsFormLockAllowed())
               value = true;
         }
         if (FormsEnabled == value)
            return;

         FormsEnabled = value;

         if (!Events.PeekEndOfWork())
            Application.UseWaitCursor = !value;

         if (value)
         {
            //QCR #312443, .Net does not perform set cursor when UseWaitCursor cursor becomes No, 
            //we need to it ourselves 
            FormCollection openForms = Application.OpenForms;
            foreach (Form form in openForms)
               form.Cursor = form.Cursor;
         }
         //TODO: do we need restore focus after this

         // if forms were disabled and we buffered key strokes, send them now
         if (AllowExtendedKeyboardBuffering && FormsEnabled)
            SendBuffer();
      }

      /// <summary>
      /// replay the stored key stroke messages 
      /// </summary>
      void SendBuffer()
      {
         foreach (Message key in keyStrokesBuffer)
         {
            SimulateKeyboard.Simulate(key);
         }

         keyStrokesBuffer.Clear();
      }

      /// <summary>
      /// Data describing control
      /// </summary>
      class ControlData
      {
         public Control Control { get; set; }
         public MapData MapData { get; set; }
      }

      /// <summary>
      /// get data describing control from the windows message
      /// </summary>
      /// <param name="m"></param>
      /// <returns> null, if not magic control,
      /// ControlData if it is magic control</returns>
      private static ControlData MessageToMapData(Message m)
      {
         ControlData controlData = null;
         Control control = Control.FromChildHandle(m.HWnd);
         if (control != null)
         {
            MapData mapData = ControlsMap.getInstance().getControlMapData(control);
            if (mapData != null) //this is a magic control
               controlData = new ControlData() { Control = control, MapData = mapData };
         }
         return controlData;
      }

      /// <summary>
      /// For non interactive tasks all clicks must be blocked.
      /// Only clicks on push buttons are allowed.
      /// </summary>
      /// <param name="m"></param>
      /// <returns>true, if the click on the specific control must be blocked</returns>
      public static bool ShouldBlockNonInteractiveMessage(Message m)
      {
         bool shouldBlock = false;
         switch (m.Msg)
         {
            case NativeWindowCommon.WM_LBUTTONDOWN:
            case NativeWindowCommon.WM_LBUTTONUP:
            case NativeWindowCommon.WM_LBUTTONDBLCLK:
               ControlData controlData = MessageToMapData(m);
               if (controlData != null)
               {
                  GuiMgControl guiMgControl = controlData.MapData.getControl();
                  if (guiMgControl != null)
                     shouldBlock = Events.ShouldBlockMouseEvents(guiMgControl);
               }
               break;
         }
         return shouldBlock;
      }

      /// <summary>
      ///   process message
      ///   find out if the message is from child of the .NET control and process it
      /// </summary>
      /// <param name = "m"></param>
      /// <returns></returns>
      private static bool handleChildrenOfDotNetControls(Message m)
      {
         PreProcessControlState state = PreProcessControlState.MessageNotNeeded;
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapData;
         switch (m.Msg)
         {
            //this are common messages that we want to handle for children of .NET controls
            case NativeWindowCommon.WM_MOUSEMOVE:
            case NativeWindowCommon.WM_LBUTTONDOWN:
            case NativeWindowCommon.WM_LBUTTONUP:
            case NativeWindowCommon.WM_LBUTTONDBLCLK:
            case NativeWindowCommon.WM_KEYDOWN:
               Control c, control = c = Control.FromChildHandle(m.HWnd);
               if (c != null)
               {
                  mapData = controlsMap.getControlMapData(c);
                  if (mapData != null) //this is a magic control
                  {
                     //this is a .NET control on magic
                     if (isDotNetControl(mapData) && m.Msg == NativeWindowCommon.WM_KEYDOWN)
                        state = ProcessAndHandleDotNetControlMessage(m, mapData, control, control); //then handle it in magic
                     else
                        return false;
                  }
                  else //look for magic parent of the control
                  {
                     while (c.Parent != null)
                     {
                        c = c.Parent;
                        mapData = controlsMap.getControlMapData(c);
                        if (mapData != null)
                        {
                           if (isDotNetControl(mapData))
                           {
                              //FOUND! This is a child of .NET control
                              state = ProcessAndHandleDotNetControlMessage(m, mapData, c, control);
                           }
                           break;
                        }
                     }
                  }
               }
               break;

            case NativeWindowCommon.WM_KEYUP:
               {
                  // Handled here because while moving between windows of windowlist using keyboard (ie. Ctrl+Tab+Tab+,...), 
                  // the list should not be sorted until we release key associated with Next/Previous window Action.
                  Control ctrl = Control.FromHandle(m.HWnd);
                  if (ctrl != null)
                  {
                     Form form = GuiUtils.FindForm(ctrl);
                     if (form != null && ControlsMap.isMagicWidget(form))
                     {
                        // Each form has a panel and panel contains all controls.
                        mapData = controlsMap.getControlMapData(((TagData)form.Tag).ClientPanel);
                        if (mapData != null)
                           Events.HandleKeyUpMessage(mapData.getForm(), (int)m.WParam);
                     }
                  }
               }
               break;
         }
         return state == PreProcessControlState.MessageProcessed;
      }

      /// <summary>
      /// process meassage and handles if for 
      /// .NET controls and its children
      /// </summary>
      /// <param name="m"></param>
      /// <param name="mapData"></param>
      /// <param name="ancestorControl"></param>
      /// <param name="control"></param>
      /// <returns></returns>
      private static PreProcessControlState ProcessAndHandleDotNetControlMessage(Message m, MapData mapData, Control ancestorControl, Control control)
      {
         MgPanel navigationKeyDialogPanel = null;
         if (IsTabbingMessage(m))
            navigationKeyDialogPanel = GuiUtils.getContainerPanel(control) as MgPanel;
         if (navigationKeyDialogPanel != null)
            //install DialogKeyReceived key handler on the parent panel
            //this will be our indication if we need to handle the message
            navigationKeyDialogPanel.DialogKeyReceived += new EventHandler(panel_DialogKeyReceived);
         keyShouldBeHandledByMagic = false;
         PreProcessControlState state = control.PreProcessControlMessage(ref m); //first let control process the message
         if (navigationKeyDialogPanel != null)
            navigationKeyDialogPanel.DialogKeyReceived -= new EventHandler(panel_DialogKeyReceived);
         handleMessage(m, mapData, control, state); //then handle it in magic

         return state;
      }

      /// <summary>
      /// true if key should be handled by magic
      /// false if control should handle event itself
      /// </summary>
      static bool keyShouldBeHandledByMagic = false;
      static void panel_DialogKeyReceived(object sender, EventArgs e)
      {
         keyShouldBeHandledByMagic = true;
      }



      /// <summary>
      /// 
      /// </summary>
      /// <param name="mapData"></param>
      /// <returns></returns>
      private static bool isDotNetControl(MapData mapData)
      {
         return mapData.getControl() != null && mapData.getControl().IsDotNetControl();
      }

      /// <summary>
      ///   Helper function to convert a Windows lParam into a Point
      /// </summary>
      /// <param name = "lParam">The parameter to convert</param>
      /// <returns>A Point where X is the low 16 bits and Y is the
      ///   high 16 bits of the value passed in</returns>
      private static Point LParamToPoint(int lParam)
      {
         uint ulParam = (uint)lParam;
         return new Point(
            (int)(ulParam & 0x0000ffff),
            (int)((ulParam & 0xffff0000) >> 16));
      }

      /// <summary>
      ///   create mouse event
      /// </summary>
      /// <param name = "clicks"></param>
      /// <param name = "lParam"></param>
      /// <returns></returns>
      private static MouseEventArgs createLeftMouseEventArgs(int clicks, int lParam)
      {
         Point p = LParamToPoint(lParam);
         return new MouseEventArgs(MouseButtons.Left, clicks, p.X, p.Y, 0);
      }

      /// <summary>
      ///   handle message of  child of .NET control
      ///   create event according to message and handle it in usual manner
      /// </summary>
      /// <param name = "m"> message </param>
      /// <param name = "mapData"> map data of .NET control</param>
      /// <param name = "control">control which receive message - the child of .NET control</param>
      /// <param name="state"></param>
      private static void handleMessage(Message m, MapData mapData, Control control, PreProcessControlState state)
      {
         EventArgs e = null;
         HandlerBase.EventType eventType = HandlerBase.EventType.NONE;

         switch (m.Msg)
         {
            case NativeWindowCommon.WM_LBUTTONDOWN:
               e = createLeftMouseEventArgs(1, (int)m.LParam);
               eventType = HandlerBase.EventType.MOUSE_DOWN;
               break;

            case NativeWindowCommon.WM_LBUTTONUP:
               e = createLeftMouseEventArgs(1, (int)m.LParam);
               eventType = HandlerBase.EventType.MOUSE_UP;
               break;

            case NativeWindowCommon.WM_LBUTTONDBLCLK:
               e = createLeftMouseEventArgs(2, (int)m.LParam);
               eventType = HandlerBase.EventType.MOUSE_DBLCLICK;
               break;

            case NativeWindowCommon.WM_MOUSEMOVE:
               e = createLeftMouseEventArgs(1, (int)m.LParam);
               eventType = HandlerBase.EventType.MOUSE_MOVE;
               break;

            case NativeWindowCommon.WM_KEYDOWN:
               eventType = HandlerBase.EventType.KEY_DOWN;
               Keys keyCode = GetKeyCode(m);
               e = new KeyEventArgs(keyCode);
               DotNetHandler.getInstance().handleEvent("KeyDown", control, e, mapData);


               // we use preProcessResult only for navigation keys
               // if control returns preProcessResult = false for a navigation key then we do not
               // interfere because the control already handled the navigation internally.
               if (state == PreProcessControlState.MessageNeeded)
               {
                  foreach (var item in _navigationKeys)
                  {
                     if (keyCode == item)
                        return;
                  }
               }
               if (keyCode == Keys.Tab && !keyShouldBeHandledByMagic) //QCR #728708
                  //special handling for a tab key, in future may be extended for othe keys as weel.
                  return;

               break;

            default:
               Debug.Assert(false);
               break;
         }

         DefaultHandler.getInstance().handleEvent(eventType, control, e, mapData);
      }

      /// <summary>
      /// get message key code
      /// </summary>
      /// <param name="m"></param>
      /// <returns></returns>
      private static Keys GetKeyCode(Message m)
      {
         Keys keyCode = (Keys)m.WParam & Keys.KeyCode;
         keyCode |= Control.ModifierKeys;
         return keyCode;
      }

      /// <summary>
      /// return true if message represent a tab key message
      /// </summary>
      /// <param name="m"></param>
      /// <returns></returns>
      private static bool IsTabbingMessage(Message m)
      {
         if (m.Msg == NativeWindowCommon.WM_KEYDOWN)
         {
            Keys keyCode = GetKeyCode(m);
            if (keyCode == Keys.Tab)
               return true;
         }
         return false;
      }

      /// <summary>
      /// perform default magic handling for the message
      /// </summary>
      /// <param name="m"></param>
      private static void handleMessage(Message m)
      {
         ControlData controlData = MessageToMapData(m);
         if (controlData != null)
            handleMessage(m, controlData.MapData, controlData.Control, PreProcessControlState.MessageNeeded);
      }

      #region Nested type: PaintMessageFilter

      /// <summary>
      ///   Filter messages
      ///   
      ///   Prevent default windows handling for some windows messages in specific situations.
      ///   
      ///   For example:
      ///   1.For rich client - do not process messages while worker thread is busy
      ///   2.For batch tasks - do not process mouse messages for some controls
      ///   3.etc..
      ///   
      /// </summary>
      internal class MessageFilter : IMessageFilter
      {
         private readonly Filter _formsController;

         internal MessageFilter(Filter inst)
         {
            _formsController = inst;
         }

         // Summary:
         //     Filters out a message before it is dispatched.
         //
         // Parameters:
         //   m:
         //     The message to be dispatched.
         //
         // Returns:
         //     true to filter the message and stop it from being dispatched; false to allow
         //     the message to continue to the next filter or control.
         public bool PreFilterMessage(ref Message m)
         {
            if (ShouldBlockNonInteractiveMessage(m))
            {
               handleMessage(m); //perform magic handling of the message
               return true;      //prevent default windows handling of the message
            }
            if (_formsController.FormsEnabled)
            {
               return handleChildrenOfDotNetControls(m);
            }
            if (m.Msg >= NativeWindowCommon.WM_USER) //internal .NET messages  - cause deadlock if not handled
               return false;

            switch (m.Msg)
            {
               case NativeWindowCommon.WM_PAINT:
               case NativeWindowCommon.WM_TIMER: //need timers to wake up
               case NativeWindowCommon.WM_SYSTIMER:
               case NativeWindowCommon.WM_MOUSELEAVE:
               case NativeWindowCommon.WM_MOUSEHOVER:
                  // case NativeWindowCommon.WM_KEYDOWN: //if it is not processed it causes process hanging on exit 
                  return false;
               case NativeWindowCommon.WM_KEYDOWN:
               case NativeWindowCommon.WM_CHAR:
               case NativeWindowCommon.WM_KEYUP:
               case NativeWindowCommon.WM_SYSKEYDOWN:
               case NativeWindowCommon.WM_SYSCHAR:
               case NativeWindowCommon.WM_SYSKEYUP:
               case NativeWindowCommon.WM_IME_CHAR:
                  if (Events.InIncrementalLocate())
                     return false;
                  break;
            }

            // check for keyboard buffering state
            if (_formsController.AllowKeyboardBuffering)
               return BufferKeyboardMessage(m);
            else if (_formsController.AllowExtendedKeyboardBuffering)
               return ExtendedBufferKeyboardMessage(m);

            return true;
         }


         /// <summary>
         /// Should the filter allow processing of keyboard events
         /// </summary>
         /// <param name="hwnd"></param>
         /// <returns></returns>
         private bool BufferKeyboardMessage(Message m)
         {
            switch (m.Msg)
            {
               case NativeWindowCommon.WM_KEYDOWN:
               case NativeWindowCommon.WM_CHAR:
               case NativeWindowCommon.WM_KEYUP:
               case NativeWindowCommon.WM_IME_CHAR:
                  // allow processing of those messages, for textbox only
                  Control control = Control.FromChildHandle(m.HWnd);
                  if (control.GetType() == typeof(MgTextBox))
                     return false; // = don't block it
                  break;
            }
            return true; // block message
         }

         /// <summary>
         /// extended -buffering - stop all messages, but store the relevant messages, so they can be replayed later
         /// </summary>
         /// <param name="hwnd"></param>
         /// <returns></returns>
         private bool ExtendedBufferKeyboardMessage(Message m)
         {
            switch (m.Msg)
            {
               case NativeWindowCommon.WM_KEYDOWN:
               case NativeWindowCommon.WM_KEYUP:
               case NativeWindowCommon.WM_SYSKEYDOWN:
               case NativeWindowCommon.WM_SYSKEYUP:
                  // store the key strokes, so they can be replayed later
                  _formsController.keyStrokesBuffer.Add(m);
                  break;
            }
            return true; // = block message
         }
      }

      #endregion
   }
}
