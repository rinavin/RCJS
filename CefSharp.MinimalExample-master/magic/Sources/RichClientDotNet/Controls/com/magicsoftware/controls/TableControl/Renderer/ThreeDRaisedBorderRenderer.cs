using com.magicsoftware.controls;
using com.magicsoftware.util;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Controls.com.magicsoftware.controls.Renderer
{
   internal class ThreeDRaisedBorderRenderer : IBorderRenderer
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

         Pen.Alignment = PenAlignment.Inset;

         //     draw_3d_rect (Gui, hDC, &Rect, TRUE);
         BorderRenderer.PaintBorder(graphics, Pen, rect, ControlStyle.ThreeDSunken, true);

         //  InflateRect (&Rect, -factor, -factor);
         rect.Inflate(-1, -1);

         //   Gui->ctx->gui_.dc_->frame_rect (&Rect, FIX_SYS_COLOR_BTNTEXT, 1, 0L);
         BorderRenderer.PaintBorder(graphics, SystemPens.ControlText, rect, ControlStyle.TwoD, true);

         Pen.Alignment = originalAlignment;
      }
   }
}