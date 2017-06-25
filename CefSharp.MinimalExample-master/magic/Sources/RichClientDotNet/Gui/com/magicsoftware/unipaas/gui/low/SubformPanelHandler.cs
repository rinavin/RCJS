using System;
using System.Windows.Forms;
using com.magicsoftware.controls;
using System.Drawing;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Handler for inner composite of shell or subform
   /// </summary>
   /// <author>  rinav</author>
   class SubformPanelHandler : HandlerBase
   {
      private static SubformPanelHandler _instance;
      internal static SubformPanelHandler getInstance()
      {
         if (_instance == null)
            _instance = new SubformPanelHandler();
         return _instance;
      }

      private SubformPanelHandler()
      {
      }

      /// <summary> 
      /// adds events for form
      /// </summary>
      /// <param name="control"></param>
      internal void addHandler(Control control)
      {
         control.MouseDown += MouseDownHandler;
         control.MouseUp += MouseUpHandler;
         control.MouseMove += MouseMoveHandler;
#if !PocketPC
           control.MouseDoubleClick += MouseDoubleClickHandler;
           control.PreviewKeyDown += PreviewKeyDownHandler;
           control.Layout += LayoutHandler;
           control.DragDrop += DragDropHandler;
           control.DragOver += DragOverHandler;
           control.GiveFeedback += GiveFeedBackHandler;
           control.MouseWheel += MouseWheelHandler;

           if (control is ScrollableControl)
           {
              ScrollableControl scrollableControl = control as ScrollableControl;
              scrollableControl.Scroll += ScrollHandler;
           }

#endif
         control.KeyDown += KeyDownHandler;
         control.KeyPress += KeyPressHandler;
         control.Resize += ResizeHandler;
         control.Disposed += DisposedHandler;
         control.Paint += PaintHandler;
#if !PocketPC
         if (control is MgPanel)
            ((MgPanel)control).NCMouseDown += NCMouseDownHandler;
#endif

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

         Object guiMgObject = mapData.getControl();
         if (guiMgObject == null)
            guiMgObject = mapData.getForm();
         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgObject));
         try
         {
            switch (type)
            {
#if !PocketPC
               case EventType.SCROLL:
                  {
                     if (sender is ScrollableControl)
                     {
                        ScrollableControl scrollableControl = sender as ScrollableControl;
                        TagData tg = (TagData)scrollableControl.Tag;
                        if (scrollableControl.BackgroundImage != null)
                           scrollableControl.Invalidate();
                        else
                        {
                           //This is a.Net bug. When scrollbar gets hidden during the process of thumb drag, framework still keeps 
                           //a rectangular bar visible to keep dragging on. Now, this rectangle is not removed even when the scrolling is stopped.
                           //The workaround is to repaint the form if scrollbar is not present on the form when scroll dragging is stopped.

                           ScrollEventArgs se = (ScrollEventArgs)e;
                           if (se.Type == ScrollEventType.ThumbPosition)
                           {
                              bool hasVerticalScrollBar = scrollableControl.AutoScrollMinSize.Height > scrollableControl.ClientSize.Height;

                              if (!hasVerticalScrollBar)
                                 scrollableControl.Invalidate();
                           }
                        }
                     }
                  }
                  break;
#endif
               case EventType.RESIZE:
                  onResize((Control)sender);
                  break;
            }
            DefaultContainerHandler.getInstance().handleEvent(type, sender, e);

#if PocketPC
            // paint the subform's border. Do it after the controls are painted, so we can paint over them.
            if (type == EventType.PAINT && ((MgPanel)sender).BorderStyle != BorderStyle.None)
            {
               BorderRenderer.PaintBorder(((PaintEventArgs)e).Graphics, ((Control)sender).ClientRectangle,
                                          Color.Black, ControlStyle.Windows, false);
            }
#endif
         }
         finally 
         {
            contextIDGuard.Dispose(); 
         }
      }
    
      /// <summary>
      /// resize the subform panel cause the image to be updated
      /// </summary>
      /// <param name="panel"></param>
      private void onResize(Control panel)
      {
         GuiUtils.setBackgroundImage((Control)panel);
#if !PocketPC
         //fixed bug#:714206, and problem whith MDI client refresh. 
         //we have missing invalidate on the panel.
         panel.Invalidate();
#endif
      }


   }
}
