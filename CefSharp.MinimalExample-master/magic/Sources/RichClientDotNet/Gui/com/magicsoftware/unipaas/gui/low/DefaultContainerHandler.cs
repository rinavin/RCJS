using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
#if PocketPC
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// default container handles - implement default container behavior
   /// </summary>
   /// <author>  rinav</author>
   internal class DefaultContainerHandler : HandlerBase //: System.ICloneable
   {
      /// <summary>
      /// singleton
      /// </summary>
      private static DefaultContainerHandler _instance;
      internal static DefaultContainerHandler getInstance()
      {
         if (_instance == null)
            _instance = new DefaultContainerHandler();
         return _instance;
      }

      /// <summary>
      /// 
      /// </summary>
      private DefaultContainerHandler()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal override void handleEvent(EventType type, object sender, EventArgs e)
      {
         handleEvent(type, (Control)sender, e, null);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      /// <param name="mapData"></param>
      internal void handleEvent(EventType type, Control sender, EventArgs e, MapData mapData)
      {
         MapData orgMapData = mapData;
         ContainerManager containerManager = GuiUtils.getContainerManager(sender);
         switch (type)
         {
#if !PocketPC
            case EventType.DRAG_OVER:
            case EventType.DRAG_DROP:
               // Need to get current Control, Hence needed
               Point pt = new Point(((DragEventArgs)e).X, ((DragEventArgs)e).Y);
               Point screen = sender.PointToClient(pt);
               mapData = containerManager.HitTest(screen, true, false);
               break;
#endif
            case EventType.MOUSE_MOVE:
               // the mouse move will handle the tooltip
               mapData = containerManager.HitTest(new Point(((MouseEventArgs)e).X, ((MouseEventArgs)e).Y), true, false);
               break;

            case EventType.NCMOUSE_DOWN:
               MapData containerMapData = ControlsMap.getInstance().getMapData(sender);
               Events.OnMouseDown(containerMapData.getForm(), containerMapData.getControl(), null, true, 0, true, true);
               break;

            case EventType.MOUSE_DOWN:
               // check if a point on some table child
               // fixed bug #986057 (same in online)
               // * when pressing Rclick(button != 1) on Table or on the divider the Table's Context Menu will be
               // display( if press on other row no focus will move to the new row)
               // * when pressing Rclick exactly edit the context menu of the edit will be display and the focus
               // will move to the control
               bool LeftClickWasPressed = ((MouseEventArgs)e).Button == MouseButtons.Left;
               bool findExact = (sender is TableControl && LeftClickWasPressed ? false : true);
               mapData = containerManager.HitTest(new Point(((MouseEventArgs)e).X, ((MouseEventArgs)e).Y), findExact, true);
               if (mapData != null)
               {
                  // Defect# 130085: When we click on a control (say checkbox) placed on table header, mouseDown is not needed to be processed.
                  // Actually, this is received from WM_PARENTNOTIFY from TableControl. This will avoid multiple ACT_CTRL_HIT on table header child 
                  // control and Table control. Another option was to set WS_EX_NOPARENTNOTIFY style to table header child.
                  GuiMgControl mgControl = mapData.getControl();
                  if (mgControl.IsTableHeaderChild)
                     return;
                  // mark that we need to focus on the control
                  GuiUtils.saveFocusingControl(GuiUtils.FindForm(sender), mapData);
               }
               else if (sender is TableControl)
                  GuiUtils.restoreFocus(GuiUtils.FindForm(sender));
               break;

            case EventType.MOUSE_UP:
               // for mouse up we need control the has been clicked and not nearest control
               mapData = containerManager.HitTest(new Point(((MouseEventArgs)e).X, ((MouseEventArgs)e).Y), true, true);
               break;

            case EventType.MOUSE_DBLCLICK:
               if (!(sender is TableControl)) //TODO
                  mapData = containerManager.HitTest(new Point(((MouseEventArgs)e).X, ((MouseEventArgs)e).Y), true, true);
               break;

            case EventType.PRESS:
               findExact = !(sender is TableControl);
               // Mobile clients for which this event type is applicable must 
               // pass actual press co-ords
               Point point = new Point(0, 0);
               mapData = containerManager.HitTest(point, findExact, true);
               if(mapData != null)
                  GuiUtils.saveFocusingControl(GuiUtils.FindForm(sender), mapData);
               break;

            case EventType.PAINT:
               if (containerManager is BasicControlsManager)
                  ((BasicControlsManager)containerManager).Paint(((PaintEventArgs)e).Graphics);
               break;

#if PocketPC
            case EventType.RESIZE:
               EditorSupportingPlacementLayout placementLayout1 = ((TagData)sender.Tag).PlacementLayout;
               if (placementLayout1 != null)
               {
                  LayoutEventArgs le = new LayoutEventArgs();
                  placementLayout1.layout(sender, le);
               }
               break;

#endif
            case EventType.LAYOUT:
               EditorSupportingPlacementLayout placementLayout = ((TagData)sender.Tag).PlacementLayout;
               if (placementLayout != null)
                  placementLayout.layout(sender, (LayoutEventArgs)e);
               break;

            case EventType.DISPOSED:
               containerManager.Dispose();
               break;
         }

         if (mapData == null)
            mapData = orgMapData;
         DefaultHandler.getInstance().handleEvent(type, sender, e, mapData);
      }
   }
}
