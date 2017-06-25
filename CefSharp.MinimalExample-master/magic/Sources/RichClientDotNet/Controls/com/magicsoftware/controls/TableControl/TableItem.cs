using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using LgList = System.Collections.Generic.List<Controls.com.magicsoftware.PlacementDrivenLogicalControl>;
using Controls.com.magicsoftware;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// table item class
   /// </summary>
   public class TableItem
   {
      private TableControl _tableControl;

      internal int Idx { get; set; }

      public LgList[] Controls { get; set; }
      public bool IsValid { get; set; }

      public Color RowBGColor { get; set; }

      /// <summary>
      /// This is used for now only by online programs - OL hides the controls on invalid lines, while RC removes them.
      /// </summary>
      public bool IsVisibe { get; set; }

      /// <summary>
      /// All table controls sorted by Z order
      /// </summary>
      public LgList ZOrderSortedControls { get; set; }

      public Rectangle Bounds
      {
         get { return _tableControl.getItemBounds(this); }
      }

      /// <summary>
      /// constructor
      /// </summary>
      /// <param name="idx"></param>
      /// <param name="tableControl"></param>
      internal TableItem(int idx, TableControl tableControl)
      {
         _tableControl = tableControl;
         Idx = idx;
         Controls = null;
         IsValid = true;
         RowBGColor = Color.Empty;
         IsVisibe = true;
      }

      /// <summary>
      /// dispose item
      /// </summary>
      internal void Dispose()
      {
         _tableControl.onItemDispose(this);
      }

      public LgList GetAllColumnsControlsByZOrder()
      {
         LgList allControls = new LgList();
         if (Controls != null)
         {
            foreach (LgList lst in Controls)
               if (lst != null)
                  foreach (PlacementDrivenLogicalControl lg in lst)
                     allControls.Add(lg);
         }

         if (allControls != null)
            allControls.Sort((child1, child2) => child1.ZOrder.CompareTo(child2.ZOrder));
         return allControls;
      }


      /// <summary>
      /// </summary>
      /// <param name="item"></param>
      /// <param name="GuiColumnIdx"></param>
      /// <returns></returns>
      internal LgList getColumnChildren(int GuiColumnIdx, ColumnsManager columnsManager)
      {
         var tableChildren = Controls;
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
   }
}
