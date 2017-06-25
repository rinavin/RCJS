using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Helper class for LogicalColumn
   /// </summary>
   public class LogicalColumnHelper : ILogicalColumn
   {
      private readonly TableControl _tableControl;
      private readonly int _mgColumnIdx; // magic column number (layer - 1)
      private int _width;
      public int Width { get { return _width; } set { _width = value; } }

      private int _startXPos; // starting X position - distance between table start X coordinate and column start X coordinate, supports RTL

      private int _orgWidth; // original column width 

      // properties
      private bool _placement; // column placement

      public int MgColumnIdx
      {
         get { return _mgColumnIdx; }
      }

      public bool RightToLeftLayout
      {
         get { return _tableControl.RightToLeftLayout; }
      }

      private TableColumn _tableColumn; // column control, null if column is invisible
      public TableColumn TableColumn
      {
         get { return _tableColumn; }
         set { _tableColumn = value; }
      }

      public bool Visible { get; set; }

      /// <summary>
      ///   index of column in tableControl , -1 if the column is not in tableControl(possible if not visible)
      /// </summary>
      public int GuiColumnIdx
      {
         get
         {
            return _tableColumn == null
                      ? -1
                      : _tableColumn.Index;
         } //if column is invisible its value will be -1
      }

      public LogicalColumnHelper(TableControl tableControl, int mgColumnIdx, bool createTableColumn)
      {
         _tableControl = tableControl;
         _mgColumnIdx = mgColumnIdx;
         _placement = true;

         if (createTableColumn)
         {
            int insertIdx = _tableControl.RightToLeftLayout
                      ? 0
                      : _tableControl.Columns.Count; ;
            _tableColumn = new TableColumn();
            _tableControl.Columns.Insert(insertIdx, _tableColumn);
            Visible = true;
         }
      }

      /// <summary>
      ///   set width
      /// </summary>
      /// <param name = "width"></param>
      /// <param name = "updateNow"></param>
      public void setWidthOnPlacement(int width, bool updateNow)
      {
         setWidth(width, updateNow, false);
      }

      /// <param name = "width">
      /// </param>
      /// <param name = "updateNow">sets column width, if updateNow is true update width of column widget, otherwise only updates
      ///   member
      /// </param>
      public void setWidth(int width, bool updateNow, bool moveEditors)
      {
         _width = width;
         if (_tableColumn != null && updateNow)
            _tableColumn.Width = width;
         if (_tableColumn != null && moveEditors)
            if (_tableControl.RightToLeftLayout)
               _tableControl.MoveColumns(0);
            else
               _tableControl.MoveColumns(_tableColumn.Index);
      }

      /// <summary>
      ///   update "width" member with column width
      /// </summary>
      public void updateWidth()
      {
         _width = _tableColumn.Width;
      }

      /// <summary>
      ///   return whether this column has placement
      /// </summary>
      /// <returns> </returns>
      public bool HasPlacement()
      {
         return _placement;
      }

      /// <summary>
      ///   set placement
      /// </summary>
      /// <param name = "placement"> </param>
      public void setPlacement(bool placement)
      {
         _placement = placement;
      }


      /// <summary>
      ///   set TopBorder
      /// </summary>
      /// <param name = "topBorder"> </param>
      internal void setTopBorder(bool topBorder)
      {
         _tableColumn.TopBorder = topBorder;
      }

      /// <summary>
      ///   set RightBorder
      /// </summary>
      /// <param name = "rightBorder"> </param>
      internal void setRightBorder(bool rightBorder)
      {
         _tableColumn.RightBorder = rightBorder;
      }

      /// <summary>
      ///   return width
      /// </summary>
      /// <returns> </returns>
      public int getWidth()
      {
         return _width;
      }

      /// <summary>
      ///   sets original widht of the column , sets column starting X position
      /// </summary>
      /// <param name = "orgWidth"> </param>
      public void setOrgWidth(int orgWidth)
      {
         _orgWidth = orgWidth;
      }

      /// <summary>
      ///   return column starting X position
      /// </summary>
      /// <returns> </returns>
      public int getStartXPos()
      {
         if (_tableControl.BorderStyle == BorderStyle.Fixed3D)
            if (_tableControl.RightToLeftLayout)
               return _startXPos - TableControl.STUDIO_BORDER_WIDTH;
            else
               return _startXPos + TableControl.STUDIO_BORDER_WIDTH;
         return _startXPos;
      }

      /// <summary>
      ///   get column dx - change of column width
      /// </summary>
      /// <returns> </returns>
      public int getDx()
      {
         return _width - _orgWidth;
      }


      /// <summary>
      ///   set start column position
      /// </summary>
      /// <param name = "startXPos"></param>
      public void setStartXPos(int startXPos)
      {
         _startXPos = startXPos;
      }
   }
}
