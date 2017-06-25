using System.Drawing;
using com.magicsoftware.controls;
using com.magicsoftware.util;

namespace Controls.com.magicsoftware.controls.MgShape
{
   /// <summary>
   /// This class is responsible for rendering Rectangle and Rounded Rectangle
   /// </summary>
   internal class RectangleRenderer
   {
      internal static void Draw(Graphics graphics, Pen pen, Rectangle rect, Color backColor, ControlStyle controlStyle)
      {
         // Fill rectangle
         graphics.FillRectangle(SolidBrushCache.GetInstance().Get(backColor), rect);

         // Draw border
         BorderRenderer.PaintBorder(graphics, pen, rect, controlStyle, false);
      }

      internal static void DrawRoundedRectangle(Graphics graphics, Pen pen, Rectangle clientRectangle, Color backColor, ControlStyle controlStyle)
      {
         int xRadius = clientRectangle.Width / 8;
         int yRadius = clientRectangle.Height / 8;

         if (xRadius > 2 && yRadius > 2)
         {
            // Fill Rounded Rectangle
            GraphicsExtension.FillRoundedRectangle(graphics, SolidBrushCache.GetInstance().Get(backColor), clientRectangle.X, clientRectangle.Y, clientRectangle.Width, clientRectangle.Height, xRadius, yRadius);

            // Draw border
            GraphicsExtension.DrawRoundedRectangle(graphics, pen, clientRectangle.X, clientRectangle.Y, clientRectangle.Width, clientRectangle.Height, xRadius, yRadius); 
         }
      }
   }
}