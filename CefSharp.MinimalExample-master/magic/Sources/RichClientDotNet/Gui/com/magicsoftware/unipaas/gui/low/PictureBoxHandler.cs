using System;
using System.Windows.Forms;
using com.magicsoftware.controls;
using System.Drawing;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Handler for Image Control
   /// </summary>
   /// <author>  Kaushal Sanghavi </author>
   internal class PictureBoxHandler : HandlerBase
   {
      private static PictureBoxHandler _instance;
      internal static PictureBoxHandler getInstance()
      {
         if (_instance == null)
            _instance = new PictureBoxHandler();
         return _instance;
      }

      private PictureBoxHandler()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="pictureBox"></param>
      internal void addHandler(PictureBox pictureBox)
      {
         pictureBox.MouseMove += MouseMoveHandler;
         pictureBox.MouseDown += MouseDownHandler;
         pictureBox.MouseUp += MouseUpHandler;
         pictureBox.Disposed += DisposedHandler;
#if !PocketPC
         pictureBox.MouseEnter += MouseEnterHandler;
         pictureBox.MouseHover += MouseHoverHandler;
         pictureBox.MouseLeave += MouseLeaveHandler;
         pictureBox.MouseWheel += MouseWheelHandler;
         pictureBox.MouseDoubleClick += MouseDoubleClickHandler;
         pictureBox.SizeChanged += ResizeHandler;
#endif
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="evtArgs"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs evtArgs)
      {
         MapData mapData = ControlsMap.getInstance().getMapData(sender);
         if (mapData == null)
            return;

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(mapData.getControl()));
         try
         {
            switch (type)
            {
               case EventType.RESIZE:
                  GuiUtils.setBackgroundImage((Control)sender);
                  break;

               case EventType.MOUSE_WHEEL:
               case EventType.MOUSE_DOWN:
               case EventType.MOUSE_UP:
               case EventType.MOUSE_DBLCLICK:
                  //case EventType.KeyDown:
                  break;
            }
         }
         finally
         {
            contextIDGuard.Dispose();
         }

         DefaultHandler.getInstance().handleEvent(type, sender, evtArgs);
      }
   }
}