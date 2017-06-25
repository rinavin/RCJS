using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using com.magicsoftware.win32;
using Controls.com.magicsoftware.controls.MgShape;
using Controls.com.magicsoftware.support;

#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using ColorTranslator = OpenNETCF.Drawing.ColorTranslator;
using SystemPens = com.magicsoftware.richclient.mobile.gui.SystemPens;
using ButtonBase = OpenNETCF.Windows.Forms.ButtonBase2;
#endif

namespace com.magicsoftware.controls
{
   /// <summary> This class provides methods to draw the background (color and image) and text of controls. </summary>
   public class ControlRenderer
   {
      public static float[] DASH_PATTERN = { 18, 5 };
      public static float[] DOT_PATTERN = { 3, 3 };
      public static float[] DASHDOT_PATTERN = { 8, 6, 3, 6 };
      public static float[] DASHDOTDOT_PATTERN = { 8, 3, 3, 3, 3, 3 };

      //TODO: Kaushal. When the rendering code will be moved from MgUtils to MgControls,
      //the scope of many methods in this class should be changed to private.

      /// <summary> 
      /// paint background of control
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      /// <param name="style"></param>
      /// <param name="textBoxBorder"></param>
      /// <param name="gradientColor"></param>
      /// <param name="gradientStyle"></param>
      /// <param name="borderType"></param>
      /// <returns></returns>
      public static Rectangle FillRectAccordingToGradientStyle(Graphics g, Rectangle rect, Color bgColor, Color fgColor,
                                                               ControlStyle style, bool textBoxBorder, GradientColor gradientColor,
                                                               GradientStyle gradientStyle, BorderType borderType = BorderType.Thin)
      {
         if (gradientStyle == GradientStyle.None)
            PaintBackGround(g, rect, bgColor, true);
         else
            PaintBackGroundGradient(g, rect, bgColor, fgColor, style, textBoxBorder,
                                    gradientColor, gradientStyle, false, 0);

         BorderRenderer.PaintBorder(g, rect, fgColor, style, textBoxBorder, borderType);

         return rect;
      }

#if !PocketPC
      /// <summary>
      /// Draw border for the specified rectangle.
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      /// <param name="borderColor"></param>
      /// <param name="borderWidth"></param>
      /// <param name="cornerRadius"></param>
      /// <returns></returns>
      public static Rectangle DrawRoundedRectangle(Graphics graphics, Rectangle rect, Color borderColor, ulong borderWidth, ulong cornerRadius)
      {
         if (rect.Width > 0 && rect.Height > 0)
         {
            using (Pen borderPen = new Pen(borderColor, borderWidth))
            {
               borderPen.Alignment = PenAlignment.Outset;

               // This paints border having greater thickness from outside rectangle of control passed. Hence adjust rectangle so that it paints inside actual control dimensions.
               GraphicsExtension.DrawRoundedRectangle(graphics, borderPen, rect.Left, rect.Top, rect.Width, rect.Height, (int)cornerRadius);
            }
         }

         return rect;
      }
#endif


      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="graphics"></param>
      public static void PaintMgPanel(Control control, Graphics graphics)
      {
         Rectangle RectCtrl = control.ClientRectangle;
#if !PocketPC
         RectCtrl = new Rectangle(new Point(0, 0), control.DisplayRectangle.Size);
#endif
         if (control is ScrollableControl)
            RectCtrl.Offset(((ScrollableControl)control).AutoScrollPosition.X, ((ScrollableControl)control).AutoScrollPosition.Y);

         PaintBackgoundColorAndImage(control, graphics, true, RectCtrl);
      }

      /// <summary> paint background of label control
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      /// <param name="style"></param>
      /// <param name="textBoxBorder"></param>
      public static void PaintBackgroundAndBorder(Graphics g, Rectangle rect, Color bgColor, Color fgColor,
                                                  ControlStyle style, bool textBoxBorder, bool showEnabled)
      {
         PaintBackGround(g, rect, bgColor, showEnabled);

         BorderRenderer.PaintBorder(g, rect, fgColor, style, textBoxBorder);
      }

      /// <summary> paint control backgound and image</summary>
      /// <param name="control"></param>
      /// <param name="graphics"></param>
      /// <param name="useImageSize" - do the image will be display on all the control (for button image)></param>
      /// <param name="DisplayRect" - the display rect of the color></param>
      public static void PaintBackgoundColorAndImage(Control control, Graphics graphics, bool useImageSize, Rectangle DisplayRect)
      {
         Image backgroundImage = null;
         GradientColor gradientColor = ControlUtils.GetGradientColor(control);
         GradientStyle gradientStyle = ControlUtils.GetGradientStyle(control);

         ControlRenderer.FillRectAccordingToGradientStyle(graphics, DisplayRect, control.BackColor,
                                                          control.ForeColor, ControlStyle.NoBorder,
                                                          false, gradientColor, gradientStyle);

#if !PocketPC
         backgroundImage = control.BackgroundImage;
#else
         if (control is ButtonBase)
         {
            ButtonBase button = (ButtonBase)control;
            backgroundImage = button.BackgroundImage;
         }
         else if (control is MgPanel)
         {
            MgPanel panel = (MgPanel)control;
            backgroundImage = panel.BackGroundImage;
         }
#endif

         if (backgroundImage != null)
         {
            Rectangle rectImage;
            if (useImageSize)
               rectImage = new Rectangle(DisplayRect.X, DisplayRect.Y, backgroundImage.Width, backgroundImage.Height);
            else
               rectImage = control.ClientRectangle;

            DrawImage(graphics, rectImage, backgroundImage, 0, 0);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="bgColor"></param>
      public static void PaintBackGround(Graphics g, Rectangle rect, Color bgColor, bool showEnabled)
      {
#if !PocketPC
         Brush brush = (showEnabled ? SolidBrushCache.GetInstance().Get(Utils.GetNearestColor(g, bgColor)) : SystemBrushes.ButtonFace);
#else
         Brush brush = (showEnabled ? SolidBrushCache.GetInstance().Get(bgColor) : SolidBrushCache.GetInstance().Get(SystemColors.Control));
#endif
         g.FillRectangle(brush, rect);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="bgColor"></param>
      public static void PaintRoundedRectangleBackGround(Graphics g, Rectangle rect, Color bgColor, ulong cornerRadius)
      {
#if !PocketPC
         if (rect.Width > 0 && rect.Height > 0)
         {
            Brush brush = SolidBrushCache.GetInstance().Get(Utils.GetNearestColor(g, bgColor));
            GraphicsExtension.FillRoundedRectangle(g, brush, rect.Left, rect.Top, rect.Width, rect.Height, (int)cornerRadius);
         }
#endif         
      }

      /// <summary>
      /// static method for printing text
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="color"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="MultiLine"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="enabled"></param>
      /// <param name="wordWrap"></param>
      /// <param name="AddNoPrefixFlag"></param>
      /// <param name="AddNoClipping"></param>
      /// <param name="rightToLeft"></param>
      public static void PrintText(Graphics g, Rectangle rect, Color color, FontDescription fontDescription, String text, bool multiLine,
                                   ContentAlignment contentAlignment, bool enabled, bool wordWrap, bool addNoPrefixFlag, bool addNoClipping,
                                   bool rightToLeft)
      {
         PrintText(g, rect, color, fontDescription, text, multiLine, contentAlignment, enabled, wordWrap, addNoPrefixFlag, addNoClipping, rightToLeft, false, 0, false);
      }

      /// <summary>
      /// static method for printing text
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="color"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="multiLine"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="enabled"></param>
      /// <param name="wordWrap"></param>
      /// <param name="addNoPrefixFlag"></param>
      /// <param name="addNoClipping"></param>
      /// <param name="rightToLeft"></param>
      /// <param name="orientation"></param>
      public static void PrintText(Graphics g, Rectangle rect, Color color, FontDescription fontDescription, String text, bool multiLine,
                             ContentAlignment contentAlignment, bool enabled, bool wordWrap, bool addNoPrefixFlag, bool addNoClipping,
                             bool rightToLeft, int orientation)
      {
         PrintText(g, rect, color, fontDescription, text, multiLine, contentAlignment, enabled, wordWrap, addNoPrefixFlag, addNoClipping, rightToLeft, false, orientation, false);
      }
 
      /// <summary>
      /// 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="color"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="multiLine"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="enabled"></param>
      /// <param name="wordWrap"></param>
      /// <param name="addNoPrefixFlag"></param>
      /// <param name="addNoClipping"></param>
      /// <param name="rightToLeft"></param>
      /// <param name="calcVerticalAligmentInMultiLine"></param>
      public static void PrintText(Graphics g, Rectangle rect, Color color, FontDescription fontDescription, String text, bool multiLine,
                       ContentAlignment contentAlignment, bool enabled, bool wordWrap, bool addNoPrefixFlag, bool addNoClipping,
                       bool rightToLeft, bool calcVerticalAligmentInMultiLine)
      {
         PrintText(g, rect, color, fontDescription, text, multiLine, contentAlignment, enabled, wordWrap, addNoPrefixFlag, addNoClipping, rightToLeft, calcVerticalAligmentInMultiLine, 0, false);
      }

      /// <summary>
      /// static method for printing text
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="color"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="MultiLine"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="enabled"></param>
      /// <param name="wordWrap"></param>
      /// <param name="AddNoPrefixFlag"></param>
      /// <param name="AddNoClipping"></param>
      /// <param name="rightToLeft"></param>
      public static void PrintText(Graphics g, Rectangle rect, Color color, FontDescription fontDescription, String text, bool multiLine,
                                   ContentAlignment contentAlignment, bool enabled, bool wordWrap, bool addNoPrefixFlag, bool addNoClipping,
                                   bool rightToLeft, bool calcVerticalAligmentInMultiLine, int orientation, bool clipSupport)
      {
         if (!String.IsNullOrEmpty(text))
         {
            int topOffset, flags;
            GetTextInformation(g, ref rect, ref color, fontDescription, text, multiLine, contentAlignment, enabled, wordWrap, addNoPrefixFlag, addNoClipping,
                        rightToLeft, calcVerticalAligmentInMultiLine, out topOffset, out flags);

#if !PocketPC
            if (clipSupport)
            {
               ClipSupportingDrawText(g, color, rect, topOffset, orientation, fontDescription, text, contentAlignment, flags, rightToLeft);
            }
            else
#endif
            {
               IntPtr hdc = g.GetHdc();
               DrawText(hdc, color, rect, topOffset, orientation, fontDescription, text, contentAlignment, flags, rightToLeft);
               g.ReleaseHdc(hdc);
            }
         }
      }

      /// <summary>
      /// Gets the text information.
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="color"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="multiLine"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="enabled"></param>
      /// <param name="wordWrap"></param>
      /// <param name="addNoPrefixFlag"></param>
      /// <param name="addNoClipping"></param>
      /// <param name="rightToLeft"></param>
      /// <param name="calcVerticalAligmentInMultiLine"></param>
      /// <param name="topOffset"></param>
      /// <param name="flags"></param>
      public static void GetTextInformation(Graphics g, ref Rectangle rect, ref Color color, FontDescription fontDescription, String text, bool multiLine, 
         ContentAlignment contentAlignment, bool enabled, bool wordWrap, bool addNoPrefixFlag, bool addNoClipping, bool rightToLeft,
         bool calcVerticalAligmentInMultiLine, out int topOffset, out int flags)
      {
         flags = CalcFlags(g, ref rect, ref color, multiLine, contentAlignment, enabled, wordWrap, addNoPrefixFlag, addNoClipping, rightToLeft);
         topOffset = CalcTopOffset(g, ref rect, fontDescription, text, multiLine, contentAlignment, calcVerticalAligmentInMultiLine, flags);
      }

      /// <summary>
      /// Calculate flags for text information
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="color"></param>
      /// <param name="multiLine"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="enabled"></param>
      /// <param name="wordWrap"></param>
      /// <param name="addNoPrefixFlag"></param>
      /// <param name="addNoClipping"></param>
      /// <param name="rightToLeft"></param>
      /// <returns></returns>
      private static int CalcFlags(Graphics g, ref Rectangle rect, ref Color color, bool multiLine, ContentAlignment contentAlignment, bool enabled, bool wordWrap, bool addNoPrefixFlag, bool addNoClipping, bool rightToLeft)
      {
         int flags = 0;
         flags = (int)Utils.GetTextFlags(contentAlignment, wordWrap, multiLine, addNoPrefixFlag, addNoClipping, rightToLeft);

#if !PocketPC
         color = Utils.GetNearestColor(g, (enabled ? color : SystemColors.GrayText));
         rect.Offset(new Point((int)g.Transform.OffsetX, (int)g.Transform.OffsetY));
#else
            color = enabled ? color : SystemColors.GrayText;
#endif
         return flags;
      }

      /// <summary>
      /// Calculate top offset for text information
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="multiLine"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="calcVerticalAligmentInMultiLine"></param>
      /// <param name="flags"></param>
      /// <returns></returns>
      private static int CalcTopOffset(Graphics g, ref Rectangle rect, FontDescription fontDescription, string text, bool multiLine, ContentAlignment contentAlignment, bool calcVerticalAligmentInMultiLine, int flags)
      {
         int topOffset = 0;
         if (calcVerticalAligmentInMultiLine && multiLine &&
                         (contentAlignment == ContentAlignment.BottomCenter || contentAlignment == ContentAlignment.BottomLeft || contentAlignment == ContentAlignment.BottomRight ||
                          contentAlignment == ContentAlignment.MiddleCenter || contentAlignment == ContentAlignment.MiddleLeft || contentAlignment == ContentAlignment.MiddleRight))
         {
            NativeWindowCommon.RECT retCalcTextRect;
            Utils.CalcTextRect(g, rect, fontDescription, text, flags, out retCalcTextRect);

            if (contentAlignment == ContentAlignment.BottomCenter || contentAlignment == ContentAlignment.BottomLeft || contentAlignment == ContentAlignment.BottomRight)
            {
               topOffset = rect.Bottom - (retCalcTextRect.bottom - retCalcTextRect.top);
            }
            else
            {
               topOffset = rect.Height - (((rect.Bottom - rect.Top) / 2) + ((retCalcTextRect.bottom - retCalcTextRect.top) / 2));
            }
         }

         return topOffset;
      }

#if !PocketPC
      /// <summary>
      /// Draw the text supporting clipping according to ClipRegion.
      /// </summary>
      /// <param name="g"></param>
      /// <param name="color"></param>
      /// <param name="rect"></param>
      /// <param name="topOffset"></param>
      /// <param name="orientation"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="flags"></param>
      public static void ClipSupportingDrawText(Graphics g, Color color, Rectangle rect, int topOffset, int orientation, FontDescription fontDescription, String text,
                                                ContentAlignment contentAlignment, int flags, bool rightToLeft)
      {
         IntPtr region = IntPtr.Zero;
         IntPtr currentclippedRegion = g.Clip.GetHrgn(g);
         bool IsRegionInfinite = g.Clip.IsInfinite(g);
         IntPtr hdc = g.GetHdc();

         NativeWindowCommon.SetBkMode(hdc, NativeWindowCommon.TRANSPARENT);
         NativeWindowCommon.SetTextColor(hdc, ColorTranslator.ToWin32(color));

         // Text rectangle as clip Region
         IntPtr clipRegion = NativeWindowCommon.CreateRectRgn(rect.Left, rect.Top, rect.Right, rect.Bottom);

         // If Text starts above control, it needs to clip
         if (topOffset < 0)
         {
            // if current graphics is already clipped, then we need to merge with current text rectangle clip
            if (!IsRegionInfinite)
               NativeWindowCommon.CombineRgn(currentclippedRegion, currentclippedRegion, clipRegion, NativeWindowCommon.RGN_AND);
         }

         if (IsRegionInfinite)
         {
            // If there is no clip region on graphics, set text rectangle as clip region
            region = clipRegion;
         }
         else if (currentclippedRegion != IntPtr.Zero)
         {
            // use intersected region as clip region
            region = currentclippedRegion;
         }

         // Select resultant clipped region on DC
         if (region != IntPtr.Zero)
            NativeWindowCommon.SelectClipRgn(hdc, region);


         if (clipRegion != IntPtr.Zero)
         {
            NativeWindowCommon.DeleteObject(clipRegion);
         }

         // Draw text
         DrawText(hdc, color, rect, topOffset, orientation, fontDescription, text, contentAlignment, flags, rightToLeft);

         // release objects
         g.ReleaseHdc(hdc);
         if (currentclippedRegion != IntPtr.Zero)
            g.Clip.ReleaseHrgn(currentclippedRegion);
      }
#endif

      /// <summary>
      /// Draw the text.
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="color"></param>
      /// <param name="rect"></param>
      /// <param name="topOffset"></param>
      /// <param name="orientation"></param>
      /// <param name="fontDescription"></param>
      /// <param name="text"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="flags"></param>
      public static void DrawText(IntPtr hdc, Color color, Rectangle rect, int topOffset, int orientation, FontDescription fontDescription, String text, 
                                  ContentAlignment contentAlignment, int flags, bool rightToLeft)
      {
         NativeWindowCommon.SetBkMode(hdc, NativeWindowCommon.TRANSPARENT);
         NativeWindowCommon.SetTextColor(hdc, ColorTranslator.ToWin32(color));

#if !PocketPC
         // QCR #439182 & 430913: the font is used for a control before even being initialized.
         // TODO: We need a better fix that avoids calling get_Font() for logical controls that are not initialized properly.
         if (fontDescription == null)
            fontDescription = new FontDescription(Control.DefaultFont);
#endif

         NativeWindowCommon.RECT rc = new NativeWindowCommon.RECT();
         rc.left = rect.Left;
         rc.right = rect.Right;
         rc.top = rect.Top + topOffset;
         rc.bottom = rect.Bottom;

#if !PocketPC
         if (orientation != 0)
         {
            PrintRotatedText(hdc, fontDescription, orientation, text, rect, contentAlignment, rc, rightToLeft);
         }
         else
#endif
         {
            IntPtr hFont = fontDescription.FontHandle;
            NativeWindowCommon.SelectObject(hdc, hFont);
            NativeWindowCommon.DrawText(hdc, text, text.Length, ref rc, flags);
         }
      }

#if !PocketPC
      /// <summary>
      /// 
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="fontDescription"></param>
      /// <param name="orientation"></param>
      /// <param name="text"></param>
      /// <param name="rectangle"></param>
      /// <param name="contentAlignment"></param>
      /// <param name="rect"></param>
      internal static void PrintRotatedText(IntPtr hdc, FontDescription fontDescription, int orientation, string text, Rectangle rectangle, ContentAlignment contentAlignment,
                                            NativeWindowCommon.RECT rect, bool rightToLeft)
      {
         IntPtr hFont;
         Point point;

         // get the original font and its LOGFONT
         NativeWindowCommon.LOGFONT logfont = fontDescription.LogFont;
         // Set the rotation angle
         logfont.lfEscapement = logfont.lfOrientation = orientation;

         // create the new, rotated font
         hFont = NativeWindowCommon.CreateFontIndirect(logfont);
         NativeWindowCommon.SelectObject(hdc, hFont);

         point = CalculateStartCoordinates(hdc, rectangle, orientation, text, contentAlignment);

         uint fuOptions = NativeWindowCommon.ETO_CLIPPED;
         if (rightToLeft)
            fuOptions = NativeWindowCommon.ETO_RTLREADING;

         NativeWindowCommon.ExtTextOut(hdc, point.X, point.Y, fuOptions, ref rect, text, (uint)text.Length, null);

         NativeWindowCommon.DeleteObject(hFont);
      }

      /// <summary>
      /// code from gui_dc_calc_start_point
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="rectangle"></param>
      /// <param name="orientation"></param>
      /// <param name="str"></param>
      /// <param name="contentAlignment"></param>
      /// <returns></returns>
      public static Point CalculateStartCoordinates(IntPtr hdc, Rectangle rectangle, long orientation,
                                     string str, ContentAlignment contentAlignment)
      {
         int txt;
         Point Start = new Point();

         HorizontalAlignment horizontalAlignment = Utils.ContentAlignment2HorizontalAlignment(contentAlignment);
         VerticalAlignment verticalAlignment = Utils.ContentAlignment2VerticalAlignment(contentAlignment);

         NativeWindowCommon.SIZE size = new NativeWindowCommon.SIZE();
         NativeWindowCommon.GetTextExtentPoint32(hdc, str, str.Length, out size);

         switch (orientation)
         {
            case 450:
               txt = (int)Math.Sqrt((double)size.cx * size.cx / 2);
               if (verticalAlignment == VerticalAlignment.Bottom)
                  Start.Y = rectangle.Bottom - size.cy / 2;
               else if (verticalAlignment == VerticalAlignment.Center)
                  Start.Y = rectangle.Top + ((rectangle.Bottom - rectangle.Top - size.cy / 2 + txt) / 2);
               else
                  Start.Y = rectangle.Top + txt;

               if (horizontalAlignment == HorizontalAlignment.Right)
                  Start.X = rectangle.Right - txt - size.cy / 2;
               else if (horizontalAlignment == HorizontalAlignment.Center)
                  Start.X = rectangle.Left + ((rectangle.Right - rectangle.Left - size.cy / 2 - txt) / 2);
               else
                  Start.X = rectangle.Left;
               break;

            case 900:
               if (verticalAlignment == VerticalAlignment.Bottom)
                  Start.Y = rectangle.Bottom;
               else if (verticalAlignment == VerticalAlignment.Center)
                  Start.Y = rectangle.Top + ((rectangle.Bottom - rectangle.Top + size.cx) / 2);
               else
                  Start.Y = rectangle.Top + size.cx;
               if (horizontalAlignment == HorizontalAlignment.Right)
                  Start.X = rectangle.Right - size.cy;
               else if (horizontalAlignment == HorizontalAlignment.Center)
                  Start.X = rectangle.Left + ((rectangle.Right - rectangle.Left - size.cy) / 2);
               else
                  Start.X = rectangle.Left;
               break;

            case 1350:
               txt = (int)Math.Sqrt((double)size.cx * size.cx / 2);
               if (verticalAlignment == VerticalAlignment.Bottom)
                  Start.Y = rectangle.Bottom;
               else if (verticalAlignment == VerticalAlignment.Center)
                  Start.Y = rectangle.Top + ((rectangle.Bottom - rectangle.Top + size.cy / 2 + txt) / 2);
               else
                  Start.Y = rectangle.Top + txt + size.cy / 2;
               if (horizontalAlignment == HorizontalAlignment.Right)
                  Start.X = rectangle.Right - size.cy / 2;
               else if (horizontalAlignment == HorizontalAlignment.Center)
                  Start.X = rectangle.Left + ((rectangle.Right - rectangle.Left - size.cy / 2 + txt) / 2);
               else
                  Start.X = rectangle.Left + txt;
               break;

            case 1800:
               if (verticalAlignment == VerticalAlignment.Bottom)
                  Start.Y = rectangle.Bottom;
               else if (verticalAlignment == VerticalAlignment.Center)
                  Start.Y = rectangle.Top + ((rectangle.Bottom - rectangle.Top + size.cy) / 2);
               else
                  Start.Y = rectangle.Top + size.cy;
               if (horizontalAlignment == HorizontalAlignment.Right)
                  Start.X = rectangle.Right;
               else if (horizontalAlignment == HorizontalAlignment.Center)
                  Start.X = rectangle.Left + ((rectangle.Right - rectangle.Left + size.cx) / 2);
               else
                  Start.X = rectangle.Left + size.cx;
               break;

            case 2250:
               txt = (int)Math.Sqrt((double)size.cx * size.cx / 2);
               if (verticalAlignment == VerticalAlignment.Bottom)
                  Start.Y = rectangle.Bottom - txt;
               else if (verticalAlignment == VerticalAlignment.Center)
                  Start.Y = rectangle.Top + ((rectangle.Bottom - rectangle.Top + size.cy / 2 - txt) / 2);
               else
                  Start.Y = rectangle.Top + size.cy / 2;
               if (horizontalAlignment == HorizontalAlignment.Right)
                  Start.X = rectangle.Right;
               else if (horizontalAlignment == HorizontalAlignment.Center)
                  Start.X = rectangle.Left + ((rectangle.Right - rectangle.Left + size.cy / 2 + txt) / 2);
               else
                  Start.X = rectangle.Left + txt + size.cy / 2;
               break;

            case 2700:
               if (verticalAlignment == VerticalAlignment.Bottom)
                  Start.Y = rectangle.Bottom - size.cx;
               else if (verticalAlignment == VerticalAlignment.Center)
                  Start.Y = rectangle.Top + ((rectangle.Bottom - rectangle.Top - size.cx) / 2);
               else
                  Start.Y = rectangle.Top;
               if (horizontalAlignment == HorizontalAlignment.Right)
                  Start.X = rectangle.Right;
               else if (horizontalAlignment == HorizontalAlignment.Center)
                  Start.X = rectangle.Left + ((rectangle.Right - rectangle.Left + size.cy) / 2);
               else
                  Start.X = rectangle.Left + size.cy;
               break;

            case 3150:
               txt = (int)Math.Sqrt((double)size.cx * size.cx / 2);
               if (verticalAlignment == VerticalAlignment.Bottom)
                  Start.Y = rectangle.Bottom - txt - size.cy / 2;
               else if (verticalAlignment == VerticalAlignment.Center)
                  Start.Y = rectangle.Top + ((rectangle.Bottom - rectangle.Top - size.cy / 2 - txt) / 2);
               else
                  Start.Y = rectangle.Top;
               if (horizontalAlignment == HorizontalAlignment.Right)
                  Start.X = rectangle.Right - txt;
               else if (horizontalAlignment == HorizontalAlignment.Center)
                  Start.X = rectangle.Left + ((rectangle.Right - rectangle.Left + size.cy / 2 - txt) / 2);
               else
                  Start.X = rectangle.Left + size.cy / 2;
               break;
            default:
               break;

         }

         return Start;
      }
#endif
#if PocketPC
      /// <summary>
      /// 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="gradientRect"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      /// <param name="style"></param>
      /// <param name="TextBoxBorder"></param>
      /// <param name="GradientFromColor"></param>
      /// <param name="GradientToColor"></param>
      /// <param name="GradientStyle"></param>
      /// <returns></returns>
      internal static void PaintBackGroundGradientMobile(Graphics g, Rectangle gradientRect, Color bgColor, Color fgColor,
                                                         ControlStyle style, bool textBoxBorder, GradientColor gradientColor,
                                                         GradientStyle gradientStyle)
      {
         //QCR #915016, enable has effect in foreground only
         // GradientFromColor = (enabled ? GradientFromColor : getGrayScaleColor(GradientFromColor));
         // GradientToColor = (enabled ? GradientToColor : getGrayScaleColor(GradientToColor));
         if (gradientStyle == GradientStyle.None)
            Debug.Assert(false);

         switch (gradientStyle)
         {
            case GradientStyle.Horizontal:
            case GradientStyle.HorizontalSymmetric:
            case GradientStyle.HorizontalWide:
               GradientFill.Fill(g, gradientRect, gradientColor.From, gradientColor.To,
                                 GradientFill.FillDirection.Vertical);
               break;
            case GradientStyle.Vertical:
            case GradientStyle.VerticalSymmetric:
            case GradientStyle.VerticalWide:
               GradientFill.Fill(g, gradientRect, gradientColor.From, gradientColor.To,
                                 GradientFill.FillDirection.Horizontal);
               break;
            case GradientStyle.CornerBottomRight:
            case GradientStyle.CornerBottomLeft:
            case GradientStyle.CornerTopLeft:
            case GradientStyle.CornerTopRight:
            case GradientStyle.Center:
               PaintBackgroundAndBorder(g, gradientRect, bgColor, fgColor, style, textBoxBorder, true);
               break;

            case GradientStyle.None://1
               //assert(false);
               break;

            default:
               break;
         }
      }

#else
      /// <summary>
      /// get the linear gradient mode according to the gradientStyle 
      /// </summary>
      /// <param name="GradientStyle"></param>
      /// <returns></returns>
      private static LinearGradientMode GetLinearGradientMode(GradientStyle gradientStyle)
      {
         LinearGradientMode retLinearGradientMode = LinearGradientMode.Horizontal;
         switch (gradientStyle)
         {
            case GradientStyle.Horizontal:  //2 
            case GradientStyle.HorizontalSymmetric://3
               retLinearGradientMode = LinearGradientMode.Vertical;
               break;

            case GradientStyle.Vertical://5
            case GradientStyle.VerticalSymmetric://6
               retLinearGradientMode = LinearGradientMode.Horizontal;
               break;

            case GradientStyle.DiagonalLeft://8
            case GradientStyle.DiagonalLeftSymmetric://9
               retLinearGradientMode = LinearGradientMode.ForwardDiagonal;
               break;

            case GradientStyle.DiagonalRight://10
            case GradientStyle.DiagonalRightSymmetric://11
               retLinearGradientMode = LinearGradientMode.BackwardDiagonal;
               break;

            case GradientStyle.CornerBottomRight://15
            case GradientStyle.CornerBottomLeft://14
            case GradientStyle.Center://16
            case GradientStyle.None://1
            case GradientStyle.CornerTopLeft://12
            case GradientStyle.CornerTopRight://13
               //assert(false);
               break;
         }

         return retLinearGradientMode;
      }

      //-----------------------------------------------------------------------------
      // set corner according to the to the sent gradientStyle and rect
      //-----------------------------------------------------------------------------
      private static void SetCorner(PathGradientBrush pthGrBrush, GradientStyle gradientStyle, Rectangle rect)
      {
         int x = rect.X;
         int y = rect.Y;

         switch (gradientStyle)
         {
            case GradientStyle.CornerBottomRight:
               x = rect.Right;
               y = rect.Bottom;
               break;
            case GradientStyle.CornerBottomLeft:
               x = rect.Left;
               y = rect.Bottom;
               break;
            case GradientStyle.CornerTopLeft:
               x = rect.Left;
               y = rect.Top;
               break;
            case GradientStyle.CornerTopRight:
               x = rect.Right;
               y = rect.Top;
               break;
            case GradientStyle.Center:
               x = rect.Left + ((rect.Width) / 2);
               y = rect.Top + ((rect.Height) / 2);
               break;
            default:
               // assert(FALSE);
               break;
         }

         pthGrBrush.CenterPoint = new Point(x, y);
      }

#endif

      /// <summary>
      /// paint the BackGround Gradient
      /// </summary>
      /// <param name="g"></param>
      /// <param name="gradientRect"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      /// <param name="style"></param>
      /// <param name="TextBoxBorder"></param>
      /// <param name="GradientFromColor"></param>
      /// <param name="GradientToColor"></param>
      /// <param name="GradientStyle"></param>
      /// <returns></returns>
      public static void PaintBackGroundGradient(Graphics g, Rectangle gradientRect, Color bgColor, Color fgColor,
                                                        ControlStyle style, bool textBoxBorder, GradientColor gradientColor,
                                                        GradientStyle gradientStyle, bool paintRoundedRectangle, ulong cornerRadius)
      {
         if (gradientRect.Width <= 0 || gradientRect.Height <= 0)
            return;

#if PocketPC
         PaintBackGroundGradientMobile(g, gradientRect, bgColor, fgColor, style,
                                       textBoxBorder, gradientColor, gradientStyle);
#else
         //QCR #915016, enable has effect in foreground only
         //GradientFromColor = (enabled ? GradientFromColor : getGrayScaleColor(GradientFromColor));
         // GradientToColor = (enabled ? GradientToColor : getGrayScaleColor(GradientToColor));
         if (gradientStyle == GradientStyle.None)
            Debug.Assert(false);
         // Create a horizontal linear gradient with four stops.            
         Blend blend = new Blend(3);
         blend.Factors = new float[] { 0.0f, 0.75f, 1.0f };
         blend.Positions = new float[] { 0.0f, 0.5f, 1.0f };
         switch (gradientStyle)
         {
            case GradientStyle.HorizontalSymmetric:
            case GradientStyle.VerticalSymmetric:
            case GradientStyle.DiagonalLeftSymmetric:
            case GradientStyle.DiagonalRightSymmetric:
               blend.Factors[1] = 1.0f;
               blend.Factors[2] = 0.0f;
               break;
         }
         switch (gradientStyle)
         {
            case GradientStyle.HorizontalSymmetric:
            case GradientStyle.VerticalSymmetric:
            case GradientStyle.DiagonalLeftSymmetric:
            case GradientStyle.DiagonalRightSymmetric:
            case GradientStyle.Horizontal:
            case GradientStyle.Vertical:
            case GradientStyle.DiagonalLeft:
            case GradientStyle.DiagonalRight:
               LinearGradientBrush myHorizontalGradient1 = new LinearGradientBrush(gradientRect, gradientColor.From, gradientColor.To, GetLinearGradientMode(gradientStyle));
               myHorizontalGradient1.Blend = blend;
               myHorizontalGradient1.GammaCorrection = true;
               // Use the brush to paint the rectangle.
               if (paintRoundedRectangle)
                  GraphicsExtension.FillRoundedRectangle(g, myHorizontalGradient1, gradientRect.Left, gradientRect.Top, gradientRect.Width, gradientRect.Height, (int)cornerRadius);
               else
                  g.FillRectangle(myHorizontalGradient1, gradientRect);
               break;
            case GradientStyle.VerticalWide:
            case GradientStyle.HorizontalWide:
               {
                  // Create a path that consists of a single rectangle.
                  GraphicsPath path = new GraphicsPath();
                  /// Add the rectangle
                  path.AddRectangle(gradientRect);
                  // Use the path to construct a brush.
                  PathGradientBrush pthGrBrush = new PathGradientBrush(path);
                  // Set the center point to a location that is not the centroid of the path.
                  if (gradientStyle == GradientStyle.HorizontalWide)
                     pthGrBrush.FocusScales = new PointF(0.92f, 0.5f);
                  else if (gradientStyle == GradientStyle.VerticalWide)
                     pthGrBrush.FocusScales = new PointF(0.5f, 0.92f);
                  // Set the color at the center point to blue.
                  pthGrBrush.CenterColor = gradientColor.To;
                  // Set the color along the entire boundary of the path to aqua.
                  Color[] colors = new Color[] { gradientColor.From };
                  pthGrBrush.SurroundColors = colors;
                  //? pthGrBrush.GammaCorrection = true;
                  // https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=94101&wa=wsignin1.0
                  if (paintRoundedRectangle)
                     GraphicsExtension.FillRoundedRectangle(g, pthGrBrush, gradientRect.Left, gradientRect.Top, gradientRect.Width, gradientRect.Height, (int)cornerRadius);
                  else
                     g.FillRectangle(pthGrBrush, gradientRect);
               }
               break;
            case GradientStyle.CornerBottomRight:
            case GradientStyle.CornerBottomLeft:
            case GradientStyle.CornerTopLeft:
            case GradientStyle.CornerTopRight:
            case GradientStyle.Center:
               {
                  // Create a path that consists of a single rectangle.
                  GraphicsPath path = new GraphicsPath();
                  /// Add the rectangle
                  path.AddRectangle(gradientRect);
                  // Use the path to construct a brush.
                  PathGradientBrush pthGrBrush = new PathGradientBrush(path);
                  // Set the center point to a location that is not the centroid of the path.
                  SetCorner(pthGrBrush, gradientStyle, gradientRect);
                  // Set the color at the center point to blue.
                  pthGrBrush.CenterColor = gradientColor.From;
                  // Set the color along the entire boundary of the path to aqua.
                  Color[] colors = new Color[] { gradientColor.To };
                  pthGrBrush.SurroundColors = colors;
                  //? pthGrBrush.GammaCorrection = true;
                  // https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=94101&wa=wsignin1.0
                  if (paintRoundedRectangle)
                     GraphicsExtension.FillRoundedRectangle(g, pthGrBrush, gradientRect.Left, gradientRect.Top, gradientRect.Width, gradientRect.Height, (int)cornerRadius);
                  else                  
                     g.FillRectangle(pthGrBrush, gradientRect);
               }
               break;
            case GradientStyle.None:
               //assert(false);
               break;
            default:
               break;
         }
#endif
      }

      /// <summary>draw image</summary>
      /// <param name="gr"></param>
      /// <param name="displayRect"></param>
      public static void DrawImage(Graphics gr, Rectangle displayRect, Image image, int srcX, int srcY)
      {
         if (image != null)
         {
            Region region = gr.Clip;
            ImageAttributes ia = new ImageAttributes();
            //Draws the specified portion of the specified Image at the specified location and with the specified size.
            //The srcX, srcY, srcWidth, and srcHeight parameters specify a rectangular portion, of the 
            //image object to draw. The rectangle is relative to the upper-left corner of the source image. 
            //This portion is scaled to fit inside the rectangle specified by the destRect parameter.
            gr.DrawImage(image, displayRect, srcX, srcY, image.Width, image.Height, GraphicsUnit.Pixel, ia);
            gr.Clip = region;
         }
      }

      /// <summary>
      /// Paints the Line control
      /// TODO : Move this method to LineRenderer
      /// </summary>
      public static void PaintLineForeGround(Graphics g, Color fgColor, ControlStyle style, int lineWidth, CtrlLineType lineStyle, Point pt1, Point pt2)
      {
#if !PocketPC
         SmoothingMode orgmode = g.SmoothingMode;
         g.SmoothingMode = SmoothingMode.AntiAlias;
#endif

         Pen pen = new Pen(fgColor, lineWidth); // TODO : use new pen's cache here

         float[] dashPattern = ControlRenderer.GetDashPattern(lineStyle);
         if (dashPattern != null)
            pen.DashPattern = dashPattern;

         LineRenderer.Draw(g, pen, pt1, pt2, style);

#if !PocketPC
         g.SmoothingMode = orgmode;
#endif
      }

      /// <summary>
      /// return the dashPattern according to the line type
      /// </summary>
      /// <param name="lineType"></param>
      /// <returns></returns>
      public static float[] GetDashPattern(CtrlLineType lineType)
      {
         float[] dashPattern = null;
         switch (lineType)
         {
            case CtrlLineType.Normal:
               break;
            case CtrlLineType.Dash:
#if !PocketPC
               dashPattern = DASH_PATTERN;
               break;
#endif
            case CtrlLineType.Dot:
#if !PocketPC
               dashPattern = DOT_PATTERN;
               break;
#endif
            case CtrlLineType.Dashdot:
#if !PocketPC
               dashPattern = DASHDOT_PATTERN;
               break;
#endif
            case CtrlLineType.Dashdotdot:
#if !PocketPC
               dashPattern = DASHDOTDOT_PATTERN;
#else
                        // Mobile supports only solid or dashed lines. All other styles fall through to dash
                        pen.DashStyle = DashStyle.Dash;
#endif
               break;
         }
         return dashPattern;
      }

      /// <summary>
      /// Renders the owner-drawn controls below the specified control.
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="control"></param>
      public static void RenderOwnerDrawnControlsOfParent(Graphics graphics, Control control)
      {
         // loop through children of parent which are under ourselves
         int start = control.Parent.Controls.GetChildIndex(control);
         for (int i = control.Parent.Controls.Count - 1; i > start; i--)
         {
            Control childControl = control.Parent.Controls[i];
            IOwnerDrawnControl ownerDrawnControl = childControl as IOwnerDrawnControl;

            if (ownerDrawnControl != null)
            {
               // skip ...
               // ... invisible controls
               // ... or controls that have zero width/height
               // ... or controls that don't intersect with current control
               if (!childControl.Visible || childControl.Width == 0 || childControl.Height == 0 || !control.Bounds.IntersectsWith(childControl.Bounds))
                  continue;

               ownerDrawnControl.Draw(graphics, childControl.Bounds);
            }
         }
      }
   }
}
