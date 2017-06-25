using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using com.magicsoftware.controls;
using com.magicsoftware.util;

namespace Controls.com.magicsoftware.controls.MgShape
{
   /// <summary>
   /// This class is responsible for rendering Line
   ///
   /// </summary>
   public class LineRenderer
   {
      /// <summary>
      /// Paints the Line control
      /// </summary>
      internal static void Draw(Graphics graphics, Pen pen, Rectangle clientRectangle, Color backColor, ControlStyle controlStyle, LineDirection lineDirection)
      {
         Point[] linePoints = LineRenderer.GetLinePoints(clientRectangle, (int)pen.Width, lineDirection);
         Draw(graphics, pen, linePoints[0], linePoints[1], controlStyle);
      }

      /// <summary>
      /// Paints the Line control
      /// TODO [Sadhana] : Make this method private when we move PaintLineForeGround() from ControlRenderer here
      /// </summary>
      internal static void Draw(Graphics g, Pen pen, Point pt1, Point pt2, ControlStyle controlStyle)
      {
         switch (controlStyle)
         {
            case ControlStyle.TwoD:
#if !PocketPC
               pen.StartCap = LineCap.Round;
               pen.EndCap = LineCap.Round;
#endif

               g.DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
               break;

            case ControlStyle.ThreeD:
               if (pt1.X == pt2.X)
               {
                  //draw horizontal
                  g.DrawLine(SystemPens.ButtonHighlight, pt1.X, pt1.Y, pt2.X, pt2.Y);
                  pt1.X = ++pt2.X;
                  g.DrawLine(SystemPens.ButtonShadow, pt1.X, pt1.Y, pt2.X, pt2.Y);
               }
               else
               {
                  pt1.Y--;
                  pt2.Y--;
                  g.DrawLine(SystemPens.ButtonHighlight, pt1.X, pt1.Y, pt2.X, pt2.Y);
                  pt1.Y++;
                  pt2.Y++;
                  g.DrawLine(SystemPens.ButtonShadow, pt1.X, pt1.Y, pt2.X, pt2.Y);
               }
               break;

            case ControlStyle.ThreeDSunken:
               if (pt1.X == pt2.X)
               {
                  //draw horizontal
                  g.DrawLine(SystemPens.ButtonShadow, pt1.X, pt1.Y, pt2.X, pt2.Y);
                  pt1.X = ++pt2.X;
                  g.DrawLine(SystemPens.ButtonHighlight, pt1.X, pt1.Y, pt2.X, pt2.Y);
               }
               else
               {
                  g.DrawLine(SystemPens.ButtonShadow, pt1.X, pt1.Y, pt2.X, pt2.Y);
                  pt1.Y++;
                  pt2.Y++;
                  g.DrawLine(SystemPens.ButtonHighlight, pt1.X, pt1.Y, pt2.X, pt2.Y);
               }
               break;
         }
      }

      /// <summary>
      /// Set the Line's Start and End points
      /// TODO [Sadhana] : make this method private after MgLine will not use Points
      /// </summary>
      public static Point[] GetLinePoints(Rectangle Rectangle, int LineWidth, LineDirection lineDirection)
      {
         int X = Rectangle.X;
         int Y = Rectangle.Y;
         int Width = Rectangle.Width;
         int Height = Rectangle.Height;

         Point StartPoint = Point.Empty;
         Point EndPoint = Point.Empty;

         int offset = 0;
         if (LineWidth > 1)
            offset = (int)Math.Ceiling((double)LineWidth / 2);

         switch (lineDirection)
         {
            case LineDirection.Horizontal:   // Centered Vertically
               StartPoint = new Point(X + offset, Y + (Height / 2));
               EndPoint = new Point(X + (Width - offset), Y + (Height / 2));
               break;

            case LineDirection.Vertical:     // Centered Horizontally
               StartPoint = new Point(X + (Width / 2), Y + offset);
               EndPoint = new Point(X + (Width / 2), Y + (Height - offset));
               break;

            case LineDirection.NESW: // '/' shape line
               offset += 1;
               StartPoint = new Point(X + (Width - offset), Y + offset);
               EndPoint = new Point(X + offset, Y + (Height - offset));
               break;

            case LineDirection.NWSE: // '\' shape
               offset += 1;
               StartPoint = new Point(X + offset, Y + offset);
               EndPoint = new Point(X + (Width - offset), Y + (Height - offset));
               break;

            default:
               Debug.Assert(false, "This line direction is not supported ");
               break;
         }

         return new Point[] { StartPoint, EndPoint };
      }
   }
}