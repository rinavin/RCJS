using com.magicsoftware.win32;
using Controls.com.magicsoftware.controls.Renderer;
using System;
using System.Drawing;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// paint header dividers for ThreeD style
   /// </summary>
   internal class ThreeDRaisedStyleHeaderRenderer : ThreeDStyleHeaderRenderer
   {
      private static IntPtr ButtonHighlightPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(SystemColors.ButtonHighlight));
      


      private static IntPtr darkpen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, TableControl.Factor,
                                     ColorTranslator.ToWin32(SystemColors.ControlText));

      /// <summary>
      /// denoted we are dragging header
      /// </summary>
      bool isSectionDragging = false;

      public ThreeDRaisedStyleHeaderRenderer(TableControl tableControl, Header header) : base(tableControl, header)
      {
         header.BeforeSectionTrack += Header_BeforeSectionTrack;
         header.AfterSectionTrack += Header_AfterSectionTrack;
      }


      private void Header_BeforeSectionTrack(object sender, HeaderSectionWidthConformableEventArgs ea)
      {
         isSectionDragging = true;
         header.Invalidate(); // repaint
      }

      private void Header_AfterSectionTrack(object sender, HeaderSectionWidthEventArgs ea)
      {
         isSectionDragging = false;
      }

      public override Color GetDividerColor()
      {
         return SystemColors.ControlLight;
      }

      /// <summary>
      /// Paint the dividers
      /// Code from table_3d_paint() Ctrl_pnt.cpp
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="rect"></param>
      /// <param name="headerHeight"></param>
      internal override void DrawDivider(IntPtr hdc, int index, ref NativeWindowCommon.RECT rect, int headerHeight)
      {
         // Since we have white line at top , start the divider from 1
         rect.top++;

         int left, right;

         // order of line is reverse for rtl hence swap left and right
         if (tableControl.RightToLeftLayout)
         {
            right = (rect.right) - TwoDTableStyleRenderer.MARGIN * TableControl.Factor + TableControl.Factor - 1;
            left = rect.right - 1;
         }
         else
         {
            left = (rect.right) - TwoDTableStyleRenderer.MARGIN * TableControl.Factor + TableControl.Factor - 1;
            right = rect.right - 1;
         }

         // Gui->ctx->gui_.dc_->frame_rect(&Rect1, FIX_SYS_COLOR_BTNSHADOW, factor, 0L);
         DrawLine(hdc, new Point(left, rect.top), new Point(left, headerHeight), ButtonShadowPen);

         left = tableControl.RightToLeftLayout ? left - 1 : left + 1;

         DrawLine(hdc, new Point(left, rect.top), new Point(left, headerHeight), ButtonShadowPen);

         // Rect1.left += 2 * factor;
         // Rect1.right += 2 * factor;
         // Gui->ctx->gui_.dc_->frame_rect(&Rect1, FIX_SYS_COLOR_BTNHIGHLIGHT, factor, 0L);
         DrawLine(hdc, new Point(right, rect.top), new Point(right, headerHeight), ButtonHighlightPen);

         right = tableControl.RightToLeftLayout ? right + 1 : right - 1;

         DrawLine(hdc, new Point(right, rect.top), new Point(right, headerHeight), ButtonHighlightPen);

         // Rect1.top = Rect1.bottom = TitleRect.top;
         // Rect1.left -= factor;
         // Rect1.right -= factor;
         // Gui->ctx->gui_.dc_->line_to(&Rect1, FIX_SYS_COLOR_BTNSHADOW, factor, 0L);

         int x = tableControl.RightToLeftLayout ? left - (2 * TableControl.Factor) : left + (2 * TableControl.Factor);
         DrawLine(hdc, new Point(left, rect.top), new Point(x, rect.top), ButtonShadowPen);

         // Rect1.left -= factor;
         // Rect1.bottom = Rect1.top = TitleRect.bottom - factor;
         // Gui->ctx->gui_.dc_->line_to(&Rect1, FIX_SYS_COLOR_BTNHIGHLIGHT, factor, 0L);

         x = tableControl.RightToLeftLayout ? right + (2 * TableControl.Factor) : right - (2 * TableControl.Factor);
         DrawLine(hdc, new Point(right, headerHeight - TableControl.Factor), new Point(x, headerHeight - TableControl.Factor), ButtonHighlightPen);

      }

      internal override void PaintBackGround(NativeCustomDraw.NMCUSTOMDRAW pcust, NativeWindowCommon.RECT rect)
      {
         base.PaintBackGround(pcust, rect);


         // top line
         //SetRect(&TitleRect, Rect.left, Rect.top, Rect.right, Rect.top);
         //Gui->ctx->gui_.dc_->line_to(&TitleRect, TitleTopLeftColor, factor, 0L);
         DrawLine(pcust.hdc, new Point(rect.left, rect.top), new Point(rect.right, rect.top), WhitePen); // dark

         // bottom lines
         //SetRect(&Rect1, Rect.left, Row, Rect.right, Row);
         //Gui->ctx->gui_.dc_->line_to(&Rect1, FIX_SYS_COLOR_BTNTEXT, factor, 0L);
         rect.bottom--;
         DrawLine(pcust.hdc, new Point(rect.left, rect.bottom), new Point(rect.right, rect.bottom), darkpen);
         //Rect1.bottom -= factor;
         //Gui->ctx->gui_.dc_->line_to(&Rect1, FIX_SYS_COLOR_BTNSHADOW, factor, 0L);
         rect.bottom--;
         DrawLine(pcust.hdc, new Point(rect.left, rect.bottom), new Point(rect.right, rect.bottom), ButtonShadowPen);


         // draw left for first header section when we are not dragging
         if (!isSectionDragging)
         {
            if (rect.left == 0)
               DrawLine(pcust.hdc, new Point(rect.left, rect.top), new Point(rect.left, rect.bottom), tableControl.RightToLeftLayout ? ButtonShadowPen : WhitePen);

            // draw right borders for last  header section
            if (rect.right == (tableControl.Bounds.Right - 2 * tableControl.BorderHeight))
               DrawLine(pcust.hdc, new Point(rect.right - 1, rect.top), new Point(rect.right - 1, rect.bottom), tableControl.RightToLeftLayout ? WhitePen : ButtonShadowPen);
         }
      }

      /// <summary>
      /// get divider height
      /// </summary>
      /// <param name="header"></param>
      /// <returns></returns>
      internal override int GetDividerHeight()
      {
         // divider is smaller by 2 pixels
         return header.Height - (2 * TableControl.Factor); ;
      }

      public override void Dispose()
      {
         base.Dispose();
         header.BeforeSectionTrack -= Header_BeforeSectionTrack;
         header.AfterSectionTrack -= Header_AfterSectionTrack;
      }

   }
}