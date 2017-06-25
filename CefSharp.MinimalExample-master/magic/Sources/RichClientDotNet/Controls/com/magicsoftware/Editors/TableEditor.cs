using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.editors;
using System.Diagnostics;
using Controls.com.magicsoftware;
using Controls.com.magicsoftware.controls.MgLine;
using com.magicsoftware.util;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Class table editor - manages window table control
   /// </summary>
   public class TableEditor : Editor
   {
      private TableColumn _column = null;
      public int RowIdx { get; set; } // table row
      public BoundsComputer BoundsComputer { get; set; } //interface for computing editors bounds
      public TableColumn Column // set table column
      {
         get { return _column; }
         set
         {
            if (_column != null)
            {
               _column.AfterTrackHandler -= new TableColumn.SectionTrackHandler(columnChange);
               _column.MoveHandler -= new TableColumn.SectionTrackHandler(columnChange);
               _column.DisposeHandler -= new TableColumn.SectionTrackHandler(columnDispose);
            }
            if (value != null)
            {
               Debug.Assert(!value.IsDisposed);
               _column = value;
               _column.AfterTrackHandler += new TableColumn.SectionTrackHandler(columnChange);
               _column.MoveHandler += new TableColumn.SectionTrackHandler(columnChange);
               _column.DisposeHandler += new TableColumn.SectionTrackHandler(columnDispose);
            }
         }
      }

      /// <summary>
      /// Editors z order
      /// </summary>
      public int EditorZOrder { get; set; }

      public bool IsHeaderEditor { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="ea"></param>
      void columnDispose(object sender, EventArgs ea)
      {
         _column = null;
      }

      /// <summary>
      /// activated on any change 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="ea"></param>
      void columnChange(object sender, EventArgs ea)
      {
         Layout();
      }

      public TableEditor(TableControl table)
         : this(table, 0, null)
      {
      }

      /// <summary>
      /// constructor
      /// </summary>
      /// <param name="table"></param>
      /// <param name="row"></param>
      /// <param name="columnIdx"></param>
      public TableEditor(TableControl table, int row, TableColumn column)
         : base(table)
      {
         RowIdx = row;
         Column = column;
         // Register TableControl.RowHeightChanged event so as to adjust the height 
         // according to row height.
         table.RowHeightChanged += new EventHandler(OnRowHeightChanged);
      }

      /// <summary> Re-adjust the editors when table's row height changes. </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnRowHeightChanged(object sender, EventArgs ea)
      {
         // Defect 132041: Do not apply placement on Header control when row height changes
         if(!IsHeaderEditor)
            Layout();
      }

      /// <summary>
      /// return true if control is hidden
      /// </summary>
      /// <returns></returns>
      public override bool isHidden()
      {
         return BoundsComputer == null;
      }

      /// <summary>
      /// calculate editor bounds
      /// </summary>
      /// <returns></returns>
      public override Rectangle Bounds()
      {
         Rectangle rect = new Rectangle();

         if (_column != null)
         {
            rect = ((TableControl)parentControl).getCellRect(_column.Index, RowIdx);
            if (IsHeaderEditor)
            {
               ICoordinator coordinator = BoundsComputer as ICoordinator;
               if (coordinator != null)
               {
                  if (coordinator.X < 0)
                  {
                     TableControl tableControl = (TableControl)parentControl;
                     // Defect# 130148: When a column is re-ordered with 1st column, then only use actual co-ordinates for X,
                     // otherwise this problem occurs i.e. control from 1st column remains at same location
                     if ((!(tableControl.RightToLeftLayout) && _column.Index == 0) ||
                        (tableControl.RightToLeftLayout && tableControl.ColumnCount - 1 == _column.Index))
                        rect.X = coordinator.X;
                  }
                  rect.Y = coordinator.Y < 0 ? coordinator.Y : 0;
               }

               rect.Height = Math.Abs(rect.Y) + ((TableControl)parentControl).Height;
               if (((TableControl)parentControl).RightToLeftLayout)
               {
                  rect.Width = rect.Width + rect.X;
                  rect.X = 0;
               }
               else
                  rect.Width = parentControl.Bounds.Width - rect.X;
            }
         }

         // Defect 140258: If header control is hidden when column to which it is attached is hidden, 
         // BoundsComputer is null. So return empty rectangle in this case.
         if (BoundsComputer != null)
            return BoundsComputer.computeEditorBounds(rect, IsHeaderEditor);
         else
            return Rectangle.Empty;
      }

      protected override void UpdateControlInfo(Control control, int height)
      {
         MgLine line = control as MgLine;
         if (line != null)
         {
            TableCoordinatorBase coordinator = BoundsComputer as TableCoordinatorBase;
            if (coordinator != null)
            {
               Point start = coordinator.GetLeftTop();
               Point end = coordinator.GetRightBottom();

               if (start.X == end.X)
                  line.DirectionOfLine = LineDirection.Vertical;
               else if (start.Y == end.Y)
                  line.DirectionOfLine = LineDirection.Horizontal;
               else if ((start.X > end.X && start.Y < end.Y) || (start.X < end.X && start.Y > end.Y))
                  line.DirectionOfLine = LineDirection.NESW;
               else
                  line.DirectionOfLine = LineDirection.NWSE;
            }
         }
         if (control is MgComboBox)
            ControlUtils.SetComboBoxItemHeight(control, height);
      }

      /// <summary>
      /// dispose table editor
      /// </summary>
      public override void Dispose()
      {
         base.Dispose();
         Column = null;
         BoundsComputer = null;
      }

      /// <summary>
      /// hide editor
      /// </summary>
      public override void Hide()
      {
         BoundsComputer = null;
         base.Hide();
      }
   }
}
