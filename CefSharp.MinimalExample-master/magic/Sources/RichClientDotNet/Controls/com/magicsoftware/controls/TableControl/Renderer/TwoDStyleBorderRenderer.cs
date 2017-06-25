using com.magicsoftware.controls;
using com.magicsoftware.util;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Controls.com.magicsoftware.controls.Renderer
{
   internal class TwoDStyleBorderRenderer : IBorderRenderer
   {
      /// <summary>
      /// draw border
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="Pen"></param>
      /// <param name="borderSize"></param>
      /// <param name="rect"></param>
      public void DrawBorder(Graphics graphics, Pen Pen, int borderSize, Rectangle rect, bool rightToLeft)
      {
         // back up the original properties
         float originalwidth = Pen.Width;
         PenAlignment originalAlignment = Pen.Alignment;

         Pen.Width = borderSize;
         Pen.Alignment = PenAlignment.Inset;
         BorderRenderer.PaintBorder(graphics, Pen, rect, ControlStyle.TwoD, true);

         // restore the original properties 
         Pen.Width = originalwidth;
         Pen.Alignment = originalAlignment;
      }
   }
}