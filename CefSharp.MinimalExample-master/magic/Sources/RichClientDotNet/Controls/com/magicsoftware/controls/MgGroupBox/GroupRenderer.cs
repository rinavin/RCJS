using com.magicsoftware.controls;
using com.magicsoftware.util;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.MgShape
{
   /// <summary>
   /// Group control renderer.
   /// </summary>
   public class GroupRenderer
   {
      /// <summary>
      /// Paint background color and border excluding clip rectangle of Group box control.
      /// </summary>
      /// <param name="g"></param>
      /// <param name="bounds"></param>
      /// <param name="pen"></param>
      /// <param name="backColor"></param>
      /// <param name="controlStyle"></param>
      /// <param name="textRect"></param>
      /// <param name="font"></param>
      /// <param name="keepTopBorderMargin"></param>
      internal static void Draw(Graphics g, Rectangle bounds, Pen pen, Color backColor, ControlStyle controlStyle, Rectangle textRect, Font font, bool keepTopBorderMargin)
      {
         // Fill  rectangle
         g.FillRectangle(SolidBrushCache.GetInstance().Get(backColor), bounds);
         DrawBorder(g, bounds, pen, controlStyle, textRect, font, keepTopBorderMargin);
      }

      /// <summary>
      /// Paint the border excluding clip rectangle of Group Box control.
      /// </summary>
      /// <param name="g"></param>
      /// <param name="bounds"></param>
      /// <param name="pen"></param>
      /// <param name="controlStyle"></param>
      /// <param name="textRect"></param>
      /// <param name="font"></param>
      /// <param name="keepTopBorderMargin"></param>
      internal static void DrawBorder(Graphics g, Rectangle bounds, Pen pen, ControlStyle controlStyle, Rectangle textRect, Font font, bool keepTopBorderMargin)
      {
         if (!keepTopBorderMargin)
         {
            // adjust the border according to text rectangle.
            bounds.Y += textRect.Height / 2;
            bounds.Height -= textRect.Height / 2;
         }
         else
         {
            // adjust the border according to font height.
            bounds.Y += font.Height / 2;
            bounds.Height -= font.Height / 2;
         }
         Region originalClip = g.Clip;
         g.ExcludeClip(textRect);    // Don't draw border in text area

         BorderRenderer.PaintBorder(g, pen, bounds, controlStyle, true);

         g.Clip = originalClip;  // reset original clip
      }
   }
}