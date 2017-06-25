using com.magicsoftware.controls;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   ///
   /// </summary>
   internal class WindowsStyleColumnDividerRenderer : IColumnDividerRenderer
   {
      private const int DIVIDER_OFFSET = -1;

      public void Render(Graphics graphics, TableControl tableControl, Pen dividerPen, int left, int top, int bottom)
      {
         int offset = tableControl.RightToLeftLayout ? 0 : DIVIDER_OFFSET;
         left += offset;
         graphics.DrawLine(dividerPen, left, top, left, bottom);
      }
   }
}