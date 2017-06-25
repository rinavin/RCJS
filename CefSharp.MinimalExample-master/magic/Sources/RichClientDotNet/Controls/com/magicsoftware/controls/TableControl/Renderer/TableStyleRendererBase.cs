using com.magicsoftware.controls;
using System.Drawing;
using System;
using com.magicsoftware.util;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// Renders different style of Table Control
   /// </summary>
   public abstract class TableStyleRendererBase : IDisposable
   {
      protected TableControl tableControl;
      public IColumnDividerRenderer ColumnDividerRenderer;
      public IRowDividerRenderer RowDividerRenderer;
      protected IBorderRenderer BorderRenderer;

      public TableStyleRendererBase(TableControl tableControl)
      {
         this.tableControl = tableControl;
      }

      /// <summary>
      /// get the size of border
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      internal abstract int GetBorderSize();

      /// <summary>
      /// get the color of divider
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      internal virtual Color GetDividerColor()
      {
         return tableControl.DividerColor;
      }

      /// <summary>
      /// get the no of column dividers
      /// </summary>
      /// <param name="header"></param>
      /// <returns></returns>
      internal virtual int GetNumberOfColumnDividers(Header header)
      {
         return header.Sections.Count - 1;
      }

      
      /// <summary>
      /// get column rectangle
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="header"></param>
      /// <param name="col"></param>
      /// <param name="forLineArea"></param>
      /// <returns></returns>
      internal Rectangle GetColumnRectangle(Header header, int col, bool forLineArea)
      {
         Rectangle rect = new Rectangle();
         int i = 0;
         int maxCol = col;

         if (col == TableControl.TAIL_COLUMN_IDX)
            maxCol = tableControl.Columns.Count;

         //rect.X = GetFirstColumnXOffset(tableControl);
         rect.X = GetClientRectangle().X;

         while (i < maxCol)
            rect.X += tableControl.Columns[i++].Width;

         if (col == TableControl.TAIL_COLUMN_IDX)
            rect.Width = GetClientRectangle().Right - rect.X;
         else
            rect.Width = tableControl.Columns[col].Width;

         //handle show lines
         if (tableControl.ShowColumnDividers)
            AdjustColumnWidth(col, ref rect);

         if (tableControl.RightToLeftLayout && forLineArea)
            tableControl.mirrorRectangle(ref rect);

         if (forLineArea)
            tableControl.AdjustXCorner(ref rect);
         
         rect.Y = header.Height;
         rect.Height = GetClientRectangle().Bottom - header.Height;
         return rect;
      }

      /// <summary>
      /// adjust width of columns
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="col"></param>
      /// <param name="rect"></param>
      protected virtual void AdjustColumnWidth(int col, ref Rectangle rect) { }

      
      /// <summary>
      /// get the top of cell
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="_topIndex"></param>
      /// <param name="row"></param>
      /// <returns></returns>
      internal int GetCellTop(int _topIndex, int row)
      {
         return GetClientRectangle().Y + tableControl.TitleHeight + ((row - _topIndex) * tableControl.RowHeight);
      }

      /// <summary>
      /// get color of cell
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="row"></param>
      /// <param name="colIndex"></param>
      /// <returns></returns>
      internal virtual Color GetCellColor(int row, int colIndex)
      {
         Color color = tableControl.BgColor;

         if (tableControl.ShouldCheckAlternateOrColumnColor(row))
         {
            if (tableControl.IsRowAlternate(row))
               color = tableControl.AlternateColor;
            else if (tableControl.IsColoredByRow)
               color = tableControl.GetColorbyRow(row);
            else
            {
               // paint with column color
               color = ColumnColor(colIndex);
            }
         }
         return color;
      }

      /// <summary>
      /// calc color of column
      /// </summary>
      /// <param name="guiColumn"></param>
      /// <returns></returns>
      private Color ColumnColor(int guiColumn)
      {
         Color color = tableControl.BgColor;
         if (tableControl.ColorBy == TableColorBy.Column)
         {
            if (guiColumn != TableControl.TAIL_COLUMN_IDX)
               color = tableControl.getColumn(guiColumn).BgColor;
            else
            {
               //If _colorBy == TableColorBy.Column, table color is set to transparent. So, for the the extra 
               //area which is outside the columns will also become transparent. To avoid this, use default 
               //color for the extra area.
               color = tableControl.BgColor;
            }
         }
         return color;
      }

      /// <summary>
      /// paint the row and column dividers 
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="header"></param>
      /// <param name="g"></param>
      /// <param name="pen"></param>
      internal virtual void PaintDividers(Header header, Graphics g, Pen pen)
      {
         if (tableControl.ShowColumnDividers)
            DrawColumnDividers(header, g, pen);
         if (tableControl.ShowLineDividers)
            DrawLineDividers(header, g, pen);
      }

      // draw border of table
      internal void DrawBorder(Graphics g, Pen dividerPen)
      {
         if (BorderRenderer != null && PaintBorder)
            BorderRenderer.DrawBorder(g, dividerPen, tableControl.BorderHeight, tableControl.Bounds, tableControl.RightToLeftLayout);
      }

      /// <summary>
      /// paint row dividers
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="header"></param>
      /// <param name="g"></param>
      /// <param name="pen"></param>
      protected void DrawLineDividers(Header header, Graphics g, Pen pen)
      {
         //draw lines
         //Point corner = GetCorner();
         if (tableControl.RowHeight > 0)
         {
            int y = GetClientRectangle().Top + tableControl.TitleHeight - 1;

            if (tableControl.RowHeight > 0)
            {
               while (y < tableControl.GetTotalRowDividerHeight())
               {
                  if (y > tableControl.TitleHeight) // don't paint the divider inside header
                     RowDividerRenderer.Render(g, pen, tableControl.BorderHeight, TableControl.Factor, GetClientRectangle().Left, y, GetClientRectangle().Right -1 );
                  y += tableControl.RowHeight;
               } 
            }
         }
      }

      /// <summary>
      /// draw column divider
      /// </summary>
      /// <param name="g"></param>
      protected void DrawColumnDividers(Header header, Graphics g, Pen pen)
      {
         Point corner = tableControl.GetCorner();
         int columnPos = 0;

         //draw dividers
         for (int i = 0; i < GetNumberOfColumnDividers(header); i++)
         {
            HeaderSection headerSection = header.Sections[i];
            columnPos += tableControl.Columns[i].Width; ;
            // int x = tableControl.tranlateXCoordinate(columnPos) - _corner.X;
            int x = GetColumnX(header, columnPos, i);
            ColumnDividerRenderer.Render(g, tableControl, pen, x, header.Height + GetClientRectangle().Top, GetClientRectangle().Bottom );
         }
      }

      /// <summary>
      ///  get the x cordinate for painting dividers
      /// </summary>
      /// <param name="header"></param>
      /// <param name="columnPos"></param>
      /// <param name="i"></param>
      /// <returns></returns>
      internal virtual int GetColumnX(Header header, int columnPos, int i)
      {
         return tableControl.RightToLeftLayout ? GetColumnRectangle(header, i, true).Left : GetColumnRectangle(header, i, true).Right;
      }

      public virtual int GetHeaderSectionWidthFromColumnWidth(int index, int width)
      {
         return width;
      }

      internal virtual int GetColumnWidthFromSectionWidth(int i, Header header)
      {
         return header.Sections[i].Width;
      }


      /// <summary>
      /// get rectangle for header
      /// </summary>
      /// <param name="header"></param>
      /// <returns></returns>
      internal virtual Rectangle GetHeaderRectangle(Header header)
      {
         // Since we paint borders , move the header by 2 pixels
         int borderSize = tableControl.BorderHeight * TableControl.Factor;
         return new Rectangle(new Point(borderSize, borderSize),
                              new Size(GetClientRectangle().Width, header.GetHeight(tableControl.TitleHeight)));
      }


      /// <summary>
      /// get the client rectangle 
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      protected virtual Rectangle GetClientRectangle()
      {
         Rectangle clientRectangle = tableControl.ClientRectangle;
         clientRectangle.Inflate(-tableControl.BorderHeight * TableControl.Factor, -tableControl.BorderHeight * TableControl.Factor);
         return clientRectangle;
      }

      public void Dispose()
      {

      }

      /// <summary>
      ///  indicate if border should be painted
      /// </summary>
      protected virtual bool PaintBorder 
      {
         get { return true; }
      }

   }
}