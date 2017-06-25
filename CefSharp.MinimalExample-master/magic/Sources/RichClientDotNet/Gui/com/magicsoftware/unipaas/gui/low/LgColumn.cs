using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;

#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ContextMenu = com.magicsoftware.controls.MgMenu.MgContextMenu;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   internal class LgColumn : LogicalControl, IFontOrientation, ILogicalColumn
   {
      private readonly TableManager _tableManager;
      private readonly TableControl _tableControl;
      private int fontOrientation;
      internal int WidthForFillTablePlacement { get; set; }

      LogicalColumnHelper _logicalColumnHelper;

      private readonly List<GuiMgControl> _children; // list all controls that belong to this column

      internal bool IsSortable { get; set; } // sortable

      internal LgColumn(TableManager tableManager, GuiMgControl columnControl, int mgColumnIdx)
         : base(columnControl, tableManager.getTable())
      {
         _tableManager = tableManager;
         _children = new List<GuiMgControl>();
         List<GuiMgControl> tableChildren = tableManager.getChildren();
         int rowChildrenCount = 0;

         for (int i = 0;
              i < tableChildren.Count;
              i++)
         {
            GuiMgControl guiMgControl = tableChildren[i];
            if (guiMgControl.Layer - 1 == mgColumnIdx)
            {
               // add control to the children's list
               _children.Add(guiMgControl);

               if (!guiMgControl.IsTableHeaderChild)
               {
                  // save index in the list on MgControlData
                  tableManager.setIndexIncolumn(guiMgControl, rowChildrenCount++);
               }
            }
         }
         _tableControl = tableManager.getTable();
         _logicalColumnHelper = new LogicalColumnHelper(_tableControl, mgColumnIdx, GuiUtils.AccessTest);
         ContentAlignment = ContentAlignment.MiddleLeft;
         TopBorder = true;
         RightBorder = true;

         if (TableColumn != null)
         {
            base.Visible = true;
            TableColumn.Tag = new TagData();
            ColumnHandler.getInstance().addHandler(TableColumn);
            var td = (TagData)TableColumn.Tag;
            td.ColumnManager = this;
         }
      }

      #region ILogicalColumn members

      /// <summary>
      /// get Width of column
      /// </summary>
      /// <returns></returns>
      public int getWidth()
      {
         return _logicalColumnHelper.getWidth();
      }

      /// <summary>
      /// Set width of column
      /// </summary>
      /// <param name="width"></param>
      /// <param name="updateNow"></param>
      public void setWidthOnPlacement(int width, bool updateNow)
      {
         _logicalColumnHelper.setWidthOnPlacement(width, updateNow);
      }

      /// <param name = "width">
      /// </param>
      /// <param name = "updateNow">sets column width, if updateNow is true update width of column widget, otherwise only updates
      ///   member
      /// </param>      
      public void setWidth(int width, bool updateNow, bool moveEditors)
      {
         _logicalColumnHelper.setWidth(width, updateNow, moveEditors);
         WidthForFillTablePlacement = width;
      }

      /// <summary>
      /// Return whether this column has placement
      /// </summary>
      /// <returns></returns>
      public bool HasPlacement()
      {
         return _logicalColumnHelper.HasPlacement();
      }

      /// <summary>
      ///   set visibility
      /// </summary>
      public override bool Visible
      {
         get { return base.Visible; }
         set
         {
            if (Visible != value)
               setVisible(value);
         }
      }

      /// <summary>
      /// index of column in tableControl , -1 if the column is not in tableControl(possible if not visible)
      /// </summary>
      public int GuiColumnIdx
      {
         get
         {
            return _logicalColumnHelper.GuiColumnIdx;
         }
      }

      /// <summary>
      /// magic column number (layer - 1)
      /// </summary>
      public int MgColumnIdx
      {
         get
         {
            return _logicalColumnHelper.MgColumnIdx;
         }
      }

      /// <summary>
      /// returns TableColumn
      /// </summary>
      public TableColumn TableColumn
      {
         get
         {
            return _logicalColumnHelper.TableColumn;
         }
         set
         {
            _logicalColumnHelper.TableColumn = value;
         }
      }

      /// <summary>
      /// get column dx - change of column width
      /// </summary>
      /// <returns></returns>
      public int getDx()
      {
         return _logicalColumnHelper.getDx();
      }

      /// <summary>
      /// return column starting X position
      /// </summary>
      /// <returns></returns>
      public int getStartXPos()
      {
         return _logicalColumnHelper.getStartXPos();
      }

      #endregion

      /// <summary>
      ///   update "width" member with column width
      /// </summary>
      public void updateWidth()
      {
         _logicalColumnHelper.updateWidth();
         WidthForFillTablePlacement = _logicalColumnHelper.getWidth();
      }

      public bool RightToLeftLayout
      {
         get
         {
            return _logicalColumnHelper.RightToLeftLayout;
         }
      }

      /// <summary>
      ///   set placement
      /// </summary>
      /// <param name = "placement"> </param>
      public void setPlacement(bool placement)
      {
         _logicalColumnHelper.setPlacement(placement);
      }

      /// <summary>
      ///   sets original widht of the column , sets column starting X position
      /// </summary>
      /// <param name = "orgWidth"> </param>
      public void setOrgWidth(int orgWidth)
      {
         _logicalColumnHelper.setOrgWidth(orgWidth);
      }

      /// <summary>
      ///   set start column position
      /// </summary>
      /// <param name = "startXPos"></param>
      public void setStartXPos(int startXPos)
      {
         _logicalColumnHelper.setStartXPos(startXPos);
      }

      protected override void InitializeContentAlignment()
      {
         
      }

      /// <summary>
      ///   set titile
      /// </summary>
      internal override string Text
      {
         get { return base.Text; }
         set
         {
            base.Text = value;
            if (TableColumn != null)
               TableColumn.Text = Text;
         }
      }

      internal override ContentAlignment ContentAlignment
      {
         get { return base.ContentAlignment; }
         set
         {
            base.ContentAlignment = value;
            if (TableColumn != null)
               TableColumn.ContentAlignment = ContentAlignment;
         }
      }

      private bool allowFilter;
      internal bool AllowFilter
      {
         get 
         {
            return allowFilter;
         }
         set
         {
            allowFilter = value;
            if (TableColumn != null)
               TableColumn.AllowFilter = AllowFilter;
         }
      }

      private bool rightBorder;
      internal bool RightBorder
      {
         get
         {
            return rightBorder;
         }
         set
         {
            rightBorder = value;
            if (TableColumn != null)
               TableColumn.RightBorder = RightBorder;
         }
      }

      private bool topBorder;
      internal bool TopBorder
      {
         get
         {
            return topBorder;
         }
         set
         {
            topBorder = value;
            if (TableColumn != null)
               TableColumn.TopBorder = TopBorder;
         }
      }

      /// <summary>
      ///   set visible column
      /// </summary>
      /// <param name = "visible"> </param>
      internal void setVisible(bool visible)
      {
         if (visible == Visible)
            return;

         bool applyPlacement = false;
         bool layoutSuspended = false;

         if (visible == false)
         {
            TableColumn.Dispose();
            TableColumn = null;

            //If there is no scrollbar (i.e. there is blank area) after hiding the column, apply the placement.
            if (_tableManager.FillWidth)
               applyPlacement = !_tableControl.isHscrollShown();
         }
         else if (visible)
         {
            if (_tableManager.FillWidth)
            {
               //If there is scrollbar even before showing the new column, do not apply the placement.
               applyPlacement = !_tableControl.isHscrollShown();

               if (applyPlacement)
               {
                  _tableManager._tableControl.SuspendLayout();
                  layoutSuspended = true;
               }
            }

            List<ILogicalColumn> columns = _tableManager.getColumns();
            int guiColumnIdx = 0;

            // find new Gui index
            int orgGuiIndex = columns.IndexOf(this);
            for (int i = 0;
                 i < orgGuiIndex;
                 i++)
            {
               if (((LgColumn)columns[i]).GuiColumnIdx >= 0)
                  guiColumnIdx++;
            }

            TableColumn = new TableColumn();
            _tableControl.Columns.Insert(guiColumnIdx, TableColumn);

            TableColumn.Tag = new TagData();
            ColumnHandler.getInstance().addHandler(TableColumn);
            var td = (TagData)TableColumn.Tag;
            td.ColumnManager = this;

            // set column properties
            TableColumn.Text = Text;
            TableColumn.FgColor = FgColor;
            TableColumn.BgColor = BgColor;

            TableColumn.Font = Font;
            TableColumn.ContentAlignment = ContentAlignment;
            TableColumn.FontOrientation = FontOrientation;

            TableColumn.AllowFilter = AllowFilter;
            TableColumn.RightBorder = RightBorder;
            TableColumn.TopBorder = TopBorder;

            TableColumn.Width = Width;
         }

         _logicalColumnHelper.Visible = visible;
         base.Visible = visible;

         if (applyPlacement)
         {
            _tableManager.ExecuteFillTablePlacement();

            if (layoutSuspended)
               _tableManager._tableControl.ResumeLayout();
         }

         _tableManager.updateEditorsOnColumnVisibility(MgColumnIdx, visible);
      }

      /// <summary>
      ///   Width
      /// </summary>
      public override int Width
      {
         get { return _logicalColumnHelper.Width; }
         set { _logicalColumnHelper.Width = value; }
      }

      /// <summary>
      ///   remove columns from controls Map
      /// </summary>
      internal void removeColumn()
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         controlsMap.remove(GuiMgControl);
      }

      /// <summary>
      ///   return column's children
      /// </summary>
      /// <returns> </returns>
      internal List<GuiMgControl> getChildren()
      {
         return _children;
      }

#if !PocketPC
      //tmp
      /// <summary>
      ///   This method returns the current context menu. In case a context menu is not set on the object itself, we
      ///   get the parent's context menu
      /// </summary>
      internal ToolStrip getContextMenu()
      {
         ControlsMap controlsMap = ControlsMap.getInstance();

         ToolStrip ret = base.ContextMenu;
         if (ret == null)
         {
            GuiMgMenu contextMenu = Events.OnGetContextMenu(GuiMgControl);
            if (contextMenu != null)
            {
               Form form = GuiUtilsBase.FindForm(_tableControl);
               MapData mapData = controlsMap.getFormMapData(form);

               MenuReference menuRefernce = contextMenu.getInstantiatedMenu(mapData.getForm(),
                                                                            MenuStyle.MENU_STYLE_CONTEXT);
               ret = (ToolStrip)controlsMap.object2Widget(menuRefernce);
            }
         }
         return ret;
      }
#else
      /// <summary> This method returns the current context menu. In case a context menu is not set on the object itself, we
      /// get the parent's context menu
      /// </summary>
      internal ContextMenu getContextMenu()
      {
         ControlsMap controlsMap = ControlsMap.getInstance();

         ContextMenu ret = base.ContextMenu;
         if (ret == null)
         {
            GuiMgMenu contextMenu = Events.OnGetContextMenu(GuiMgControl);
            if (contextMenu != null)
            {      
               Form form = GuiUtils.FindForm(_tableControl);
               MapData mapData = controlsMap.getFormMapData(form);

               MenuReference menuRefernce = contextMenu.getInstantiatedMenu(mapData.getForm(), MenuStyle.MENU_STYLE_CONTEXT);
               ret = (ContextMenu)controlsMap.object2Widget(menuRefernce);
            }
         }
         return ret;
      }
#endif

      /// <summary>
      /// </summary>
      /// <param name="direction"></param>
      internal void SetSortMark(int direction)
      {
         switch (direction)
         {
            case -1:
               TableColumn.SortMark = HeaderSectionSortMarks.Non;
               break;
            case 0:
               TableColumn.SortMark = HeaderSectionSortMarks.Up;
               break;
            case 1:
               TableColumn.SortMark = HeaderSectionSortMarks.Down;
               break;
            default:
               break;
         }

         List<ILogicalColumn> columns = _tableManager.getColumns();
         foreach (LgColumn lgColumn in columns)
         {
            if (lgColumn != this && lgColumn.TableColumn != null)
               lgColumn.TableColumn.SortMark = HeaderSectionSortMarks.Non;
         }
      }

      /// <summary>
      ///   clears the sort mark on the column
      /// </summary>
      internal void clearSortMark()
      {
         TableColumn.SortMark = HeaderSectionSortMarks.Non;
      }

      /// <summary>
      ///   set foreground color
      /// </summary>
      internal override Color FgColor
      {
         get { return base.FgColor; }
         set
         {
            if (TableColumn != null)
               TableColumn.FgColor = value;
            base.FgColor = value;
         }
      }

      /// <summary>
      ///   set foreground color
      /// </summary>
      internal override Color BgColor
      {
         get { return base.BgColor; }
         set
         {
            if (TableColumn != null)
               TableColumn.BgColor = value;
            base.BgColor = value;
            _tableManager.RefreshPageNeeded = true;
         }
      }

      /// <summary>
      ///   set font
      /// </summary>
      internal override Font Font
      {
         get { return base.Font; }
         set
         {
            if (TableColumn != null)
               TableColumn.Font = value;
            base.Font = value;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public int FontOrientation
      {
         get { return fontOrientation; }
         set
         {
            fontOrientation = value;
            if (TableColumn != null)
               TableColumn.FontOrientation = value;
         }
      }

      /// <summary>
      ///   get sort direction
      /// </summary>
      /// <returns></returns>
      internal int getSortDirection()
      {
         int direction = -1;

         switch (TableColumn.SortMark)
         {
            case HeaderSectionSortMarks.Non:
            case HeaderSectionSortMarks.Down:
               direction = 0;
               break;
            case HeaderSectionSortMarks.Up:
               direction = 1;
               break;
            default:
               break;
         }

         return direction;
      }
   }
}
