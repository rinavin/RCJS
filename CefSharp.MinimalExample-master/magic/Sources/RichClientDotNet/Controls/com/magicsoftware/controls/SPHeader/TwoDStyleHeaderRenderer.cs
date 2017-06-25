using com.magicsoftware.win32;
using Controls.com.magicsoftware.controls.Renderer;
using System;
using System.Drawing;

namespace com.magicsoftware.controls
{
   /// <summary>
   ///  render header divider for 2D style
   /// </summary>
   internal class TwoDStyleHeaderRenderer : HeaderRenderer
   {
      public TwoDStyleHeaderRenderer(TableControl tableControl, Header header)
         : base(tableControl, header)
      {
      }

      /// <summary>
      /// divider color is the fgcolor
      /// </summary>
      /// <returns></returns>
      public override Color GetDividerColor()
      {
         return tableControl.ForeColor;
      }

      /// <summary>
      /// header color is back color of 'Color' property of table
      /// </summary>
      public override Color HeaderBackColor
      {
         get
         {
            return tableControl.MagicBgColor;
         }
      }

      /// <summary>
      /// draw divider
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="rect"></param>
      /// <param name="headerHeight"></param>
      internal override void DrawDivider(IntPtr hdc, int index, ref NativeWindowCommon.RECT rect, int headerHeight)
      {
         rect.right--; // cannot paint on boundaries so reduce size by 1 pixel

         if (index == 0 && tableControl.RightToLeftLayout) // for painting, reduce the right of first column
            rect.right--;

         int x = (rect.right) - TwoDTableStyleRenderer.MARGIN * TableControl.Factor + TableControl.Factor;
         DrawLine(hdc, new Point(x, 0), new Point(x, headerHeight));
         DrawLine(hdc, new Point(rect.right, 0), new Point(rect.right, headerHeight));
      }

      /// <summary>
      /// paint background of header
      /// </summary>
      /// <param name="pcust"></param>
      /// <param name="rect"></param>
      internal override void PaintBackGround(NativeCustomDraw.NMCUSTOMDRAW pcust, NativeWindowCommon.RECT rect)
      {
         // Fill bg color
         base.PaintBackGround(pcust, rect);

         // draw bottom line
         rect.bottom--;
         DrawLine(pcust.hdc, new Point(rect.left, rect.bottom), new Point(rect.right, rect.bottom));
      }

      /// <summary>
      /// get title text rect
      /// </summary>
      /// <param name="index"></param>
      /// <param name="rc"></param>
      internal override void GetTitleTextRect(int index, ref NativeWindowCommon.RECT rc)
      {
         // draw_column_titles() from cnt_pnt.cpp

         rc.top -= tableControl.BorderHeight; 

         //if (Ctrl->Style & (CTRL_STYLE_THICK) || Ctrl->Style & (CTRL_STYLE_THIN))
         //   InflateRect(&Rect, -factor, -factor);

         if (tableControl.BorderType == util.BorderType.Thick ||
            tableControl.BorderType == util.BorderType.Thin)
         {
            rc.top++;
         }

         //paintRect.bottom = Rect.top + Ctrl->TitleHeight - PIX_BETWEEN_DEVIDER_AND_DIT;
         //paintRect.top = Rect.top + PIX_BETWEEN_DEVIDER_AND_DIT;
         rc.bottom = rc.top + tableControl.TitleHeight - PIX_BETWEEN_DIVIDER_AND_DIT;
         rc.top += PIX_BETWEEN_DIVIDER_AND_DIT;

         //paintRect.right -= ((COL_DIVIDER - 1) * factor);
         //paintRect.left += ((COL_DIVIDER - 1) * factor);
         rc.left += (TwoDTableStyleRenderer.MARGIN - 1) * TableControl.Factor;
         rc.right -= (TwoDTableStyleRenderer.MARGIN - 1) * TableControl.Factor  + 1;

         if (tableControl.RightToLeftLayout && index != 0)
            rc.left--;
      }
   }
}