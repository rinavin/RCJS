using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using Controls.com.magicsoftware;
using System;
using System.Drawing;
using LgList = System.Collections.Generic.List<Controls.com.magicsoftware.PlacementDrivenLogicalControl>;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Base strategy for Table with multi column display
   /// </summary>
   public abstract class TableMultiColumnStrategyBase
   {

      protected TableControl tableControl;

      public TableMultiColumnStrategyBase(TableControl tableControl)
      {
         this.tableControl = tableControl;
      }

      #region methods

      /// <summary>
      /// Brings editor control to front
      /// </summary>
      /// <param name="editor"></param>
      public virtual void BringEditorToFront(TableEditor editor)
      {
      }

      /// <summary>
      /// paint table row
      /// </summary>
      public abstract void PaintRow(TableChildRendererBase tableChildRenderer, TablePaintRowArgs ea, bool isRowMarked, bool isSelected, ColumnsManager columnsManager);

      /// <summary>
      /// paint table cell background
      /// </summary>
      public void PaintCellBackground(TableChildRendererBase tableChildRenderer, CellData cellData, Graphics g, TableItem item, bool isRowMarked, bool isSelected, ColumnsManager columnsManager)
      {
         using (var cellRegion = new Region(cellData.Rect))
         {
            g.Clip = GetClippingArea(cellRegion, g);
            //The paining of table rows is done from TableControl.OnPaint(). We should paint selected and marked rows from here.
            if (isRowMarked)
            {
               if (tableChildRenderer.RowHighlitingStyle == RowHighlightType.BackgroundControls
                   || tableChildRenderer.RowHighlitingStyle == RowHighlightType.Background)
               {
                  Color color = tableChildRenderer.HightlightBgColor;

                  //paint cell background
                  Brush brush = SolidBrushCache.GetInstance().Get(Utils.GetNearestColor(g, color));
                  g.FillRectangle(brush, cellData.Rect);
               }
            }
         }
      }

      /// <summary>
      /// Gets clipping area for cells background painting
      /// Is strategy is disabled/base or it is the last column control - the region is the cell.
      /// </summary>
      /// <param name="region"></param>
      /// <param name="g"></param>
      /// <param name="columnIdx"></param>
      /// <returns></returns>
      public virtual Region GetClippingArea(Region region, Graphics g)
      {
         return region;
      }

      /// <summary>
      /// Returns rectToIntersect intersected with draw area.
      /// </summary>
      /// <param name="rectToIntersect"></param>
      /// <param name="lg"></param>
      public virtual Rectangle GetRectToDraw(Rectangle controlRect, Rectangle cellRect)
      {
         Rectangle drawRect = GetDrawArea(cellRect);
         controlRect.Intersect(drawRect);
         return controlRect;
      }

      

      /// <summary>
      /// Gets area to draw a control
      /// </summary>
      /// <param name="lg"></param>
      /// <returns></returns>
      protected virtual Rectangle GetDrawArea(Rectangle cellRect)
      {
         return cellRect;
      }

      /// <summary>
      /// Gets list of column childrens
      /// </summary>
      /// <param name="item"></param>
      /// <param name="column"></param>
      /// <returns></returns>
      public virtual LgList GetColumnChildren(TableItem item, int column, ColumnsManager columnsManager)
      {
         return item.getColumnChildren(column, columnsManager);
      }

      #endregion methods
   }
}