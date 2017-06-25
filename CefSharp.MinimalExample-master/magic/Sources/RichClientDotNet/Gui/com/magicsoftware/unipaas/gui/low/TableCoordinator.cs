using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
using Controls.com.magicsoftware;
using com.magicsoftware.util;

#if PocketPC
using TextBox = com.magicsoftware.controls.MgTextBox;
using Appearance = com.magicsoftware.mobilestubs.Appearance;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// 
   /// </summary>
   internal class TableCoordinator : TableCoordinatorBase, BoundsComputer, IEditorProvider
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="tableManager"></param>
      /// <param name="logicalControl"></param>
      /// <param name="mgColumn"></param>
      internal TableCoordinator(TableManager tableManager, LogicalControl logicalControl, int mgColumn)
         : base(tableManager, logicalControl, mgColumn)
      {
         
      }

      internal LgColumn ColumnManager
      {
         get { return (LgColumn)_tableManager.ColumnsManager.getLgColumnByMagicIdx(MgColumn); }
      }

      #region BoundsComputer Members

      /// <summary>
      ///   implement EditorBoundsComputer interface
      /// </summary>
      public Rectangle computeEditorBounds(Rectangle cellRectangle, bool isHeaderEditor)
      {
         return getDisplayRect(cellRectangle, isHeaderEditor);
      }

      #endregion

      /// <summary>
      ///   return control of tableChild
      /// </summary>
      /// <returns> </returns>
      public Control getEditorControl()
      {
         Control control = null;
         TableEditor editorControl = ((TableManager)_tableManager).getTableEditor((LogicalControl)_logicalControl);
         if (editorControl != null)
            control = editorControl.Control;

         return control;
      }

      /// <summary>
      ///   scrolls table to show control's column
      /// </summary>
      internal void scrollToColumn()
      {
         TableColumn guiColumn = _tableManager.ColumnsManager.getColumnByMagicIdx(MgColumn);
         ((TableManager)_tableManager).getTable().showColumn(guiColumn);
      }

      /// <summary>
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      internal int transformXToTable(int x)
      {
         Rectangle cellRect = GetCellRect(MgColumn, MgRow);

         if (_tableManager.RightToLeftLayout)
            x = cellRect.Width - x;

         return x + cellRect.X;
      }

      /// <summary>
      /// </summary>
      /// <param name="y"></param>
      /// <returns></returns>
      internal int transformYToCell(int y)
      {
         return y - GetTableTop();
      }

      protected override int GetTableTop()
      {
         int tableTop = 0;

         if (((LogicalControl)_logicalControl).GuiMgControl.IsTableHeaderChild)
         {
            if (((TableManager)_tableManager).HasFixed3DBorder())
               tableTop = TableControl.STUDIO_BORDER_WIDTH;
         }
         else
         {
            tableTop = _tableManager.getMgTableTop();
         }

         return tableTop;
      }

      protected override int GetDisplayRectangleLeft(Rectangle cellRect)
      {
         if (((LogicalControl)_logicalControl).GuiMgControl.IsTableHeaderChild && X < 0 && MgColumn == 1)
            return X;
         else
            return base.GetDisplayRectangleLeft(cellRect);
      }

      protected override int GetDisplayRectangleTop(Rectangle cellRect)
      {
         int top = 0;

         if (((LogicalControl)_logicalControl).GuiMgControl.IsTableHeaderChild && Y < 0)
            top = Y;
         else
            top = base.GetDisplayRectangleTop(cellRect);            

         return top;
      }

      /// <summary>
      /// </summary>
      /// <param name="y"></param>
      /// <returns></returns>
      internal int transformYToTable(int y)
      {
         Rectangle cellRect = GetCellRect(MgColumn, MgRow);
         int top = 0;
         if (((LogicalControl)_logicalControl).GuiMgControl.IsTableHeaderChild)
            top = 0;
         else
            top = cellRect.Top;
         return y + top;
      }

      public override Rectangle getDisplayRect(Rectangle cellRect, bool isHeaderEditor)
      {
         var rect = new Rectangle(0, 0, 0, 0);
         if (_logicalControl is Line)
            rect = ((Line)_logicalControl).calcDisplayRect();
         else
            return base.getDisplayRect(cellRect, isHeaderEditor);

         return rect;
      }

      protected override bool IsComboControl()
      {
         return _logicalControl is LgCombo;
      }

      /// <summary>
      /// Returns is combobox is 2D combobox
      /// </summary>
      /// <returns></returns>
      protected override bool Is2DComboBoxControl()
      {
         return (IsComboControl() && ((_logicalControl as LgCombo).Style == ControlStyle.TwoD));
      }

      public override Rectangle GetCellRect(int mgColumn, int mgRow)
      {
         return ((TableManager)_tableManager).getCellRect(mgColumn, mgRow);
      }

      public override void Refresh(bool changed)
      {
         if (changed && _tableManager != null && ((LogicalControl)_logicalControl).GuiMgControl.IsTableHeaderChild)
            ((TableManager)_tableManager).RefreshHeader = true;
         else
            base.Refresh(changed);
      }
   }
}
