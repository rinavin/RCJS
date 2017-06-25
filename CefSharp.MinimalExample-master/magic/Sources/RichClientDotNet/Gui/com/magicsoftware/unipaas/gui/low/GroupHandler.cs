using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using com.magicsoftware.controls;
using com.magicsoftware.controls.utils;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Handler for Group
   /// </summary>
   /// <author>  rinav </author>
   internal class GroupHandler : HandlerBase
   {
      private static GroupHandler _instance;
      internal static GroupHandler getInstance()
      {
         if (_instance == null)
            _instance = new GroupHandler();
         return _instance;
      }

      /// <summary> </summary>
      private GroupHandler()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="group"></param>
      internal void addHandler(GroupBox group)
      {
         group.MouseMove += MouseMoveHandler;
         group.Layout += LayoutHandler;
         group.MouseDown += MouseDownHandler;
         group.MouseUp += MouseUpHandler;
         group.Disposed += DisposedHandler;
         group.MouseDoubleClick += MouseDoubleClickHandler;
         group.MouseLeave += MouseLeaveHandler;
         group.MouseEnter += MouseEnterHandler;

#if !PocketPC
         group.DragDrop += DragDropHandler;
         group.DragOver += DragOverHandler;
         group.GiveFeedback += GiveFeedBackHandler;
#endif
         group.Paint += PaintHandler;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         MapData mapData = ControlsMap.getInstance().getMapData(sender);
         if (mapData == null)
            return;

         DefaultContainerHandler.getInstance().handleEvent(type, sender, e);
      }
   }
}