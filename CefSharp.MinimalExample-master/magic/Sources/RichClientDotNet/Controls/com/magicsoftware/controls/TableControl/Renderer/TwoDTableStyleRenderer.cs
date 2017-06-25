using com.magicsoftware.controls;
using com.magicsoftware.util;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// class for rendering 2d style table control
   /// </summary>
   public class TwoDTableStyleRenderer : TableStyleRendererBase
   {
      public static int MARGIN = 4; // #define COL_DIVIDER 4 (win.h)

      /// <summary>
      /// Renderer for two-d table
      /// </summary>
      public TwoDTableStyleRenderer(TableControl tableControl)
         : base (tableControl)
      {
         ColumnDividerRenderer = new TwoDStyleColumnDividerRenderer();
         RowDividerRenderer = new TwoDStyleRowDividerRenderer();
         BorderRenderer = new TwoDStyleBorderRenderer();
      }

      /// <summary>
      /// get the border size
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      internal override int GetBorderSize()
      {
         switch (tableControl.BorderType)
         {
            case BorderType.Thick: 
               return 2; // For thick border , width is 2

            case BorderType.Thin:
               return 1; // For thin border , width is 1

            case BorderType.NoBorder:
            default:
               return 0;
         }
      }
      /// <summary>
      /// divider color is ForeColor
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      internal override Color GetDividerColor()
      {
         return tableControl.ForeColor;
      }

      /// <summary>
      ///  indicate if border should be painted
      /// </summary>
      protected override bool PaintBorder
      {
         get { return tableControl.BorderType != BorderType.NoBorder; }
      }

      protected override void AdjustColumnWidth(int col, ref Rectangle rect)
      {
         // Shift the columns between first and last by 1 pixel and reduce their width
         if (col != 0 && col != TableControl.TAIL_COLUMN_IDX)
         {
            rect.X += TableControl.Factor;
            rect.Width--;
         }

         //  Get_Column_Divider() 
         //  if (!CTRL_STYLE_IS_WINDOWS(tableCtrl->Style))
         //    if (colIndx == (long)GetLastAvailableColumn(tableCtrl))
         //  *right = MAX(ClientRect.right, *right);
         if (col == tableControl.Columns.Count - 1)
            rect.Width = Math.Min(GetClientRectangle().Right - rect.X, rect.Width);
      }

      /// <summary>
      /// get columnwidth from section width
      /// </summary>
      /// <param name="i"></param>
      /// <param name="_header"></param>
      /// <returns></returns>
      internal override int GetColumnWidthFromSectionWidth(int i, Header _header)
      {
         if (i == 0)
            return _header.Sections[i].Width - 1;
         else
            return base.GetColumnWidthFromSectionWidth(i, _header);
      }

      /// <summary>
      /// set section width 
      /// </summary>
      /// <param name="headerSection"></param>
      /// <param name="index"></param>
      /// <param name="width"></param>
      public override int GetHeaderSectionWidthFromColumnWidth( int index, int width)
      {
         if (index == 0)
            return width + 1;
         else
            return base.GetHeaderSectionWidthFromColumnWidth(index, width);
      }

      internal override int GetColumnX(Header header, int columnPos, int i)
      {
         int x = base.GetColumnX(header, columnPos, i);

         // GetColumnDividers() of column.cpp
         //if (tableCtrl->Style & CTRL_STYLE_HEBREW)
         //{
         //   if (colIndx != tableCtrl->Columns)
         //      *left -= factor;
         //}
         if (tableControl.RightToLeftLayout && i != 0)
            x--;

         return x;
      }
   }
}