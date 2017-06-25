using System;
using System.Collections.Generic;
using com.magicsoftware.controls;
using System.Windows.Forms;
using System.Collections;
using LgList = System.Collections.Generic.List<Controls.com.magicsoftware.PlacementDrivenLogicalControl>;
using System.Drawing;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// manages TableControl for limited items
   /// </summary>
   internal class TableManagerLimitedItems : TableManager
   {
      /// <summary>
      /// while table in the suspended mode it the refreshtable is not perform
      /// In suspenedRefreshLevel we save maximum refresh level for the table, to be used in resume paint
      /// </summary>
      RefreshLevel suspenedRefreshLevel; 
      TableControlLimitedItems TableControlLimitedItems { get { return (TableControlLimitedItems)_tableControl; } }

      // SuspendPaint indicates if painting was explicitly suspended. This should not be confused with
      // Locking/Unlocking window update while dragging.

      internal override void  SuspendPaint()
      {
         suspenedRefreshLevel = RefreshLevel.None;
         TableControlLimitedItems.SuspendPaint = true;
      }


      internal override void ResumePaint()
      {
         TableControlLimitedItems.SuspendPaint = false;

         //since refresh table was prevented during suspend, we must execute it now according to its level
         switch (suspenedRefreshLevel)
         {
            case RefreshLevel.None:
               break;
            case RefreshLevel.Rows:
               refreshTable(false);
               break;
            case RefreshLevel.All:
               refreshTable(true);
               break;
            default:
               break;
         }
         suspenedRefreshLevel = RefreshLevel.None;
      }

      /// <summary>
      /// Ctor
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="mgControl"></param>
      /// <param name="children"></param>
      /// <param name="columnsCount"></param>
      /// <param name="style"></param>
      internal TableManagerLimitedItems(TableControl tableControl, GuiMgControl mgControl, List<GuiMgControl> children,
                      int columnsCount, int style)
         : base(tableControl, mgControl, children, columnsCount, style)
      {
         // must be inited to zero. If font of table not refreshed intitally, rollback this change.
         _rowsInPage = 0;
      }

      /// <summary> 
      /// vertical scroll
      /// </summary>
      /// <param name="ea"></param>
      internal override void ScrollVertically(ScrollEventArgs ea)
      {
         bool isPageScroll = (ea.Type == ScrollEventType.LargeDecrement || ea.Type == ScrollEventType.LargeIncrement);

         // invoke scroll table event
         Events.OnScrollTable(_mgControl, 0, ea.NewValue - ea.OldValue, isPageScroll, false, false);
      }

      /// <summary>
      /// gets the items count of table control
      /// </summary>
      /// <returns></returns>
      internal int GetTableControlItemsCount()
      {
         return _tableControl.getItemsCount();
      }

      /// <summary>return the number of hidden rows (partially or fully) in table</summary>
      /// <returns></returns>
      internal int GetHiddenRowsCountInTable()
      {
         return TableControlLimitedItems.HiddenRowsInPage;
      }

      /// <summary>
      /// sets the items count of table control
      /// </summary>
      /// <param name="count"></param>
      internal void SetTableControlItemsCount(int count)
      {
#if !PocketPC
         //prevent unnecessary resizes
         _tableControl.Layout -= TableHandler.getInstance().LayoutHandler;
#endif

         _tableControl.SetItemsCount(count);

#if !PocketPC
         //prevent unnecessary resizes
         _tableControl.Layout += TableHandler.getInstance().LayoutHandler;
#endif
      }

      /// <summary>
      /// sets the virtual items count of table control
      /// </summary>
      /// <param name="count">the count to set</param>
      internal override void SetTableVirtualItemsCount(int count)
      {
         _tableControl.SetVirtualItemsCount(count);
      }

      /// <summary>
      /// set vertical scroll thumb position of table control
      /// </summary>
      /// <param name="pos"></param>
      internal override void SetVScrollThumbPos(int pos)
      {
         _tableControl.SetVScrollThumbPos(pos);
         if (_tableControl.HasAlternateColor)
            _tableControl.Invalidate();
      }

      /// <summary>
      /// set page size for vertical scrollbar
      /// </summary>
      /// <param name="pageSize"></param>
      internal override void SetVScrollPageSize(int pageSize)
      {
         _tableControl.SetVScrollPageSize(pageSize);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newRowsInPage"></param>
      internal override bool resize()
      {
         int orgNumberOfRowsInPage = _rowsInPage;
         bool success = base.resize();

         if (success)
         {
            _inResize = true;

            if (_rowsInPage != 0 && orgNumberOfRowsInPage != _rowsInPage)
               Events.OnTableResize(_mgControl, _rowsInPage);

            _inResize = false;
         }

         return success;
      }

      /// <summary>
      /// sets Table Items Count
      /// </summary>
      /// <param name="newCount">the count to set</param>
      internal override void SetTableItemsCount(int newCount)
      {
         _mgCount = newCount;

         SetTableControlItemsCount(newCount);
      }

      /// <summary>
      /// remove rows from table control
      /// </summary>
      /// <param name="rowStart">the index at which to begin remove</param>
      /// <param name="rowsToRemove">number of rows to remove</param>
      internal override void RemoveRows(int rowStart, int rowsToRemove, bool manipulateEditors)
      {
         int numOfRows = _tableControl.getItemsCount();

         if (rowStart < 0 || rowStart >= numOfRows && rowsToRemove <= 0)
            return;

         if (rowStart + rowsToRemove > numOfRows)
            rowsToRemove = numOfRows - rowStart;

         _mgCount -= rowsToRemove;

         for (int idx = rowStart; idx < rowStart + rowsToRemove; idx++)
            _tableControl.RemoveItem(getGuiRowIndex(rowStart));

         if (manipulateEditors)
         {
            // if allowTesting, do not perform remove, use shiftrows instead.
            if (GuiUtils.AccessTest)
               ShiftEditors(rowStart, -rowsToRemove);
            else
               RemoveEditorsForVisibleColumns(rowStart, rowsToRemove);
         }

         // update controlsMap
         for (int i = 0; i < _children.Count; i++)
         {
            if (!_children[i].IsTableHeaderChild)
            {
               ArrayList arrayControl = controlsMap.object2WidgetArray(_children[i], GuiConstants.ALL_LINES);

               // lg.Dispose() can remove entries from controlsMap and hence arrayControl can be null.
               // In that case no need to continue even for other columns in same row.
               if (arrayControl == null)
                  break;

               for (int idx = 0; idx < rowsToRemove && rowStart < arrayControl.Count; idx++)
               {
                  long entriesBeforeDispose = arrayControl.Count;
                  LogicalControl lg = (LogicalControl)arrayControl[rowStart] ?? null;
                  if (lg != null)
                  {
                     // As we delete entries from arrayControl, we should adjust _mgRow in
                     // logical controls. _mgRow is used as index to remove Control from map
                     lg._mgRow = rowStart;
                     lg.Dispose();
                  }

                  // LogicalControl.Dispose removes entry from the map only all entries after current
                  // entry are null. If Dispose has not removed the entry, then remove it.
                  if (entriesBeforeDispose == arrayControl.Count)
                     arrayControl.RemoveAt(rowStart);
               }

               for (int j = rowStart; j < arrayControl.Count; j++)
               {
                  LogicalControl lg = (LogicalControl)arrayControl[j] ?? null;
                  if (lg != null)
                  {
                     lg._mgRow = j;
                     ((TableCoordinator)lg.Coordinator).MgRow = j;

                     TableEditor editor = getTableEditor(lg);
                     // reapply properties on the editor
                     if (editor != null && editor.Control != null)
                     {
                        TagData tagData = (TagData)(((Control)editor.Control).Tag);
                        if (tagData.MapData is MapData)
                        {
                           MapData mapData = (MapData)tagData.MapData;
                           int row = mapData.getIdx();
                           row = j;
                           mapData.setIdx(row);
                        }
                     }
                  }
               }
            }
         }

         // remove from _rowsToRefresh
         for (int idx = rowStart; idx < rowStart + rowsToRemove; idx++)
         {
            if (_rowsToRefresh.Contains(getGuiRowIndex(rowStart)))
               _rowsToRefresh.Remove(getGuiRowIndex(rowStart));
         }
      }

      /// <summary>
      /// insert rows into table control
      /// </summary>
      /// <param name="rowStart">the index at which to begin insert</param>
      /// <param name="rowsToInsert">number of rows to be inserted</param>
      internal override void InsertRows(int rowStart, int rowsToInsert, bool manipulateEditors)
      {
         if (rowStart < 0 || rowsToInsert <= 0)
            return;

         _mgCount += rowsToInsert;

         for (int idx = rowStart; idx < rowStart + rowsToInsert; idx++)
            _tableControl.InsertItem(getGuiRowIndex(rowStart));

         if (manipulateEditors)
         {
            // if allowTesting, do not perform insert, use shiftrows instead.
            if (GuiUtils.AccessTest)
               ShiftEditors(rowStart, rowsToInsert);
            else
               InsertEditorsForVisibleColumns(rowStart, rowsToInsert);
         }

         // move rows after rowStart in controlsMap
         for (int i = 0; i < _children.Count; i++)
         {
            if (!_children[i].IsTableHeaderChild)
            {
               ArrayList arrayControl = controlsMap.object2WidgetArray(_children[i], GuiConstants.ALL_LINES);

               if (arrayControl == null)
                  break;

               if (rowStart <= arrayControl.Count)
               {
                  for (int idx = rowStart; idx < rowStart + rowsToInsert; idx++)
                  {
                     arrayControl.Insert(rowStart, null);
                  }
               }

               for (int j = rowStart + rowsToInsert; j < arrayControl.Count; j++)
               {
                  LogicalControl lg = (LogicalControl)arrayControl[j] ?? null;
                  if (lg != null)
                  {
                     lg._mgRow = j;
                     ((TableCoordinator)lg.Coordinator).MgRow = j;

                     TableEditor editor = getTableEditor(lg);
                     // reapply properties on the editor
                     if (editor != null && editor.Control != null)
                     {
                        TagData tagData = (TagData)(((Control)editor.Control).Tag);
                        if (tagData.MapData is MapData)
                        {
                           MapData mapData = (MapData)tagData.MapData;
                           int row = mapData.getIdx();
                           row = j;
                           mapData.setIdx(row);
                        }
                     }
                  }
               }
            }
         }
      }

      /// <summary>
      /// gets the number of rows to refresh
      /// </summary>
      /// <returns></returns>
      protected override int GetNumberOfRowsToRefresh()
      {
         int count = _tableControl.getItemsCount();
         if (count > _rowsInPage)
            count = _rowsInPage;
         return count;
      }

      /// <summary>
      /// insert into editors for visible columns
      /// </summary>
      /// <param name="rowStart">the index at which to begin insert</param>
      /// <param name="rowsToInsert">number of rows to be inserted</param>
      private void InsertEditorsForVisibleColumns(int rowStart, int rowsToInsert)
      {
         // insert in editors
         List<TableEditor> colEditors = null;
         GuiMgControl guiMgControl = null;
         for (int i = 0; i < _editors.Length; i++)
         {
            guiMgControl = _children[_editor2ConrolMap[i]];
            if (isColumnVisible(guiMgControl))
            {
               colEditors = _editors[i];

               for (int idx = rowStart; idx < rowStart + rowsToInsert; idx++)
                  colEditors.Insert(idx, createPermanentEditor(guiMgControl));
            }
         }
      }

      /// <summary>
      /// remove from editors for visible columns.
      /// </summary>
      /// <param name="rowStart">the index at which to begin remove</param>
      /// <param name="rowsToRemove">number of rows to be removed</param>
      private void RemoveEditorsForVisibleColumns(int rowStart, int rowsToRemove)
      {
         // remove in editors
         List<TableEditor> colEditors = null;
         for (int i = 0; i < _editors.Length; i++)
         {
            GuiMgControl guiMgControl = _children[_editor2ConrolMap[i]];
            if (isColumnVisible(guiMgControl))
            {
               colEditors = _editors[i];

               for (int idx = rowStart; idx < rowStart + rowsToRemove; idx++)
               {
                  TableEditor editor = colEditors[rowStart];
                  colEditors.RemoveAt(rowStart);
                  disposeEditor(editor);
               }
            }
         }
      }

      /// <summary>
      /// shift editors in table control
      /// </summary>
      /// <param name="rowStart">the index at which to begin shift</param>
      /// <param name="rowsToShift">number of rows to be shifted from rowStart: negative to shift up, positive to shift down</param>
      internal void ShiftEditors(int rowStart, int rowsToShift)
      {
         int overwriteIdx = -1;
         bool shiftUp = false;
         List<TableEditor> colEditors = null;

         if (rowsToShift < 0)
         {
            rowsToShift = -rowsToShift;
            shiftUp = true;
         }

         for (int i = 0; i < _editors.Length; i++)
         {
            colEditors = _editors[i];

            if (shiftUp)
            {
               overwriteIdx = colEditors.Count - 1;

               // shift the values
               for (int idx = rowStart + rowsToShift; idx < colEditors.Count; idx++)
               {
                  overwriteIdx = idx - rowsToShift;
                  if (overwriteIdx >= 0)
                     CopyEditorValues(colEditors[overwriteIdx], colEditors[idx]);
               }

               // clear the remaining indexes
               for (overwriteIdx = overwriteIdx + 1; overwriteIdx < colEditors.Count; overwriteIdx++)
                  ClearEditorValues(colEditors[overwriteIdx]);
            }
            else // shift down
            {
               overwriteIdx = colEditors.Count;

               // shift the values
               for (int idx = colEditors.Count - rowsToShift - 1; idx >= rowStart; idx--)
               {
                  overwriteIdx = idx + rowsToShift;
                  if (overwriteIdx < colEditors.Count)
                     CopyEditorValues(colEditors[overwriteIdx], colEditors[idx]);
               }

               // clear the remaining indexes
               for (overwriteIdx = overwriteIdx - 1; overwriteIdx != -1 && overwriteIdx >= rowStart; overwriteIdx--)
                  ClearEditorValues(colEditors[overwriteIdx]);
            }
         }
      }

      /// <summary>
      /// clear the value of control in 'editor'
      /// </summary>
      /// <param name="editor"></param>
      private void ClearEditorValues(TableEditor editor)
      {
         // For Combobox unregister SelectedIndexChangeHandler before clearing the selection and then register again.
         if (editor.Control is ComboBox)
            ((ComboBox)editor.Control).SelectedIndexChanged -= ComboHandler.getInstance().SelectedIndexChangedHandler;

         editor.Control.Text = "";
         editor.Control.ForeColor = SystemColors.WindowText;
         editor.Control.BackColor = SystemColors.Window;
         editor.Control.Enabled = true;
         editor.Control.Visible = true;
         editor.Control.Bounds = new Rectangle(0, 0, 0, 0);
         editor.Control.Tag = new TagData();
         ((TagData)editor.Control.Tag).IsEditor = true;

#if !PocketPC
         editor.Control.BackgroundImage = null;
         if (editor.Control is ButtonBase)
            ((ButtonBase)editor.Control).Image = null;
         editor.Control.AllowDrop = true;
         GuiUtils.setTooltip(editor.Control, "");
#endif
         if (editor.Control is ComboBox)
            ((ComboBox)editor.Control).SelectedIndexChanged += ComboHandler.getInstance().SelectedIndexChangedHandler;
      }

      /// <summary>
      /// copies values in control of 'editorFrom' into 'editorTo'
      /// </summary>
      /// <param name="editorTo"></param>
      /// <param name="editorFrom"></param>
      private void CopyEditorValues(TableEditor editorTo, TableEditor editorFrom)
      {
         // For ComboBox unregister SelectedIndexChangeHandler before copying new values and then register again.
         if (editorTo.Control is ComboBox)
            ((ComboBox)editorTo.Control).SelectedIndexChanged -= ComboHandler.getInstance().SelectedIndexChangedHandler;

         editorTo.Control.Text = editorFrom.Control.Text;
         editorTo.Control.ForeColor = editorFrom.Control.ForeColor;
         editorTo.Control.BackColor = editorFrom.Control.BackColor;
         editorTo.Control.Font = editorFrom.Control.Font;
         editorTo.Control.Enabled = editorFrom.Control.Enabled;
         editorTo.Control.Visible = editorFrom.Control.Visible;
         editorTo.Control.Bounds = editorFrom.Control.Bounds;
         editorTo.Control.Tag = editorFrom.Control.Tag;

#if !PocketPC
         editorTo.Control.BackgroundImage = editorFrom.Control.BackgroundImage;
         if (editorTo.Control is ButtonBase && editorFrom.Control is ButtonBase)
            ((ButtonBase)editorTo.Control).Image = ((ButtonBase)editorFrom.Control).Image;
         editorTo.Control.AllowDrop = editorFrom.Control.AllowDrop;
         GuiUtils.setTooltip(editorTo.Control, GuiUtils.getTooltip(editorFrom.Control));
#endif
         if (editorTo.Control is ComboBox)
            ((ComboBox)editorTo.Control).SelectedIndexChanged += ComboHandler.getInstance().SelectedIndexChangedHandler;
      }



      /// <summary>
      /// Toggles color for first row when a table has alternate color.
      /// </summary>
      internal void ToggleAlternateColorForFirstRow()
      {
         _tableControl.ToggleAlternateColorForFirstRow();
      }

       internal override void refreshTable(bool refreshAll)
      {
         if (TableControlLimitedItems.SuspendPaint)
         {
            if (refreshAll)
               suspenedRefreshLevel = RefreshLevel.All;
            else if (suspenedRefreshLevel != RefreshLevel.All)
               suspenedRefreshLevel = RefreshLevel.Rows;
            return;
         }

         base.refreshTable(refreshAll);
      }

      /// <summary>
       /// Indicates whether _rowsInPage includes partial rows or not.
      /// </summary>
      /// <returns></returns>
      protected override bool IsPartialRowIncludedInRowsInPage()
      {
         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="isVScrollBarVisible"></param>
      /// <param name="isHScrollBarExisted"></param>
      /// <param name="isHScrollBarExists"></param>
      protected override void tableControl_VScrollBarVisibleChanged(bool isVScrollBarVisible, bool isHScrollBarExisted, bool isHScrollBarExists)
      {
         base.tableControl_VScrollBarVisibleChanged(isVScrollBarVisible, isHScrollBarExisted, isHScrollBarExists);

         if (_tableControl.RightToLeft == RightToLeft.Yes)
         {
            _tableControl.SuspendLayout();
            _tableControl.AllowPaint = false;
            _tableControl.UpdateDisplayWidth();
            _tableControl.MoveColumns(0);
            _tableControl.AllowPaint = true;
            _tableControl.ResumeLayout(true);
         }
      }

      enum RefreshLevel
      {
         None = 0,
         Rows,
         All
      }

   }
}
