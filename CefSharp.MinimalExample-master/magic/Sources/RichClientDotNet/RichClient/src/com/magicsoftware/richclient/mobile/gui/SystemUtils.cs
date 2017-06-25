using System.Drawing;
using RECT = com.magicsoftware.win32.NativeWindowCommon.RECT;
using System;
using com.magicsoftware.win32;

namespace com.magicsoftware.richclient.mobile.gui
{
   /// <summary> Implement required SystemPens, not supported on .Net Compact Framework
   /// </summary>
   class SystemPens
   {
      static SystemPens()
      {
         ButtonShadow = new Pen(SystemColors.ControlDark);
         ButtonHighlight = new Pen(SystemColors.ControlLightLight);
      }
      internal static Pen ButtonShadow { get; set; }
      internal static Pen ButtonHighlight { get; set; }
   }


   /// <summary> Implement functions from ControlPaint, not supported on .Net Compact Framework
   /// </summary>
   internal class ControlPaint
   {
      internal enum Border3DStyle
      {
         RaisedOuter = 1,
         SunkenOuter = 2,
         RaisedInner = 4,
         Raised = 5,
         Etched = 6,
         SunkenInner = 8,
         Bump = 9,
         Sunken = 10,
         Adjust = 8192,
         Flat = 16394,
      }

      /// <summary> implement ControlPaint.DrawBorder3D.
      /// This is a basic implementation, using DrawEdge. For now, it is good enough for 
      /// the sunken style we need.
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rectangle"></param>
      /// <param name="style"></param>
      internal static void DrawBorder3D(Graphics graphics, Rectangle rectangle, Border3DStyle style)
      {
         RECT rc = new RECT();
         rc.left = rectangle.Left;
         rc.top = rectangle.Top;
         rc.right = rectangle.Right;
         rc.bottom = rectangle.Bottom;
         IntPtr hdc = graphics.GetHdc();
         // Border3DStyle flags are the same values as DrawEdge flags
         NativeWindowCommon.DrawEdge(hdc, ref rc, (uint)style, NativeWindowCommon.BF_RECT);
         graphics.ReleaseHdc(hdc);
      }

      /// <summary> implement ControlPaint.DrawFocusRectangle
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rectangle"></param>
      internal static void DrawFocusRectangle(Graphics graphics, Rectangle rectangle)
      {
         IntPtr hdc = graphics.GetHdc();
         RECT rc = new RECT();
         rc.left = rectangle.Left;
         rc.top = rectangle.Top;
         rc.right = rectangle.Right;
         rc.bottom = rectangle.Bottom;
         NativeWindowCommon.DrawFocusRect(hdc, ref rc);
         graphics.ReleaseHdc(hdc);
      }
   }
}