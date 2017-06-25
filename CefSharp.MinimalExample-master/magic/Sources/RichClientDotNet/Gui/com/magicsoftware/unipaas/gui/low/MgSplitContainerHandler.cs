using System;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// 
   /// </summary>
   internal class MgSplitContainerHandler : HandlerBase
   {
      private static MgSplitContainerHandler _instance;
      internal static MgSplitContainerHandler getInstance()
      {
         if (_instance == null)
            _instance = new MgSplitContainerHandler();
         return _instance;
      }

      /// <summary> </summary>
      private MgSplitContainerHandler()
      {
      }

      /// <summary> 
      /// adds events for MgSplitContainer
      /// </summary>
      /// <param name="mgSplitContainer"></param>
      internal void addHandler(MgSplitContainer mgSplitContainer)
      {
         mgSplitContainer.KeyDown += KeyDownHandler;
         mgSplitContainer.Paint += PaintHandler;
         mgSplitContainer.Resize += ResizeHandler;
      }

      /// <summary> handle the event of the MgSplitContainer</summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         MgSplitContainer mgSplitContainer = (MgSplitContainer)sender;
         DefaultHandler defaultHandler = DefaultHandler.getInstance();

         switch (type)
         {
            case EventType.PAINT:
               Graphics G = ((PaintEventArgs)e).Graphics;
               if (GuiUtils.isOutmostMgSplitContainer(mgSplitContainer))
               {
                  Rectangle rect = ((Control)mgSplitContainer).ClientRectangle;
                  Pen pen = new Pen(Color.Black, 1);

                  // draw top horizontal lines -                     
                  pen.Color = SystemColors.ButtonShadow;
                  G.DrawLine(pen, rect.X, rect.Y, rect.Width - 2, rect.Y);
                  pen.Color = Color.Black;
                  G.DrawLine(pen, rect.X + 1, rect.Y + 1, rect.Width - 4, rect.Y + 1);

                  // draw left vertical lines |-
                  pen.Color = SystemColors.ButtonShadow;
                  G.DrawLine(pen, rect.X, rect.Y, rect.X, rect.Height - 3);
                  pen.Color = Color.Black;
                  G.DrawLine(pen, rect.X + 1, rect.Y + 1, rect.X + 1, rect.Height - 4);

                  // draw bottom horizontal lines _
                  pen.Color = SystemColors.ButtonHighlight;
                  G.DrawLine(pen, rect.X + 1, rect.Height - 2, rect.Width - 2, rect.Height - 2);

                  //// draw right vertical lines -|
                  pen.Color = SystemColors.ButtonHighlight;
                  G.DrawLine(pen, rect.Width - 2, rect.Y, rect.Width - 2, rect.Height - 1);

                  pen.Dispose();
               }
               break;

            case EventType.KEY_DOWN:
               defaultHandler.handleEvent(type, sender, e);
               break;

            case EventType.RESIZE:
               mgSplitContainer.onResize();
               break;
         }
      }
   }
}