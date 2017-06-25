using System;
using Controls.com.magicsoftware;
using com.magicsoftware.controls;
using System.Drawing;

namespace com.magicsoftware.unipaas.gui.low
{
   internal class FillTablePlacementStrategy : ITablePlacementStrategy
   {
      public int GetColumnWidth(ILogicalColumn column)
      {
         return ((LgColumn)column).WidthForFillTablePlacement;
      }

      public bool ShouldApplyPlacementToHiddenColumns(bool defaultVal)
      {
         return false;
      }
   }
}
