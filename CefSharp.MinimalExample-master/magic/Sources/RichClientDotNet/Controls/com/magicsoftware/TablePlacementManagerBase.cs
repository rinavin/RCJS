using System;
using System.Collections.Generic;
using System.Drawing;
using com.magicsoftware.controls;
using System.Windows.Forms;

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// Manages placement for controls in TableControl
   /// </summary>
   public class TablePlacementManagerBase
   {
      #region fields/properties
      public bool ShouldApplyPlacementToHiddenColumns { get; set; }

      #endregion

      #region Ctors
      int _placementIdx;
      public TablePlacementManagerBase(int columnsCount)
      {
         _placementIdx = columnsCount - 1;
         ShouldApplyPlacementToHiddenColumns = true;
      }

      #endregion

      #region Helper methods

      /// <summary>
      /// 
      /// </summary>
      /// <param name="bounds"></param>
      public virtual void SetTableControlBounds(TableControl tableControl, Rectangle bounds)
      {
      }

        /// <summary>
        /// executes placement on the table set columns width according to placement this method is based on
        /// On_Table_Placement from gui_table.cpp
        /// </summary>
        /// <param name="tablecontrol"></param>
        /// <param name="columns"></param>
        /// <param name="prevWidth">previous table width </param>
        /// <param name="dx">width change </param>
        /// <param name="newRect">new table rectangle </param>
        /// <param name="considerEditors"></param>
        /// <param name="maxColumnWithEditor"></param>
        /// <param name="tablePlacementStrategy"></param>
        public void executePlacement(TableControl tablecontrol, List<ILogicalColumn> columns, int prevWidth, int dx, Rectangle newRect, bool considerEditors, int maxColumnWithEditor, TablePlacementStrategy tablePlacementStrategy)
        {
            int logSize;
            int NCWidth = tablecontrol.Width - tablecontrol.ClientRectangle.Width;
            int newWidth = newRect.Width - NCWidth;

            logSize = getLogSize(columns);
            prevWidth -= NCWidth;

            // Leave space for vertical scrollbar if newWidth > logical size of columns. 
            // This is needed post-2.5 because since then vertical scrollbar is shown dynamically (depending on whether no. of records > no. of rows)
            // If scrollbar isn't taken into account, then there can be situations where, after applying placement, columns will occupy entire
            // table width and then vertical scrollbar is displayed if few records are created. The problem is that in such
            // situations, even horizontal scrollbar is shown (because now vertical scrollbar is occupying some space from last column)
            // Since the whole purpose is to avoid showing horizontal scrollbar while showing vertical scrollbar, this should be done only if 
            // horizontal scrollbar isn't visible
            if (!tablecontrol.IsHorizontalScrollBarVisible)
            { 
                if (logSize < newWidth - SystemInformation.VerticalScrollBarWidth)
                    newWidth -= SystemInformation.VerticalScrollBarWidth;
                // Similarly, if logical size of columns > prevWidth, scrollbar width shouldn't be considered for placement
                if (logSize > prevWidth + SystemInformation.VerticalScrollBarWidth)
                    prevWidth -= SystemInformation.VerticalScrollBarWidth;
            }

         if (prevWidth < logSize)
         // scroll bar is present
         {
            if (logSize > newWidth)
               // scroll bar is still present after the resize
               dx = 0;
            // scrollbar will be removed after resize
            else
            {
               if (dx > 0)
                  dx -= (logSize - prevWidth);
            }
         }

         if (dx >= 0)
         {
            SetTableControlBounds(tablecontrol, newRect);
         }

         // Handle Row Placement
         if (tablecontrol.HasRowPlacement)
            tablecontrol.ComputeAndSetRowHeight();

         if (dx != 0)
            ExecutePlacement(tablecontrol, columns, dx, newRect, considerEditors, maxColumnWithEditor, tablePlacementStrategy);

         if (dx < 0)
         {
            SetTableControlBounds(tablecontrol, newRect);
         }
      }

      /// <summary>
      /// Execute the placement
      /// </summary>
      /// <param name="tablecontrol"></param>
      /// <param name="columns"></param>
      /// <param name="dx"></param>
      /// <param name="newRect"></param>
      /// <param name="considerEditors"></param>
      /// <param name="maxColumnWithEditor"></param>
      /// <param name="tablePlacementStrategy"></param>
      public void ExecutePlacement(TableControl tablecontrol, List<ILogicalColumn> columns, int dx, Rectangle newRect, bool considerEditors, int maxColumnWithEditor, ITablePlacementStrategy tablePlacementStrategy)
      {
         int colNum = 0;

         // compute number of columns for placement
         for (int i = 0; i < columns.Count; i++)
         {
            ILogicalColumn column = columns[i];
            if (dx <= 0 && tablePlacementStrategy.GetColumnWidth(column) == 0)
               continue;
            if (PlacementAllowed(column, tablePlacementStrategy.ShouldApplyPlacementToHiddenColumns(ShouldApplyPlacementToHiddenColumns)))
               colNum++;
         }

         // Handle Column Placement
         if (colNum != 0)
         {

            int colDx = dx / colNum; // dx to move a column
            int rem = Math.Abs(dx) - Math.Abs(colDx) * colNum;
            int addOn = 0;
            bool secondRound = false;

            for (int i = columns.Count - 1; i >= 0; i--)
            {
               ILogicalColumn column = columns[i];

               int colWidth = tablePlacementStrategy.GetColumnWidth(column);

               if (!PlacementAllowed(column, tablePlacementStrategy.ShouldApplyPlacementToHiddenColumns(ShouldApplyPlacementToHiddenColumns)))
                  continue;
               if (colDx < 0 && colWidth == 0)
                  continue;
               if (colDx < 0 && (colWidth < Math.Abs(colDx) + Math.Abs(addOn)) && colWidth > 0)
               {
                  // the column width is less then the amount need to shrink it, pass the remainder of ColMoveDX to
                  // the next col's.
                  addOn += colWidth + colDx; // save the remainder
                  column.setWidthOnPlacement(0, false);
                  secondRound = true;
               }
               else
               {
                  column.setWidthOnPlacement(Math.Max(0, colWidth + colDx + addOn), false);
                  secondRound = false;
                  addOn = 0;
               }
            }
            if (secondRound)
            {
               for (int i = columns.Count - 1; i >= 0; i--)
               {
                  ILogicalColumn column = columns[i];
                  int colWidth = column.getWidth();
                  if (addOn < 0 && PlacementAllowed(column, tablePlacementStrategy.ShouldApplyPlacementToHiddenColumns(ShouldApplyPlacementToHiddenColumns)) && colWidth > 0)
                  {
                     if (colWidth < Math.Abs(addOn))
                     {
                        addOn += colWidth;
                        column.setWidthOnPlacement(0, false);
                     }
                     else
                     {
                        column.setWidthOnPlacement(colWidth + addOn, false);
                        addOn = 0;
                     }
                  }
               }
            }

            // resize columns the remainder of dx
            int count = 0;
           
            while (rem > 0 && count < columns.Count)
            {
               ILogicalColumn column = columns[_placementIdx];
               if (PlacementAllowed(column, tablePlacementStrategy.ShouldApplyPlacementToHiddenColumns(ShouldApplyPlacementToHiddenColumns)))
               {
                  int colWidth = column.getWidth();
                  if (dx > 0 || (dx < 0 && colWidth > 0))
                  {
                     long ResizeDx = rem;

                     if (dx < 0)
                        // can not set widht less then 0
                        ResizeDx = Math.Min(ResizeDx, colWidth);
                     colWidth = (int)(colWidth + (dx > 0 ? ResizeDx : -ResizeDx));
                     column.setWidthOnPlacement(colWidth, false);
                     rem = (int)(rem - ResizeDx);
                  }
               }
               if (--_placementIdx < 0)
                  _placementIdx = columns.Count - 1;
               count++;
            }
            //QCR #913300, column.setWidthOnPlacement causes repaints,
            //this is will improve performance

            tablecontrol.AllowPaint = false;

            if (considerEditors)
            {
               if (tablecontrol.RightToLeft == System.Windows.Forms.RightToLeft.Yes)
               {
                  for (int i = columns.Count - 1; i >= 0; i--)
                  {
                     ILogicalColumn column = columns[i];
                     HandleEditors(tablecontrol, column, i <= maxColumnWithEditor, tablePlacementStrategy.ShouldApplyPlacementToHiddenColumns(ShouldApplyPlacementToHiddenColumns));
                  }
               }
               else
               {
                  for (int i = 0; i < columns.Count; i++)
                  {
                     ILogicalColumn column = columns[i];
                     HandleEditors(tablecontrol, column, i <= maxColumnWithEditor, tablePlacementStrategy.ShouldApplyPlacementToHiddenColumns(ShouldApplyPlacementToHiddenColumns));
                  }
               }
            }

            tablecontrol.MoveColumns(0);

            tablecontrol.AllowPaint = true;
            tablecontrol.Invalidate();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="column"></param>
      /// <param name="shouldPaintImmediately"></param>
      private void HandleEditors(TableControl tableControl, ILogicalColumn column, bool shouldPaintImmediately, bool shouldApplyPlacementToHiddenColumns)
      {
         if (PlacementAllowed(column, shouldApplyPlacementToHiddenColumns))
         {
            //we must paint all columns before persistent editors, otherwise these columns get 
            //garbaged by the editors
            if (shouldPaintImmediately)
               tableControl.AllowPaint = true;
            column.setWidthOnPlacement(column.getWidth(), true);
            if (shouldPaintImmediately)
               tableControl.AllowPaint = false;
         }
      }

      /// <summary>
      /// return table log size which is sum of it's visible columns width
      /// </summary>
      /// <param name="columns"></param>
      /// <returns></returns>
      internal int getLogSize(List<ILogicalColumn> columns)
      {
         int logSize = 0;
         for (int i = 0; i < columns.Count; i++)
         {
            ILogicalColumn column = columns[i];
            if (ShouldApplyPlacementToHiddenColumns || column.Visible)
               logSize += column.getWidth();
         }
         return logSize;
      }

      /// <summary>
      /// compute allowed placement for table depending on column visibility
      /// </summary>
      /// <param name="dx"></param>
      /// <param name="columns"></param>
      /// <returns></returns>
      public int computeWidthPlacement(int dx, List<ILogicalColumn> columns)
      {
         int allowedPlacement = allowedPlacementWidth(dx, columns);
         // if allowedPlacementWidth returns -1, it means that no columns were found for placement
         if (allowedPlacement == -1)
            dx = 0;
         else
            dx = Math.Max(-allowedPlacement, dx);

         return dx;
      }

      /// <summary>
      /// Accumulates all the widthes of columns with placement if there is no such columns return -1
      /// </summary>
      /// <param name="dx"></param>
      /// <param name="columns"></param>
      /// <returns></returns>
      public int allowedPlacementWidth(int dx, List<ILogicalColumn> columns)
      {
         bool hasPlacement = false;
         int width = 0;

         for (int i = 0; i < columns.Count; i++)
         {
            ILogicalColumn column = columns[i];
            if (PlacementAllowed(column, ShouldApplyPlacementToHiddenColumns))
            {
               // for such column placement is not allowed
               if (dx < 0 && column.getWidth() == 0)
                  continue;
               hasPlacement = true;
               width += column.getWidth();
            }
         }
         if (!hasPlacement)
            width = -1;
         return width;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="column"></param>
      /// <returns></returns>
      bool PlacementAllowed(ILogicalColumn column, bool shouldApplyPlacementToHiddenColumns)
      {
         return (shouldApplyPlacementToHiddenColumns || column.Visible) && column.HasPlacement();
      }

      #endregion
   }
}
