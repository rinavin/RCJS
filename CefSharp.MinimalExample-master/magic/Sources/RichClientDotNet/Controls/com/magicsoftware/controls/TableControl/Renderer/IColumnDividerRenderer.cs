using com.magicsoftware.controls;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// Renders column divider for Table Control
   /// </summary>
   public interface IColumnDividerRenderer
   {
      /// <summary>
      /// Renders column divider for Table Control
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="dividerPen"></param>
      /// <param name="x1"></param>
      /// <param name="y1"></param>
      /// <param name="x2"></param>
      /// <param name="y2"></param>
      void Render(Graphics graphics, TableControl tableControl, Pen dividerPen, int left, int top, int bottom);
   }
}