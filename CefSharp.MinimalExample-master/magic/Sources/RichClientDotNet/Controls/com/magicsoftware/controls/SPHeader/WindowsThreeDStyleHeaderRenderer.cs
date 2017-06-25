using com.magicsoftware.win32;
using System;
using System.Drawing;

namespace com.magicsoftware.controls
{
   internal class WindowsThreeDStyleHeaderRenderer : ThreeDStyleHeaderRenderer
   {
      private static IntPtr darkpen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, TableControl.Factor,
                                    ColorTranslator.ToWin32(SystemColors.ControlDark));

      private static IntPtr lightPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, TableControl.Factor,
                                  ColorTranslator.ToWin32(SystemColors.ControlLight));

      public WindowsThreeDStyleHeaderRenderer(TableControl tableControl , Header header) : base(tableControl, header)
      {
      }

      public override Color GetDividerColor()
      {
         return SystemColors.ControlText;
      }

      internal override void DrawDivider(IntPtr hdc, int index, ref NativeWindowCommon.RECT rect, int headerHeight)
      {
         int left = rect.right - (2 * TableControl.Factor);

         //  Gui->ctx->gui_.dc_->line_to (&DividerRect, DividerColor, factor, 0L);
         DrawLine(hdc, new Point(left, rect.top), new Point(left, headerHeight - TableControl.Factor));

         //SetRect(&DividerRect, Tmp - factor, Rect.top + factor, Tmp - factor, Rect.top + tableCtrl->TitleHeight - 2 * factor);
         //Gui->ctx->gui_.dc_->line_to(&DividerRect, DividerLeftBorderColor, factor, 0L);

         int x1 = tableControl.RightToLeftLayout ? left + TableControl.Factor : left - TableControl.Factor;
         DrawLine(hdc, new Point(x1, rect.top + TableControl.Factor), new Point(x1, headerHeight - TableControl.Factor), ButtonShadowPen);

         //SetRect(&DividerRect, Tmp + factor, Rect.top + factor, Tmp + factor, Rect.top + tableCtrl->TitleHeight - 2 * factor);
         //Gui->ctx->gui_.dc_->line_to(&DividerRect, DividerRightBorderColor, factor, 0L);

         int x2 = tableControl.RightToLeftLayout ? left - TableControl.Factor : left + TableControl.Factor;
         DrawLine(hdc, new Point(x2, rect.top + TableControl.Factor), new Point(x2, headerHeight - TableControl.Factor), WhitePen);
      }

      internal override void PaintBackGround(NativeCustomDraw.NMCUSTOMDRAW pcust, NativeWindowCommon.RECT rect)
      {
         base.PaintBackGround(pcust, rect);

         // draw top lines

         //SetRect(&TitleRect, Rect.left, Rect.top, Rect.right, Rect.top);
         //Gui->ctx->gui_.dc_->line_to(&TitleRect, TitleTopLeftColor, factor, 0L);
         DrawLine(pcust.hdc, new Point(rect.left, rect.top), new Point(rect.right, rect.top), WhitePen); // dark
         
         //SetRect(&TitleRect, Rect.left + factor, Rect.top + factor, Rect.right - factor, Rect.top + factor);
         //Gui->ctx->gui_.dc_->line_to(&TitleRect, FIX_SYS_COLOR_3DLIGHT, factor, 0L);
         DrawLine(pcust.hdc, new Point(rect.left + TableControl.Factor, rect.top + 1), new Point(rect.right, rect.top + TableControl.Factor), lightPen); // dark

         // draw bottom lines
         
         //SetRect(&TitleRect, Rect.left, Rect.top + Ctrl->TitleHeight - 2 * factor, Rect.right, Rect.top + Ctrl->TitleHeight - 2 * factor);
         //Gui->ctx->gui_.dc_->line_to(&TitleRect, TitleBottomBorder, factor, 0L);
         rect.bottom--;
         DrawLine(pcust.hdc, new Point(rect.left, rect.bottom), new Point(rect.right, rect.bottom)); // dark
         
         //SetRect(&TitleRect, Rect.left + factor, Rect.top + Ctrl->TitleHeight - 3 * factor, Rect.right - factor, Rect.top + Ctrl->TitleHeight - 3 * factor);
         //Gui->ctx->gui_.dc_->line_to(&TitleRect, FIX_SYS_COLOR_BTNSHADOW, factor, 0L);
         rect.bottom--;
         DrawLine(pcust.hdc, new Point(rect.left, rect.bottom), new Point(rect.right, rect.bottom), darkpen);

         // draw left lines 

         if (!tableControl.RightToLeftLayout && rect.left == 0) // draw only for 1st section
         {
            //   SetRect(&TitleRect, Rect.left, Rect.top, Rect.right, Rect.top);
            //   Gui->ctx->gui_.dc_->line_to(&TitleRect, TitleTopLeftColor, factor, 0L);
            DrawLine(pcust.hdc, new Point(0, rect.top), new Point(0, rect.bottom), WhitePen);

            //SetRect(&TitleRect, Rect.left + factor, Rect.top + factor, Rect.left + factor, Rect.top + Ctrl->TitleHeight - 3 * factor);
            //Gui->ctx->gui_.dc_->line_to(&TitleRect, FIX_SYS_COLOR_3DLIGHT, factor, 0L);
            DrawLine(pcust.hdc, new Point(1, rect.top + 1), new Point(1, rect.bottom), lightPen);
         }
         else if (tableControl.RightToLeftLayout && rect.right == (tableControl.Bounds.Right - 2 * tableControl.BorderHeight))
         {
            DrawLine(pcust.hdc, new Point(rect.right - 1, rect.top), new Point(rect.right - 1, rect.bottom), WhitePen);

            //SetRect(&TitleRect, Rect.left + factor, Rect.top + factor, Rect.left + factor, Rect.top + Ctrl->TitleHeight - 3 * factor);
            //Gui->ctx->gui_.dc_->line_to(&TitleRect, FIX_SYS_COLOR_3DLIGHT, factor, 0L);
            DrawLine(pcust.hdc, new Point(rect.right - 2, rect.top + 1), new Point(rect.right - 2, rect.bottom), lightPen);

         }
      }
   }
}