using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// Renders row divider for Table Control
   /// </summary>
   public interface IRowDividerRenderer
   {
      /// <summary>
      /// Renders row divider for Table Control
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="dividerPen"></param>
      /// <param name="x1"></param>
      /// <param name="y1"></param>
      /// <param name="x2"></param>
      /// <param name="y2"></param>
      void Render(Graphics graphics, Pen dividerPen, int borderSize, int factor, int left, int top, int right);
   }
}