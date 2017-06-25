using com.magicsoftware.win32;
using System;
using System.Drawing;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// class for painting 3d style header
   /// </summary>
   internal abstract class ThreeDStyleHeaderRenderer : HeaderRenderer
   {
      protected static IntPtr WhitePen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, TableControl.Factor,
                             ColorTranslator.ToWin32(Color.White));
         

      protected static IntPtr ButtonShadowPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(SystemColors.ButtonShadow));

      public ThreeDStyleHeaderRenderer(TableControl tableControl, Header header) : base(tableControl , header)
      {
      }

      internal override void GetTitleTextRect(int index, ref NativeWindowCommon.RECT rc)
      {
         // draw_column_titles() from cnt_pnt.cpp
       
         //InflateRect(&Rect, -factor, -factor);
         rc.left++;
         rc.top++;

         // Get_Divider_Cordinates(Gui, Ctrl, col + 1, &paintRect.right, &paintRect.left);
         rc.left = tableControl.GetColumnRectangle(index, false).Left;

         rc.left -= tableControl.BorderHeight; // start from bounds and not clientrectangle 
         rc.top -= tableControl.BorderHeight;

         rc.right = GetTextRightCoordinate(index, rc.left);

         //paintRect.right -= PIX_BETWEEN_DEVIDER_AND_DIT * factor;
         //paintRect.left += PIX_BETWEEN_DEVIDER_AND_DIT * factor
         rc.right -= PIX_BETWEEN_DIVIDER_AND_DIT * TableControl.Factor;
         rc.left += PIX_BETWEEN_DIVIDER_AND_DIT * TableControl.Factor;

         //paintRect.bottom = Rect.top + Ctrl->TitleHeight - PIX_BETWEEN_DEVIDER_AND_DIT;
         //paintRect.top = Rect.top + PIX_BETWEEN_DEVIDER_AND_DIT;
         rc.bottom = rc.top + this.tableControl.TitleHeight - PIX_BETWEEN_DIVIDER_AND_DIT;
         rc.top = rc.top+ PIX_BETWEEN_DIVIDER_AND_DIT * TableControl.Factor;
      }

      protected int GetTextRightCoordinate(int  index , int left)
      {
         // get right coordinate
         return left + tableControl.GetColumnRectangle(index, false).Width;
      }
   }
}