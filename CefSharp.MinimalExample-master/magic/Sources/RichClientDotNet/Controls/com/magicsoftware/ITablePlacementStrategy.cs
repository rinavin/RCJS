using System;
using com.magicsoftware.controls;
using System.Drawing;

namespace Controls.com.magicsoftware
{
   public interface ITablePlacementStrategy
   {
      int GetColumnWidth(ILogicalColumn column);

      bool ShouldApplyPlacementToHiddenColumns(bool defaultVal);
   }
}
