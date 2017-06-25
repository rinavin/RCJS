using Controls.com.magicsoftware;
using System.Drawing;
using LgList = System.Collections.Generic.List<Controls.com.magicsoftware.PlacementDrivenLogicalControl>;
using System;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Strategy for Table with multi column display enabled
   /// </summary>
   public class TableMultiColumnEnabledStrategy : TableMultiColumnStrategyBase
   {
      public TableMultiColumnEnabledStrategy(TableControl tableControl)
         : base(tableControl)
      {

      }

      #region overriden methods

      public override void BringEditorToFront(TableEditor editor)
      {
         editor.Control.BringToFront();
      }
      public override void PaintRow(TableChildRendererBase tableChildRenderer, TablePaintRowArgs ea, bool isRowMarked, bool isSelected, ColumnsManager columnsManager)
      {
         Region orgRegion = ea.Graphics.Clip;
         Region tableRegion = tableControl.isHscrollShown() ? orgRegion : GetTableColumnsRegion();

         for (int i = ea.CellsData.Count - 1; i >= 0; i--)
         {
            PaintCellBackground(tableChildRenderer, ea.CellsData[i], ea.Graphics, ea.Item, isRowMarked, isSelected, columnsManager);
         }
         ea.Graphics.Clip = tableRegion;
         PaintRowControls(tableChildRenderer, ea.Graphics, ea.Item, isRowMarked, isSelected, columnsManager);
         ea.Graphics.Clip = orgRegion;
         tableRegion.Dispose();
      }

      /// <summary>
      /// Paint table controls for row
      /// </summary>
      /// <param name="tableRenderer"></param>
      /// <param name="g"></param>
      /// <param name="item"></param>
      /// <param name="isRowMarked"></param>
      /// <param name="isSelected"></param>
      /// <param name="columnsManager"></param>
      private void PaintRowControls(TableChildRendererBase tableChildRenderer, Graphics g, TableItem item, bool isRowMarked, bool isSelected, ColumnsManager columnsManager)
      {
         if (item != null)
         {
            item.ZOrderSortedControls = item.ZOrderSortedControls != null && item.ZOrderSortedControls.Count > 0 ? item.ZOrderSortedControls : item.GetAllColumnsControlsByZOrder();
            if (item.ZOrderSortedControls != null)
            {
               foreach (PlacementDrivenLogicalControl lg in item.ZOrderSortedControls)
                  tableChildRenderer.PaintControl(lg, tableChildRenderer.GetCellRect(lg), g, isRowMarked, isSelected, columnsManager);
            }
         }
      }

      protected override Rectangle GetDrawArea(Rectangle cellRect)
      {
         return GetTableColumnsRectangle();
      }

      public override Region GetClippingArea(Region region, Graphics g)
      {
         return g.Clip;
      }

      /// <summary>
      /// Gets table columns region
      /// </summary>
      /// <returns></returns>
      private Region GetTableColumnsRegion()
      {
         return new Region(GetTableColumnsRectangle());
      }

      public override Rectangle GetRectToDraw(Rectangle controlRect, Rectangle cellRect)
      {
         Rectangle drawRect = GetDrawArea(cellRect);
         return CutRectSides(drawRect, controlRect);
      }

      /// <summary>
      /// Intersect rects only left/right sides
      /// </summary>
      /// <param name="drawArea"></param>
      /// <param name="controlRect"></param>
      /// <returns></returns>
      private Rectangle CutRectSides(Rectangle drawArea, Rectangle controlRect)
      {
         int x = Math.Max(drawArea.X, controlRect.X);
         int right = Math.Min(drawArea.Right, controlRect.Right);

         return new Rectangle(x, controlRect.Y, right - x, controlRect.Height);
      }
      /// <summary>
      /// Gets table columns rectangle
      /// </summary>
      /// <returns></returns>
      public Rectangle GetTableColumnsRectangle()
      {
         if (tableControl.ColumnCount > 0)
         {
            int firstColumnIdx = tableControl.RightToLeftLayout ? tableControl.ColumnCount - 1 : 0;
            Rectangle columnsRect = tableControl.GetColumnRectangle(firstColumnIdx, true);

            foreach (TableColumn col in tableControl.Columns)
            {
               if (col.Index != firstColumnIdx)
                  columnsRect = Rectangle.Union(columnsRect, tableControl.GetColumnRectangle(col.Index, true));
            }
            //fix for the last table line + add header 
            columnsRect.Height = 2 * columnsRect.Height + tableControl.GetHeaderRectangle().Height;
            columnsRect.Y = 0;

            return columnsRect;
         }
         return new Rectangle();
      }

      public override LgList GetColumnChildren(TableItem item, int column, ColumnsManager columnsManager)
      {
         return item.GetAllColumnsControlsByZOrder();
      }

      #endregion overriden methods
   }
}
