using com.magicsoftware.win32;
using System;
using System.Drawing;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// renderer for header
   /// </summary>
   internal abstract class HeaderRenderer : IDisposable
   {
      protected TableControl tableControl;
      protected Header header;
      internal IntPtr HeaderDividerPen;

      public const int PIX_BETWEEN_DIVIDER_AND_DIT = 2; // #define PIX_BETWEEN_DEVIDER_AND_DIT

      protected IntPtr TitleBrush { get; set; }

      /// <summary>
      /// background color of header
      /// </summary>
      public virtual Color HeaderBackColor
      {
         get
         {
            return SystemColors.ButtonFace; //  long TitleBGColor = FIX_SYS_COLOR_BTNFACE;
         }
      }


      public HeaderRenderer(TableControl tableControl, Header header)
      {
         this.tableControl = tableControl;
         this.header = header;
         UpdateTitleBrush();
      }

      /// <summary>
      /// update the divider pen
      /// </summary>
      public void UpdateDividerPen()
      {
         NativeWindowCommon.DeleteObject(HeaderDividerPen); //Release previous
         HeaderDividerPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(GetDividerColor()));
      }

      /// <summary>
      /// update the title brush
      /// </summary>
      public void UpdateTitleBrush()
      {
         NativeWindowCommon.DeleteObject(TitleBrush);
         TitleBrush = HeaderBackColor == Color.Empty ? IntPtr.Zero :
               NativeWindowCommon.CreateSolidBrush(ColorTranslator.ToWin32(HeaderBackColor));
      }

      /// <summary>
      /// get the color of divider
      /// </summary>
      /// <returns></returns>
      public abstract Color GetDividerColor();

      /// <summary>
      /// draw divider
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="rect"></param>
      /// <param name="headerHeight"></param>
      internal abstract void DrawDivider(IntPtr hdc, int index, ref NativeWindowCommon.RECT rect, int headerHeight);

      protected void PaintDividers(NativeCustomDraw.NMCUSTOMDRAW pcust, ref NativeWindowCommon.RECT rect, int difference)
      {
         // paint divider for last header only when "Show Last Divider " of table control is true 
         if (header.ShowDividers)
         {
            // Check if last divider is to be painted .
            // For last section , last divider should be painted when 'ShowLastDivider' property is set to true
            bool showlastDivider = (pcust.dwItemSpec.ToInt32() == header.Sections.Count - 1 ? header.ShowLastDivider : true);

            if (showlastDivider)
               DrawDivider(pcust.hdc, pcust.dwItemSpec.ToInt32(), ref rect, header.DividerHeight);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      public void DrawLine(IntPtr hdc, Point start, Point end)
      {
         DrawLine(hdc, start, end, HeaderDividerPen);
      }

      /// <summary>
      /// draw line
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      /// <param name="pen"></param>
      public void DrawLine(IntPtr hdc, Point start, Point end, IntPtr pen)
      {
         NativeWindowCommon.MoveToEx(hdc, start.X, start.Y, IntPtr.Zero);
         NativeWindowCommon.SelectObject(hdc, pen);
         NativeWindowCommon.LineTo(hdc, end.X, end.Y);
      }

      public virtual void Dispose()
      {
         NativeWindowCommon.DeleteObject(HeaderDividerPen);
         if (TitleBrush != IntPtr.Zero)
            NativeWindowCommon.DeleteObject(TitleBrush);
      }

      /// <summary>
      /// paint header background
      /// </summary>
      /// <param name="pcust"></param>
      /// <param name="rect"></param>
      internal virtual void PaintBackGround(NativeCustomDraw.NMCUSTOMDRAW pcust, NativeWindowCommon.RECT rect)
      {
         if (TitleBrush != IntPtr.Zero)
            NativeWindowCommon.FillRect(pcust.hdc, ref rect, TitleBrush);
      }

      internal virtual void PaintItemBackGround(NativeCustomDraw.NMCUSTOMDRAW pcust, NativeWindowCommon.RECT rect)
      {
         if (rect.left < rect.right)
            PaintBackGround(pcust, rect);

         PaintDividers(pcust, ref rect, 0);
      }
      
      /// <summary>
      /// get the header height
      /// </summary>
      /// <param name="height"></param>
      /// <returns></returns>
      internal virtual int GetHeaderHeight(int height)
      {
         return height;
      }

      /// <summary>
      ///  get height of divider 
      /// </summary>
      /// <param name="header"></param>
      /// <returns></returns>
      internal virtual int GetDividerHeight()
      {
         int dividerHeight = 0;

         if (header.DividerColor == Color.Empty)
            dividerHeight = header.Height;
         else
         {
            if (header.TableColumnDivider)
               dividerHeight = header.Height + 2;
            else
            {
               if (header.TitleColor != Color.Empty)
                  dividerHeight = header.Height;
               else if (header.DividerColor != Color.Empty)
                  dividerHeight = header.Height - 2;
            }
         }
         return dividerHeight;
      }

      /// <summary>
      /// get the title rect
      /// </summary>
      /// <param name="index"></param>
      /// <param name="rc"></param>
      internal virtual void GetTitleTextRect(int index , ref NativeWindowCommon.RECT rc)
      {
         NativeWindowCommon.InflateRect(ref rc, -2, -2);
      }
   }
}