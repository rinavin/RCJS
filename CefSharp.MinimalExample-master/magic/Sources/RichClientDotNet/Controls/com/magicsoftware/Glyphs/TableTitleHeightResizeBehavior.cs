using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.ComponentModel;

namespace com.magicsoftware.Glyphs
{
  /// <summary>
  /// Title height resize behavior
  /// </summary>
   internal class TableTitleHeightResizeBehavior : ResizeBehavior
   {
      TableControl tableControl;
      public TableTitleHeightResizeBehavior(TableControl tableControl, IServiceProvider seviceProvider)
         : base(tableControl, seviceProvider)
      {
         this.tableControl = tableControl;

      }

      override protected bool CanResize(int newSize)
      {
         if (newSize >= 0 && newSize < tableControl.ClientSize.Height - tableControl.RowHeight)
            return true;
         return false;
      }

      override protected void SetNewValueOnDrag(int value)
      {
         tableControl.TitleHeight = value;
      }

      override protected int GetOrgValue()
      {
         return tableControl.TitleHeight;

      }

      override protected PropertyDescriptor GetChangedProp()
      {
         return TypeDescriptor.GetProperties(tableControl)["TitleHeight"];
      }


   }
}
