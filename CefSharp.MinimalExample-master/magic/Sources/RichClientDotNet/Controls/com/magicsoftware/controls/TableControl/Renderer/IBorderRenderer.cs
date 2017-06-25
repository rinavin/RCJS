using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   public interface IBorderRenderer
   {
      /// <summary>
      /// Draw the border
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="dividerPen"></param>
      /// <param name="borderSize"></param>
      /// <param name="rect"></param>
      void DrawBorder(Graphics graphics, Pen dividerPen, int borderSize, Rectangle rect, bool rightToLeft);
   }
}