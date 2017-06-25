using Controls.com.magicsoftware;
using System.Drawing;
using LgList = System.Collections.Generic.List<Controls.com.magicsoftware.PlacementDrivenLogicalControl>;
using System;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Strategy for Table with multi column display disabled
   /// </summary>
   public class TableMultiColumnDisabledStrategy : TableMultiColumnStrategyBase
   {
      public TableMultiColumnDisabledStrategy(TableControl tableControl)
         :base(tableControl)
      {

      }

      #region overriden methods
      public override void PaintRow(TableChildRendererBase tableChildRenderer, TablePaintRowArgs ea, bool isRowMarked, bool isSelected, ColumnsManager columnsManager)
      {
         Region orgRegion = ea.Graphics.Clip;
         for (int i = ea.CellsData.Count - 1; i >= 0; i--)
         {
            PaintCellBackground(tableChildRenderer, ea.CellsData[i], ea.Graphics, ea.Item, isRowMarked, isSelected, columnsManager);
            PaintCellControls(tableChildRenderer, ea.Item, ea.Graphics, ea.CellsData[i], columnsManager, isRowMarked, isSelected);
         }

         ea.Graphics.Clip = orgRegion;
      }

      /// <summary>
      /// Paint table cells controls
      /// </summary>
      /// <param name="tableRenderer"></param>
      /// <param name="item"></param>
      /// <param name="g"></param>
      /// <param name="cellData"></param>
      /// <param name="columnsManager"></param>
      /// <param name="isRowMarked"></param>
      /// <param name="isSelected"></param>
      private void PaintCellControls(TableChildRendererBase tableChildRenderer, TableItem item, Graphics g, CellData cellData, ColumnsManager columnsManager, bool isRowMarked, bool isSelected)
      {
         //paint cell's controls
         if (item != null)
         {
            LgList tableChildren = getColumnChildren(item, cellData.ColumnIdx, columnsManager);
            if (tableChildren != null)
            {
               foreach (PlacementDrivenLogicalControl lg in tableChildren)
                  tableChildRenderer.PaintControl(lg, cellData.Rect, g, isRowMarked, isSelected, columnsManager);
            }
         }
      }

      /// <summary>
      /// Gets table column children
      /// </summary>
      /// <param name="item"></param>
      /// <param name="GuiColumnIdx"></param>
      /// <param name="columnsManager"></param>
      /// <returns></returns>
      internal LgList getColumnChildren(TableItem item, int GuiColumnIdx, ColumnsManager columnsManager)
      {
         var tableChildren = item.Controls;
         if (tableChildren == null)
            return null;
         else
         {
            if (GuiColumnIdx == TableControl.TAIL_COLUMN_IDX)
               return null;
            int column = columnsManager.getMagicColumnIndex(GuiColumnIdx);
            if (column == -1)
               return null;
            else
               return tableChildren[column];
         }
      }

      #endregion overrided methods
   }
}
