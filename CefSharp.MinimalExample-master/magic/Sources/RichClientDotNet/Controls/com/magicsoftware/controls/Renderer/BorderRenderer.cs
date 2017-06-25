using System.Drawing;
using com.magicsoftware.util;
using System.Drawing.Drawing2D;
#if !PocketPC
using VisualStyles = System.Windows.Forms.VisualStyles;
using System.Windows.Forms;
#else
using SystemPens = com.magicsoftware.richclient.mobile.gui.SystemPens;
#endif

namespace com.magicsoftware.controls
{
   public class BorderRenderer
   {
#if !PocketPC
      private static VisualStyles.VisualStyleRenderer renderer_NEVER_USE_THIS_DIRECTLY = null;
      //render is used for themed border painting
      private static VisualStyles.VisualStyleRenderer Renderer
      {
         get
         {
            if (Application.RenderWithVisualStyles && VisualStyles.VisualStyleRenderer.IsElementDefined(_element))
            {
               if (renderer_NEVER_USE_THIS_DIRECTLY == null)
                  renderer_NEVER_USE_THIS_DIRECTLY = new VisualStyles.VisualStyleRenderer(_element);
            }
            else
               renderer_NEVER_USE_THIS_DIRECTLY = null;
            return renderer_NEVER_USE_THIS_DIRECTLY;
         }
      }
      private static readonly VisualStyles.VisualStyleElement _element = VisualStyles.VisualStyleElement.TextBox.TextEdit.Normal;

#endif

      /// <summary>
      /// 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="fgColor"></param>
      /// <param name="style"></param>
      /// <param name="textBoxBorder"></param>
      /// <param name="borderType"></param>
      public static void PaintBorder(Graphics g, Rectangle rect, Color fgColor, ControlStyle style, bool textBoxBorder, BorderType borderType = BorderType.Thin)
      {
         Pen pen = null;
         bool shouldPaintBorder = !(style == ControlStyle.TwoD && borderType == BorderType.NoBorder);

         if (shouldPaintBorder)
         {
            if (style == ControlStyle.TwoD)
               pen = PensCache.GetInstance().Get(fgColor);

            if (style == ControlStyle.TwoD && borderType == BorderType.Thick)
            {
               // back up the original properties of pen
               PenAlignment originalAlligment = pen.Alignment;
               float originalwidth = pen.Width;

               // set properties of pen
               pen.Width = 2;
               pen.Alignment = PenAlignment.Inset;

               PaintBorder(g, pen, rect, style, textBoxBorder);

               // restore the original properties of pen
               pen.Alignment = originalAlligment;
               pen.Width = originalwidth;
            }
            else
               PaintBorder(g, pen, rect, style, textBoxBorder);
         }
      }

      public static void PaintBorder(Graphics g, Pen pen, Rectangle rect, ControlStyle style, bool textBoxBorder)
      {
         switch (style)
         {
            case ControlStyle.TwoD:
               if (pen.Width == 1)  // When width is 1, we should reduce the height and width by 1 since we can't paint on border
               {
                  rect.Width -= 1;
                  rect.Height -= 1; 
               }
               g.DrawRectangle(pen, rect);
               break;
            case ControlStyle.ThreeD:
            case ControlStyle.ThreeDSunken:
               DrawSunkenRaisedBorder(g, rect, style == ControlStyle.ThreeDSunken);
               break;
            case ControlStyle.Windows:
               if (textBoxBorder)
                  rect = DrawTextBoxBorder(g, rect);
               else
                  DrawWindowsOrEmbosedBorder(g, rect, style == ControlStyle.Windows);
               break;
            case ControlStyle.Emboss:
               DrawWindowsOrEmbosedBorder(g, rect, style == ControlStyle.Windows);
               break;
            case ControlStyle.NoBorder:
               break;
            default:
               break;
         }
      }

      /// <summary>
      ///  from ctrl_pnt.cpp draw_3d_rect(...)
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      private static void DrawSunkenRaisedBorder(Graphics g, Rectangle rect, bool sunken)
      {
         Point pt1, pt2;

         pt1 = rect.Location;
         pt2 = new Point(rect.Left, rect.Bottom);

         //draw bottom horizontal line
         pt1 = new Point(rect.Right - 1, rect.Bottom - 1);
         pt2 = new Point(rect.Left, rect.Bottom - 1);
         g.DrawLine((sunken ? SystemPens.ButtonHighlight : SystemPens.ButtonShadow), pt1.X, pt1.Y, pt2.X, pt2.Y);

         //draw  right vertical line
         pt1 = new Point(rect.Right - 1, rect.Bottom - 1);
         pt2 = new Point(rect.Right - 1, rect.Top);
         g.DrawLine((sunken ? SystemPens.ButtonHighlight : SystemPens.ButtonShadow), pt1.X, pt1.Y, pt2.X, pt2.Y);

         //draw top horizontal line
         pt1 = new Point(rect.Left, rect.Top);
         pt2 = new Point(rect.Right - 1, rect.Top);
         g.DrawLine((sunken ? SystemPens.ButtonShadow : SystemPens.ButtonHighlight), pt1.X, pt1.Y, pt2.X, pt2.Y);

         // draw left vertical line
         pt1 = new Point(rect.Left, rect.Top);
         pt2 = new Point(rect.Left, rect.Bottom - 1);
         g.DrawLine((sunken ? SystemPens.ButtonShadow : SystemPens.ButtonHighlight), pt1.X, pt1.Y, pt2.X, pt2.Y);
      }

      /// <summary>
      /// drawWindowsOrEmbosedBorder
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      private static void DrawWindowsOrEmbosedBorder(Graphics g, Rectangle rect, bool windows)
      {
         // from ctrl_pnt.cpp static_3d_paint(...)
         Pen pen1, pen2;
         Point pt1, pt2;
         Rectangle borderRect = rect;

         if (windows)
         {
            pen1 = SystemPens.ButtonShadow;
            pen2 = SystemPens.ButtonHighlight;
         }
         else
         {
            pen1 = SystemPens.ButtonHighlight;
            pen2 = SystemPens.ButtonShadow;
         }
         borderRect.Width -= 2;
         borderRect.Height -= 2;
         g.DrawRectangle(pen1, borderRect);

         //top
         pt1 = borderRect.Location;
         pt1.Offset(1, 1);
         pt2 = new Point(borderRect.Right - 1, pt1.Y);
         g.DrawLine(pen2, pt1.X, pt1.Y, pt2.X, pt2.Y);

         //left
         pt2 = new Point(pt1.X, borderRect.Bottom - 1);
         g.DrawLine(pen2, pt1.X, pt1.Y, pt2.X, pt2.Y);

         //bottom
         pt2.Y += 2;
         pt1 = new Point(borderRect.Right + 1, pt2.Y);
         g.DrawLine(pen2, pt1.X, pt1.Y, pt2.X, pt2.Y);

         //right
         pt1 = new Point(borderRect.Right + 1, borderRect.Y + 1);
         pt2 = new Point(pt1.X, borderRect.Bottom);
         g.DrawLine(pen2, pt1.X, pt1.Y, pt2.X, pt2.Y);
      }

      /// <summary> draw native textbox border</summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <returns></returns>
      private static Rectangle DrawTextBoxBorder(Graphics g, Rectangle rect)
      {
#if !PocketPC
         if (Renderer != null)
         {
            Region orgRegion = g.Clip;
            using (Region r = new Region(rect))
            {
               Rectangle orgRect = rect;
               rect.Inflate(-SystemInformation.BorderSize.Width, -SystemInformation.BorderSize.Height);
               r.Exclude(rect);
               g.Clip = r;
               Renderer.DrawBackground(g, orgRect);
            }
            g.Clip = orgRegion;
         }
         else
         {
            ControlPaint.DrawBorder3D(g, rect, Border3DStyle.Sunken);
            rect.Inflate(-SystemInformation.Border3DSize.Width, -SystemInformation.Border3DSize.Height);
         }
#else
         // VisualStyle elements and renderers do not exist in compact framework, so just use 
         // the basic border drawing.         
         DrawWindowsOrEmbosedBorder(g, rect, true);
         rect.Inflate(-1, -1);
#endif
         return rect;
      }

      /// <summary>
      /// Draw Border for Ellipse
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="pen"></param>
      /// <param name="rectangle"></param>
      /// <param name="controlStyle"></param>
      public static void DrawEllipseBorder(Graphics graphics, Pen pen, Rectangle rectangle, ControlStyle controlStyle)
      {
         // When width is 1, we should reduce the height and width by 1 since we can't paint on border
         if (pen.Width == 1)
         {
            rectangle.Width -= 1;
            rectangle.Height -= 1;
         }

         switch (controlStyle)
         {
            case ControlStyle.TwoD:
               graphics.DrawEllipse(pen, rectangle);
               break;

            case ControlStyle.ThreeD:
               pen.Color = SystemColors.ButtonShadow;
               graphics.DrawArc(pen, rectangle, 315, 180);
               pen.Color = SystemColors.ButtonHighlight;
               graphics.DrawArc(pen, rectangle, 135, 180);
               break;

            case ControlStyle.ThreeDSunken:
               pen.Color = SystemColors.ButtonHighlight;
               graphics.DrawArc(pen, rectangle, 315, 180);
               pen.Color = SystemColors.ButtonShadow;
               graphics.DrawArc(pen, rectangle, 135, 180);
               break;
         }
      }
   }
}
