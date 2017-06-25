using com.magicsoftware.controls;
using com.magicsoftware.util;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Controls.com.magicsoftware.controls.Renderer
{
   internal class WindowsThreeDBorderRenderer : IBorderRenderer
   {
      public void DrawBorder(Graphics graphics, Pen Pen, int borderSize, Rectangle rect, bool rightToLeft)
      {
         // back up the original properties
         float originalwidth = Pen.Width;
         PenAlignment originalAlignment = Pen.Alignment;

         //  draw_3d_rect(Gui, hdc, &Rect, TRUE, TRUE);	
         // 
         BorderRenderer.PaintBorder(graphics, Pen, rect, ControlStyle.ThreeDSunken, true);
         rect.Width--;
         rect.Height--;

         // InflateRect(&Rect, -factor, -factor);
         rect.Inflate(-1, -1);
         
         // left line and top lines in Black
         //  draw_table_secondary_border ()

         graphics.DrawLine(rightToLeft ? SystemPens.ControlLight:  SystemPens.ControlText, new Point(rect.Left, rect.Top), new Point(rect.Left, rect.Bottom));

         graphics.DrawLine(SystemPens.ControlText, new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Top));


         // right and bottom lines in ControlLight
         if (rightToLeft)
            rect.Width++;

         graphics.DrawLine(rightToLeft ? SystemPens.ControlText : SystemPens.ControlLight, new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Bottom));
         graphics.DrawLine(SystemPens.ControlLight, new Point(rect.Left, rect.Bottom), new Point(rect.Right, rect.Bottom));

         Pen.Alignment = originalAlignment;
      }
   }
}