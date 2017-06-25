using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
using LgList = System.Collections.Generic.List<Controls.com.magicsoftware.PlacementDrivenLogicalControl>;
using com.magicsoftware.util;
using com.magicsoftware.controls.utils;
using Controls.com.magicsoftware;

#if PocketPC
using ContextMenu = com.magicsoftware.controls.MgMenu.MgContextMenu;
#else

#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// manages a TableControl and its MgControl.
   /// </summary>
   internal abstract class TableManager : ItemsManager, ITableManager
   {
      protected internal readonly TableControl _tableControl;
      protected internal readonly GuiMgControl _mgControl; // table's MgControl

      // placementManager for TableControl
      public TablePlacementManagerBase TablePlacementManager { get; private set; }

      TableChildRenderer _tableChildRenderer;

      internal bool RefreshHeader { get; set; }

      /// <summary>
      /// Columns Manager
      /// </summary>
      public ColumnsManager ColumnsManager { get; private set; }

      private int _mgTitleHeight; // title's height

      protected readonly List<GuiMgControl> _children; // list of controls(not columns) that are table's children

      List<PlacementDrivenLogicalControl> headerLogicalControlList;

      private Color _frameFgColor = Color.Empty; // foregorund color, used for highliting


      internal Color DisabledTextColor
      {
         get { return _tableChildRenderer.DisabledTextColor; }
      }

      private bool _refreshPageNeeded; // true, if page must be refreshed
      internal bool RefreshPageNeeded
      {
         get { return _refreshPageNeeded; }
         set { _refreshPageNeeded = value; }
      }

      protected List<TableEditor>[] _editors; // table's editors
      public List<TableEditor> _headerEditors; // table's editors
      private TableEditor _tmpEditor; // table's temporary editor
      private int _editorsCountInRow; // number of controls that have persistent editors in row
      private int _editorsCountInHeader; // number of controls that have persistent editors in header
      protected int[] _editor2ConrolMap; // maps editor column in editors to the number
                                         // of mgControl in children array
      protected int[] _headerEditor2ConrolMap;
      private Hashtable _control2DataMap; // maps controls to it's data  matrix
      protected int _selectionIndex; // tables current selection
      private int _prevSelectionIndex; // tables previous selection
      protected int _prevTopGuiIndex; // save top index each time it is changed
      protected bool _repaintNeeded; //true, if table needs to be repainted only, no need to change focus

      protected readonly List<int> _rowsToRefresh; // list of rows to be refrested
      protected int _mgCount; // count of magic rows int the table
      protected internal int _guiTopIndex; // last set top index  

#if !PocketPC
      private ContextMenuStrip _contextMenu; // context menu
#else
      private ContextMenu _contextMenu; // context menu
#endif
      private int _maxColumnWithEditor; //number of last column that has permanent editor
      protected bool _inResize;

      internal RowHighlightType RowHighlitingStyle //row highlighting type
      {
         get
         {
            return _tableChildRenderer.RowHighlitingStyle;
         }
         set { _tableChildRenderer.RowHighlitingStyle = value; }
      }
      /// <summary>
      /// pen for drawing rectangle for selected frame in multimarking
      /// </summary>

      /// <summary>
      /// RightToLeftLayout
      /// </summary>
      public bool RightToLeftLayout
      {
         get { return _tableControl.RightToLeftLayout; }
      }

      internal BottomPositionInterval BottomPositionInterval { get; private set; }

      internal bool FillWidth { get; set; }

      private TablePlacementStrategy tablePlacementStrategy { get; set; }
      private FillTablePlacementStrategy fillTablePlacementStrategy { get; set; }
      private TableMultiColumnEnabledStrategy tableHeaderMultiColumnEnabledStrategy { get; set; }

      protected TableManager(TableControl tableControl, GuiMgControl mgControl, List<GuiMgControl> children,
                            int columnsCount, int style)
         : base(tableControl)
      {
         _tableControl = tableControl;
         _mgControl = mgControl;
         _children = children;

         TablePlacementManager = new TablePlacementManager(columnsCount);

         _tableChildRenderer = new TableChildRenderer(_tableControl);
         _tableChildRenderer.GuiRowIndex += TableManager_GuiRowIndex;
         _tableChildRenderer.IsInMultimark += new TableChildRenderer.IsInMultimarkDelegate(tableRenderer_IsInMultimark);

         _tableControl.VScrollBarVisibleChanged += tableControl_VScrollBarVisibleChanged;

         ColumnsManager = new ColumnsManager(columnsCount);

         _rowsInPage = -1; // we set initial _rowsInPage to -1, so that editors for partial row will always be created on first resize
         _selectionIndex = GuiConstants.NO_ROW_SELECTED;
         _prevSelectionIndex = 0;
         setTitleHeight(0);

         // set tableManager on table's widget
         controlsMap.setMapData(mgControl, 0, tableControl);

         // Default colors
         _frameFgColor = Color.Black;

         SetTableControlInfo();

         // Editors
         initEditorsAndData();

         // last click for double click check
         lastClicked = null;
         lastClickTime = -1;

         RefreshPageNeeded = true;
         _rowsToRefresh = new List<int>();

         GuiUtils.setRightToLeft(tableControl, (style & Styles.PROP_STYLE_RIGHT_TO_LEFT) > 0);

         tablePlacementStrategy = new TablePlacementStrategy();
         tableHeaderMultiColumnEnabledStrategy = new TableMultiColumnEnabledStrategy(_tableControl);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="isVScrollBarVisible"></param>
      /// <param name="isHScrollBarExisted"></param>
      /// <param name="isHScrollBarExists"></param>
      protected virtual void tableControl_VScrollBarVisibleChanged(bool isVScrollBarVisible, bool isHScrollBarExisted, bool isHScrollBarExists)
      {
         if (FillWidth)
         {
            bool applyPlacement = false;

            if (isVScrollBarVisible)
               applyPlacement = !isHScrollBarExisted;
            else
               applyPlacement = !isHScrollBarExists;

            if (applyPlacement)
               ExecuteFillTablePlacement();
         }
      }

      /// <summary>
      /// Updates TableControl information
      /// </summary>
      void SetTableControlInfo()
      {
         // Update whether table has atleast one header control
         foreach (GuiMgControl control in _children)
         {
            if (control.IsTableHeaderChild)
            {
               _tableControl.HasTableHeaderChild = true;
               break;
            }
         }
      }

      /// <summary>
      /// return true if we are in multumark state
      /// </summary>
      /// <returns></returns>
      bool tableRenderer_IsInMultimark()
      {
         return IsInMultimark;
      }


      /// <summary> compute control background color
      /// </summary>
      /// <param name="child">table child</param>
      /// <param name="control">editor control, can be null for painting control without editor</param>
      /// <param name="isRowMarked">is row marked</param>
      /// <param name="recievingFocus">TODO</param>
      /// <param name="guiRow">gui row number</param>
      /// <returns></returns>
      internal Color computeControlBgColor(LogicalControl child, ColumnsManager columnsManager, Control control, bool isRowMarked, bool isFocusedControl,
                                           int guiRow, bool ownerDraw, out bool keepColor)
      {
         return _tableChildRenderer.computeControlBgColor(child, columnsManager, control, isRowMarked, isFocusedControl, guiRow, ownerDraw, out keepColor);
      }

      /// <summary>
      /// Get Gui Row index
      /// </summary>
      /// <param name="mgIndex"></param>
      /// <returns></returns>
      int TableManager_GuiRowIndex(int mgIndex)
      {
         return getGuiRowIndex(mgIndex);
      }

      /// <summary> set row height
      /// </summary>
      /// <param name="rowHeight"></param>
      internal void setRowHeight(int rowHeight)
      {
         _tableControl.RowHeight = rowHeight;

         // Forcefully compute rows in page when row height is changed explicitly (i.e. not 
         // as a result of table resize)
         _tableControl.ComputeAndSetRowsInPage(true);

         //After setting the row height and no of rows, call resize, so that if required, the editors
         //will be created/removed and the woker thread will be informed about the new no of rows.
         _tableControl.SuspendLayout();
         resize();
         _tableControl.ResumeLayout();
      }

      /// <summary> Returns the change in current row height from its original row height </summary>
      /// <returns></returns>
      public int GetChangeInRowHeight()
      {
         return (_tableControl.RowHeight - _tableControl.OriginalRowHeight);
      }

      /// <summary>
      /// Gets Table Multi Column Strategy. In case of header editor, the strategy is always enabled.
      /// </summary>
      /// <param name="isHeaderEditor"></param>
      /// <returns></returns>
      public TableMultiColumnStrategyBase GetTableMultiColumnStrategy(bool isHeaderEditor)
      {
         if (isHeaderEditor)
            return tableHeaderMultiColumnEnabledStrategy;
         else
            return _tableControl.TableMultiColumnStrategy;
      }

      /// <summary>
      /// </summary>
      /// <param name="titleHeight"></param>
      internal void setTitleHeight(int titleHeight)
      {
         _mgTitleHeight = titleHeight;
         _tableControl.TitleHeight = titleHeight;
         RefreshPageNeeded = true;
      }

      /// <summary>
      /// </summary>
      /// <param name="bottomPositionInterval"></param>
      internal void setBottomPositionInterval(BottomPositionInterval bottomPositionInterval)
      {
         BottomPositionInterval = bottomPositionInterval;
      }

      /// <summary> get Table's widget
      /// </summary>
      /// <returns> </returns>
      internal TableControl getTable()
      {
         return _tableControl;
      }

      /// <summary>
      /// sets table top index
      /// </summary>
      /// <param name="index"></param>
      internal void SetTopIndex(int index)
      {
         int row = getGuiRowIndex(index);
         _prevTopGuiIndex = row;
         RefreshPageNeeded = true;
         SetGuiTopIndex(row);

      }

      /// <summary> returns table's top index</summary>
      internal override int getTopIndex()
      {
         int index = _tableControl.TopIndex;
         index = getMagicIndex(index);
         return index;
      }

      /// <summary> sets table selection index We do not trust SWT tabe to perform selection, it should be done according to
      /// magic logic, i.e. sometimes we do not want select clicked row. So, table has style HIDE_SELECTION, and
      /// we perform paint ourselves using selectionIndex
      /// </summary>
      /// <param name="index"></param>
      internal override void setSelectionIndex(int index)
      {
         _selectionIndex = index;
         // Defect 76910. Check the previous index.
         if (_prevSelectionIndex != GuiConstants.NO_ROW_SELECTED && !_rowsToRefresh.Contains(_prevSelectionIndex))
            _rowsToRefresh.Add(_prevSelectionIndex);

         if (!noRowSelected() && !_rowsToRefresh.Contains(index))
            _rowsToRefresh.Add(index);

         _prevSelectionIndex = index;
      }

      /// <summary> validate row
      /// </summary>
      /// <param name="mgIndex"></param>
      internal void validateRow(int mgIndex)
      {
         int idx = getGuiRowIndex(mgIndex);
         if (isValidIndex(idx))
         {
            TableItem item = _tableControl.getItem(idx);
            item.IsValid = true;
         }
      }

      /// <summary>
      /// create Table children for the row
      /// </summary>
      /// <param name="mgIndex"></param>
      internal void createRow(int mgIndex)
      {
         int idx = getGuiRowIndex(mgIndex);
         if (isValidIndex(idx))
         {
            var tableChildren = new LgList[ColumnsManager.ColumnsCount];
            for (int i = 0; i < ColumnsManager.ColumnsCount; i++)
               tableChildren[i] = new LgList();

            // put children into array by columns
            for (int i = 0; i < _children.Count; i++)
            {
               GuiMgControl control = _children[i];

               if (!control.IsTableHeaderChild)
               {
                  int mgColumn = control.Layer - 1;

                  LogicalControl lg = LogicalControl.createControl(control.getCreateCommandType(), control, _tableControl,
                                                                   mgIndex, mgColumn);
                  controlsMap.add(control, mgIndex, lg);
                  tableChildren[mgColumn].Add(lg);
               }
            }

            // set the array on the table item
            TableItem item = _tableControl.getItem(idx);
            item.Controls = tableChildren;
         }
      }

      /// <summary>
      /// rundo create row operation
      /// </summary>
      /// <param name="mgIndex"></param>
      internal void undoCreateRow(int mgIndex)
      {
         int idx = getGuiRowIndex(mgIndex);
         if (isValidIndex(idx))
         {
            TableItem item = _tableControl.getItem(idx);

            cleanItem(item);
            item.Controls = null;
         }
      }

      /// <summary>
      /// set visibility of the row
      /// </summary>
      /// <param name="mgIndex"></param>
      /// <param name="hide">true to hide, false to show</param>
      internal void SetTableRowVisibility(int mgIndex, bool show, bool applyChildrenVisibility)
      {
         int idx = getGuiRowIndex(mgIndex);
         if (isValidIndex(idx))
         {
            TableItem item = _tableControl.getItem(idx);

            SetItemVisibility(item, show, applyChildrenVisibility);
         }
      }

      /// <summary>
      /// clears the sort mark for all columns on the table
      /// </summary>
      internal void clearColumnsSortMark()
      {
         List<ILogicalColumn> columns = getColumns();
         foreach (LgColumn lgColumn in columns)
         {
            lgColumn.clearSortMark();
         }
      }

      /// <summary>
      /// Refreshes all header controls
      /// </summary>
      void RefreshHeaderControls()
      {
         if (headerLogicalControlList.Count > 0)
         {
            for (int i = 0; i < _editorsCountInHeader; i++)
            {
               GuiMgControl guiMgControl = _children[_headerEditor2ConrolMap[i]];

               bool isControlVisible = false;
               if (isColumnVisible(guiMgControl))
               {
                  isControlVisible = ((LogicalControl)headerLogicalControlList[i]).Visible; // Defect 132977: Consider control visibility also in addition to column visibility
                  linkControlToEditor((LogicalControl)headerLogicalControlList[i], _headerEditors[i], false, false);
               }
               else
                  isControlVisible = false;
            }
         }
      }

      /// <summary> refresh the row
      /// </summary>
      /// <param name="mgIndex">mgindex of the row</param>
      internal void refreshRow(int mgIndex)
      {
         int idx = getGuiRowIndex(mgIndex);

         if (isValidIndex(idx))
         {
            TableItem item = _tableControl.getItem(idx);
            var tableChildren = item.Controls;

            Rectangle rect = item.Bounds;

            _tableControl.Invalidate(rect, true);

            if (tableChildren != null)
            {
               foreach (var columnChildren in tableChildren)
               {
                  // for all controls in the column
                  for (int j = 0; j < columnChildren.Count; j++)
                  {
                     LogicalControl child = (LogicalControl)columnChildren[j];
                     TableEditor editor = getTableEditor(child);
                     // reapply properties on the editor
                     if (editor != null)
                        linkControlToEditor(child, editor, false, false);
                  }
               }
            }
         }
      }

      /// <summary>
      /// Hide old / invalide temporary editor. This neded for proper display and to prevent refreshes and performance problem
      /// </summary>
      private void HideOldTmpEditor()
      {
         if (_tmpEditor != null)
         {
            int guiRowIdx = getGuiRowIndex(_selectionIndex);
            if (_tmpEditor.RowIdx != guiRowIdx && !_tmpEditor.isHidden())
               // this is invalided tmpEditor, it belongs to previous selected row we must hide it.
               // tmpEditor.Hide() causes invalidate on the whole table (.NET feature / bug)
               // so we peform the Hide() here to prevent additional refreshes . QCRs #913802, 775670
               _tmpEditor.Hide();

            // If no rows selected, set _tmpEditor.Control to null.
            if (_selectionIndex == GuiConstants.NO_ROW_SELECTED)
               _tmpEditor.Control = null;
         }
      }

      internal bool HasTmpEditor()
      {
         return _tmpEditor != null && _tmpEditor.Control != null;
      }

      /// <summary> set table's background color
      /// </summary>
      /// <param name="color"></param>
      internal void setMgBgColor(MgColor color)
      {
         //save color and Transparent data on the control
         //it will be used to calcultate if table is transparent according to this and other parameters

         //save real color of the table control
         _tableControl.MagicBgColor = ControlUtils.MgColor2Color(color, false, true);
         //save if the color was maked as transparent
         _tableControl.IsMagicBackGroundColorTransparent = color.IsTransparent;

         RefreshPageNeeded = true;
      }

      /// <summary>
      /// </summary>
      /// <param name="allowColumnResize"></param>
      internal void setResizable(bool allowColumnResize)
      {
         _tableControl.AllowColumnResize = allowColumnResize;
      }

      internal void SetMultiColumnDisplay(bool multiColumnDisplay)
      {
         if (multiColumnDisplay)
            _tableControl.EnableMultiColumnDisplayStrategy();
         else
            _tableControl.DisableMultiColumnDisplayStrategy();
      }


      internal void ShowEllipsis(bool showEllipsis)
      {
         _tableControl.AddEndEllipsesFlag = showEllipsis;
      }

      /// <summary> no row is selected - possible in empty dataview</summary>
      /// <returns></returns>
      private bool noRowSelected()
      {
         return _selectionIndex == GuiConstants.NO_ROW_SELECTED;
      }

      private bool isItemSelected(int row)
      {
         if (noRowSelected())
            return false;

         int guiSelectionIndex = getGuiRowIndex(_selectionIndex);
         return row == guiSelectionIndex;
      }



      /// <summary>
      /// paint table row
      /// </summary>
      internal void PaintRow(TablePaintRowArgs ea)
      {
#if !PocketPC //tmp
         if (_tableControl.IsDisposed)
            return;
#endif
         bool isSelected = isItemSelected(ea.Row);
         bool isRowMarked = ShouldPaintRowAsMarked(getMagicIndex(ea.Row), isSelected);

         _tableControl.TableMultiColumnStrategy.PaintRow(_tableChildRenderer, ea, isRowMarked, isSelected, ColumnsManager);

         if (isSelected)
            DrawRowFrameIfNeeded(ea.Graphics, ea.Rect);
      }

      /// <summary>
      /// draws row frame if 
      /// - table's RowHighlitingStyle is frame or
      /// - we are in multi mark
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      private void DrawRowFrameIfNeeded(Graphics g, Rectangle rect)
      {

         rect.Height--;
         rect.Width--;

#if !PocketPC
         if (IsInMultimark)
            ControlPaint.DrawFocusRectangle(g, rect);
         else
#endif
            if (RowHighlitingStyle == RowHighlightType.Frame)
         {

            if (_tableControl.ShowLineDividers)
               rect.Height--;
            Color color = _tableChildRenderer.ShouldUseActiveColor ? _frameFgColor : _tableChildRenderer.InactiveHightlightFgColor;
            Pen pen = PensCache.GetInstance().Get(Utils.GetNearestColor(g, color));
            g.DrawRectangle(pen, rect);
         }
      }

      /// <summary>
      /// mark row
      /// </summary>
      /// <param name="index"></param>
      public override void MarkRow(int index)
      {
         base.MarkRow(index);
         addRowToRefresh(index);
      }

      /// <summary>
      /// unmark row
      /// </summary>
      /// <param name="index"></param>
      public override void UnMarkRow(int index)
      {
         base.UnMarkRow(index);
         addRowToRefresh(index);
      }

      /// <summary>
      /// unmark all rows
      /// </summary>
      public override void UnMarkAll()
      {
         if (IsInMultimark)
         {
            base.UnMarkAll();
            _repaintNeeded = true;
         }
      }

      /// <summary> compute foregrrounf color of control
      /// </summary>
      /// <param name="child"></param>
      /// <param name="isSelected"></param>
      /// <returns></returns>
      internal Color computeControlFgColor(LogicalControl child, bool isSelected)
      {
         Color color = child.FgColor;
         if (RowHighlitingStyle == RowHighlightType.BackgroundControls &&
             child.GuiMgControl.isTextControl() && isSelected && !child.Modifable &&
             ShouldPaintRowAsMarked(child._mgRow, isSelected))
            color = _tableChildRenderer.HightlightFgColor;
         return color;
      }

      /// <summary>
      /// returns if tableControl has fixed 3D border
      /// </summary>
      /// <returns></returns>
      public bool HasFixed3DBorder()
      {
         return _tableControl.BorderStyle == BorderStyle.Fixed3D;
      }

      /// <summary> return header heigth
      /// </summary>
      /// <returns></returns>
      public int getMgTableTop()
      {
         int mgTop = _mgTitleHeight;
         if (_tableControl.BorderStyle == BorderStyle.Fixed3D)
            mgTop += TableControl.STUDIO_BORDER_WIDTH; // + magic border width
         return mgTop;
      }

      /// <summary> creates  editor for a child
      /// </summary>
      private TableEditor createEditor()
      {
         var editor = new TableEditor(_tableControl);
         return editor;
      }

      /// <summary> create temporary editor
      /// </summary>
      /// <param name="lg"></param>
      /// <returns> </returns>
      internal Control showTmpEditor(LogicalControl lg)
      {
         Debug.Assert(lg.Visible);
         bool useExisting = false;
         if (_tmpEditor == null)
            _tmpEditor = createEditor();
         GuiUtils.SetTmpEditorOnTagData(GuiUtils.FindForm(_tableControl), _tmpEditor);
         Control control = _tmpEditor.Control;
         if (control != null)
         {
            // check if existing temporary editor is of the same style and may be reused               
            // check the type of the controls                
            if ((control is MgTextBox && lg.GuiMgControl.Type == MgControlType.CTRL_TYPE_TEXT) ||
                (control is MgComboBox && lg.GuiMgControl.Type == MgControlType.CTRL_TYPE_COMBO))
               useExisting = true;
            if (!useExisting)
            {
               GuiUtilsBase.setContextMenu(control, null);
               GuiUtilsBase.ControlToDispose = control;
            }
         }
         //Fix bug#:783602 & 932024
         //932024 cause bug#783602 I remove fix of bug #:932024 and added the correct fix :        
         if (lg is LgCombo && (!useExisting || (control is MgComboBox && (!((MgComboBox)control).DroppedDown))))
            ((LgCombo)lg).setRefreshItemList(true);

         linkControlToEditor(lg, _tmpEditor, !useExisting, true);

         control = _tmpEditor.Control;
         addRowToRefresh(lg._mgRow);

         return _tmpEditor.Control;
      }

      /// <summary> create premanent editor
      /// </summary>
      /// <param name="mgControl"> </param>
      /// <returns> </returns>
      protected TableEditor createPermanentEditor(GuiMgControl mgControl)
      {
         TableEditor editor = createEditor();
         editor.EditorZOrder = mgControl.ControlZOrder;
         Control control = toControl(mgControl);
         editor.Control = control;
         return editor;
      }

      /// <summary>
      /// Create editor for header control
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <returns></returns>
      TableEditor CreateHeaderEditor(GuiMgControl guiMgControl)
      {
         TableEditor newEditor = createPermanentEditor(guiMgControl);
         newEditor.IsHeaderEditor = true;
         return newEditor;
      }

      /// <summary>
      /// link control to the editor , copy all its properties
      /// </summary>
      /// <param name="lg"></param>
      /// <param name="editor"></param>
      /// <param name="create"></param>
      /// <param name="recievingFocus"></param>
      internal void linkControlToEditor(LogicalControl lg, TableEditor editor, bool create, bool recievingFocus)
      {
         Control control = (create
                                ? toControl(lg.GuiMgControl)
                                : editor.Control);
         ControlsMap.getInstance().setMapData(lg.GuiMgControl, lg._mgRow, control);

         int guiRowIdx = getGuiRowIndex(lg._mgRow);

         editor.Column = ColumnsManager.getColumnByMagicIdx(((TableCoordinator)lg.Coordinator).MgColumn);
         editor.BoundsComputer = (BoundsComputer)lg.Coordinator;
         editor.Control = control;
         editor.RowIdx = guiRowIdx;
         editor.EditorZOrder = lg.ZOrder;
         lg.setProperties(control, isItemSelected(guiRowIdx), recievingFocus);
         if (!recievingFocus)
            editor.Layout();

         if (create)
            lg.setPasswordToControl(control);
      }

      /// <summary> Determine where the mouse was clicked, Checks table point and returns MapData of control that is on the
      /// point or near to the point
      /// </summary>
      /// <param name="pt">point clicked </param>
      /// <param name="findExact">if true, find control that includes the point </param>
      /// <param name="checkEnabled">if true, consider only enabled control </param>
      /// <returns> </returns>
      internal MapData pointToMapData(Point pt, bool findExact, bool checkEnabled)
      {
         MapData mapData = null;
         int column = TableControl.TAIL_COLUMN_IDX;
         // Determine which row was selected
         int row = _tableControl.findRowByY(pt.Y);
         bool forTableHeader = row == -1;

         if (isValidIndex(row) || forTableHeader)
         {
            TableItem item = !forTableHeader ? _tableControl.getItem(row) : null;
            if (item != null)
               column = _tableControl.findColumnByX(_tableControl.getXCorner() + pt.X);

            if (column != TableControl.TAIL_COLUMN_IDX || forTableHeader)
            {
               LgList children = GetChildrenList(column, forTableHeader, item);
               if (children == null)
                  return null;
               else
               {
                  Rectangle cellRect = _tableControl.getCellRect(column, row);
                  int minDistance = Int32.MaxValue;
                  LogicalControl result = null;
                  for (int i = children.Count; i > 0; i--)
                  {
                     LogicalControl lg = (LogicalControl)children[i - 1];
                     if (!lg.canHit(checkEnabled))
                        continue;

                     Rectangle dispRect = _tableControl.TableMultiColumnStrategy.GetRectToDraw(lg.getRectangle(), cellRect);

                     if (forTableHeader)
                     {
                        // Defect# 130085: Control should be returned if clicked on it if it is present on table header.
                        // This is handled here now for table header. This ensures, button is kept on focus when
                        // placed on header and clicked.
                        TableEditor editor = _headerEditors[i - 1];
                        if (editor != null && editor.Control.Visible)
                           dispRect = editor.Bounds(); //Defect 131992 instead of old fix
                        else
                           continue;
                     }

                     if (findExact)
                     {
                        // return control only if point is on it
                        if (dispRect.Contains(pt))
                        {
                           result = lg;
                           break;
                        }
                     }
                     else
                     {
                        // check nearest control to the point inside of the cell
                        int distance = GuiUtilsBase.getDistance(dispRect, pt);
                        if (distance < minDistance)
                        {
                           result = lg;
                           minDistance = distance;
                        }
                     }
                  }
                  if (result != null)
                     mapData = new MapData(result.GuiMgControl, result._mgRow);
               }
            }
         }

         return mapData;
      }

      /// <summary>
      /// Gets table header/column/row children.
      /// </summary>
      /// <param name="column"></param>
      /// <param name="forTableHeader"></param>
      /// <param name="item"></param>
      /// <returns></returns>
      private LgList GetChildrenList(int column, bool forTableHeader, TableItem item)
      {
         if (forTableHeader)
            return headerLogicalControlList;
         else
            return _tableControl.TableMultiColumnStrategy.GetColumnChildren(item, column, ColumnsManager);
      }

      /// <summary>
      /// retruns magic row number for table Y coordinate
      /// </summary>
      /// <param name="Y"></param>
      /// <returns></returns>
      public int getRow(int Y)
      {
         int row = _tableControl.findRowByY(Y);

         if (isValidIndex(row))
         {
            TableItem item = _tableControl.getItem(row);
            var tableChildren = item.Controls;
            if (tableChildren != null)
            {
               for (int i = 0; i < ColumnsManager.ColumnsCount; i++)
               {
                  LgList columnChildren = tableChildren[i];
                  foreach (LogicalControl child in columnChildren)
                     return child._mgRow;
               }
            }
         }
         return -1;
      }

      /// <summary> get child's editor
      /// </summary>
      /// <param name="lg"> </param>
      /// <returns> </returns>
      internal TableEditor getTableEditor(LogicalControl lg)
      {
         TableEditor editorControl = null;
         Control tmpControl = null;
         if (_tmpEditor != null)
            tmpControl = _tmpEditor.Control;

         if (GuiUtilsBase.isOwnerDrawControl(lg.GuiMgControl))
         {
            int mgcolumn = ((TableCoordinator)lg.Coordinator).MgColumn;
            if (tmpControl != null && lg.Visible && (ColumnsManager.getLgColumnByMagicIdx(mgcolumn).Visible))
            {
               MapData mapData = controlsMap.getMapData(tmpControl);
               if (mapData != null && lg.GuiMgControl == mapData.getControl() && lg._mgRow == mapData.getIdx())
                  // this child has temporary editor
                  editorControl = _tmpEditor;
            }
         }
         else if (lg.GuiMgControl.IsTableHeaderChild)
         {
            int index = headerLogicalControlList.IndexOf(lg);
            editorControl = _headerEditors[index];
         }
         else
         {
            int row = lg._mgRow - getTopIndex();
            if (row >= 0 && row < _rowsInPage + 1)
            {
               // this child has permanent editor
               int editorColumn = ((MgControlData)_control2DataMap[lg.GuiMgControl]).EditorColumn;
               if (_editors[editorColumn].Count > row)
               {
                  TableEditor editor = _editors[editorColumn][row];
                  editorControl = editor;
               }
            }
         }
         return editorControl;
      }

      /// <summary> create/destroy column editors on change of column visibility
      /// </summary>
      /// <param name="mgColumn"></param>
      /// <param name="visible"></param>
      internal void updateEditorsOnColumnVisibility(int mgColumn, bool visible)
      {
         int tmpMgColumn = -1;
         resize();
         if (_tmpEditor != null)
         {
            Control control = _tmpEditor.Control;
            if (control != null)
            {
               MapData mapData = controlsMap.getMapData(control);
               if (mapData != null)
               {
                  GuiMgControl guiMgControl = mapData.getControl();
                  tmpMgColumn = guiMgControl.Layer - 1;
               }
            }
         }
         // destroy temporary editor
         if (tmpMgColumn == mgColumn && visible == false)
         {
            disposeEditor(_tmpEditor);
            _tmpEditor = null;
         }

         List<GuiMgControl> columnChildren = ((LgColumn)ColumnsManager.getLgColumnByMagicIdx(mgColumn)).getChildren();
         for (int i = 0; i < columnChildren.Count; i++)
         {
            GuiMgControl guiMgControl = columnChildren[i];
            if (hasPermanentEditor(guiMgControl))
            {
               int editorColumn = ((MgControlData)_control2DataMap[guiMgControl]).EditorColumn;
               if (guiMgControl.IsTableHeaderChild)
               {
                  if (visible)
                  {
                     TableEditor newEditor = CreateHeaderEditor(guiMgControl);
                     _headerEditors[editorColumn] = newEditor;

                     if (newEditor.Control is MgComboBox)
                        ((LgCombo)headerLogicalControlList[editorColumn]).setRefreshItemList(true);
                     else if (newEditor.Control is MgRadioPanel)
                        ((LgRadioContainer)headerLogicalControlList[editorColumn]).setRefreshItemList(true);

                  }
                  else
                  {
                     TableEditor editor = _headerEditors[editorColumn];
                     _headerEditors[editorColumn] = null;
                     disposeEditor(editor);
                  }
               }
               else
               {
                  List<TableEditor> colEditors = _editors[editorColumn];

                  if (visible)
                  {
                     int visibleRowsInPage = (IsPartialRowIncludedInRowsInPage() ? _rowsInPage : (_rowsInPage + 1));

                     for (int j = 0; j < visibleRowsInPage; j++)
                     {
                        colEditors.Add(createPermanentEditor(guiMgControl));
                     }
                  }
                  else
                  {
                     for (int k = 0; k < colEditors.Count; k++)
                     {
                        TableEditor editor = colEditors[k];
                        disposeEditor(editor);
                     }

                     colEditors.Clear();
                  }
               }
            }
         }
         BringHeaderEditorsToFront();

         refreshTable(true);
      }

      /// <summary> set table's hightlightBgColor
      /// </summary>
      /// <param name="color"> </param>
      internal void setHightlightBgColor(Color color)
      {
         _tableChildRenderer.HightlightBgColor = color;
      }



      /// <summary> set table's HightlightFgColor
      /// </summary>
      /// <param name="color"> </param>
      internal void setHightlightFgColor(Color color)
      {
         _tableChildRenderer.HightlightFgColor = color;
         _frameFgColor = color;
         if (!noRowSelected())
            addRowToRefresh(_selectionIndex);
      }

      internal void setInactiveHightlightBgColor(Color color)
      {
         _tableChildRenderer.InactiveHightlightBgColor = color;
      }

      internal void setInactiveHightlightFgColor(Color color)
      {
         _tableChildRenderer.InactiveHightlightFgColor = color;
      }

      internal void setActiveState(bool state)
      {
         if (state != _tableChildRenderer.IsActive)
         {
            //No need to refresh row if InactiveHightlightColor is not set
            if (_tableChildRenderer.InactiveHightlightFgColor != Color.Empty || _tableChildRenderer.InactiveHightlightBgColor != Color.Empty)
               if (!noRowSelected())
                  addRowToRefresh(_selectionIndex);
            _tableChildRenderer.IsActive = state;
         }
      }

      /// <summary>
      /// returns true if controls has permanent editor
      /// </summary>
      /// <param name="mgControl"> </param>
      /// <returns> </returns>
      private bool hasPermanentEditor(GuiMgControl mgControl)
      {
         if (GuiUtilsBase.isOwnerDrawControl(mgControl))
            return false;
         return true;
      }

      /// <summary> inits tables editors
      /// </summary>
      private void initEditorsAndData()
      {
         int nonHeaderTableChildCount = 0;
         foreach (GuiMgControl control in _children)
         {
            if (!control.IsTableHeaderChild)
               nonHeaderTableChildCount++;
         }

         // create mapping of control to it's editors in "editors" array and back
         _editor2ConrolMap = new int[nonHeaderTableChildCount];
         _headerEditor2ConrolMap = new int[_children.Count - nonHeaderTableChildCount];

         _control2DataMap = new Hashtable();
         _maxColumnWithEditor = -1;
         for (int i = 0; i < _children.Count; i++)
         {
            GuiMgControl child = _children[i];

            if (!child.IsTableHeaderChild)
            {
               // create mapping of control properties to gui style
               int editorIdx = (hasPermanentEditor(child) ? _editorsCountInRow : -1);

               _control2DataMap[child] = new MgControlData(editorIdx);

               if (hasPermanentEditor(child))
               {
                  _editor2ConrolMap[_editorsCountInRow++] = i;
                  int column = child.Layer - 1;

                  if (column > _maxColumnWithEditor)
                     _maxColumnWithEditor = column;
               }
            }
         }

         // initialize editors array
         _editors = new List<TableEditor>[_editorsCountInRow];
         for (int i = 0; i < _editors.Length; i++)
            _editors[i] = new List<TableEditor>();

         CreateHeaderChildrenEditors();

         // create temporary editor
         _tmpEditor = createEditor();
      }

      /// <summary>
      /// Creates editors for table header children. Also creates its logical controls
      /// </summary>
      void CreateHeaderChildrenEditors()
      {
         _headerEditors = new List<TableEditor>();
         headerLogicalControlList = new List<PlacementDrivenLogicalControl>();

         for (int i = 0; i < _children.Count; i++)
         {
            GuiMgControl guiMgControl = _children[i];
            // create editors
            if (guiMgControl.IsTableHeaderChild)
            {
               // create mapping of control properties to gui style
               int editorIdx = _editorsCountInHeader;

               _control2DataMap[guiMgControl] = new MgControlData(editorIdx);

               _headerEditor2ConrolMap[_editorsCountInHeader++] = i;

               TableEditor editor = CreateHeaderEditor(guiMgControl);
               // add to editors list
               _headerEditors.Add(editor);

               int mgColumn = guiMgControl.Layer - 1;

               // create logical control
               LogicalControl lg = LogicalControl.createControl(guiMgControl.getCreateCommandType(), guiMgControl, _tableControl,
                                                                0, mgColumn);
               // add logical control to controlsMap
               controlsMap.add(guiMgControl, 0, lg);
               // add logical control to header list
               headerLogicalControlList.Add(lg);
            }
         }
         _headerEditors.Sort((child1, child2) => child1.EditorZOrder.CompareTo(child2.EditorZOrder));
         headerLogicalControlList.Sort((child1, child2) => child1.ZOrder.CompareTo(child2.ZOrder));

         BringHeaderEditorsToFront();
      }

      /// <summary>
      /// Brings header control editors to front by Z-order sorted _headerEditors list.
      /// </summary>
      void BringHeaderEditorsToFront()
      {
         foreach (TableEditor editor in _headerEditors)
            if (editor != null)
               editor.Control.BringToFront();
      }

      /// <summary>
      /// refreshes page: - reassigns editors to controls - applies table child properties to the editors -
      /// performs redraw to paint owner draw controls
      /// </summary>
      /// </summary>
      internal void refreshPage()
      {
         refreshPage(true);
      }

      /// <summary>
      /// refreshes page: - reassigns editors to controls - applies table child properties to the editors -
      /// performs redraw to paint owner draw controls
      /// </summary>
      /// <param name="allowFocusChanges"> if false, do not perform any focus changes, only repaint</param>
      internal void refreshPage(bool allowFocusChanges)
      {
         // find page's top & last index
         int topIndex = _tableControl.TopIndex;
         int count = GetNumberOfRowsToRefresh();
         int lastIndex = GetLastRowIdxInPage();

         // hide old editor
         if (allowFocusChanges)
            HideOldTmpEditor();

         RefreshHeaderControls();

         // for all rows in the page
         for (int rowIdx = topIndex; rowIdx < lastIndex; rowIdx++)
         {
            LgList[] tableChildren = null;
            if (rowIdx < count)
            {
               TableItem item = _tableControl.getItem(rowIdx);
               tableChildren = item.Controls;

               if (tableChildren != null)
               {
                  // go over all controls that have editors
                  for (int i = 0; i < _editorsCountInRow; i++)
                  {
                     GuiMgControl guiMgControl = _children[_editor2ConrolMap[i]];

                     if (!guiMgControl.IsTableHeaderChild)
                     {
                        // find tablechild of this mgControl
                        int mgColumn = guiMgControl.Layer - 1;

                        int guiColumnIdx = ColumnsManager.getGuiColumnIdx(mgColumn);
                        if (guiColumnIdx != -1)
                        // column is not hidden
                        {
                           int indexInColumn = ((MgControlData)_control2DataMap[guiMgControl]).IndexInColumn;
                           LogicalControl child = (LogicalControl)tableChildren[mgColumn][indexInColumn];
                           if (_editors[i].Count >= rowIdx - topIndex)
                           {
                              TableEditor tableEditor = _editors[i][rowIdx - topIndex];
                              linkControlToEditor(child, tableEditor, false, false);
                           }
                        }
                     }
                  }
               }
            }
            else
            {
               for (int i = 0; i < _editorsCountInRow; i++)
               {
                  int row = rowIdx - topIndex;
                  if (_editors[i].Count > row)
                  {
                     TableEditor tableEditor = _editors[i][row];
                     tableEditor.Control.Size = new Size();
                     tableEditor.BoundsComputer = null;
                     tableEditor.Control.Text = "";
                  }
               }
            }
         }

         RefreshPageNeeded = false;
         _repaintNeeded = false;
         _rowsToRefresh.Clear();

         if (allowFocusChanges)
            restoreEditorFocus();

         if (_tmpEditor != null && _tmpEditor.Control != null)
            _tmpEditor.Control.Size = new Size(0, 0);
         //table.Paint -= TableHandler.getInstance().PaintHandler;
         if (_tmpEditor != null)
            _tmpEditor.Layout();

         //table.Paint += TableHandler.getInstance().PaintHandler;
         _tableControl.Refresh();
      }

      /// <summary>
      /// QCR #740151
      /// When we scroll table if the focus was on table child it now belongs to different control
      /// we need to move it to the correct control
      /// </summary>
      private void restoreEditorFocus()
      {
         Form form = GuiUtilsBase.FindForm(_tableControl);
         Form activeForm = GuiUtils.getActiveForm();
         if (form == activeForm || activeForm == null) // defect 125870: we should not execute focus, if other magic form is in focus, otherwise focus is stolen from correct form
         // I've added test for null for the theoretical case when we come from another application
         // ( as we have in the formhandler - for real cases) - if it creates problem we can remove it
         {
            MapData mapData = ((TagData)form.Tag).LastFocusedMapData;

            if (mapData != null && mapData.getControl() != null)
            {
               Object obj = controlsMap.object2Widget(mapData.getControl(), mapData.getIdx());
               var lg = obj as LogicalControl;
               if (lg != null && lg.ContainerManager == this)
               {
                  Control control = lg.getEditorControl();
                  if (control != null && !control.Focused)
                  {
                     GuiUtilsBase.setFocus(control, false, false);
                  }
               }
            }
         }
      }

      /// <summary> 
      /// since there were records added at the beginning of table, 
      /// we must update index of temporary editor
      /// </summary>
      internal void updateTmpEditorIndex()
      {
         if (_tmpEditor != null)
         {
            if (!noRowSelected())
            {
               Control control = _tmpEditor.Control;
               if (control != null && control.Focused) //  table.getDisplay().getFocusControl() == control)
               {
                  Debug.Assert(noRowSelected() == false);
                  //reassign to correct item
                  _tmpEditor.RowIdx = getGuiRowIndex(_selectionIndex);
                  MapData mapData = controlsMap.getMapData(control);
                  //update line index
                  if (mapData != null)
                     mapData.setIdx(_selectionIndex);
               }
            }
         }
      }

      /// <summary> 
      /// save top index
      /// </summary>
      internal void updatePrevTopIndex()
      {
         _prevTopGuiIndex = _tableControl.TopIndex;
      }

      /// <summary> add to to refresh list rows
      /// </summary>
      /// <param name="mgRow"> </param>
      public void addRowToRefresh(int mgRow)
      {
         if (!_rowsToRefresh.Contains(mgRow))
            _rowsToRefresh.Add(mgRow);
      }

      /// <summary> refresh table
      /// </summary>
      internal virtual void refreshTable(bool refreshAll)
      {
         if (RefreshPageNeeded || refreshAll)
            refreshPage();
         else if (_repaintNeeded)
            refreshPage(false);
         else
         {
            if (_rowsToRefresh.Count > 0)
               HideOldTmpEditor();

            if (RefreshHeader)
            {
               RefreshHeaderControls();
               RefreshHeader = false;
            }

            foreach (int row in _rowsToRefresh)
               refreshRow(row);

            if (_rowsToRefresh.Count > 0 && _tmpEditor != null)
            {
               _tableControl.Update();
               _tmpEditor.Layout();
            }
            _rowsToRefresh.Clear();
         }
      }

      /// <summary> check if index is vald table index
      /// </summary>
      /// <param name="index"> </param>
      /// <returns> </returns>
      protected bool isValidIndex(int index)
      {
         return (index < _tableControl.getItemsCount() && index >= 0);
      }

      /// <summary> return all columns
      /// </summary>
      /// <returns> </returns>
      internal List<ILogicalColumn> getColumns()
      {
         return ColumnsManager.Columns;
      }

      /// <summary> return column Manager of column
      /// </summary>
      /// <param name="idx"> </param>
      /// <returns> </returns>
      internal LgColumn getColumn(int idx)
      {
         return (LgColumn)ColumnsManager.getColumn(idx);
      }

      /// <summary> create column
      /// </summary>
      /// <param name="columnControl"> </param>
      /// <param name="mgColumn"> </param>
      /// <returns> </returns>
      internal LgColumn createColumn(GuiMgControl columnControl, int mgColumn)
      {
         ILogicalColumn column = new LgColumn(this, columnControl, mgColumn);
         ColumnsManager.Insert(column, mgColumn, _tableControl.RightToLeftLayout);
         return (LgColumn)column;
      }

      /// <summary>
      /// returns true if we are in creation of columns
      /// </summary>
      internal bool InColumnsCreation
      {
         get
         {
            return ColumnsManager.Columns.Exists(c => c == null);
         }
      }

      /// <summary> executes placement on the table set columns width according to placement this method is based on
      /// On_Table_Placement from gui_table.cpp
      /// </summary>
      /// <param name="prevWidth">previous table width </param>
      /// <param name="dx">width change </param>
      /// <param name="newRect">new table rectangle </param>
      public void ExecuteTablePlacement(int prevWidth, int dx, Rectangle newRect)
      {
         TablePlacementManager.executePlacement(_tableControl, ColumnsManager.Columns, prevWidth, dx, newRect, true, _maxColumnWithEditor, tablePlacementStrategy);
      }

      /// <summary>
      /// Executes the placement on the columns based on the empty area left on the table control.
      /// </summary>
      public void ExecuteFillTablePlacement()
      {
         Debug.Assert(FillWidth);

         if (fillTablePlacementStrategy == null)
            fillTablePlacementStrategy = new FillTablePlacementStrategy();

         int dx = (_tableControl.ClientSize.Width - GetWidthForFillTablePlacement());

         TablePlacementManager.ExecutePlacement(_tableControl, ColumnsManager.Columns, dx, new Rectangle(), true, _maxColumnWithEditor, fillTablePlacementStrategy);
      }

      /// <summary>
      /// Returns the total WidthForFillTablePlacement for all visible columns.
      /// </summary>
      /// <returns></returns>
      private int GetWidthForFillTablePlacement()
      {
         int totalWidth = 0;

         List<ILogicalColumn> columns = ColumnsManager.Columns;
         for (int i = columns.Count - 1; i >= 0; i--)
         {
            LgColumn column = (LgColumn)columns[i];

            if (column.Visible)
               totalWidth += column.WidthForFillTablePlacement;
         }

         return totalWidth;
      }

      /// <summary> sets all rows to be invalidated
      /// </summary>
      internal void invalidateTable()
      {
         for (int i = 0; i < _tableControl.getItemsCount(); i++)
         {
            TableItem item = _tableControl.getItem(i);
            if (item.Controls != null)
               item.IsValid = false;
         }
      }

      /// <summary> computes boundds of cell
      /// </summary>
      /// <param name="mgColumn"></param>
      /// <param name="mgRow"></param>
      /// <returns></returns>
      public Rectangle getCellRect(int mgColumn, int mgRow)
      {
         int guiRow = getGuiRowIndex(mgRow);
         int guiColumn = ColumnsManager.getGuiColumnIdx(mgColumn);
         var rect = new Rectangle(0, 0, 0, 0);
         if (guiRow >= 0 && guiColumn >= 0)
            rect = _tableControl.getCellRect(guiColumn, guiRow);
         return rect;
      }

      /// <summary> return table's children
      /// </summary>
      /// <returns></returns>
      internal List<GuiMgControl> getChildren()
      {
         return _children;
      }

      /// <summary> sets index in column to control's MgControlData
      /// </summary>
      /// <param name="mgControl"></param>
      /// <param name="indexInColumn"></param>
      internal void setIndexIncolumn(GuiMgControl mgControl, int indexInColumn)
      {
         ((MgControlData)_control2DataMap[mgControl]).IndexInColumn = indexInColumn;
      }

      /// <summary> Inner class that will save data that MgControl will be the key
      /// </summary>
      /// <author>  rinat</author>
      internal class MgControlData
      {
         internal int EditorColumn { private set; get; } // number of column in editors, -1 for column that doesn't have permanent editor
         internal int IndexInColumn { set; get; } // index of control in column

         internal MgControlData(int editorColumn)
         {
            EditorColumn = editorColumn;
         }
      }

      /// <summary> set alternating color
      /// </summary>
      /// <param name="color"></param>
      internal void setAlternateColor(Color color)
      {
         _tableControl.AlternateColor = color;
      }

      /// <summary> change color by property
      /// </summary>
      /// <param name="colorBy"></param>
      internal void setColorBy(int colorBy)
      {
         var newVal = (TableColorBy)colorBy;
         _tableControl.ColorBy = newVal;
      }

#if !PocketPC //tmp
      /// <summary> This method returns the current context menu. In case a context menu is not set on the object
      /// itself, we get the parent's context menu
      /// </summary>
      internal ToolStrip getContextMenu()
      {
         ToolStrip ret = _contextMenu;

         if (ret == null)
         {
            GuiMgMenu contextMenu = Events.OnGetContextMenu(_mgControl);
            if (contextMenu != null)
            {
               Form form = GuiUtils.FindForm(_tableControl);
               MapData mapData = controlsMap.getFormMapData(form);

               MenuReference menuRefernce = contextMenu.getInstantiatedMenu(mapData.getForm(), MenuStyle.MENU_STYLE_CONTEXT);
               ret = (ToolStrip)controlsMap.object2Widget(menuRefernce);
            }
         }
         return ret;
      }

      /// <summary> This method sets the current context menu.</summary>
      /// <param name="menu">menu to be set</param>
      internal void setContextMenu(ToolStrip menu)
      {
         _contextMenu = (ContextMenuStrip)menu;
         GuiUtils.setContextMenu(_tableControl, menu);
      }
#else
      /// <summary> This method returns the current context menu. In case a context menu is not set on the object
      /// itself, we get the parent's context menu
      /// </summary>
      internal ContextMenu getContextMenu()
      {
         ContextMenu ret = _contextMenu;

         if (ret == null)
         {
            GuiMgMenu contextMenu = Events.OnGetContextMenu(_mgControl);
            if (contextMenu != null)
            {
               Form form = GuiUtilsBase.FindForm(_tableControl);
               MapData mapData = controlsMap.getFormMapData(form);

               MenuReference menuRefernce = contextMenu.getInstantiatedMenu(mapData.getForm(),
                                                                            MenuStyle.MENU_STYLE_CONTEXT);
               ret = (ContextMenu) controlsMap.object2Widget(menuRefernce);
            }
         }
         return ret;
      }

      /// <summary> This method sets the current context menu.</summary>
      /// <param name="menu">- menu to be set </param>
      internal void setContextMenu(ContextMenu menu)
      {
         _contextMenu = menu;
         GuiUtilsBase.setContextMenu(_tableControl, menu);
      }
#endif

      /// <summary>
      /// This method handles the context menu event.
      /// </summary>
      /// <param name="SourceControl"></param>
      /// <param name="pt"></param>
      internal void handleContextMenu(Control SourceControl, Point pt)
      {
         int columnIndex = -1;
#if !PocketPC
         ToolStrip menu = null;
#else
         ContextMenu menu = null;
#endif

         columnIndex = getColumnIndexFromPoint(pt);
         if (columnIndex > 0 && columnIndex <= ColumnsManager.ColumnsCount)
         {
            LgColumn column = (LgColumn)ColumnsManager.Columns[columnIndex - 1];
            menu = column.getContextMenu();
         }
         // use the table's context menu
         else if (SourceControl == controlsMap.object2Widget(_mgControl))
            menu = getContextMenu();

         GuiUtilsBase.setContextMenu(_tableControl, menu);
         //Save the control on which context menu is invoked. This is required later for creating dummy context menu.
         if (menu != null)
            ((TagData)menu.Tag).MouseDownOnControl = _tableControl;
      }

      /// <summary> This method calculates the matching column of the passed point.</summary>
      /// <param name="pt">- point to check for column</param>
      /// <returns> - matching column index. In case no column exists in the point, return -1.</returns>
      private int getColumnIndexFromPoint(Point pt)
      {
         int columnIndex = 0;
         int sum = -1;

         for (int i = 0; i < ColumnsManager.ColumnsCount; i++)
         {
            LgColumn column = (LgColumn)ColumnsManager.Columns[i];
            if (sum < pt.X)
            {
               if (column.Visible)
               {
                  sum += column.getWidth();
                  columnIndex++;
               }
            }
            else
               break;
         }

         if (sum < pt.X)
            columnIndex++;
         return columnIndex;
      }

      /// <summary> restore table top index to last set
      /// </summary>
      internal void restoreGuitTopIndex()
      {
         _tableControl.TopIndex = _guiTopIndex;
      }

      /// <summary>
      /// dispose table item
      /// </summary>
      /// <param name="item"></param>
      internal void cleanItem(TableItem item)
      {
         var tableChildren = item.Controls;
         if (tableChildren != null)
         {
            for (int i = 0; i < ColumnsManager.ColumnsCount; i++)
            {
               LgList columnChildren = tableChildren[i];
               foreach (LogicalControl child in columnChildren)
                  child.Dispose();
            }
         }
      }

      /// <summary>
      /// set the visibility of the table item
      /// </summary>
      /// <param name="item"></param>
      /// <param name="hide">true to hide, false to show</param>
      internal void SetItemVisibility(TableItem item, bool show, bool applyChildrenVisibility)
      {
         var tableChildren = item.Controls;
         if (tableChildren != null && applyChildrenVisibility)
         {
            for (int i = 0; i < ColumnsManager.ColumnsCount; i++)
            {
               LgList columnChildren = tableChildren[i];
               foreach (LogicalControl child in columnChildren)
                  child.Visible = show;
            }
         }

         item.IsVisibe = show;
      }

      /// <summary>
      /// execute table reorder
      /// </summary>
      /// <param name="tableColumn"></param>
      /// <param name="newtableColumn"></param>
      internal void reorder(TableColumn tableColumn, TableColumn newTableColumn)
      {
         LgColumn movedColumn = (LgColumn)ColumnsManager.getLgColumnByColumn(tableColumn);
         LgColumn newColumn = (LgColumn)ColumnsManager.getLgColumnByColumn(newTableColumn);

         reorder(movedColumn, newColumn);
      }

      /// <summary> reorders columnManagers
      /// </summary>
      /// <param name="movedColumn"></param>
      /// <param name="newColumn"></param>
      internal void reorder(LgColumn movedColumn, LgColumn newColumn)
      {
         int newIndex = ColumnsManager.Columns.IndexOf(newColumn);
         ColumnsManager.Columns.Remove(movedColumn);
         ColumnsManager.Columns.Insert(newIndex, movedColumn);
         var tabOrderList = new List<GuiMgControl>();
         foreach (LgColumn column in ColumnsManager.Columns)
         {
            List<GuiMgControl> children = column.getChildren();
            foreach (GuiMgControl guiMgControl in children)
               tabOrderList.Add(guiMgControl);
         }
         Events.OnTableReorder(_mgControl, tabOrderList);
      }

      /// <summary>
      /// implement HitTest
      /// </summary>
      /// <param name="pt"></param>
      /// <param name="findExact"></param>
      /// <param name="checkEnabled"></param>
      /// <returns></returns>
      internal override MapData HitTest(Point pt, bool findExact, bool checkEnabled)
      {
         return pointToMapData(pt, findExact, checkEnabled);
      }

      /// <summary>
      /// take care of table's columns
      /// </summary>
      internal override void Dispose()
      {
         foreach (LogicalControl control in headerLogicalControlList)
         {
            control.Dispose();
         }

         for (int i = 0; i < ColumnsManager.ColumnsCount; i++)
         {
            LgColumn column = (LgColumn)ColumnsManager.Columns[i];
            column.removeColumn();
         }

         base.Dispose();
      }

      internal override Editor getEditor()
      {
         return _tmpEditor;
      }

      /// <summary>
      ///  Checks whether the column associated with the GuiMgControl is visible or not.
      /// </summary>
      /// <param name="guiMgControl">MgControl attached to a column control</param>
      /// <returns></returns>
      internal bool isColumnVisible(GuiMgControl guiMgControl)
      {
         Debug.Assert(!guiMgControl.isColumnControl());
         Debug.Assert(guiMgControl.IsTableChild);

         int mgColumn = guiMgControl.Layer - 1;
         int guiColumnIdx = ColumnsManager.getGuiColumnIdx(mgColumn);

         return (guiColumnIdx != -1); // column is not hidden
      }

      /// <summary>
      /// gets the RowIdx of last row in the page
      /// </summary>
      /// <returns></returns>
      private int GetLastRowIdxInPage()
      {
         return _tableControl.TopIndex + (IsPartialRowIncludedInRowsInPage() ? _rowsInPage : (_rowsInPage + 1));
      }

#if PocketPC
      internal void PerformLayout(object sender, EventArgs e)
      {
         _tableControl.PerformLayout();
      }
#endif
      /// <summary>
      /// make all logical controls on the table recalculate the colors
      /// </summary>
      public void RecalculateColors()
      {
         for (int i = 0; i < _tableControl.getItemsCount(); i++)
         {
            // don't refresh the controls if in the selected row and the row is highlighted by the table
            bool refreshControls = (i != _selectionIndex) || (RowHighlitingStyle != RowHighlightType.BackgroundControls);

            TableItem tableItem = _tableControl.getItem(i);
            if (tableItem.Controls != null)
            {
               foreach (LgList lgList in tableItem.Controls)
               {
                  foreach (var item in lgList)
                  {
                     if (item is LogicalControl)
                        ((LogicalControl)item).RecalculateColors(refreshControls);
                  }
               }
            }
         }

         // force refresh of the table and the current editor
         refreshPage(false);
         if (_tmpEditor.Control != null)
            _tmpEditor.Control.Invalidate();
      }

      /// <summary>
      /// make all logical controls on the table recalculate the font
      /// </summary>
      public void RecalculateFonts()
      {
         for (int i = 0; i < _tableControl.getItemsCount(); i++)
         {
            TableItem tableItem = _tableControl.getItem(i);
            if (tableItem.Controls != null)
            {
               foreach (LgList lgList in tableItem.Controls)
               {
                  foreach (var item in lgList)
                  {
                     if (item is LogicalControl)
                        ((LogicalControl)item).RecalculateFonts();
                  }
               }
            }
         }

         // force refresh of the table and the current editor
         refreshPage(false);
         if (_tmpEditor.Control != null)
            _tmpEditor.Control.Invalidate();
      }

      /// <summary>
      /// set the background color for the specified line
      /// </summary>
      /// <param name="line"></param>
      /// <param name="mgColor"></param>
      public void SetRowBGColor(int line, MgColor mgColor)
      {
         line = getGuiRowIndex(line);
         Color color = ControlUtils.MgColor2Color(mgColor, true, true);
         _tableControl.SetRowBGColor(line, color);
      }
      #region abstract and virtual functions

      /// <summary>
      /// sets Table Items Count
      /// </summary>
      /// <param name="newCount">the count to set</param>
      internal abstract void SetTableItemsCount(int newCount);

      /// <summary>
      /// sets the virtual items count of table control
      /// </summary>
      /// <param name="count">the count to set</param>
      internal abstract void SetTableVirtualItemsCount(int count);

      /// <summary> 
      /// vertical scroll
      /// </summary>
      /// <param name="ea"></param>
      internal abstract void ScrollVertically(ScrollEventArgs ea);

      /// <summary>
      /// gets the number of rows to refresh
      /// </summary>
      /// <returns></returns>
      protected abstract int GetNumberOfRowsToRefresh();

      /// <summary>
      /// Indicates whether _rowsInPage includes partial rows or not.
      /// </summary>
      /// <returns></returns>
      protected abstract bool IsPartialRowIncludedInRowsInPage();


      /// <summary>
      /// set vertical scroll thumb position of table control
      /// </summary>
      /// <param name="pos"></param>
      internal virtual void SetVScrollThumbPos(int pos)
      {
         Debug.Assert(false);
      }

      /// <summary>
      /// set pageSize for vertical scrollbar
      /// </summary>
      /// <param name="pageSize"></param>
      internal virtual void SetVScrollPageSize(int pageSize)
      {
         Debug.Assert(false);
      }

      /// <summary>
      /// set and save last top index
      /// </summary>
      /// <param name="guiTopIndex"></param>
      internal virtual void SetGuiTopIndex(int guiTopIndex)
      {
      }

      /// <summary> 
      /// resize table
      /// </summary>
      internal virtual bool resize()
      {
         if (_inResize)
            return false;

         // On Mobile we may get the 1st WM_SIZE before the columns are added. If we process it,
         // we lose the extra editor used for partially visible lines.
         // Same may happen on subform QCR #997424 
         if (ColumnsManager.ColumnsCount > 0 && ColumnsManager.Columns[0] == null)
            return false;

         _inResize = true;

         // If table has row placement, rows in page should remain same and row height should change.
         // else, row height should remain same and rows in page should change.
         if (_tableControl.HasRowPlacement)
            _tableControl.ComputeAndSetRowHeight();
         else
            _tableControl.ComputeAndSetRowsInPage(false);

         //Whenever the table control is resized, which changes the no. of rows displayed, create/remove
         //editors depending on the increase/decrease in the no. of rows.
         //In general, if the table 'HasRowPlacement', there won't be any change in the no. of rows.
         //But this can happen in a special case --- when row height is set for the first time.
         //When the row height is set for the first time, the no. of rows in computed and thereafter it
         //remains constant. But when no. of rows is first computed, we should create the editors.
         int newRowsInPage = _tableControl.RowsInPage;
         if (newRowsInPage != _rowsInPage)
         {
            for (int i = 0; i < _editors.Length; i++)
            {
               GuiMgControl guiMgControl = _children[_editor2ConrolMap[i]];

               if (!guiMgControl.IsTableHeaderChild)
               {
                  LgColumn lgColumn = (LgColumn)ColumnsManager.getLgColumnByMagicIdx(guiMgControl.Layer - 1);
                  if (lgColumn != null && lgColumn.Visible)
                  {
                     List<TableEditor> colEditors = _editors[i];
                     if (newRowsInPage > _rowsInPage)
                     {
                        for (int j = _rowsInPage; j < newRowsInPage; j++)
                        {
                           TableEditor editor = createPermanentEditor(guiMgControl);
                           colEditors.Add(editor);
                           GetTableMultiColumnStrategy(false).BringEditorToFront(editor);
                        }
                     }
                     else if (colEditors.Count > 0)
                     {
                        for (int j = newRowsInPage; j < _rowsInPage; j++)
                        {
                           TableEditor editor = colEditors[colEditors.Count - 1];
                           colEditors.RemoveAt(colEditors.Count - 1);
                           disposeEditor(editor);
                        }
                     }
                  }
                  RefreshPageNeeded = true;
               }
            }

            _rowsInPage = newRowsInPage;
         }


         _inResize = false;

         return true;
      }

      /// <summary> translates magic row number into SWT line number
      /// </summary>
      /// <param name="mgIndex"></param>
      /// <returns></returns>
      internal virtual int getGuiRowIndex(int mgIndex)
      {
         return mgIndex;
      }

      /// <summary> translates gui line number into magic row number
      /// </summary>
      /// <param name="guiIndex"></param>
      /// <returns></returns>
      internal virtual int getMagicIndex(int guiIndex)
      {
         return guiIndex;
      }

      /// <summary>
      /// remove rows from table control
      /// </summary>
      /// <param name="rowStart">the index at which to begin remove</param>
      /// <param name="rowsToRemove">number of rows to remove</param>
      internal virtual void RemoveRows(int rowStart, int rowsToRemove, bool manipulateEditors)
      {
         Debug.Assert(false);
      }

      /// <summary>
      /// insert rows into table control
      /// </summary>
      /// <param name="rowStart">the index at which to begin insert</param>
      /// <param name="rowsToInsert">number of rows to be inserted</param>
      internal virtual void InsertRows(int rowStart, int rowsToInsert, bool manipulateEditors)
      {
         Debug.Assert(false);
      }

      internal virtual void SuspendPaint()
      {
         Debug.Assert(false);
      }


      internal virtual void ResumePaint()
      {
         Debug.Assert(false);
      }
      
      #endregion
   }
}
