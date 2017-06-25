using System;
using com.magicsoftware.controls;
#if !PocketPC
using System.Windows.Forms;
using System.Drawing;
#else
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
using HandledMouseEventArgs = com.magicsoftware.mobilestubs.HandledMouseEventArgs;
using System.Windows.Forms;

#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Handler for table control
   /// </summary>
   /// <author>  rinat</author>
   class TableHandler : HandlerBase
   {
      private static TableHandler _instance;

      /// <returns>
      /// </returns>
      internal static TableHandler getInstance()
      {
         if (_instance == null)
            _instance = new TableHandler();
         return _instance;
      }

      /// <summary> </summary>
      private TableHandler()
      {
      }

      /// <summary> 
      /// adds handler for Table control
      /// </summary>
      /// <param name="table"></param>
      internal void addHandler(TableControl table)
      {
         table.MouseMove += MouseMoveHandler;
         table.MouseDown += MouseDownHandler;
#if !PocketPC
         table.MouseLeave += MouseLeaveHandler;
         table.Layout += LayoutHandler;
         table.MouseWheel += MouseWheelHandler;
         table.DragOver += DragOverHandler;
         table.DragDrop += DragDropHandler;
         table.GiveFeedback += GiveFeedBackHandler;
#endif
         table.Scroll += ScrollHandler;
         table.Reorder += ReorderHandler;
         table.MouseUp += MouseUpHandler;
         table.Resize += ResizeHandler;
        
         table.PaintItem += PaintItemHandler;
         //table.EraseItem += EraseItemHandler;
         table.ItemDisposed += DisposedItemHandler;
         table.Disposed += DisposedHandler;
         table.ReorderEnded += ReorderEndedHandler;
         table.NCMouseDown += NCMouseDownHandler;
         table.HorizontalScrollVisibilityChanged += HorizontalScrollVisibilityChangedHandler;
         table.KeyPress += KeyPressHandler;
         table.KeyDown += KeyDownHandler;
         table.PreviewKeyDown += PreviewKeyDownHandler;

      }

 
      /// <summary> </summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         TableControl table = (TableControl)sender;
         TableManager tableManager = GuiUtils.getTableManager(table);
         if (tableManager == null)
            return;

         switch (type)
         {
            case EventType.PAINT_ITEM:
               tableManager.PaintRow((TablePaintRowArgs)e);
               return;

            case EventType.ERASE_ITEM:
               return;

            case EventType.SCROLL:
               if (((ScrollEventArgs)e).ScrollOrientation == ScrollOrientation.VerticalScroll)
                  tableManager.ScrollVertically((ScrollEventArgs)e);
               return;

#if PocketPC
            case EventType.RESIZE:
               tableManager.resize();
               tableManager.PerformLayout(sender, e);
               break;
#endif

            case EventType.LAYOUT:
               if (((LayoutEventArgs)e).AffectedControl == sender)
               {
#if !PocketPC
                  //Defer the resizing of table until the form is in resize mode.
                  //Resize doesn't mean resizing the table itself but it actually 
                  //means post-resize stuff like creating new rows, fetching data, etc.
                  Form parent = GuiUtils.FindForm(table);

                  // Parent can be null, when we just created a control and before attaching parent, we reset its size.
                  if (parent != null)
                  {
                     TagData tagData = (TagData)parent.Tag;
                     if (!tagData.IgnoreWindowResizeAndMove)
                        tableManager.resize();
                  }
#else
                  tableManager.resize();
#endif
               }
               return;

            case EventType.MOUSE_DOWN:
               bool leftClickWasPressed = ((MouseEventArgs)e).Button == MouseButtons.Left;

#if !PocketPC
               if (leftClickWasPressed)
               {
                  MapData mapData = tableManager.HitTest(new Point(((MouseEventArgs)e).X, ((MouseEventArgs)e).Y), false, true);
                  if (mapData != null)
                  {
                     int row = mapData.getIdx();
                     Modifiers modifiers = GuiUtils.getModifier(Control.ModifierKeys);
                     // Defect 124555. Copy from Gui_Table.cpp LButtonDown_On_Table: for click on a table with AllowDrag
                     // with rows selected and the click on the selected row do not put MUTI_MARK_HIT action.
                     if (row >= 0 && !(((TagData)table.Tag).AllowDrag && tableManager.IsInMultimark && tableManager.IsItemMarked(row)))
                        Events.OnMultiMarkHit(ControlsMap.getInstance().getMapData(sender).getControl(), row + 1, modifiers);
                  }
               }
#endif
               break;

            case EventType.REORDER_STARTED:
               TableReorderArgs ea = (TableReorderArgs)e;
               tableManager.reorder(ea.column, ea.NewColumn);
               break;

            case EventType.REORDER_ENDED:
               tableManager.refreshPage();
               break;

            case EventType.DISPOSE_ITEM:
               tableManager.cleanItem(((TableItemDisposeArgs)e).Item);
               return;

            case EventType.HORIZONTAL_SCROLL_VISIBILITY_CHANGED:
               tableManager.resize();
               return;
         }
         DefaultContainerHandler.getInstance().handleEvent(type, sender, e);
      }
   }
}
