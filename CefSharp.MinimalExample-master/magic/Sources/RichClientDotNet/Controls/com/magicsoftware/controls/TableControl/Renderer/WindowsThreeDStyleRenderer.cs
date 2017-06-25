using com.magicsoftware.controls;
using System.Drawing;
using System;
using System.Windows.Forms;

namespace Controls.com.magicsoftware.controls.Renderer
{
   internal class WindowsThreeDStyleRenderer : TableStyleRendererBase
   {
      public WindowsThreeDStyleRenderer(TableControl tableControl)
         : base(tableControl)
      {
         RowDividerRenderer = new WindowsStyleRowDividerRenderer();
         ColumnDividerRenderer = new WindowsThreeDColumnDividerRenderer();
         BorderRenderer = new WindowsThreeDBorderRenderer();
      }
      
      internal override int GetColumnWidthFromSectionWidth(int i, Header _header)
      {
         if (i == 0)
            return _header.Sections[i].Width - 2;
         else
            return base.GetColumnWidthFromSectionWidth(i, _header);
      }

      /// <summary>
      /// set section width 
      /// </summary>
      /// <param name="headerSection"></param>
      /// <param name="index"></param>
      /// <param name="width"></param>
      public override int GetHeaderSectionWidthFromColumnWidth(int index, int width)
      {
         if (index == 0)
            return width + 2;
         else
            return base.GetHeaderSectionWidthFromColumnWidth(index, width);
      }

      /// <summary>
      ///  return border size
      /// </summary>
      /// <returns></returns>
      internal override int GetBorderSize()
      {
         return SystemInformation.Border3DSize.Width;
      }

      protected override void AdjustColumnWidth(int col, ref Rectangle rect)
      {
         if (tableControl.RightToLeftLayout)
         {
            // since there is only 1 border on right side in case of RTL, move the columns
            rect.X -= TableControl.Factor;
            rect.Width++;
         }
      }


      protected override Rectangle GetClientRectangle()
      {
         Rectangle rect = base.GetClientRectangle();
         // since there is only 1 border on right side in case of RTL
         if (tableControl.RightToLeftLayout)
            rect.Width++;
         return rect;
      }
   }
}