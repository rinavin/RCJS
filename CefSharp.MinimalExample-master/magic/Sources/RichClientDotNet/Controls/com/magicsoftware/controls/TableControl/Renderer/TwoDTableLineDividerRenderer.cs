using com.magicsoftware.controls;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// Renderer for painting rows of 2-D table control
   /// </summary>
   internal class TwoDTableLineDividerRenderer : IRowDividerRenderer
   {
      /// <summary>
      /// draw the row divider
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="dividerPen"></param>
      /// <param name="borderSize"></param>
      /// <param name="factor"></param>
      /// <param name="left"></param>
      /// <param name="top"></param>
      /// <param name="right"></param>
      public void Render(Graphics graphics, Pen dividerPen, int borderSize, int factor, int left, int top, int right)
      {
         // Adjust the coordinates considering border
         left = left + (borderSize * factor);
         top = top + (borderSize * factor);
         right = right - (borderSize * factor);

         graphics.DrawLine(dividerPen, left, top, right, top);
      }
   }
}