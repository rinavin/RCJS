using System;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   Handler for composite control of tab's item
   /// </summary>
   /// <author>  rinav </author>
   internal class TabControlPanelHandler : HandlerBase, ICloneable
   {
      private static TabControlPanelHandler _instance;
      internal static TabControlPanelHandler getInstance()
      {
         if (_instance == null)
            _instance = new TabControlPanelHandler();
         return _instance;
      }

      private TabControlPanelHandler()
      {
      }

      #region ICloneable Members

      public Object Clone()
      {
         throw new Exception("CloneNotSupportedException");
      }

      #endregion

      /// <summary>
      ///   add handler for the text
      /// </summary>
      /// <param name = "panel"></param>
      internal void addHandler(Panel panel)
      {
         panel.MouseMove += MouseMoveHandler;
#if !PocketPC
         panel.MouseEnter += MouseEnterHandler;
         panel.MouseHover += MouseHoverHandler;
         panel.MouseLeave += MouseLeaveHandler;
         panel.MouseWheel += MouseWheelHandler;
         panel.MouseDoubleClick += MouseDoubleClickHandler;
         panel.Layout += LayoutHandler;
         panel.DragOver += DragOverHandler;
         panel.DragDrop += DragDropHandler;
         panel.GiveFeedback += GiveFeedBackHandler;
#else
         panel.Resize += ResizeHandler;
#endif
         panel.MouseDown += MouseDownHandler;
         panel.MouseUp += MouseUpHandler;
         panel.GotFocus += GotFocusHandler;
         panel.Paint += PaintHandler;
         panel.Disposed += DisposedHandler;
      }

      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         MgPanel tabControlPanel = (MgPanel) sender;

         TabControl tabControl = ((TagData) (tabControlPanel.Tag)).ContainerTabControl;

         MapData mapData = controlsMap.getMapData(tabControl);

         switch (type)
         {
            case EventType.PAINT:
               ControlRenderer.FillRectAccordingToGradientStyle(((PaintEventArgs)e).Graphics, tabControlPanel.ClientRectangle, tabControlPanel.BackColor,
                                                                tabControlPanel.ForeColor, ControlStyle.NoBorder, false, tabControlPanel.GradientColor, 
                                                                tabControlPanel.GradientStyle);
               break;
         }

         DefaultContainerHandler.getInstance().handleEvent(type, (Control) sender, e, mapData);

         if (type == EventType.DISPOSED)
            tabControl.Tag = null;
      }
   }
}