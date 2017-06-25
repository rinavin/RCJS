using System.Drawing;
using com.magicsoftware.util;
using com.magicsoftware.controls;

namespace Controls.com.magicsoftware.controls.MgShape
{
   /// <summary>
   /// This class is responsible for rendering ellipse
   /// </summary>
   internal class EllipseRenderer
   {
      internal static void Draw(Graphics graphics, Pen pen, Rectangle rect, Color backColor, ControlStyle controlStyle)
      {
         // Fill  the ellipse . In this case reduce the height and width of rect by 1 so that it is painted filled only inside the border
         graphics.FillEllipse(SolidBrushCache.GetInstance().Get(backColor), new Rectangle(rect.Left, rect.Top, rect.Width -1, rect.Height -1));

         // Draw border
         if (rect.Width > 1 && rect.Height > 1)
         {
            BorderRenderer.DrawEllipseBorder(graphics, pen, rect, controlStyle); 
         }
      }
   }
}