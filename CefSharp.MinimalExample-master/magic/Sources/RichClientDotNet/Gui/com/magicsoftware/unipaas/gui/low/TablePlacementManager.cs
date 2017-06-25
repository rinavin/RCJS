using System;
using System.Collections.Generic;
using System.Text;
using Controls.com.magicsoftware;
using System.Drawing;
using com.magicsoftware.controls;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// Placement manager for TableControl
   /// </summary>
   public class TablePlacementManager : TablePlacementManagerBase
   {
      #region Ctors

      public TablePlacementManager(int columnsCount)
         : base(columnsCount)
      {

      }

      #endregion

      #region overridden methods

      public override void SetTableControlBounds(TableControl tableControl, Rectangle bounds)
      {
         GuiUtilsBase.setBounds(tableControl, bounds);
      }

      #endregion
   }
}
