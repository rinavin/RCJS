using com.magicsoftware.util;
using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.win32;
using com.magicsoftware.controls.utils;

namespace com.magicsoftware.controls
{
   public class PaintHelper
   {
      /// <summary>
      /// Creates pen for border painting
      /// </summary>
      /// <returns></returns>
      public static Pen InitPenForPaint(Color foreColor, ControlStyle controlStyle, CtrlLineType lineType, int lineWidth)
      {
         Pen pen = new Pen(foreColor);

         // set properties of pen
         pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset; // Paint from inside when pen width is greater than 1
         if (controlStyle == ControlStyle.TwoD)
         {
            pen.Width = lineWidth;
            float[] dashPattern = ControlRenderer.GetDashPattern(lineType);
            if (dashPattern != null)
               pen.DashPattern = dashPattern;
         }

         return pen;
      }

      /// <summary>
      /// Get font text metrics points.
      /// </summary>
      /// <param name="hFont"></param>
      /// <param name="str"></param>
      /// <param name="control"></param>
      /// <returns></returns>
      public static Size GetTextExt(IntPtr hFont, string str, Control control)
      {
         Size size = new Size();
         using (Graphics g = control.CreateGraphics())
         {
            IntPtr hdc = g.GetHdc();
            IntPtr hFontOld = (IntPtr)NativeWindowCommon.SelectObject(hdc, hFont);
            NativeWindowCommon.SIZE nativeSize = new NativeWindowCommon.SIZE();
            NativeWindowCommon.GetTextExtentPoint32(hdc, str, str.Length, out nativeSize);
            size.Width = nativeSize.cx;
            size.Height = nativeSize.cy;
            NativeWindowCommon.SelectObject(hdc, hFontOld);
            g.ReleaseHdc(hdc);
         }
         return size;
      }

      /// <summary>
      /// Convert back logFont.lfHeight and convert to TWIPS (20ths of a point). 
      /// </summary>
      /// <param name="logFont"></param>
      /// <returns></returns>
      public static int LogFontHeightToFontSize(NativeWindowCommon.LOGFONT logFont, Control control)
      {
         Point resolution = Utils.GetResolution(control);
         return -NativeWindowCommon.MulDiv(logFont.lfHeight, 72 * 20, (int)resolution.Y);
      }
   }
}
