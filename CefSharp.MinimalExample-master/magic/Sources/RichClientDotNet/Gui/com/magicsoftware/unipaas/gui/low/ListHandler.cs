using System;
using System.Windows.Forms;
using System.Drawing;

namespace com.magicsoftware.unipaas.gui.low
{
   internal class ListHandler : HandlerBase
   {
      private static ListHandler _instance;
      internal static ListHandler getInstance()
      {
         if (_instance == null)
            _instance = new ListHandler();
         return _instance;
      }

      /// <summary> </summary>
      private ListHandler()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="listBox"></param>
      internal void addHandler(ListBox listBox)
      {
         listBox.GotFocus += GotFocusHandler;
         listBox.KeyDown += KeyDownHandler;
         listBox.KeyPress += KeyPressHandler;
#if !PocketPC
         listBox.Click += ClickHandler;
         listBox.PreviewKeyDown += PreviewKeyDownHandler;
         listBox.MouseEnter += MouseEnterHandler;
         listBox.MouseHover += MouseHoverHandler;
         listBox.MouseLeave += MouseLeaveHandler;
         listBox.MouseWheel += MouseWheelHandler;
         listBox.MouseDoubleClick += MouseDoubleClickHandler;
         listBox.MouseMove += MouseMoveHandler;
         listBox.MouseDown += MouseDownHandler;
         listBox.MouseUp += MouseUpHandler;
         listBox.DragOver += DragOverHandler;
         listBox.DragDrop += DragDropHandler;
         listBox.GiveFeedback += GiveFeedBackHandler;
#endif
         listBox.Disposed += DisposedHandler;
         listBox.SelectedIndexChanged += SelectedIndexChangedHandler;
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
         Control control = (Control)sender;
         MapData mapData = controlsMap.getMapData(control);
         if (mapData == null)
            return;

         Type senderType = sender.GetType();
         ListBox listBox = (ListBox)sender;

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(mapData.getControl()));
         try
         {
            switch (type)
            {

               case EventType.MOUSE_DOWN:
                  MouseEventArgs mouseEvtArgs = (MouseEventArgs)e;
                  GuiUtils.SaveLastClickInfo(mapData, (Control)sender, new Point(mouseEvtArgs.X, mouseEvtArgs.Y));
                  bool leftClickWasPressed = (((MouseEventArgs)e).Button == MouseButtons.Left);
                  GuiUtils.SetOnClickOnTagData(control, leftClickWasPressed);

#if !PocketPC
                  if (leftClickWasPressed)
                     GuiUtils.AssessDrag(control, (MouseEventArgs)e, mapData);
#endif

                  return;

               case EventType.MOUSE_UP:
               case EventType.GOT_FOCUS:
               case EventType.LOST_FOCUS:
                  GuiUtils.SetOnClickOnTagData(control, false);

#if !PocketPC
                  if (type == EventType.MOUSE_UP)
                     GuiUtils.ResetDragInfo(control);
#endif
                  return;

               case EventType.KEY_DOWN:
                  if (OnkeyDown(type, sender, e))
                     return;
                  break;
               case EventType.CLICK:
                  // we do not want the process selection to accure here. only in SelectedIndexChanged.
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
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      /// <returns></returns>
      private bool OnkeyDown(EventType type, Object sender, EventArgs e)
      {
         bool ignoreEvent = false;
         KeyEventArgs keyEventArgs = (KeyEventArgs)e;
         ListBox listBox = (ListBox)sender;

         // Definition of the behavior (the same in on line)
         // ---------------------------------------------
         // page down or up : move to the next\prev record
         // up,down : move to the next option in the list
         // left,right : move to the next\prev field
         // Home, End : select the first\end options in the popup

         switch (keyEventArgs.KeyCode)
         {
            case Keys.Down:
            case Keys.Up:
               #if !PocketPC
               if (listBox.SelectionMode != SelectionMode.One)
               {
                  keyEventArgs.Handled = false;
                  ignoreEvent = true;
               }
               else
               #endif
                  keyEventArgs.Handled = true;
               break;

            case Keys.Left:
            case Keys.Right:
            case Keys.PageUp:
            case Keys.PageDown:
               //  events.doit = false;
               //keyEventArgs.Handled = true;
               #if !PocketPC
               if (listBox.SelectionMode != SelectionMode.One)
               {
                  keyEventArgs.Handled = false;
                  ignoreEvent = true;
               }
               else
#endif
                  keyEventArgs.Handled = true;
               break;

            default:
               break;

         }
         return ignoreEvent;
      }
   }
}