using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.ComponentModel;

namespace com.magicsoftware.Glyphs
{
   /// <summary>
   /// class for table RowHeight Resize
   /// </summary>
   internal class TableRowHeightResizeBehavior : ResizeBehavior
   {
      TableControl tableControl;
      public TableRowHeightResizeBehavior(TableControl tableControl, IServiceProvider seviceProvider) : base(tableControl, seviceProvider)
      {
         this.tableControl = tableControl;
      }
      
      /// <summary>
      /// check if new RowHeight is valid
      /// </summary>
      /// <param name="newSize"></param>
      /// <returns></returns>
      override protected bool CanResize(int newSize)
      {
         if (newSize > 0 && newSize < tableControl.ClientSize.Height - tableControl.TitleHeight)
            return true;
         return false;
      }

      /// <summary>
      /// Ser new value
      /// </summary>
      /// <param name="value"></param>
      override protected void SetNewValueOnDrag(int value)
      {
         tableControl.RowHeight = value;
      }

      override protected int GetOrgValue()
      {
         return tableControl.RowHeight;

      }

      override protected PropertyDescriptor GetChangedProp()
      {
         return TypeDescriptor.GetProperties(tableControl)["RowHeight"];
      }


   }
}
