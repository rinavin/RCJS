using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace com.magicsoftware.controls
{
#if !PocketPC
   public static class GraphicsExtension
   {
      public static GraphicsPath GenerateRoundedRectangle(
         Graphics graphics,
         RectangleF rectangle,
         float xRadius, float yRadius)
      {
         float xDiameter;
         float yDiameter;
         GraphicsPath path = new GraphicsPath();
         if (xRadius <= 0.0F)
         {
            path.AddRectangle(rectangle);
            path.CloseFigure();
            return path;
         }
         else
         {
            if (xRadius >= rectangle.Width / 2.0)
               return GenerateCapsule(graphics, rectangle);
            if (yRadius >= rectangle.Height / 2.0)
               return GenerateCapsule(graphics, rectangle);
            xDiameter = xRadius * 2.0F;
            yDiameter = yRadius * 2.0F;
            SizeF sizeF = new SizeF(xDiameter, yDiameter);
            RectangleF arc = new RectangleF(rectangle.Location, sizeF);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - xDiameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - yDiameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
         }
         return path;
      }
      private static GraphicsPath GenerateCapsule(
         Graphics graphics,
         RectangleF baseRect)
      {
         float diameter;
         RectangleF arc;
         GraphicsPath path = new GraphicsPath();
         try
         {
            if (baseRect.Width > baseRect.Height)
            {
               diameter = baseRect.Height;
               SizeF sizeF = new SizeF(diameter, diameter);
               arc = new RectangleF(baseRect.Location, sizeF);
               path.AddArc(arc, 90, 180);
               arc.X = baseRect.Right - diameter;
               path.AddArc(arc, 270, 180);
            }
            else if (baseRect.Width < baseRect.Height)
            {
               diameter = baseRect.Width;
               SizeF sizeF = new SizeF(diameter, diameter);
               arc = new RectangleF(baseRect.Location, sizeF);
               path.AddArc(arc, 180, 180);
               arc.Y = baseRect.Bottom - diameter;
               path.AddArc(arc, 0, 180);
            }
            else path.AddEllipse(baseRect);
         }
         catch { path.AddEllipse(baseRect); }
         finally { path.CloseFigure(); }
         return path;
      }

      /// <summary>
      /// Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius 
      /// for the arcs that make the rounded edges.
      /// </summary>
      /// <param name="brush">System.Drawing.Pen that determines the color, width and style of the rectangle.</param>
      /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to draw.</param>
      /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to draw.</param>
      /// <param name="width">Width of the rectangle to draw.</param>
      /// <param name="height">Height of the rectangle to draw.</param>
      /// <param name="radius">The radius of the arc used for the rounded edges.</param>

      public static void DrawRoundedRectangle(
         Graphics graphics,
         Pen pen,
         float x,
         float y,
         float width,
         float height,
         float xRadius,
         float yRadius)
      {
         RectangleF rectangle = new RectangleF(x, y, width, height);
         if (pen.Width == 1)
         {
            rectangle.Width -= 1;
            rectangle.Height -= 1;
         }

         using (GraphicsPath path = GenerateRoundedRectangle(graphics, rectangle, xRadius, yRadius))
         {
            SmoothingMode old = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawPath(pen, path);
            graphics.SmoothingMode = old;
         }
      }

      /// <summary>
      /// Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius 
      /// for the arcs that make the rounded edges.
      /// </summary>
      /// <param name="brush">System.Drawing.Pen that determines the color, width and style of the rectangle.</param>
      /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to draw.</param>
      /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to draw.</param>
      /// <param name="width">Width of the rectangle to draw.</param>
      /// <param name="height">Height of the rectangle to draw.</param>
      /// <param name="radius">The radius of the arc used for the rounded edges.</param>

      public static void DrawRoundedRectangle(
         Graphics graphics,
         Pen pen,
         int x,
         int y,
         int width,
         int height,
         int xRadius,
         int yRadius)
      {
         DrawRoundedRectangle(graphics,
            pen,
            Convert.ToSingle(x),
            Convert.ToSingle(y),
            Convert.ToSingle(width),
            Convert.ToSingle(height),
            Convert.ToSingle(xRadius),
            Convert.ToSingle(yRadius));
      }

      public static void DrawRoundedRectangle(
        Graphics graphics,
        Pen pen,
        int x,
        int y,
        int width,
        int height,
        int radius)
      {
         DrawRoundedRectangle(graphics, pen, x, y, width, height, radius, radius);
      }

      /// <summary>
      /// Fills the interior of a rounded rectangle specified by a pair of coordinates, a width, a height
      /// and the radius for the arcs that make the rounded edges.
      /// </summary>
      /// <param name="brush">System.Drawing.Brush that determines the characteristics of the fill.</param>
      /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to fill.</param>
      /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to fill.</param>
      /// <param name="width">Width of the rectangle to fill.</param>
      /// <param name="height">Height of the rectangle to fill.</param>
      /// <param name="radius">The radius of the arc used for the rounded edges.</param>

      public static void FillRoundedRectangle(
         Graphics graphics,
         Brush brush,
         float x,
         float y,
         float width,
         float height,
         float xRadius, float yRadius)
      {
         RectangleF rectangle = new RectangleF(x, y, width - 1, height - 1);
         using (GraphicsPath path = GenerateRoundedRectangle(graphics, rectangle, xRadius, yRadius))
         {
            SmoothingMode old = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillPath(brush, path);
            graphics.SmoothingMode = old;
         }
      }

      /// <summary>
      /// Fills the interior of a rounded rectangle specified by a pair of coordinates, a width, a height
      /// and the radius for the arcs that make the rounded edges.
      /// </summary>
      /// <param name="brush">System.Drawing.Brush that determines the characteristics of the fill.</param>
      /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to fill.</param>
      /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to fill.</param>
      /// <param name="width">Width of the rectangle to fill.</param>
      /// <param name="height">Height of the rectangle to fill.</param>
      /// <param name="radius">The radius of the arc used for the rounded edges.</param>

      public static void FillRoundedRectangle(
         Graphics graphics,
         Brush brush,
         int x,
         int y,
         int width,
         int height,
         int radius)
      {
         FillRoundedRectangle(graphics, brush, x, y, width, height, radius, radius);
      }

      public static void FillRoundedRectangle(
         Graphics graphics,
         Brush brush,
         int x,
         int y,
         int width,
         int height,
         int xRadius, 
         int yRadius)
      {
         FillRoundedRectangle(graphics,
            brush,
            Convert.ToSingle(x),
            Convert.ToSingle(y),
            Convert.ToSingle(width),
            Convert.ToSingle(height),
            Convert.ToSingle(xRadius),
            Convert.ToSingle(yRadius));
      }

   }
#endif
}
