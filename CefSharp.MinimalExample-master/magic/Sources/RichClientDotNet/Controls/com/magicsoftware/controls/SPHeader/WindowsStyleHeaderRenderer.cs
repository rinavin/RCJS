using com.magicsoftware.win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Render header divider for windows style
   /// </summary>
   internal class WindowsStyleHeaderRenderer : HeaderRenderer
   {
      static IntPtr HeaderItemBorderPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(SystemColors.ControlLight));

      public WindowsStyleHeaderRenderer(TableControl tableControl, Header header)
         : base(tableControl, header)
      {
      }

      public override Color GetDividerColor()
      {
         Color defaultDividerColor = !Application.RenderWithVisualStyles ? Color.Black : SystemColors.ControlLight;
         return header.DividerColor != Color.Empty ? tableControl.DividerColor : defaultDividerColor;
      }


      internal override void PaintBackGround(NativeCustomDraw.NMCUSTOMDRAW pcust, NativeWindowCommon.RECT rect)
      {
         if (header.TitleColor != Color.Empty)
         { 
            if (header.ShowBottomBorder)
            {
               PaintBottomBorderOnColoredHeader(pcust, ref rect);
            }

            base.PaintBackGround(pcust, rect);
         }
         else if (header.DividerColor != Color.Empty)
         {
            if (header.ShowBottomBorder)
            {
               PaintBottomBorderOnSystemHeader(pcust, ref rect, 0);
            }
         }
      }

      internal override void PaintItemBackGround(NativeCustomDraw.NMCUSTOMDRAW pcust, NativeWindowCommon.RECT rect)
      {
         if (header.TitleColor != Color.Empty)
         {
            PaintDividers(pcust, ref rect, 0);

            if (header.ShowBottomBorder)
            {
               PaintBottomBorderOnColoredHeader(pcust, ref rect);
            }

            if (rect.left < rect.right)
               base.PaintBackGround(pcust, rect);
         }

         else if (header.DividerColor != Color.Empty)
         {
            PaintDividers(pcust, ref rect, 0);

            if (header.ShowBottomBorder)
            {
               PaintBottomBorderOnSystemHeader(pcust, ref rect, 1);
            }
         }
      }

      internal override void DrawDivider(IntPtr hdc, int index, ref NativeWindowCommon.RECT rect, int headerHeight)
      {
         rect.right--;

         IntPtr pen = header.DividerColor != Color.Empty ? HeaderDividerPen : HeaderItemBorderPen;

         DrawLine(hdc, new Point(rect.right, 0), new Point(rect.right, headerHeight), pen);
      }

      private void PaintBottomBorderOnColoredHeader(NativeCustomDraw.NMCUSTOMDRAW pcust, ref NativeWindowCommon.RECT rect)
      {
         if (rect.bottom == header.Height)
         {
            rect.bottom--;

            IntPtr pen = header.DividerColor != Color.Empty ? HeaderDividerPen : HeaderItemBorderPen;

            DrawLine(pcust.hdc, new Point(rect.left, rect.bottom), new Point(rect.right, rect.bottom), pen);
         }
      }


      private void PaintBottomBorderOnSystemHeader(NativeCustomDraw.NMCUSTOMDRAW pcust, ref NativeWindowCommon.RECT rect, int diff)
      {
         if (rect.bottom == header.Height)
         {
            rect.bottom--;
            if ((header.TableLineDivider) || (!header.TableLineDivider && (header.DividerColor != Color.Empty)))
               DrawLine(pcust.hdc, new Point(rect.left, rect.bottom), new Point(rect.right + diff, rect.bottom), HeaderDividerPen);
         }
      }

      /// <summary>
      ///  header back color is 'TitleColor'
      /// </summary>
      public override Color HeaderBackColor
      {
         get
         {
            return tableControl.TitleColor;
         }
      }
   }
}