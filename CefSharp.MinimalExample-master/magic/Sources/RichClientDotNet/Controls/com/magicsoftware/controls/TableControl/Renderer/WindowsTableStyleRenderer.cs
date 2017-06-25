using com.magicsoftware.controls;
using com.magicsoftware.util;
using System.Drawing;
using System.Windows.Forms;
using System;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   ///  Renders Windows style Table Control
   /// </summary>
   public class WindowsTableStyleRenderer : TableStyleRendererBase
   {
      public WindowsTableStyleRenderer(TableControl tableControl)
         : base(tableControl)
      {
         ColumnDividerRenderer = GetColumnDividerRenderer();
         RowDividerRenderer = new WindowsStyleRowDividerRenderer();
      }

      /// <summary>
      /// get the column divider
      /// </summary>
      /// <returns></returns>
      protected virtual IColumnDividerRenderer GetColumnDividerRenderer()
      {
         return new WindowsStyleColumnDividerRenderer();
      }

      /// <summary>
      /// adjust the width of columns
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="col"></param>
      /// <param name="rect"></param>
      protected override void AdjustColumnWidth(int col, ref Rectangle rect )
      {
         // reduce the width by 1 for all columns except last
          if (col != TableControl.TAIL_COLUMN_IDX && tableControl.Columns[col].HeaderSection.RightBorder)
            rect.Width--;
      }
          
      /// <summary>
      ///  return size of border
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      internal override int GetBorderSize()
      {
         switch (tableControl.BorderStyle)
         {
            case BorderStyle.Fixed3D:
               return SystemInformation.Border3DSize.Height;

            case BorderStyle.FixedSingle:
               return SystemInformation.BorderSize.Height;

            case BorderStyle.None:
            default:
               return 0;
         }
      }
     
      /// <summary>
      /// return no of column divider
      /// </summary>
      /// <param name="header"></param>
      /// <returns></returns>
      internal override int GetNumberOfColumnDividers(Header header)
      {
         return header.Sections.Count;
      }

      /// <summary>
      /// return the rectangle  of header
      /// </summary>
      /// <param name="header"></param>
      /// <returns></returns>
      internal override Rectangle GetHeaderRectangle(Header header)
      {
         int width = header.Width;
         int logWidth = tableControl.getLogWidth();
         int curWidth = tableControl.ClientSize.Width;
         if (width < curWidth)
            width = curWidth;
         else
         {
            if (logWidth < curWidth)
               width = curWidth;
            else
               width = logWidth;
         }

         return new Rectangle(Point.Empty, new Size(width, header.GetHeight(tableControl.TitleHeight)));
      }

      /// <summary>
      /// Get the client rectangle
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      protected override Rectangle GetClientRectangle()
      {
         return tableControl.ClientRectangle;
      }
      /// <summary>
      /// Get divider coordinate
      /// </summary>
      /// <param name="header"></param>
      /// <param name="columnPos"></param>
      /// <param name="i"></param>
      /// <returns></returns>
      internal override int GetColumnX(Header header, int columnPos, int i)
      {
         return tableControl.GetColumnX(columnPos);
      }
   }
}