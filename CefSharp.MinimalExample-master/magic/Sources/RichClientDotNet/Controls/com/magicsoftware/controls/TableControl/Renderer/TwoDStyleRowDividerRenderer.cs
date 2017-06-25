using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   internal class TwoDStyleRowDividerRenderer : IRowDividerRenderer
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
         graphics.DrawLine(dividerPen, left, top, right, top);
      }
   }
}