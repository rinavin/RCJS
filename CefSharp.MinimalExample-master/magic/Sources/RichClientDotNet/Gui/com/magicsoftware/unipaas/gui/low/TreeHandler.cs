using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Class implements handlers for the tree controls, this class is a singleton
   /// </summary>
   /// <author>  rinat</author>
   class TreeHandler : HandlerBase
   {
      private static TreeHandler _instance;
      internal static TreeHandler getInstance()
      {
         if (_instance == null)
            _instance = new TreeHandler();
         return _instance;
      }

      /// <summary> 
      /// add handler for the tree
      /// </summary>
      /// <param name="tree">tree</param>
      internal void addHandler(MgTreeView tree)
      {
         tree.MouseDown += MouseDownHandler;
         tree.MouseUp += MouseUpHandler;
         tree.MouseMove += MouseMoveHandler;
         tree.MouseEnter += MouseEnterHandler;
         tree.MouseHover += MouseHoverHandler;
         tree.MouseLeave += MouseLeaveHandler;
         tree.MouseWheel += MouseWheelHandler;
         tree.MouseDoubleClick += MouseDoubleClickHandler;
         tree.PreviewKeyDown += PreviewKeyDownHandler;
         tree.KeyDown += KeyDownHandler;
         tree.KeyPress += KeyPressHandler;
         tree.KeyUp += KeyUpHandler;
         tree.GotFocus += GotFocusHandler;
         tree.LostFocus += LostFocusHandler;
         tree.Resize += ResizeHandler;
         tree.BeforeExpand += BeforeExpandHandler;
         tree.BeforeCollapse += BeforeCollapseHandler;
         tree.BeforeLabelEdit += LabelEditHandler;
         tree.Disposed += DisposedHandler;
         tree.NodeMouseHover += NodeMouseHoverHandler;
         tree.Scroll += ScrollHandler;
         tree.MouseWheel += MouseWheelHandler;
         tree.DragOver += DragOverHandler;
         tree.DragDrop += DragDropHandler;
         tree.GiveFeedback += GiveFeedBackHandler;
         tree.BeforeSelect += TreeNodeBeforeSelect;

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
         TreeView tree = (TreeView)sender;
         TreeManager treeManager = GuiUtils.getTreeManager(tree);
         Point location = new Point();
         MapData mapData = null;
         Point pt;

         if (e is MouseEventArgs)
            location = ((MouseEventArgs)e).Location;
         if (tree.IsDisposed)
            return;

         switch (type)
         {
            case EventType.GOT_FOCUS:
               treeManager.setFocusTime();
               break;

            case EventType.NODE_MOUSE_HOVER:
               mapData = controlsMap.getMapData(((TreeNodeMouseHoverEventArgs)e).Node);
               break;

            case EventType.MOUSE_LEAVE:
               //workaround for .NET problem mouseleave is sent too much times for every tree node
               //we must check if the tooltip left tree
               pt = tree.PointToClient(Control.MousePosition);
               if (tree.ClientRectangle.Contains(pt))
                  return;
               break;

            case EventType.MOUSE_UP:
               tree.MouseMove += TreeHandler.getInstance().MouseMoveHandler;
               treeManager.InExpand = false;
               break;

            case EventType.MOUSE_DOWN:
               treeManager.setMouseDownTime();
               break;

            case EventType.MOUSE_MOVE:
               // Mouse move only to decide, whether we should start drag operation or not.
               // Should not handle MouseMove further : Need to handle NodeMouseHover is instead of mousemove

               Control control = (Control)sender;
               if (!treeManager.InExpand && GuiUtils.ShouldPerformBeginDrag(control, (MouseEventArgs)e))
               {
                  mapData = GuiUtils.getContainerManager(control).HitTest(new Point(((MouseEventArgs)e).X, ((MouseEventArgs)e).Y), true, false);
                  // mapData == null means we are on the node's sign.
                  if (mapData != null)
                  {
                     //Before starting to drag, if we are dragging from a different node
                     //the selected node, move the selected node before starting to drag.
                     // (* it will actually do something only for RTE not RC).
                     if (tree.SelectedNode != null)
                     {
                        MapData oldNodmapDataDrag = controlsMap.getMapData(tree.SelectedNode);
                        int oldLineDrag = oldNodmapDataDrag.getIdx();

                        if (mapData.getIdx() != oldLineDrag)
                           Events.OnTreeNodeSelectChange(mapData.getControl(), oldLineDrag, mapData.getIdx());
                     }

                     GuiUtils.BeginDrag(control, (MouseEventArgs)e, mapData);
                  }
               }
               return;

            case EventType.DRAG_DROP:
               control = (Control)sender;
               pt = new Point(((DragEventArgs)e).X, ((DragEventArgs)e).Y);
               Point screen = control.PointToClient(pt);
               mapData = GuiUtils.getContainerManager(control).HitTest(screen, true, false);

               // mapData == null means we are on the node's sign.
               if (mapData != null)
               {
                  //Before starting to drop, if we are dropping on a different node
                  //the selected node, move the selected node before starting to drop.
                  // (* it will actually do something only for RTE not RC).
                  if (tree.SelectedNode != null)
                  {
                     MapData oldNodmapDataDrop = controlsMap.getMapData(tree.SelectedNode);
                     int oldLineDrop = oldNodmapDataDrop.getIdx();

                     if (mapData.getIdx() != oldLineDrop)
                        Events.OnTreeNodeSelectChange(mapData.getControl(), oldLineDrop, mapData.getIdx());
                  }
               }
               break;

            case EventType.LABEL_EDIT:
               if (!treeManager.IsLabelEditAllowed)
                  ((NodeLabelEditEventArgs)e).CancelEdit = true;
               else
               {
                  mapData = controlsMap.getMapData(((NodeLabelEditEventArgs)e).Node);
                  Events.OnEditNode(mapData.getControl(), mapData.getIdx());
               }
               return;

            case EventType.KEY_DOWN:
               KeyEventArgs keyEventArgs = (KeyEventArgs)e;
               // check if we should handle the key down (in default handler) or let the
               // tree continue with its default behavior.
               if (!Events.ShouldHandleTreeKeyDown(keyEventArgs.KeyCode))
                  return; // let tree send default expand collapse event
               break;

            case EventType.RESIZE:
               treeManager.resize();
               return;

            case EventType.SCROLL:
            case EventType.MOUSE_WHEEL:
               GuiUtils.checkAndCloseTreeEditorOnClick(tree);
               return;

            case EventType.BEFOR_EXPAND:
               treeManager.InExpand = true;
               
               mapData = controlsMap.getMapData(((TreeViewCancelEventArgs)e).Node);
               TreeChild treeChild = (TreeChild)controlsMap.object2Widget(mapData.getControl(), mapData.getIdx());
               
               Events.OnExpand(mapData.getControl(), mapData.getIdx());
               ((TreeViewCancelEventArgs)e).Cancel = true;
               return;

            case EventType.BEFOR_COLLAPSE:
               mapData = controlsMap.getMapData(((TreeViewCancelEventArgs)e).Node);
               Events.OnCollapse(mapData.getControl(), mapData.getIdx());
               ((TreeViewCancelEventArgs)e).Cancel = true;
               return;

            case EventType.BEFORE_SELECT:
               MapData oldNodmapData = null; 
               int oldLine = 0;

               mapData = controlsMap.getMapData(((TreeViewCancelEventArgs)e).Node);
               if (mapData != null)
               {
                  GuiMgControl mgControl = mapData.getControl();
                  int newLine = mapData.getIdx();

                  if (tree.SelectedNode != null)
                  {
                     oldNodmapData = controlsMap.getMapData(tree.SelectedNode);
                     oldLine = oldNodmapData.getIdx();
                  }

                  // if true, cancel the change (true for online, false for RC)
                  // in online there is handling for the select change and we don't want it to 
                  // happen here.
                  if (Events.OnTreeNodeSelectChange(mgControl, oldLine, newLine))
                     ((TreeViewCancelEventArgs)e).Cancel = true;

               }
               else
                  ((TreeViewCancelEventArgs)e).Cancel = true;

               return;
         }
         DefaultContainerHandler.getInstance().handleEvent(type, (Control)sender, e, mapData);
      }
   }
}