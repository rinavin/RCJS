using System;
using com.magicsoftware.controls;
using System.Drawing;

namespace Controls.com.magicsoftware
{
   public class TablePlacementStrategy : ITablePlacementStrategy
   {
      public int GetColumnWidth(ILogicalColumn column)
      {
         return column.getWidth();
      }

      public bool ShouldApplyPlacementToHiddenColumns(bool defaultVal)
      {
         return defaultVal;
      }
   }
}
