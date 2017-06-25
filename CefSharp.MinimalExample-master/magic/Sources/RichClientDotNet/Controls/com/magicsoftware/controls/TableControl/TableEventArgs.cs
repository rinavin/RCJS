using System.Text;
using System.Windows.Forms;
using com.magicsoftware.controls;
using System;
using System.Drawing;
using com.magicsoftware.win32;
using System.Collections.Generic;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// implements table control : responsible for events arguments
   /// </summary>
   /// <summary>
   /// Table Item Dispose Args
   /// </summary>
   [Serializable]
   public class TableItemDisposeArgs : EventArgs
   {
      //Fields
      public TableItem Item { get; set; }

      internal TableItemDisposeArgs(TableItem item)
      {
         this.Item = item;
      }

   }
   /// <summary>
   /// arguments for paint roe events
   /// </summary>
   public class TablePaintRowArgs : EventArgs
   {
      // Fields
      private Rectangle rect;
      private Graphics gr;
      private TableItem item;
      private int row;

      //Properties
      public Rectangle Rect { get { return rect; } } //rectangle
      public Graphics Graphics { get { return gr; } } //graphics

      public TableItem Item { get { return item; } } //table item
      public int Row { get { return row; } } //row
      public List<CellData> CellsData { get { return cellsData; } } //list of cells with its data
      List<CellData> cellsData = new List<CellData>();

      // Construction
      internal TablePaintRowArgs(Graphics gr, int row, TableItem item, Rectangle rect)
      {

         this.gr = gr; //graphics
         this.rect = rect; //rectangle
         this.item = item;
         this.row = row;
      }

      public void addCellData(CellData cellData)
      {
         cellsData.Add(cellData);
      }
   }

   /// <summary>
   /// class for data of the cell
   /// </summary>
   public class CellData
   {
      private int columnIdx;
      private Rectangle rect;

      public int ColumnIdx { get { return columnIdx; } }
      public Rectangle Rect { get { return rect; } }

      // Construction
      internal CellData(int columnIdx, Rectangle rect)
      {
         this.rect = rect;
         this.columnIdx = columnIdx;
      }
   }

   /// <summary>
   /// reorder event
   /// </summary>
   [Serializable]
   public class TableReorderArgs : EventArgs
   {
      //Fields
      public TableColumn column { get; private set; }
      public TableColumn NewColumn { get; private set; }

      internal TableReorderArgs(TableColumn column, TableColumn newColumn)
      {
         this.column = column;
         NewColumn = newColumn;
      }
   }

   /// <summary>
   /// reorder event
   /// </summary>
   [Serializable]
   public class TableColumnArgs : EventArgs
   {
      //Fields
      public TableColumn Column { get; private set; }

      internal TableColumnArgs(TableColumn column)
      {
         this.Column = column;
      }
   }

#if PocketPC
   /// <summary>
   /// 
   /// </summary>
   public class ScrollEventArgs : EventArgs
   {
      public int NewValue { get; set; }
      public int OldValue { get; set; }
      public ScrollEventType Type { get; set; }
      public ScrollOrientation ScrollOrientation { get; set; }

      public ScrollEventArgs()
      {
         throw new NotImplementedException();
      }

      public ScrollEventArgs(ScrollEventType type, int oldValue, int newValue, ScrollOrientation scroll)
      {
         NewValue = newValue;
         OldValue = oldValue;
         Type = type;
         ScrollOrientation = scroll;
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public enum ScrollEventType
   {
      SmallDecrement = 0,
      SmallIncrement = 1,
      LargeDecrement = 2,
      LargeIncrement = 3,
      ThumbPosition = 4,
      ThumbTrack = 5,
      First = 6,
      Last = 7,
      EndScroll = 8,
   }

   public enum ScrollOrientation
   {
      HorizontalScroll = 0,
      VerticalScroll = 1,
   }
#endif
}