using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.win32;
using System.Runtime.InteropServices;
using Controls.com.magicsoftware.support;
using System.Globalization;
#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using PointF = com.magicsoftware.mobilestubs.PointF;

#else
using System.Reflection;
using System.Windows.Forms.Design;
using System.Drawing.Design;
#endif

using com.magicsoftware.util;

namespace com.magicsoftware.controls.utils
{
#if PocketPC
   public enum TextFormatFlags
   {
      Default                            =          0,
      Top                                =          0,
      Left                               =          0,
      GlyphOverhangPadding               =          0,
      HorizontalCenter                   =        0x1,
      Right                              =        0x2,
      VerticalCenter                     =        0x4,
      Bottom                             =        0x8,
      WordBreak                          =       0x10,
      SingleLine                         =       0x20,
      ExpandTabs                         =       0x40,
      NoClipping                         =      0x100,
      ExternalLeading                    =      0x200,
      NoPrefix                           =      0x800,
      Internal                           =     0x1000,
      TextBoxControl                     =     0x2000,
      PathEllipsis                       =     0x4000,
      EndEllipsis                        =     0x8000,
      ModifyString                       =    0x10000,
      RightToLeft                        =    0x20000,
      WordEllipsis                       =    0x40000,
      NoFullWidthCharacterBreak          =    0x80000,
      HidePrefix                         =   0x100000,
      PrefixOnly                         =   0x200000,
      PreserveGraphicsClipping           =  0x1000000,
      PreserveGraphicsTranslateTransform =  0x2000000,
      NoPadding                          = 0x10000000,
      LeftAndRightPadding                = 0x20000000
   }
#endif
   /// <summary>
   /// support class for general utilities
   /// </summary>
   public static class Utils
   {
      private static readonly Dictionary<Font, PointF> _metricsCache = new Dictionary<Font, PointF>();
      private static readonly Dictionary<MgFont, PointF> _metricsMgFontCache = new Dictionary<MgFont, PointF>();

      /// <summary>
      /// defect 117706
      /// When true and resolution != 100% we may manually affect factor calcultaions
      /// </summary>
      public static bool SpecialTextSizeFactoring { get; set; }

      public static bool SpecialFlatEditOnClassicTheme { get; set; }

      public static bool SpecialSwipeFlickeringRemoval { get; set; }

      public static char Language{ get; set; }

      public static bool IsHebrew
      {
         get
         {
            return Language == 'H';
         }
      }
      /// <summary>
      /// create one InputLanguageHeb
      /// </summary>
      public static InputLanguage InputLanguageHeb = InputLanguage.FromCulture(new CultureInfo("he-IL"));

      /// <summary>
      /// check if the current input is hebrew 
      /// </summary>
      public static bool IsCurrentInputLanguageHebrew
      {
         get
         {
            return Application.CurrentInputLanguage.Equals(InputLanguageHeb);
         }
      }
      /// <summary>
      /// translate ContentAlignment to HorizontalAlignment
      /// </summary>
      /// <param name="contentAlignment"></param>
      /// <returns></returns>
      public static HorizontalAlignment ContentAlignment2HorizontalAlignment(ContentAlignment contentAlignment)
      {
         HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;
         switch (contentAlignment)
         {
            case ContentAlignment. BottomCenter:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.TopCenter:
               horizontalAlignment = HorizontalAlignment.Center;
               break;

            case ContentAlignment.BottomLeft:
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.TopLeft:
               horizontalAlignment = HorizontalAlignment.Left;
               break;

            case ContentAlignment.BottomRight:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.TopRight:
               horizontalAlignment = HorizontalAlignment.Right;
               break;
            default:
               break;
         }
         return horizontalAlignment;

      }

      public static void SetDropDownHeight(ComboBox combo, int visibleLines, Font font, out int itemHeight)
      {
         Rectangle DeskTopRect = new Rectangle(0, 0, 0, 0);
         DeskTopRect = Screen.GetBounds(combo);

         if (combo.DrawMode == DrawMode.Normal)
         // QCR #312350: limit the size of the dropdown list of the combo box to the height of the desktop.
         // #770790 - we need 2 pixel extra for margin between item.
         {
            PointF retPoint = Utils.GetFontMetricsByFont(combo, font);
            itemHeight = (int)retPoint.Y;
          }
         else// (combo.DrawMode == DrawMode.OwnerDrawFixed)
            itemHeight = combo.ItemHeight;
        combo.DropDownHeight = Math.Min(visibleLines * itemHeight + 2, DeskTopRect.Height);
      }

      /// <summary>
      /// translate ContentAlignment to VerticalAlignment
      /// </summary>
      /// <param name="contentAlignment"></param>
      /// <returns></returns>
      public static VerticalAlignment ContentAlignment2VerticalAlignment(ContentAlignment contentAlignment)
      {
         VerticalAlignment verticalAlignment = VerticalAlignment.Top;
         switch (contentAlignment)
         {
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomLeft:
            case ContentAlignment.BottomRight:
               verticalAlignment = VerticalAlignment.Bottom;
               break;

            case ContentAlignment.MiddleCenter:
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.MiddleRight:
               verticalAlignment = VerticalAlignment.Center;
               break;

            case ContentAlignment.TopCenter:
            case ContentAlignment.TopLeft:
            case ContentAlignment.TopRight:
               verticalAlignment = VerticalAlignment.Top;
               break;
         }
         return verticalAlignment;
      }

      /// <summary>
      /// translate ContentAlignment to TextFlags
      /// </summary>
      /// <param name="contentAlignment"></param>
      /// <returns></returns>

      
      public static TextFormatFlags ContentAlignment2TextFlags(ContentAlignment contentAlignment)
      {
         TextFormatFlags tff = 0;
         switch (contentAlignment)
         {
            case ContentAlignment.BottomCenter:
               tff |= TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom;
               break;

            case ContentAlignment.MiddleCenter:
               tff |= TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
               break;
            case ContentAlignment.TopCenter:
               tff |= TextFormatFlags.HorizontalCenter | TextFormatFlags.Top;
               break;

            case ContentAlignment.BottomLeft:
               tff |= TextFormatFlags.Left | TextFormatFlags.Bottom; ;
               break;
            case ContentAlignment.MiddleLeft:
               tff |= TextFormatFlags.Left | TextFormatFlags.VerticalCenter; ;
               break;
            case ContentAlignment.TopLeft:
               tff |= TextFormatFlags.Left | TextFormatFlags.Top;
               break;

            case ContentAlignment.BottomRight:
               tff |= TextFormatFlags.Right | TextFormatFlags.Bottom; ;
               break;
            case ContentAlignment.MiddleRight:
               tff |= TextFormatFlags.Right | TextFormatFlags.VerticalCenter; ;
               break;
            case ContentAlignment.TopRight:
               tff |= TextFormatFlags.Right | TextFormatFlags.Top; 
               break;
            default:
               break;
         }
         return tff;

      }

      /// <summary>
      /// Sends KeyDown and KeyUp messages to the window
      /// </summary>
      /// <param name="hwnd"></param>
      /// <param name="keyCode"></param>
      /// <param name="extended"></param>
      public static void SendKey(IntPtr hwnd, int keyCode, bool extended)
      {
         uint scanCode = NativeWindowCommon.MapVirtualKey((uint)keyCode, 0);
         uint lParam;

         //KEY DOWN
         lParam = (0x00000001 | (scanCode << 16));
         if (extended)
         {
            lParam = lParam | 0x01000000;
         }

         NativeWindowCommon.PostMessage(hwnd, NativeWindowCommon.WM_KEYDOWN, (int)keyCode, (int)lParam);

         //KEY UP

         NativeWindowCommon.PostMessage(hwnd, NativeWindowCommon.WM_KEYUP, (int)keyCode, (int)lParam);
      }

      /// <summary>
      /// Retrun TRUE if Visual Styles for the application are active.
      /// </summary>
      /// <returns></returns>
      public static bool IsXPStylesActive()
      {
         return isWinXPOrNewer() && NativeWindowCommon.IsThemeActive();
      }

      /// <summary>
      /// see : http://blog.devstone.com/aaron/default,date,2008-09-07.aspx
      /// </summary>
      /// <returns></returns>
      /// 
      internal static bool isWinXPOrNewer()
      {
         bool result = false;
         OperatingSystem os = System.Environment.OSVersion;
         // decide whether the OS even supports visual styles
         result = (os.Platform == PlatformID.Win32NT)
                            && (((os.Version.Major == 5) && (os.Version.Minor >= 1))
                                 || (os.Version.Major > 5));
         return result;
      }


      /// <summary>
      /// scroll info for scrollbar
      /// </summary>
      /// <param name="fnBar"></param>
      /// <returns></returns>
      public static NativeScroll.SCROLLINFO ScrollInfo(Control control, int fnBar)
      {
         NativeScroll.SCROLLINFO sc = new NativeScroll.SCROLLINFO();

         sc.fMask = NativeScroll.SIF_ALL;
         sc.cbSize = Marshal.SizeOf(sc);
         bool res = NativeScroll.GetScrollInfo(control.Handle, fnBar, ref sc);
         return sc;
      }

      /// <summary>return the size of the string according to the font</summary>
      /// <param name="font"></param>
      /// <param name="str"></param>
      /// <param name="control"></param>
      /// <returns></returns>
      public static Size GetTextExt(Font font, String str, Control control)
      {
         Size size = new Size();

         if (control != null && str != null)
         {
#if PocketPC
            Form form = (Form)((control is Form)? control : control.TopLevelControl);
            using (Graphics g = form.CreateGraphics())
            {
               // Mobile does not support GetTextExtentPoint32 or similar functions
               SizeF sizef = g.MeasureString(str, font);
               size.Width = (int)Math.Ceiling(sizef.Width);
               size.Height = (int)Math.Ceiling(sizef.Height);
            }
#else
            using (Graphics g = control.CreateGraphics())
            {
               IntPtr hdc = g.GetHdc();
               IntPtr hFont = FontHandlesCache.GetInstance().Get(font).FontHandle;
               IntPtr hFontOld = (IntPtr)NativeWindowCommon.SelectObject(hdc, hFont);
               NativeWindowCommon.SIZE nativeSize = new NativeWindowCommon.SIZE();
               NativeWindowCommon.GetTextExtentPoint32(hdc, str, str.Length, out nativeSize);
               size.Width = nativeSize.cx; //(int)Math.Ceiling(size.cx);
               size.Height = nativeSize.cy;//(int)Math.Ceiling(size.cy);
               NativeWindowCommon.SelectObject(hdc, hFontOld);
               g.ReleaseHdc(hdc);
            }
#endif
         }

         return size;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="font"></param>
      /// <returns></returns>
      public static PointF GetFontMetricsByFont(Control control, Font font)
      {
         PointF retPoint;
         
         lock (_metricsCache)
         {
            if (!_metricsCache.TryGetValue(font, out retPoint))
            {
#if PocketPC
               // In PocketPC, Panels don't have CreateGraphics. We go up the parents 
               // tree until we find a non-panel one.
               while (control is Panel)
                  control = control.Parent;
#endif
               using (Graphics g = control.CreateGraphics())
               {
                  IntPtr hFont = FontHandlesCache.GetInstance().Get(font).FontHandle;
                  GetTextMetrics(g, hFont, font.Name, (int)font.Size, out retPoint);
               }

               _metricsCache.Add(font, retPoint);
            }
         }

         return retPoint;
      }

      /// <summary>
      /// Returns the font text metrics for true type and non true type fonts assigned to control according to mgFont.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="mgFont"></param>
      /// <returns></returns>
      public static PointF GetFontMetricsByMgFont(Control control, MgFont mgFont)
      {
         PointF retPoint;

         lock (_metricsMgFontCache)
         {
            if (!_metricsMgFontCache.TryGetValue(mgFont, out retPoint))
            {

#if PocketPC
               // In PocketPC, Panels don't have CreateGraphics. We go up the parents 
               // tree until we find a non-panel one.
               while (control is Panel)
                  control = control.Parent;
#endif
              
               using (Graphics g = control.CreateGraphics())
               {
                  int dpiY = (int)g.DpiY;

                  NativeWindowCommon.LOGFONT nativeLogFont = new NativeWindowCommon.LOGFONT();

                  nativeLogFont.lfFaceName = mgFont.TypeFace;
                  nativeLogFont.lfHeight = -NativeWindowCommon.MulDiv(mgFont.Height, dpiY, 72);

                  // Set the rotation angle
                  nativeLogFont.lfEscapement = nativeLogFont.lfOrientation = mgFont.Orientation;
                  nativeLogFont.lfItalic = Convert.ToByte(mgFont.Italic);
                  nativeLogFont.lfStrikeOut = Convert.ToByte(mgFont.Strikethrough);
                  nativeLogFont.lfUnderline = Convert.ToByte(mgFont.Underline);

                  if (mgFont.Bold)
                     nativeLogFont.lfWeight = (int)NativeWindowCommon.FontWeight.FW_BOLD;
                  else
                     nativeLogFont.lfWeight = (int)NativeWindowCommon.FontWeight.FW_NORMAL;

                  nativeLogFont.lfCharSet = (byte)mgFont.CharSet;
                  IntPtr hFont = NativeWindowCommon.CreateFontIndirect(nativeLogFont);

                  // use the font in the DC
                  GetTextMetrics(g, hFont, mgFont.TypeFace, mgFont.Height, out retPoint);

                  //Release the resources.
                  nativeLogFont = null;
                  NativeWindowCommon.DeleteObject(hFont);
               }

               _metricsMgFontCache.Add(mgFont, retPoint);
            }
         }

         return retPoint;
      }

      /// <summary>
      /// Get font text metrics points.
      /// </summary>
      /// <param name="g"></param>
      /// <param name="hFont"></param>
      /// <param name="fontName"></param>
      /// <param name="fontSize"></param>
      /// <param name="retPoint"></param>
      public static void GetTextMetrics(Graphics g, IntPtr hFont, string fontName, int fontSize, out PointF retPoint)
      {
         retPoint = new PointF();

         int dpiX = (int)g.DpiX;

         // create the handle of DC
         IntPtr hdc = g.GetHdc();

         IntPtr hFontOld = (IntPtr)NativeWindowCommon.SelectObject(hdc, hFont);
         NativeWindowCommon.TEXTMETRIC tm;
         NativeWindowCommon.GetTextMetrics(hdc, out tm);

         retPoint.X = tm.tmAveCharWidth;
         retPoint.Y = tm.tmHeight;
         if (SpecialTextSizeFactoring && dpiX == 120) //defect 117706
         {
            //please, create an infrastructure for this if you need to add another font ...

            if (fontName == "Microsoft Sans Serif" && fontSize == 8 && retPoint.X == 6)
               //For 100% resolution we got tm.tmAveCharWidth = 5 (average char width)
               //For 125% resolution we got tm.tmAveCharWidth = 6 and in prepareUOMConversion we use this data as factor for calculating all magic sizes for all controls and form.
               //So, the difference between 125% resolution and 100% resolution is 6/5  = 1.2, i.e 120%  which is not enough 
               //Here we manually set factor
               retPoint.X = 5 * 1.275F;
         }
         NativeWindowCommon.SelectObject(hdc, hFontOld);

         // release the resources
         g.ReleaseHdc(hdc);
      }

      /// <summary> Gets the resolution of the control. </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public static Point GetResolution(Control control)
      {
         Point point = new Point();

         using (Graphics g = control.CreateGraphics())
         {
            point.X = (int)g.DpiX;
            point.Y = (int)g.DpiY;
         }

         return point;
      }

      /// <summary>
      /// Gets the horizontal resolution ratio of the control's graphics.
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public static float GetDpiScaleRatioX(Control control)
      {
         using (Graphics g = control.CreateGraphics())
         {
            return g.DpiX / 96f;
         }
      }

      /// <summary>
      /// Gets the vertical resolution ratio of the control's graphics.
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public static float GetDpiScaleRatioY(Control control)
      {
         using (Graphics g = control.CreateGraphics())
         {
            return g.DpiY / 96f;
         }
      }

      /// <summary></summary>
      /// <param name="contentAlignment"></param>
      /// <param name="wordWrap"></param>
      /// <param name="MultiLine"></param>
      /// <returns></returns>
      public static TextFormatFlags GetTextFlags(ContentAlignment contentAlignment, bool wordWrap,
                                                 bool multiLine, bool addNoPrefixFlag, bool addNoClipping, bool rightToLeft)
      {
         TextFormatFlags textFormatFlags = new TextFormatFlags();
         textFormatFlags |= TextFormatFlags.NoPadding;

         if (addNoPrefixFlag)
            textFormatFlags |= TextFormatFlags.NoPrefix;

         textFormatFlags |= controls.utils.Utils.ContentAlignment2TextFlags(contentAlignment);
         if (wordWrap)
            textFormatFlags |= TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;

         if (!multiLine)
            textFormatFlags |= TextFormatFlags.SingleLine;

         if (addNoClipping)
            textFormatFlags |= TextFormatFlags.NoClipping;

         if (rightToLeft)
            textFormatFlags |= TextFormatFlags.RightToLeft;

         textFormatFlags |= TextFormatFlags.ExpandTabs;

         return textFormatFlags;
      }

      /// <summary>
      /// calc text rect
      /// use ContentAlignment.MiddleRight for the calculation because if we use ContentAlignment.MiddleCenter
      /// then GuiUtils.CalcTextRect() returns wrong results. The problem is that the height returned by GuiUtils.CalcTextRect()
      /// will be of a single line always.
      /// 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="font"></param>
      /// <param name="text"></param>
      /// <param name="flags"></param>
      /// <param name="retCalcTextRect"></param>
      public static void CalcTextRect(Graphics g, Rectangle rect, FontDescription font, String text, int flags, out NativeWindowCommon.RECT retCalcTextRect)
      {
         IntPtr hdc1;
         IntPtr hFont1;

         hdc1 = g.GetHdc();

         hFont1 = font.FontHandle;

         NativeWindowCommon.SelectObject(hdc1, hFont1);
         retCalcTextRect = new NativeWindowCommon.RECT();
         retCalcTextRect.left = rect.Left;
         retCalcTextRect.right = rect.Right;
         retCalcTextRect.top = rect.Top;
         retCalcTextRect.bottom = rect.Bottom;
         int calcRectFlags = (int)(TextFormatFlags)flags | 0x400; // TextFormatFlags.CalcRect == 0x400 == DT_CALCRECT
         NativeWindowCommon.DrawText(hdc1, text, text.Length, ref retCalcTextRect, calcRectFlags);
         g.ReleaseHdc(hdc1);
      }

      /// <summary>
      /// returns nearest color , do not change transparent color , 
      /// </summary>
      /// <param name="gr"></param>
      /// <param name="color"></param>
      /// <returns></returns>
      public static Color GetNearestColor(Graphics gr, Color color)
      {
#if !PocketPC
         if (color != Color.Transparent)
            return gr.GetNearestColor(color);
#endif
         return color;
      }


      /// <summary>
      /// Return true if the dragged component is toolbox Item
      /// </summary>
      /// <returns></returns>
#if !PocketPC
      public static bool GetIsDroppedFromToolbox (ParentControlDesigner designer)
      {
         FieldInfo fi = typeof(ParentControlDesigner).GetField("mouseDragTool", BindingFlags.Instance | BindingFlags.NonPublic);
         return (fi.GetValue(designer) is ToolboxItem);
      }
#endif

   }
}
