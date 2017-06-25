using com.magicsoftware.controls;
using com.magicsoftware.util;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// Render the columns for 2-D style table
   /// </summary>
   internal class TwoDStyleColumnDividerRenderer : IColumnDividerRenderer
   {
      public void Render(Graphics graphics, TableControl tableControl, Pen dividerPen, int left, int top, int bottom)
      {  
   
         // Code from draw_column_divider() of column.cpp
         //   SetRect(&Rect1, Tmp - COL_DIVIDER * TableControl.Factor + TableControl.Factor, Rect.top,
         //            Tmp - COL_DIVIDER * TableControl.Factor + TableControl.Factor, Rect.top + tableCtrl->TitleHeight - TableControl.Factor);


         int x = tableControl.RightToLeftLayout ?
              left + TwoDTableStyleRenderer.MARGIN * TableControl.Factor - TableControl.Factor :
              left - TwoDTableStyleRenderer.MARGIN * TableControl.Factor + TableControl.Factor;

         // Draw first line 
         graphics.DrawLine(dividerPen, x, top, x, bottom);

         //  SetRect(&Rect2, Tmp, Rect.top, Tmp, Rect.bottom);
         // Draw second line
         graphics.DrawLine(dividerPen, left, top, left, bottom);

      }
   }
}