using com.magicsoftware.controls;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   internal class ThreeDRaisedStyleColumnDividerRenderer : IColumnDividerRenderer
   {
      public void Render(Graphics graphics, TableControl tableControl, Pen dividerPen, int left, int top, int bottom)
      {
         bottom--;     // InflateRect(&Rect, -factor, -factor);

         // code from Ctrl_pnt.cpp , table_3d_paint()
         graphics.DrawLine(ThreeDRaisedStyleRenderer.ShadowPen, left, top, left, bottom); // Gui->ctx->gui_.dc_->line_to (&Rect1, FIX_SYS_COLOR_BTNSHADOW, factor, 0L);

         // Rect1.left -= factor;
         // Gui->ctx->gui_.dc_->line_to(&Rect1, FIX_SYS_COLOR_BTNFACE, 1, 0L);
         // Rect1.left -= factor;
         // Gui->ctx->gui_.dc_->line_to(&Rect1, FIX_SYS_COLOR_BTNFACE, 1, 0L);

         left -= TableControl.Factor;
         graphics.DrawLine(ThreeDRaisedStyleRenderer.BackGroundPen, left, top, left, bottom);
         left -= TableControl.Factor;
         graphics.DrawLine(ThreeDRaisedStyleRenderer.BackGroundPen, left, top, left, bottom);

         left -= TableControl.Factor;
         graphics.DrawLine(ThreeDRaisedStyleRenderer.HighLightPen, left, top, left, bottom); // Gui->ctx->gui_.dc_->line_to (&Rect1, FIX_SYS_COLOR_BTNHIGHLIGHT, factor, 0L);
      }
   }
}